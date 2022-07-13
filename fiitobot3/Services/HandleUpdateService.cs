using System;
using System.Linq;
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
        private readonly SheetContactsRepository contactsRepository;
        private readonly DetailsRepository detailsRepository;
        private BotData botData;
        private readonly IPresenter presenter;
        private readonly BotDataRepository botDataRepo;
        private readonly IPhotoRepository photoRepository;
        private readonly Random random = new Random();

        public HandleUpdateService(SheetContactsRepository contactsRepository, DetailsRepository detailsRepository,
            BotDataRepository botDataRepo,
            IPhotoRepository photoRepository,
            IPresenter presenter)
        {
            this.contactsRepository = contactsRepository;
            this.detailsRepository = detailsRepository;
            botData = botDataRepo.Load();
            this.botDataRepo = botDataRepo;
            this.photoRepository = photoRepository;
            this.presenter = presenter;
        }

        public HandleUpdateService(SheetContactsRepository contactsRepository, DetailsRepository detailsRepository,
            BotData botData,
            IPhotoRepository photoRepository,
            IPresenter presenter)
        {
            this.contactsRepository = contactsRepository;
            this.detailsRepository = detailsRepository;
            this.botData = botData;
            this.photoRepository = photoRepository;
            this.presenter = presenter;
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
            if (!await EnsureHasAdminRights(callbackQuery.From, callbackQuery.Message!.Chat.Id)) return;
            await HandlePlainText(callbackQuery.Data!, callbackQuery.Message!.Chat.Id, AccessRight.Staff);
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
            var accessRight = GetRights(message.From);
            if (message.Type == MessageType.Text)
                if (message.ForwardFrom != null)
                    await HandleForward(message.ForwardFrom!, message.Chat.Id, accessRight);
                else
                    await HandlePlainText(message.Text!, message.Chat.Id, accessRight);
        }

        private async Task HandleForward(User user, long fromChatId, AccessRight accessRight)
        {
            if (accessRight == AccessRight.External)
            {
                await presenter.SayNoRights(fromChatId, accessRight);
                return;
            }
            var person = botData.FindPersonByTgId(user.Id);
            if (person != null)
                await presenter.ShowContact(person.Contact, fromChatId, accessRight);
            else
                await presenter.SayNoResults(fromChatId);
        }

        private async Task<bool> EnsureHasAdminRights(User user, long? chatId)
        {
            var right = GetRights(user);
            if (right == AccessRight.Admin) return true;
            if (chatId != null)
                await presenter.SayNoRights(chatId.Value, right);
            return false;
        }

        private AccessRight GetRights(User user)
        {
            if (user.Id == 33598070 || botData.IsAdmin(user.Id) || user.Username != null && botData.IsAdmin(user.Username)) 
                return AccessRight.Admin;
            if (botData.Teachers.Any(c => c.TgId == user.Id || c.Telegram.Trim('@') == user.Username)) 
                return AccessRight.Staff;
            if (botData.Students.Any(p => p.Contact.TgId == user.Id || p.Contact.Telegram.Trim('@') == user.Username)) 
                return AccessRight.Student;
            return AccessRight.External;
        }

        public async Task HandlePlainText(string text, long fromChatId, AccessRight accessRight)
        {
            if (text.StartsWith("asstudent ") && accessRight != AccessRight.External)
            {
                text = text.Replace("asstudent ", "");
                accessRight = AccessRight.Student;
            }
            if (text == "/start" || text == "/help")
            {
                await presenter.ShowHelp(fromChatId, accessRight);
                return;
            }
            if (text.StartsWith("/reload"))
            {
                if (accessRight != AccessRight.Admin)
                {
                    await presenter.SayNoRights(fromChatId, accessRight);
                }
                else
                {
                    await presenter.SayReloadStarted(fromChatId);
                    ReloadDataFromSpreadsheets();
                    await presenter.SayReloaded(botData.AllContacts.Count(), fromChatId);
                }
                return;
            }
            if (text.StartsWith("Досье") && accessRight.IsOneOf(AccessRight.Admin, AccessRight.Staff))
            {
                await ShowDetails(text.Split(" ").Skip(1).StrJoin(" "), fromChatId);
                return;
            }

            if (accessRight == AccessRight.External)
            {
                await presenter.SayNoRights(fromChatId, accessRight);
                return;
            }

            if (text == "/random")
            {
                var contact = botData.Students[random.Next(botData.Students.Length)];
                await presenter.ShowContact(contact.Contact, fromChatId, accessRight);
                return;
            }
            if (text.IsOneOf("/me", "я"))
            {
                var contact = botData.AllContacts.FirstOrDefault(p => p.Contact.TgId == fromChatId);
                if (contact != null)
                {
                    await SayCompliment(contact.Contact, fromChatId);
                    await presenter.ShowContact(contact.Contact, fromChatId, accessRight);
                    return;
                }
            }
            var contacts = SearchPeople(text);
            const int maxResultsCount = 1;
            foreach (var person in contacts.Take(maxResultsCount))
            {
                if (person.Contact.TgId == fromChatId)
                    await SayCompliment(person.Contact, fromChatId);
                await presenter.ShowContact(person.Contact, fromChatId, accessRight);
				if (accessRight.IsOneOf(AccessRight.Admin, AccessRight.Staff))
				{
	                var photo = await photoRepository.FindRandomPhoto(person.Contact);
	                if (photo != null)
	                    await presenter.ShowPhoto(person.Contact, photo, fromChatId, accessRight);
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
                await presenter.SayNoResults(fromChatId);
            }
        }

        private async Task<bool> ShowContactsListBy(string text, Func<Contact, string> getProperty, long chatId,
            AccessRight accessRight)
        {
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
            return new Regex($@"\b{query}\b", RegexOptions.IgnoreCase).IsMatch(value);
        }

        private async Task SayCompliment(Contact contact, long fromChatId)
        {
            if (contact.Patronymic.EndsWith("вна"))
                await presenter.Say("Ты прекрасна, спору нет! ❤", fromChatId);
            else
                await presenter.Say("Ты прекрасен, спору нет! ✨", fromChatId);
        }

        private void ReloadDataFromSpreadsheets()
        {
            var contacts = contactsRepository.GetAllContacts();
            var students = detailsRepository.EnrichWithDetails(contacts);
            botData = new BotData
            {
                Administrators = contactsRepository.GetAllAdmins(),
                Teachers = contactsRepository.GetAllTeachers(),
                SourceSpreadsheets = contactsRepository.GetOtherSpreadsheets(),
                Students = students
            };
            botDataRepo.Save(botData);
        }

        private async Task ShowDetails(string query, long fromChatId)
        {
            var contacts = SearchPeople(query);
            if (contacts.Length == 1)
                await presenter.ShowDetails(contacts[0], botData.SourceSpreadsheets, fromChatId);
            else
                await presenter.SayBeMoreSpecific(fromChatId);
        }

        private PersonData[] SearchPeople(string text)
        {
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
