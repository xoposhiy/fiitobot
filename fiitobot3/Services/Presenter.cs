using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace fiitobot.Services
{
    public interface IPresenter
    {
        Task Say(string text, long chatId);
        Task ShowContact(Contact contact, long chatId, AccessRight right);
        Task ShowPhoto(Contact contact, PersonPhoto photo, long chatId, AccessRight right);
        Task ShowOtherResults(Contact[] otherContacts, long chatId);
        Task SayNoResults(long chatId);
        Task SayNoRights(long chatId, AccessRight userAccessRights);
        Task SayBeMoreSpecific(long chatId);
        Task InlineSearchResults(string inlineQueryId, Contact[] foundContacts, AccessRight right);
        Task ShowDetails(PersonData person, string[] sources, long fromChatId);
        Task SayReloadStarted(long chatId);
        Task SayReloaded(int contactsCount, long chatId);
        Task ShowErrorToDevops(Update incomingUpdate, string errorMessage);
        Task ShowHelp(long fromChatId, AccessRight right);
        Task ShowContactsBy(string criteria, IList<Contact> people, long chatId, AccessRight accessRight);
    }

    public class Presenter : IPresenter
    {
        private readonly ITelegramBotClient botClient;
        private readonly long devopsChatId;
        private readonly string spreadsheetId;

        public Presenter(ITelegramBotClient botClient, long devopsChatId, string spreadsheetId)
        {
            this.botClient = botClient;
            this.devopsChatId = devopsChatId;
            this.spreadsheetId = spreadsheetId;
        }

        public async Task InlineSearchResults(string inlineQueryId, Contact[] foundContacts, AccessRight right)
        {
            var results = foundContacts.Select(c =>
                new InlineQueryResultArticle(c.GetHashCode().ToString(), $"{c.LastName} {c.FirstName} {c.FormatMnemonicGroup(DateTime.Now)} {c.Telegram}",
                    new InputTextMessageContent(FormatContactAsHtml(c, right))
                    {
                        ParseMode = ParseMode.Html
                    }));
            await botClient.AnswerInlineQueryAsync(inlineQueryId, results, 60);
        }

        public async Task ShowDetails(PersonData person, string[] sources, long chatId)
        {
            var text = new StringBuilder();
            var contact = person.Contact;
            text.AppendLine(
                $@"<b>{contact.LastName} {contact.FirstName} {contact.Patronymic}</b> {contact.FormatMnemonicGroup(DateTime.Now)} (год поступления: {contact.AdmissionYear})");
            text.AppendLine();
            foreach (var rubric in person.Details.GroupBy(d => d.Rubric))
            {
                var sourceId = rubric.First().SourceId;
                var url = sources[sourceId];
                text.AppendLine(
                    $"<b>{EscapeForHtml(rubric.Key)}</b> (<a href=\"{url}\">источник</a>)");
                foreach (var detail in rubric)
                    text.AppendLine($" • {EscapeForHtml(detail.Parameter.TrimEnd('?'))}: {EscapeForHtml(detail.Value)}");
                text.AppendLine();
            }
            await botClient.SendTextMessageAsync(chatId, text.ToString().TrimEnd(), ParseMode.Html);
        }

        public async Task SayReloadStarted(long chatId)
        {
            await botClient.SendTextMessageAsync(chatId, $"Перезагружаю данные из многочисленных гуглтаблиц. Это может занять минуту-другую.", ParseMode.Html);
        }

        public async Task SayReloaded(int contactsCount, long chatId)
        {
            await botClient.SendTextMessageAsync(chatId, $"Загружено {contactsCount.Pluralize("контакт|контакта|контактов")}", ParseMode.Html);
        }

        public async Task ShowErrorToDevops(Update incomingUpdate, string errorMessage)
        {
            await botClient.SendTextMessageAsync(devopsChatId, FormatErrorHtml(incomingUpdate, errorMessage),
                ParseMode.Html);
        }

        public Task ShowHelp(long fromChatId)
        {
            throw new NotImplementedException();
        }

        public async Task ShowHelp(long fromChatId, AccessRight accessRight)
        {
            var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
            var b = new StringBuilder("Это бот для команды и студентов ФИИТ УрФУ. Напиши фамилию и/или имя студента ФИИТ и я расскажу всё, что о нём знаю. Но только если ты из ФИИТ.");
            if (accessRight.IsOneOf(AccessRight.Admin))
                b.AppendLine(
                    "\n\nВ любом другом чате напиши @fiitobot и после пробела начни писать фамилию. Я покажу, кого я знаю с такой фамилией, и после выбора конкретного студента, запощу карточку про студента в чат." +
                    $"\n\nВсе данные я беру из гугл-таблицы {spreadsheetUrl}");
            await botClient.SendTextMessageAsync(fromChatId, b.ToString(), ParseMode.Html);
        }

        private string FormatErrorHtml(Update incomingUpdate, string errorMessage)
        {
            var formattedUpdate = FormatIncomingUpdate(incomingUpdate);
            var formattedError = EscapeForHtml(errorMessage);
            return $"Error handling message: {formattedUpdate}\n\nError:\n<pre>{formattedError}</pre>";
        }

        public string FormatIncomingUpdate(Update incomingUpdate)
        {
            var incoming = incomingUpdate.Type switch
            {
                UpdateType.Message => $"From: {incomingUpdate.Message!.From} Message: {incomingUpdate.Message!.Text}",
                UpdateType.EditedMessage =>
                    $"From: {incomingUpdate.EditedMessage!.From} Edit: {incomingUpdate.EditedMessage!.Text}",
                UpdateType.InlineQuery =>
                    $"From: {incomingUpdate.InlineQuery!.From} Query: {incomingUpdate.InlineQuery!.Query}",
                UpdateType.CallbackQuery =>
                    $"From: {incomingUpdate.CallbackQuery!.From} Query: {incomingUpdate.CallbackQuery.Data}",
                _ => $"Message with type {incomingUpdate.Type}"
            };

            return
                $"<pre>{EscapeForHtml(incoming)}</pre>";
        }
        private string EscapeForHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        public async Task ShowContact(Contact contact, long chatId, AccessRight right)
        {
            if (contact.Type == ContactType.Student)
            {
                var inlineKeyboardMarkup = right.IsOneOf(AccessRight.Admin, AccessRight.Staff)
                    ? new InlineKeyboardMarkup(new InlineKeyboardButton("Подробнее!")
                        { CallbackData = $"Досье {contact.LastName} {contact.FirstName}" })
                    : null;
                var htmlText = FormatContactAsHtml(contact, right);
                await botClient.SendTextMessageAsync(chatId, htmlText, ParseMode.Html,
                    replyMarkup: inlineKeyboardMarkup);
            }
            else if (contact.Type == ContactType.Administration)
            {
                var htmlText = FormatContactAsHtml(contact, right);
                await botClient.SendTextMessageAsync(chatId, htmlText, ParseMode.Html);
            }
        }

        public async Task ShowPhoto(Contact contact, PersonPhoto photo, long chatId, AccessRight right)
        {
             await botClient.SendPhotoAsync(chatId, new InputOnlineFile(photo.RandomPhoto), caption: $"<a href='{photo.PhotosDirectory}'>{photo.DirName}</a>", parseMode:ParseMode.Html);
        }

        public async Task SayNoResults(long chatId)
        {
            var text = "Не нашлось никого подходящего :(\n\nНе унывайте! Найдите кого-нибудь случайного /random! Или поищите по своей школе или городу!";
            await botClient.SendTextMessageAsync(chatId, text, ParseMode.Html);
        }

        public async Task SayBeMoreSpecific(long chatId)
        {
            await botClient.SendTextMessageAsync(chatId, $"Уточните свой запрос", ParseMode.Html);
        }

        public async Task SayNoRights(long chatId, AccessRight userAccessRights)
        {
            if (userAccessRights == AccessRight.External)
                await botClient.SendTextMessageAsync(chatId, $"Этот бот только для команды ФИИТ", ParseMode.Html);
            else
                await botClient.SendTextMessageAsync(chatId, $"Это только для админов", ParseMode.Html);
        }

        public string FormatContactAsHtml(Contact contact, AccessRight right)
        {
            var b = new StringBuilder();
            b.AppendLine($"<b>{contact.LastName} {contact.FirstName} {contact.Patronymic}</b>");
            if (contact.Type == ContactType.Student)
            {
                b.AppendLine($"{contact.FormatMnemonicGroup(DateTime.Now)} (год поступления: {contact.AdmissionYear})");
                if (!string.IsNullOrWhiteSpace(contact.School))
                    b.AppendLine($"🏫 Школа: {contact.School}");
                if (!string.IsNullOrWhiteSpace(contact.City))
                    b.AppendLine($"🏙️ Город: {contact.City}");
                if (right.IsOneOf(AccessRight.Admin, AccessRight.Staff))
                    b.AppendLine($"Поступление {FormatConcurs(contact.Concurs)} c рейтингом {contact.Rating}");
            }
            if (contact.Type == ContactType.Administration)
            {
                b.AppendLine($"Чем занимается: {contact.Job}");
            }
            b.AppendLine();
            if (!string.IsNullOrWhiteSpace(contact.Email))
                b.AppendLine($"📧 {contact.Email}");
            if (!string.IsNullOrWhiteSpace(contact.Phone))
                b.AppendLine($"📞 {contact.Phone}");
            if (!string.IsNullOrWhiteSpace(contact.Telegram))
                b.AppendLine($"💬 {contact.Telegram}");
            b.AppendLine($"{EscapeForHtml(contact.Note)}");
            return b.ToString();
        }

        private string FormatConcurs(string concurs)
        {
            if (concurs == "О") return "по общему конкурсу";
            else if (concurs == "БЭ") return "по олимпиаде";
            else if (concurs == "К") return "по контракту";
            else if (concurs == "КВ") return "по льготной квоте";
            else if (concurs == "Ц") return "по целевой квоте";
            else return "неизвестно как 🤷‍";
        }

        public async Task ShowContactsBy(string criteria, IList<Contact> people, long chatId, AccessRight accessRight)
        {
            people = people.OrderByDescending(p => p.AdmissionYear).ThenBy(p => p.LastName).ThenBy(p => p.FirstName).ToList();
            var listCount = people.Count > 20 ? 15 : people.Count;
            var list = string.Join("\n", people.Select(p => $"<b>{p.LastName} {p.FirstName}</b> {p.FormatMnemonicGroup(DateTime.Now)} {p.Telegram}").Take(20));
            var ending = listCount < people.Count ? $"\n\nЕсть ещё {people.Count - listCount} подходящих человек" : "";
            await botClient.SendTextMessageAsync(chatId, $"{criteria}:\n\n{list}{ending}", ParseMode.Html);
        }

        public async Task ShowOtherResults(Contact[] otherContacts, long chatId)
        {
            await ShowContactsBy("Ещё результаты", otherContacts, chatId, AccessRight.Student);
        }

        public async Task Say(string text, long chatId)
        {
            await botClient.SendTextMessageAsync(chatId, text, ParseMode.Html);
        }
    }
}
