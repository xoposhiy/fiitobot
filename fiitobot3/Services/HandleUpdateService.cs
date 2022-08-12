using fiitobot.Services.Commands;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace fiitobot.Services
{
    public class HandleUpdateService
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;
        private readonly INamedPhotoDirectory namedPhotoDirectory;
        private readonly IPhotoRepository photoRepo;
        private readonly IChatCommandHandler[] commands;
        private readonly ITelegramFileDownloader fileDownloader;

        public HandleUpdateService(IBotDataRepository botDataRepo,
            INamedPhotoDirectory namedPhotoDirectory,
            IPhotoRepository photoRepo,
            ITelegramFileDownloader fileDownloader,
            IPresenter presenter, IChatCommandHandler[] commands)
        {
            this.botDataRepo = botDataRepo;
            this.namedPhotoDirectory = namedPhotoDirectory;
            this.photoRepo = photoRepo;
            this.presenter = presenter;
            this.commands = commands;
            this.fileDownloader = fileDownloader;
        }

        public async Task Handle(Update update)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                UpdateType.Message => BotOnMessageReceived(update.Message!),
                UpdateType.InlineQuery => BotOnInlineQuery(update.InlineQuery!),
                UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQuery(update.CallbackQuery!),
                _ => UnknownUpdateHandlerAsync(update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(update, exception);
            }
        }

        private async Task BotOnCallbackQuery(CallbackQuery callbackQuery)
        {
            //if (!await EnsureHasAdminRights(callbackQuery.From, callbackQuery.Message!.Chat.Id)) return;
            var sender = GetSenderContact(null, callbackQuery.From);
            await HandlePlainText(callbackQuery.Data!, callbackQuery.Message!.Chat.Id, sender);
        }

        private async Task BotOnInlineQuery(InlineQuery inlineQuery)
        {
            if (inlineQuery.Query.Length < 2) return;
            var sender = GetSenderContact(null, inlineQuery.From);
            if (!sender.Type.IsOneOf(ContactTypes.AllNotExternal)) return;
            var foundPeople = botDataRepo.GetData().SearchPeople(inlineQuery.Query);
            if (foundPeople.Length > 10) return;
            await presenter.InlineSearchResults(inlineQuery.Id, foundPeople.Select(c => c.Contact).ToArray());
        }

        private async Task BotOnMessageReceived(Message message)
        {
            // logger.LogInformation(
            //     "Receive message type {messageType}: {text} from {message.From} charId {message.Chat.Id}", message.Type,
            //     message.Text, message.From, message.Chat.Id);
            var silentOnNoResults = message.ReplyToMessage != null;
            var messageFrom = message.From;
            if (messageFrom == null) return;
            var sender = GetSenderContact(message.Chat, message.From);
            var fromChatId = message.Chat.Id;
            var inGroupChat = messageFrom.Id != fromChatId;
            if (message.Type == MessageType.Text)
                if (message.ForwardFrom != null)
                    await HandleForward(message.ForwardFrom!, sender, fromChatId);
                else
                    await HandlePlainText(message.Text!, fromChatId, sender, silentOnNoResults);
            else if (!inGroupChat && message.Type == MessageType.Photo)
                await HandlePhoto(message, sender, fromChatId);
        }

        private async Task HandlePhoto(Message message, Contact sender, long fromChatId)
        {
            if (sender.Type != ContactType.External)
            {
                var fileId = message.Photo!.Last().FileId;
                byte[] file = await fileDownloader.GetFileAsync(fileId);
                await photoRepo.SetPhotoForModeration(fromChatId, file);
                await presenter.PromptChangePhoto(fromChatId);
            }
            else
            {
                await presenter.SayNoRights(fromChatId, sender.Type);
            }
        }

        private Contact GetSenderContact(Chat chat, User user)
        {
            return
                botDataRepo.GetData().FindPersonByTgId(user.Id)?.Contact
                ?? botDataRepo.GetData().FindPersonByTelegramName(user.Username)?.Contact
                ?? new Contact(ContactType.External, user.Id, user.LastName, user.FirstName, "") { Telegram = user.Username };
        }

        private async Task HandleForward(User forwardFrom, Contact sender, long fromChatId)
        {
            if (sender.Type == ContactType.External)
            {
                await presenter.SayNoRights(fromChatId, sender.Type);
                return;
            }
            var person = GetSenderContact(null, forwardFrom);
            if (person.Type != ContactType.External)
                await presenter.ShowContact(person, fromChatId, person.GetDetailsLevelFor(sender));
            else
                await presenter.SayNoResults(fromChatId);
        }

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            if (sender.Type == ContactType.Administration)
            {
                if (text.StartsWith("/as_staff "))
                {
                    text = text.Replace("/as_staff ", "");
                    sender.Type = ContactType.Staff;
                }
                if (text.StartsWith("/as_student "))
                {
                    text = text.Replace("/as_student ", "");
                    sender.Type = ContactType.Student;
                }
                if (text.StartsWith("/as_external "))
                {
                    text = text.Replace("/as_external ", "");
                    sender.Type = ContactType.External;
                }
            }

            var command = commands.FirstOrDefault(c => text.StartsWith(c.Command));

            if (command != null)
            {
                if (sender.Type.IsOneOf(command.AllowedFor))
                    await command.HandlePlainText(text, fromChatId, sender, silentOnNoResults);
                else
                    await presenter.SayNoRights(fromChatId, sender.Type);
                return;
            }

            if (sender.Type == ContactType.External)
            {
                await presenter.SayNoRights(fromChatId, sender.Type);
                return;
            }
            if (text.StartsWith("/"))
                return;
            var contacts = botDataRepo.GetData().SearchPeople(text);
            const int maxResultsCount = 1;
            foreach (var person in contacts.Take(maxResultsCount))
            {
                if (person.Contact.TgId == fromChatId)
                    await SayCompliment(person.Contact, fromChatId);
                ContactDetailsLevel detailsLevel = person.Contact.GetDetailsLevelFor(sender);
                await presenter.ShowContact(person.Contact, fromChatId, detailsLevel);
                var selfUploadedPhoto = await photoRepo.TryGetModeratedPhoto(person.Contact.TgId);
                if (selfUploadedPhoto != null)
                {
                    await presenter.ShowPhoto(person.Contact, selfUploadedPhoto, fromChatId);
                }
                else
                {
                    var photo = await namedPhotoDirectory.FindPhoto(person.Contact);
                    if (photo != null)
                        await presenter.ShowPhoto(person.Contact, photo, fromChatId, sender.Type);
                    else
                    {
                        if (fromChatId == person.Contact.TgId)
                            await presenter.OfferToSetHisPhoto(fromChatId);
                    }
                }
            }

            if (contacts.Length > maxResultsCount)
                await presenter.ShowOtherResults(contacts.Skip(1).Select(p => p.Contact).ToArray(), fromChatId);
            if (contacts.Length == 0)
            {
                if (await ShowContactsListBy(text, c => c.MainCompany, fromChatId))
                    return;
                if (await ShowContactsListBy(text, c => c.FiitJob, fromChatId))
                    return;
                if (await ShowContactsListBy(text, c => c.School, fromChatId))
                    return;
                if (await ShowContactsListBy(text, c => c.City, fromChatId))
                    return;
                if (await ShowContactsListBy(text, c => c.Note, fromChatId))
                    return;
                if (!silentOnNoResults)
                    await presenter.SayNoResults(fromChatId);
            }
        }

        private async Task<bool> ShowContactsListBy(string text, Func<Contact, string> getProperty, long chatId)
        {
            var botData = botDataRepo.GetData();
            var contacts = botData.AllContacts.Select(p => p.Contact).ToList();
            var res = contacts.Where(c => SmartContains(getProperty(c) ?? "", text))
                .ToList();
            if (res.Count == 0) return false;
            var bestGroup = res.GroupBy(getProperty).MaxBy(g => g.Count());
            await presenter.ShowContactsBy(bestGroup.Key, res, chatId);
            return true;
        }

        private bool SmartContains(string value, string query)
        {
            return new Regex($@"\b{Regex.Escape(query)}\b", RegexOptions.IgnoreCase).IsMatch(value);
        }

        private async Task SayCompliment(Contact contact, long fromChatId)
        {
            if (contact.Patronymic.EndsWith("вна"))
                await presenter.Say("Ты прекрасна, спору нет! ❤", fromChatId);
            else
                await presenter.Say("Ты прекрасен, спору нет! ✨", fromChatId);
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            //logger.LogWarning("Unknown update type: {updateType}", update.Type);
            return Task.CompletedTask;
        }

        public async Task HandleErrorAsync(Update incomingUpdate, Exception exception)
        {
            try
            {
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException =>
                        $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                //logger.LogError("HandleError: {ErrorMessage}", errorMessage);
                await presenter.ShowErrorToDevops(incomingUpdate, errorMessage);
                //logger.LogInformation("Send error to devops");
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.ToString());
            }
        }
    }
}
