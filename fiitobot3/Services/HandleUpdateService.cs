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
    public enum AccessRight
    {
        Admin, // Can reload data
        Staff, // Can see all data about students. Can't reload.
        Student, // Can see short info about students.
        External // Can't see anything.
    }


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
            await HandlePlainText(callbackQuery.Data!, callbackQuery.Message!.Chat.Id, sender, AccessRight.Staff);
        }

        private async Task BotOnInlineQuery(InlineQuery inlineQuery)
        {
            if (inlineQuery.Query.Length < 2) return;
            var right = GetRights(inlineQuery.From);
            if (!right.IsOneOf(AccessRight.Admin, AccessRight.Staff, AccessRight.Student)) return;
            var foundPeople = SearchPeople(inlineQuery.Query);
            if (foundPeople.Length > 10) return;
            await presenter.InlineSearchResults(inlineQuery.Id, foundPeople.Select(c => c.Contact).ToArray(), right);
        }

        private async Task BotOnMessageReceived(Message message)
        {
            // logger.LogInformation(
            //     "Receive message type {messageType}: {text} from {message.From} charId {message.Chat.Id}", message.Type,
            //     message.Text, message.From, message.Chat.Id);
            var silentOnNoResults = message.ReplyToMessage != null;
            var messageFrom = message.From;
            if (messageFrom == null) return;
            var accessRight = GetRights(messageFrom);
            var sender = GetSenderContact(message.Chat, message.From);
            var fromChatId = message.Chat.Id;
            var inGroupChat = messageFrom.Id != fromChatId;
            if (inGroupChat && accessRight.IsOneOf(AccessRight.Admin, AccessRight.Staff))
                accessRight = AccessRight.Student; // в групповых чатах не показывать секретную инфу.
            if (message.Type == MessageType.Text)
                if (message.ForwardFrom != null)
                    await HandleForward(message.ForwardFrom!, sender, fromChatId, accessRight);
                else
                    await HandlePlainText(message.Text!, fromChatId, sender, accessRight, silentOnNoResults);
            else if (!inGroupChat && message.Type == MessageType.Photo)
                await HandlePhoto(message, accessRight, fromChatId);
        }

        private async Task HandlePhoto(Message message, AccessRight accessRight, long fromChatId)
        {
            if (accessRight.IsOneOf(AccessRight.Admin, AccessRight.Staff, AccessRight.Student))
            {
                var fileId = message.Photo!.Last().FileId;
                byte[] file = await fileDownloader.GetFileAsync(fileId);
                await photoRepo.SetPhotoForModeration(fromChatId, file);
                await presenter.PromptChangePhoto(fromChatId);
            }
            else
            {
                await presenter.SayNoRights(fromChatId, accessRight);
            }
        }

        private Contact GetSenderContact(Chat chat, User user)
        {
            return 
                botDataRepo.GetData().FindPersonByTgId(user.Id)?.Contact
                ?? botDataRepo.GetData().FindPersonByTelegramName(user.Username)?.Contact
                ?? new Contact(-1, user.LastName, user.FirstName, "", -1, -1, "", "", "", "", user.Username, "", "",
                    chat?.Bio + "\n" + chat?.Description, user.Id, "", ContactType.External, "", "", null);
        }

        private async Task HandleForward(User forwardFrom, Contact sender, long fromChatId, AccessRight accessRight)
        {
            if (accessRight == AccessRight.External || sender.Type == ContactType.External)
            {
                await presenter.SayNoRights(fromChatId, accessRight);
                return;
            }
            var person = GetSenderContact(null, forwardFrom);
            if (person.Type != ContactType.External)
                await presenter.ShowContact(person, fromChatId, person.GetDetailsLevelFor(sender));
            else
                await presenter.SayNoResults(fromChatId);
        }

        private AccessRight GetRights(User user)
        {
            var botData = botDataRepo.GetData();
            if (user.Id == 33598070 || botData.IsAdmin(user.Id, user.Username)) 
                return AccessRight.Admin;
            if (botData.IsTeacher(user.Id, user.Username))
                return AccessRight.Staff;
            if (botData.IsStudent(user.Id, user.Username)) 
                return AccessRight.Student;
            return AccessRight.External;
        }

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, AccessRight accessRight,
            bool silentOnNoResults = false)
        {
            bool replyAsForStudent = false;
            if (text.StartsWith("asstudent ") && sender.Type != ContactType.External)
            {
                text = text.Replace("asstudent ", "");
                replyAsForStudent = true;
            }

            var command = commands.FirstOrDefault(c => c.Synonyms.Any(synonym => text.StartsWith(synonym)));

            if (command != null)
            {
                if (accessRight.IsOneOf(command.AllowedFor))
                    await command.HandlePlainText(text, fromChatId, sender, silentOnNoResults);
                else
                    await presenter.SayNoRights(fromChatId, accessRight);
                return;
            }

            if (accessRight == AccessRight.External)
            {
                await presenter.SayNoRights(fromChatId, accessRight);
                return;
            }
            if (text.StartsWith("Досье") && accessRight.IsOneOf(AccessRight.Admin, AccessRight.Staff))
            {
                await ShowDetails(text.Split(" ").Skip(1).StrJoin(" "), fromChatId);
                return;
            }
            if (text.StartsWith("/"))
                return;
            var contacts = SearchPeople(text);
            const int maxResultsCount = 1;
            foreach (var person in contacts.Take(maxResultsCount))
            {
                if (person.Contact.TgId == fromChatId)
                    await SayCompliment(person.Contact, fromChatId);
                ContactDetailsLevel detailsLevel = person.Contact.GetDetailsLevelFor(sender);
                if (replyAsForStudent && detailsLevel > ContactDetailsLevel.Minimal)
                    detailsLevel = ContactDetailsLevel.Minimal;
                await presenter.ShowContact(person.Contact, fromChatId, detailsLevel);
                var selfUploadedPhoto = await photoRepo.TryGetModeratedPhoto(person.Contact.TgId);
                if (selfUploadedPhoto != null)
                {
                    await presenter.ShowPhoto(person.Contact, selfUploadedPhoto, fromChatId, accessRight);
                }
                else
                {
                    var photo = await namedPhotoDirectory.FindPhoto(person.Contact);
                    if (photo != null)
                        await presenter.ShowPhoto(person.Contact, photo, fromChatId, accessRight);
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
                if (await ShowContactsListBy(text, c => c.School, fromChatId, accessRight))
                    return;
                if (await ShowContactsListBy(text, c => c.City, fromChatId, accessRight))
                    return;
                if (!silentOnNoResults)
                    await presenter.SayNoResults(fromChatId);
            }
        }

        private async Task<bool> ShowContactsListBy(string text, Func<Contact, string> getProperty, long chatId,
            AccessRight accessRight)
        {
            var botData = botDataRepo.GetData();
            var contacts = botData.Students.Select(p => p.Contact).ToList();
            var res = contacts.Where(c => SmartContains(getProperty(c), text))
                .ToList();
            if (res.Count == 0) return false;
            var bestGroup = res.GroupBy(getProperty).MaxBy(g => g.Count());
            await presenter.ShowContactsBy(bestGroup.Key, bestGroup.ToList(), chatId, accessRight);
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

        private async Task ShowDetails(string query, long fromChatId)
        {
            var botData = botDataRepo.GetData();
            var contacts = SearchPeople(query);
            if (contacts.Length == 1)
                await presenter.ShowDetails(contacts[0], botData.SourceSpreadsheets, fromChatId);
            else
                await presenter.SayBeMoreSpecific(fromChatId);
        }

        private PersonData[] SearchPeople(string text)
        {
            var botData = botDataRepo.GetData();
            var res = botData.FindPerson(text);
            if (res.Length > 0) return res;
            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(part => botData.FindPerson(part))
                       .Where(g => g.Length > 0)
                       .MinBy(g => g.Length)
                   ?? Array.Empty<PersonData>();
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
