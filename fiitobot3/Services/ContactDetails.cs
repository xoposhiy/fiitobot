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
        public readonly List<ContactDetail> Details;

        public void UpdateOrAddMark(BrsStudentMark mark, int year, int yearPart, int courseNumber)
        {
            UpdateOrAddDetail(Rubrics.Semester(year, yearPart, courseNumber), mark.ModuleTitle, $"{mark.Total} ({mark.Mark})");
        }
        
        public void UpdateOrAddDetail(string rubric, string parameter, string value)
        {
            var detail = Details.FirstOrDefault(d => d.Rubric == rubric && d.Parameter == parameter);
            if (detail == null)
            {
                detail = new ContactDetail(rubric, parameter, value, DateTime.Now);
                Details.Add(detail);
            }
            else
                detail.Value = value;

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
