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
        public ContactDetails(long contactId, List<ContactDetail> details = null, List<SemesterMarks> semesters = null)
        {
            ContactId = contactId;
            Details = details ?? new List<ContactDetail>();
            Semesters = semesters ?? new List<SemesterMarks>();
        }

        public readonly long ContactId;
        public long TelegramId;
        public string TelegramUsername;
        public List<SemesterMarks> Semesters;
        public TgUsernameSource TelegramUsernameSource;
        public DateTime LastUseTime;

        // TODO dialogState
        public List<ContactDetail> Details;

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
}
