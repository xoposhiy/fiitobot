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
        public ContactDetails(long contactId, List<ContactDetail> details = null)
        {
            ContactId = contactId;
            Details = details ?? new List<ContactDetail>();
        }

        public readonly long ContactId;
        public long TelegramId;
        public string TelegramUsername;
        public DateTime LastUseTime;

        // TODO dialogState
        public List<ContactDetail> Details;

        public void UpdateOrAddMark(BrsStudentMark mark, int year, int yearPart, int courseNumber)
        {
            var details = mark.Mark;
            if (!string.IsNullOrEmpty(mark.ContainerName))
                details += $" {mark.ContainerName}";
            UpdateOrAddDetail(Rubrics.Semester(year, yearPart, courseNumber), $"{mark.ModuleTitle}", $"{mark.Total} ({details})");
        }
        
        public void UpdateOrAddDetail(string rubric, string parameter, string value)
        {
            var newDetails = Details.Where(d => d.Rubric != rubric || !d.Parameter.StartsWith(parameter)).ToList();
            newDetails.Add(new ContactDetail(rubric, parameter, value, DateTime.Now));
            Details = newDetails;
        }

        public void UpdateFromTelegramUser(User user)
        {
            if (TelegramId == user.Id && TelegramUsername == user.Username)
                return;
            TelegramUsername = user.Username;
            TelegramId = user.Id;
            Changed = true;
        }

        [JsonIgnore] public bool Changed { get; set; }
    }
}
