using System;

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
        public Contact(ContactType type, long tgId, string lastName, string firstName, string patronymic = "")
        {
            Type = type;
            TgId = tgId;
            LastName = lastName;
            FirstName = firstName;
            Patronymic = patronymic;
        }

        public ContactType Type;
        public long TgId;
        public string LastName;
        public string FirstName;
        public string Patronymic;
        public int AdmissionYear = -1;
        public int GroupIndex = -1;
        public int SubgroupIndex = -1;
        public string City = "";
        public string School = "";
        public string Concurs = "";
        public string EnrollRating = "";
        public string Telegram;
        public string Phone = "";
        public string Email = "";
        public string Note = "";
        public string SecretNote = "";
        public string FiitJob = "";
        public string MainCompany = "";
        public string Status = "";
        public double? CurrentRating;

        public string FormatMnemonicGroup(DateTime now)
        {
            if (AdmissionYear <= 0) return "";
            if (GroupIndex <= 0) return "";
            var delta = now.Month >= 8 ? 0 : 1;
            var course = now.Year - AdmissionYear + 1 - delta;
            if (SubgroupIndex <= 0) return $"ФТ-{course}0{GroupIndex}";
            return $"ФТ-{course}0{GroupIndex}-{SubgroupIndex}";
        }

        public string FormatOfficialGroup(DateTime now)
        {
            if (AdmissionYear <= 0) return "";
            var delta = now.Month >= 8 ? 0 : 1;
            var course = now.Year - AdmissionYear + 1 - delta;
            var id = AdmissionYear == 2019 
                ? new[] { "0809", "0810" }[GroupIndex - 1]
                : new[] { "0801", "0802", "0809", "0810" }[GroupIndex - 1];
            return $"МЕН-{course}{AdmissionYear % 10}{id}";
        }

        public bool SameContact(string query)
        {
            query = query.Canonize();
            try
            {
                var first = FirstName.Canonize();
                var last = LastName.Canonize();
                return first == query || last == query || last + ' ' + first == query || first + ' ' + last == query ||
                       query == Telegram.ToLower() || ('@' + query) == Telegram.ToLower();
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
