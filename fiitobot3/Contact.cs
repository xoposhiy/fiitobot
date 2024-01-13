using System;
using System.Linq;
using System.Text.RegularExpressions;
using fiitobot.Services;

namespace fiitobot
{
    public enum ContactType
    {
        External = -1,
        Student = 0,
        Administration = 1,
        Staff = 2,
    }

    public enum Gender
    {
        Unknown = 0,
        M,
        F
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
        public Gender Gender;
        public int AdmissionYear = -1;
        public int GraduationYear = -1;
        public int GroupIndex = -1;
        public int SubgroupIndex = -1;
        public string City = "";
        public string School = "";
        public string Concurs = "";
        public string EnrollRating = "";
        public string Telegram;
        public string BirthDate;
        public string Phone = "";
        public string Email = "";
        public string Google = "";
        public string Github = "";
        public string Notion = "";
        public string Note = "";
        public string SecretNote = "";
        public string FiitJob = "";
        public string MainCompany = "";
        public string Status = "";
        public double? CurrentRating;
        public bool IsReceivesNotification = true;

        public bool IsGraduated(DateTime now)
        {
            return now > new DateTime(GraduationYear, 07, 01) && (Status == "Закончил" || Status == "Активный");
        }

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
            if (GroupIndex <= 0) return "";
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

        public void UpdateBirthDate(IBotDataRepository botDataRepo, Contact sender, string text = null, bool isReceivesNotif = true)
        {
            var botData = botDataRepo.GetData();

            var impersonatedUser = botData.AllContacts.FirstOrDefault(c => c.Id.ToString() == sender.Id.ToString());
            sender = impersonatedUser ?? sender;

            if (text != null)
                sender.BirthDate = text;

            sender.IsReceivesNotification = isReceivesNotif;

            botDataRepo.Save(botData);
        }

        public static int ExtractGroupIndex(string group)
        {
            group = group.Trim();
            if (group.StartsWith("МЕН-"))
            {
                var digits = group.Split("-")[1];
                //2019
                if (digits[1] == '9')
                    return new[] { "0809", "0810" }.IndexOf(digits.Substring(2)) + 1;
                //2020..2023
                return new[] { "0801", "0802", "0809", "0810" }.IndexOf(digits.Substring(2)) + 1;
            }
            else if (group.StartsWith("ФТ-", StringComparison.OrdinalIgnoreCase)
                     ||group.StartsWith("ФИИТ-", StringComparison.OrdinalIgnoreCase))
            {
                return group.Last() - '0';
            }
            else
                throw new Exception($"Unsupported group format {group}");
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
