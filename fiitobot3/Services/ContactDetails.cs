using fiitobot.Services.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace fiitobot.Services
{
    public class ContactDetails
    {
        public ContactDetails(long contactId, List<ContactDetail> details = null, List<SemesterMarks> semesters = null,
            DialogState dialogState = null, List<Spasibka> spasibki = null)
        {
            ContactId = contactId;
            Details = details ?? new List<ContactDetail>();
            Semesters = semesters ?? new List<SemesterMarks>();
            DialogState = dialogState ?? new DialogState();
            Spasibki = spasibki ?? new List<Spasibka>();
        }

        public readonly long ContactId;
        public long TelegramId;
        public string TelegramUsername;
        public List<SemesterMarks> Semesters;
        public TgUsernameSource TelegramUsernameSource;
        public DateTime LastUseTime;
        public DialogState DialogState;
        public List<ContactDetail> Details;
        public List<Spasibka> Spasibki;

        public void UpdateOrAddMark(BrsStudentMark mark, int year, int yearPart, int courseNumber)
        {
            var details = mark.Mark;
            if (!string.IsNullOrEmpty(mark.ContainerName))
                details += $" {mark.ContainerName}";
            var rubric = Rubrics.Semester(year, yearPart, courseNumber);
            var parameter = mark.ModuleTitle;
            var value = $"{mark.Total} ({details})";
            UpdateOrAddDetail(rubric, parameter, value);
        }

        public void UpdateOrAddDetail(string rubric, string parameter, string value)
        {
            var newDetails = Details
                .Where(d => d.Rubric != rubric || !d.Parameter.StartsWith(parameter))
                .ToList();
            newDetails.Add(new ContactDetail(rubric, parameter, value, DateTime.Now));
            Details = newDetails;
        }

        public void UpdateFromTelegramUser(User user)
        {
            if (TelegramId == user.Id && TelegramUsername == user.Username)
                return;
            TelegramUsername = user.Username;
            TelegramId = user.Id;
            TelegramUsernameSource = TgUsernameSource.UsernameTgMessage;
            Changed = true;
        }

        [JsonIgnore] public bool Changed { get; set; }

        public string TelegramUsernameWithSobachka =>
            string.IsNullOrEmpty(TelegramUsername) ? "" : (
            TelegramUsername.StartsWith("@") ? TelegramUsername : "@" + TelegramUsername);
    }

    public enum TgUsernameSource
    {
        GoogleSheet = 0,
        UsernameTgMessage = 1
    }



    public class DialogState
    {
        // сохраняем строку команды, которая должна вызываться на следующем этапе
        public string CommandHandlerLine = "";

        // сохраняем то что поняли из пользовательского сообщения: внутреннее_состояние ReceiverId текст_спасибки
        public string CommandHandlerData = "";

        // нужен для сохранения индекса в листе на удаление, когда листаем список
        // понимаю, что не очень хорошо, но больше некуда засунуть этот индекс

        // Для навигации по разным спискам
        public int ItemIndex;

        // айди сообщения, на котором нажали кнопку
        public int? MessageId;
    }

    public class Spasibka
    {
        public readonly long SenderContactId;
        public readonly string Content;
        public readonly DateTime PostDate;

        public Spasibka(long senderContactId, string content, DateTime postDate)
        {
            SenderContactId = senderContactId;
            Content = content;
            // TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            PostDate = postDate;
            // PostDate = Convert.ToString(postDate
            //     .ToString("dd.MM.yyyy:HH:mm"), CultureInfo.InvariantCulture);
        }
    }
}
