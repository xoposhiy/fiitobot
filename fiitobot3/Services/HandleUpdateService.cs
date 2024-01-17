using System;
using System.Linq;
using System.Text;
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
        private readonly IBotDataRepository botDataRepo;
        private readonly IChatCommandHandler[] commands;
        private readonly DemidovichService demidovichService;
        private readonly IContactDetailsRepo detailsRepo;
        private readonly ITelegramFileDownloader fileDownloader;
        private readonly INamedPhotoDirectory namedPhotoDirectory;
        private readonly IPhotoRepository photoRepo;
        private readonly IPresenter presenter;

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
                var file = await fileDownloader.GetFileAsync(fileId);
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
            var senderContact = botData.FindContactByTgId(user.Id)
                                ?? botData.FindContactByTelegramName(user.Username)
                                ?? CreateExternalContactFromTgUser(user);
            var details = await detailsRepo.FindById(senderContact.Id) ?? new ContactDetails(senderContact.Id);
            details.UpdateFromTelegramUser(user);
            senderContact.UpdateFromDetails(details);
            return new ContactWithDetails(senderContact, details);
        }

        private static Contact CreateExternalContactFromTgUser(User user)
        {
            return new Contact
            {
                Id = -1,
                Type = ContactType.External,
                TgId = user.Id,
                LastName = user.LastName,
                FirstName = user.FirstName,
                Telegram = user.Username
            };
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

        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails senderWithDetails,
            bool silentOnNoResults = false)
        {
            var sender = senderWithDetails.Contact;
            var asSelf = text.Contains("/as_self");
            (sender.Type, text) = OverrideSenderType(text, sender.Type);

            var m = Regex.Match(text, "\\/as_(\\d+)\\s*");
            if (m.Success && sender.Type == ContactType.Administration)
            {
                var allContacts = botDataRepo.GetData().AllContacts;
                var id = m.Groups[1].Value;
                var impersonatedUser = allContacts.FirstOrDefault(c =>
                    c.TgId.ToString() == id || c.SameTelegramUsername(id) || c.Id.ToString() == id);
                sender = impersonatedUser ?? sender;
                text = text.Replace(m.Value, "");
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
                var imageBytes = demidovichService == null ? null : await demidovichService.TryGetImageBytes(text);
                if (imageBytes != null)
                    await presenter.ShowDemidovichTask(imageBytes, text, fromChatId);
                else
                    await presenter.SayNoRights(fromChatId, sender.Type);
                return;
            }

            if (text.StartsWith("/"))
                return;

            var data = botDataRepo.GetData();
            if (TryHandleAsGroupName(text, data.Students, fromChatId))
                return;
            if (await TryHandleAsRequestAboutHimself(text, data.AllContacts.ToArray(), sender))
                return;
            if (await TryHandleAsRequestAsMultilineList(text, data, fromChatId))
                return;
            if (await TryHandleAsBirthdayCommands(text, sender, fromChatId))
                return;
            var contacts = data.SearchContacts(text);
            const int maxResultsCount = 1;
            foreach (var person in contacts.Take(maxResultsCount))
            {
                if (person.TgId == fromChatId || asSelf)
                    await SayCompliment(person, fromChatId);

                var details = detailsRepo.FindById(person.Id).Result;
                person.UpdateFromDetails(details);
                var detailsLevel = person.GetDetailsLevelFor(asSelf ? person : sender);
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
                    {
                        await presenter.ShowPhoto(person, photo, fromChatId, sender.Type);
                    }
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

            if (string.IsNullOrEmpty(sender.BirthDate))
            {
                await presenter.AskForBirthDate(fromChatId);
            }
        }

        private async Task<bool> TryHandleAsRequestAsMultilineList(string text, BotData data, long fromChatId)
        {
            var lines = text.Split("\n").ToArray();
            if (lines.Length < 2) return false;

            var resultLines = new StringBuilder();
            var found = 0;
            foreach (var line in lines)
            {
                var query = new string(line.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray()).Trim();
                var contacts = data.SearchContacts(query);
                if (contacts.Length >= 1)
                {
                    var res = contacts[0];
                    var question = contacts.Length > 1 ? ("(?) " + query + " ") : "";
                    resultLines.AppendLine(line + " " + res.Telegram + " " + question + res.FormatMnemonicGroup(DateTime.Now));
                    found++;
                }
                else
                    resultLines.AppendLine(line);
            }
            if (found == 0) return false;
            await presenter.SayPlainText(string.Join("\n", resultLines), fromChatId);
            return true;
        }

        private async Task<bool> TryHandleAsRequestAboutHimself(string text, Contact[] contacts, Contact sender)
        {
            if (!text.Equals("я", StringComparison.OrdinalIgnoreCase)) return false;
            var me = contacts.Where(s => s.TgId == sender.TgId).ToList();
            if (me.Count == 0) return false;
            var person = me[0];
            var detailsLevel = person.GetDetailsLevelFor(sender);
            await SayCompliment(person, sender.TgId);
            await presenter.ShowContact(person, sender.TgId, detailsLevel);

            return true;
        }

        private bool TryHandleAsGroupName(string text, Contact[] contacts, long chatId)
        {
            if (!Regex.IsMatch(text, @"^(ФТ-\d+)|(МЕН-\d+)", RegexOptions.IgnoreCase)) return false;
            var officialGroupStudent = contacts.FirstOrDefault(c =>
                c.FormatOfficialGroup(DateTime.Now).StartsWith(text, StringComparison.OrdinalIgnoreCase));
            if (officialGroupStudent != null)
            {
                presenter.Say(officialGroupStudent.FormatMnemonicGroup(DateTime.Now, false), chatId);
                return true;
            }

            var mnemonicGroupStudent = contacts.FirstOrDefault(c =>
                c.FormatMnemonicGroup(DateTime.Now).StartsWith(text, StringComparison.OrdinalIgnoreCase));
            if (mnemonicGroupStudent != null)
            {
                presenter.Say(mnemonicGroupStudent.FormatOfficialGroup(DateTime.Now), chatId);
                return true;
            }

            return false;
        }

        private async Task<bool> TryHandleAsBirthdayCommands(string text, Contact sender, long fromChatId)
        {
            var fullBirthDate = Regex.Match(text, @"^(0[1-9]|[12][0-9]|3[01])\.(0[1-9]|1[012])");
            if (fullBirthDate.Success)
            {
                await presenter.ShowBirthDateActions(sender, fromChatId, text);
                return true;
            }

            if (DateUtils.TryParseMonthName(text, out var monthNumber))
            {
                if (!await ShowContactsListBy(monthNumber, c => c.BirthDate.Split(".")[1], fromChatId, true))
                    await presenter.SayNoResults(fromChatId);
                return true;
            }

            if (!text.Equals("др", StringComparison.OrdinalIgnoreCase)) return false;
            if (sender.Type == ContactType.Student)
            {
                if (!await ShowContactsListBy(
                        sender.FormatMnemonicGroup(DateTime.Now, false),
                        c => c.FormatMnemonicGroup(DateTime.Now, false),
                        fromChatId,
                        true))
                    await presenter.SayNoResults(fromChatId);
            }
            else
                await presenter.Say("Информация по др одногруппников доступна только студентам." +
                                    "\n\nВы можете поискать др студентов и преподавателей по названию месяца, например \"сентябрь\".", fromChatId);
            return true;
        }

        private (ContactType newSenderType, string restText) OverrideSenderType(string text, ContactType senderType)
        {
            if (senderType != ContactType.Administration) return (senderType, text);

            (ContactType newSenderType, string restText)? TryHandle(string command, ContactType newSenderType)
            {
                return text.StartsWith(command)
                    ? (newSenderType, text.Replace(command, ""))
                    : ((ContactType newSenderType, string restText)?)null;
            }

            return
                TryHandle("/as_staff ", ContactType.Staff)
                ?? TryHandle("/as_student ", ContactType.Student)
                ?? TryHandle("/as_external ", ContactType.External)
                ?? TryHandle("/as_self ", senderType)
                ?? (senderType, text);
        }

        private async Task<bool> ShowContactsListBy(string text, Func<Contact, string> getProperty, long chatId, bool isBirth=false)
        {
            var botData = botDataRepo.GetData();
            var contacts = botData.AllContacts.Select(p => p).ToList();
            if (isBirth)
                contacts = contacts
                    .Where(c => !string.IsNullOrEmpty(c.BirthDate) && c.BirthDate != "no")
                    .ToList();

            var res = contacts.Where(c => SmartContains(getProperty(c) ?? "", text))
                .ToList();
            if (res.Count == 0) return false;
            var bestGroup = res.GroupBy(getProperty).MaxBy(g => g.Count());

            if (isBirth)
            {
                if (DateUtils.TryParseMonthNumber(text, out var monthName))
                {
                    await presenter.ShowContactsBy($"{monthName} — месяц, в котором родились", res, chatId);
                }
                else
                {
                    await presenter.ShowContactsBy($"Дни рождения '{text}'", res, chatId);
                }

                return true;
            }

            await presenter.ShowContactsBy(bestGroup.Key, res, chatId);
            return true;
        }

        private bool SmartContains(string value, string query)
        {
            return new Regex($@"\b{Regex.Escape(query)}\b", RegexOptions.IgnoreCase).IsMatch(value);
        }

        private async Task SayCompliment(Contact contact, long fromChatId)
        {
            if (contact.Gender == Gender.F || contact.Patronymic.EndsWith("вна"))
                await presenter.Say("Ты прекрасна, спору нет! ❤", fromChatId);
            else
                await presenter.Say("Ты прекрасен, спору нет! ✨", fromChatId);
            await presenter.Say("Что из этого видят другие пользователи фиитобота? \n" +
                                "Детали вашего поступления видят только преподаватели.\n" +
                                "e-mail, телефон, github видят только преподаватели и ваши однокурсники (того же года поступления или того же года выпуска).\n" +
                                "Телеграм и заметки видят все пользователи фиитобота.\n" +
                                "Внешние люди, не студенты, не преподаватели и не администраторы ФИИТ не видят ничего.\n\n" +
                                "О неточностях в данных пишите @xoposhiy или ещё кому-то из команды ФИИТ.",
                fromChatId);
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
