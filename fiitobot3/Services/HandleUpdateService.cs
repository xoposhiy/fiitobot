using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using fiitobot.Services.Commands;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace fiitobot.Services
{
    public class HandleUpdateService
    {
        private readonly IPresenter presenter;
        private readonly IContactDetailsRepo detailsRepo;
        private readonly IBotDataRepository botDataRepo;
        private readonly INamedPhotoDirectory namedPhotoDirectory;
        private readonly IPhotoRepository photoRepo;
        private readonly DemidovichService demidovichService;
        private readonly IChatCommandHandler[] commands;
        private readonly ITelegramFileDownloader fileDownloader;

        public HandleUpdateService(IBotDataRepository botDataRepo,
            INamedPhotoDirectory namedPhotoDirectory,
            IPhotoRepository photoRepo,
            DemidovichService demidovichService,
            ITelegramFileDownloader fileDownloader,
            IPresenter presenter,
            IContactDetailsRepo detailsRepo,
            IChatCommandHandler[] commands)
        {
            this.botDataRepo = botDataRepo;
            this.namedPhotoDirectory = namedPhotoDirectory;
            this.photoRepo = photoRepo;
            this.demidovichService = demidovichService;
            this.presenter = presenter;
            this.detailsRepo = detailsRepo;
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
            var sender = await GetSenderContact(callbackQuery.From);
            await HandlePlainText(callbackQuery.Data!, callbackQuery.Message!.Chat.Id, sender);
        }

        private async Task BotOnInlineQuery(InlineQuery inlineQuery)
        {
            if (inlineQuery.Query.Length < 2) return;
            var sender = await GetSenderContact(inlineQuery.From);
            if (!sender.Contact.Type.IsOneOf(ContactTypes.AllNotExternal)) return;
            var foundPeople = botDataRepo.GetData().SearchContacts(inlineQuery.Query);
            if (foundPeople.Length > 10) return;
            await presenter.InlineSearchResults(inlineQuery.Id, foundPeople.Select(c => c).ToArray());
        }

        private async Task BotOnMessageReceived(Message message)
        {
            // logger.LogInformation(
            //     "Receive message type {messageType}: {text} from {message.From} charId {message.Chat.Id}", message.Type,
            //     message.Text, message.From, message.Chat.Id);
            var silentOnNoResults = message.ReplyToMessage != null;
            var messageFrom = message.From;
            if (messageFrom == null) return;
            var sender = await GetSenderContact(message.From);
            sender.ContactDetails.UpdateFromTelegramUser(message.From);
            var fromChatId = message.Chat.Id;
            var inGroupChat = messageFrom.Id != fromChatId;
            if (message.Type == MessageType.Text)
                if (message.ForwardFrom != null)
                    await HandleForward(message.ForwardFrom!, sender, fromChatId);
                else
                    await HandlePlainText(message.Text!, fromChatId, sender, silentOnNoResults);
            else if (!inGroupChat && message.Type == MessageType.Photo)
                await HandlePhoto(message, sender.Contact, fromChatId);
            if (sender.ContactDetails.Changed)
                await detailsRepo.Save(sender.ContactDetails);
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

        private async Task<ContactWithDetails> GetSenderContact(User user)
        {
            var botData = botDataRepo.GetData();
            var contact = botData.FindContactByTgId(user.Id)
                          ?? botData.FindContactByTelegramName(user.Username)
                          ?? new Contact(-1, ContactType.External, user.Id, user.LastName, user.FirstName) { Telegram = user.Username };
            var details = await detailsRepo.FindById(contact.Id) ?? new ContactDetails(contact.Id);
            contact.Telegram = details.TelegramUsername;
            contact.TgId = details.TelegramId;
            return new ContactWithDetails(contact, details);
        }

        private async Task HandleForward(User forwardFrom, ContactWithDetails senderWithDetails, long fromChatId)
        {
            var sender = senderWithDetails.Contact;
            if (sender.Type == ContactType.External)
            {
                await presenter.SayNoRights(fromChatId, sender.Type);
                return;
            }
            var personWithDetails = await GetSenderContact(forwardFrom);
            var person = personWithDetails.Contact;
            if (person.Type != ContactType.External)
                await presenter.ShowContact(person, fromChatId, person.GetDetailsLevelFor(sender));
            else
                await presenter.SayNoResults(fromChatId);
        }

        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails senderWithDetails, bool silentOnNoResults = false)
        {
            var sender = senderWithDetails.Contact;
            bool asSelf = text.Contains("/as_self");
            (sender.Type, text) = OverrideSenderType(text, sender.Type);

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
            var data = botDataRepo.GetData();
            if (TryHandleAsGroupName(text, data.Students, fromChatId))
                return;
            var contacts = data.SearchContacts(text);
            const int maxResultsCount = 1;
            foreach (var person in contacts.Take(maxResultsCount))
            {
                if (person.TgId == fromChatId || asSelf)
                    await SayCompliment(person, fromChatId);
                ContactDetailsLevel detailsLevel = person.GetDetailsLevelFor(asSelf ? person : sender);
                await presenter.ShowContact(person, fromChatId, detailsLevel);
                var selfUploadedPhoto = await photoRepo.TryGetModeratedPhoto(person.TgId);
                if (selfUploadedPhoto != null)
                {
                    await presenter.ShowPhoto(person, selfUploadedPhoto, fromChatId);
                }
                else
                {
                    var photo = await namedPhotoDirectory.FindPhoto(person);
                    if (photo != null)
                        await presenter.ShowPhoto(person, photo, fromChatId, sender.Type);
                    else
                    {
                        if (fromChatId == person.TgId)
                            await presenter.OfferToSetHisPhoto(fromChatId);
                    }
                }
            }

            if (contacts.Length > maxResultsCount)
                await presenter.ShowOtherResults(contacts.Skip(1).Select(p => p).ToArray(), fromChatId);
            if (contacts.Length == 0)
            {
                // TODO: сделать абстракцию "ответчика". Спрашивать всех ответчиков, и если есть всего один ответ,
                // то показывать его. Если несколько, то показывать по кнопке на каждого ответчика и спрашивать у пользователя, что он хотел.
                var imageBytes = demidovichService == null ? null : await demidovichService.TryGetImageBytes(text);
                var foundAnswer = false;
                if (imageBytes != null)
                {
                    await presenter.ShowDemidovichTask(imageBytes, text, fromChatId);
                    foundAnswer = true;
                }
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
                if (!foundAnswer && !silentOnNoResults)
                    await presenter.SayNoResults(fromChatId);
            }
        }

        private bool TryHandleAsGroupName(string text, Contact[] contacts, long chatId)
        {
            if (!Regex.IsMatch(text, @"^(ФТ-\d+)|(МЕН-\d+)", RegexOptions.IgnoreCase)) return false;
            var officialGroupStudent = contacts.FirstOrDefault(c => c.FormatOfficialGroup(DateTime.Now).StartsWith(text, StringComparison.OrdinalIgnoreCase));
            if (officialGroupStudent != null)
            {
                presenter.Say(officialGroupStudent.FormatMnemonicGroup(DateTime.Now, false), chatId);
                return true;
            }
            var mnemonicGroupStudent = contacts.FirstOrDefault(c => c.FormatMnemonicGroup(DateTime.Now).StartsWith(text, StringComparison.OrdinalIgnoreCase));
            if (mnemonicGroupStudent != null)
            {
                presenter.Say(mnemonicGroupStudent.FormatOfficialGroup(DateTime.Now), chatId);
                return true;
            }
            return false;
        }

        private (ContactType newSenderType, string restText) OverrideSenderType(string text, ContactType senderType)
        {
            if (senderType != ContactType.Administration) return (senderType, text);

            (ContactType newSenderType, string restText)? TryHandle(string command, ContactType newSenderType) =>
                text.StartsWith(command)
                    ? (newSenderType, text.Replace(command, ""))
                    : ((ContactType newSenderType, string restText)?)null;

            return
                TryHandle("/as_staff ", ContactType.Staff)
                ?? TryHandle("/as_student ", ContactType.Student)
                ?? TryHandle("/as_external ", ContactType.External)
                ?? TryHandle("/as_self ", senderType)
                ?? (senderType, text);
        }

        private async Task<bool> ShowContactsListBy(string text, Func<Contact, string> getProperty, long chatId)
        {
            var botData = botDataRepo.GetData();
            var contacts = botData.AllContacts.Select(p => p).ToList();
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
