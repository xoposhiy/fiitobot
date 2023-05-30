using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using fiitobot.Services;
using Newtonsoft.Json;

namespace fiitobot
{
    public enum ContactType
    {
        External = -1,
        Student = 0,
        Administration = 1,
        Staff = 2,
    }

    public class Contact
    {
        public const string ActiveStatus = "Активный";

        public ContactType Type;
        public long Id;
        public long TgId;
        public string LastName;
        public string FirstName;
        public string Patronymic;
        public int AdmissionYear = -1;
        public int GraduationYear = -1;
        public int GroupIndex = -1;
        public int SubgroupIndex = -1;
        public string City = "";
        public string School = "";
        public string Concurs = "";
        public string EnrollRating = "";
        public string Telegram;
        public string Phone = "";
        public string Email = "";
        public string Google = "";
        public string Notion = "";
        public string Note = "";
        public string SecretNote = "";
        public string FiitJob = "";
        public string MainCompany = "";
        public string Status = "";
        public double? CurrentRating;

        public bool SameTelegramUsername(string tgUsername)
        {
            if (string.IsNullOrWhiteSpace(tgUsername)) return false;
            return Telegram != null &&
                   Telegram.Trim('@').Equals(tgUsername.Trim('@'), StringComparison.OrdinalIgnoreCase);
        }

        public string FormatMnemonicGroup(DateTime now, bool withSubgroup = true)
        {
            if (GraduationYear <= 0) return "";
            if (GroupIndex <= 0) return "";
            var delta = now.Month >= 8 ? 1 : 0;
            var course = 4 - (GraduationYear - (now.Year + delta));
            if (SubgroupIndex <= 0 || !withSubgroup) return $"ФТ-{course}0{GroupIndex}";
            return $"ФТ-{course}0{GroupIndex}-{SubgroupIndex}";
        }

        public string FormatOfficialGroup(DateTime now)
        {
            if (GraduationYear <= 0) return "";
            var delta = now.Month >= 8 ? 1 : 0;
            var course = 4 - (GraduationYear - (now.Year + delta));
            var id = GraduationYear == 2023 
                ? new[] { "0809", "0810" }[GroupIndex - 1]
                : new[] { "0801", "0802", "0809", "0810" }[GroupIndex - 1];
            return $"МЕН-{course}{(GraduationYear-4) % 10}{id}";
        }

        public bool SameContact(string query)
        {
            query = query.Canonize();
            try
            {
                var first = FirstName.Canonize();
                var last = LastName.Canonize();
                var patronymic = Patronymic.Canonize();
                var queryRegex = new Regex(@$" {Regex.Escape(query)} ");
                var tgUsernameLowercase = Telegram.ToLower();
                var contact = " " + first + " " + last + " " + first + " " + patronymic + " " + tgUsernameLowercase + " " + tgUsernameLowercase.TrimStart('@') + " ";
                return queryRegex.IsMatch(contact);
            }
            catch (Exception e)
            {
                throw new Exception(ToString(), e);
            }
        }
        public string FirstLastName()
        {
            return $"{FirstName} {LastName}";
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName} {Telegram} {TgId}";
        }

        public void UpdateFromDetails(ContactDetails details)
        {
            if (details == null) return;
            if (!string.IsNullOrEmpty(details.TelegramUsername))
                Telegram = details.TelegramUsernameWithSobachka;
            if (details.TelegramId != 0)
                TgId = details.TelegramId;
        }
    }

    public static class ContactTypes
    {
        public static ContactType[] All = new ContactType[]
            { ContactType.Administration, ContactType.Staff, ContactType.Student, ContactType.External };

        public static ContactType[] AllNotExternal => new[]
            { ContactType.Administration, ContactType.Staff, ContactType.Student };
    }

    public static class PluralizeExtensions
    {
        public static string Pluralize(this int count, string oneTwoManyPipeSeparated)
        {
            var parts = oneTwoManyPipeSeparated.Split("|");
            return count.Pluralize(parts[0], parts[1], parts[2]);
        }

        public static string Pluralize(this int count, string one, string two, string many)
        {
            if (count <= 0 || (count % 100 >= 10 && count % 100 <= 20) || count % 10 > 4)
                return count + " " + many;
            if (count % 10 == 1) return count + " " + one;
            return count + " " + two;
        }
    }
}
