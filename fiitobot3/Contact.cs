using System;

namespace fiitobot
{
    public class Contact
    {
        public Contact(int admissionYear, string lastName, string firstName, string patronymic, int groupIndex,
            int subgroupIndex, string city, string school, string concurs, string rating, string telegram, string phone,
            string email, string note, long tgId, string job, ContactType type, string secretNote)
        {
            AdmissionYear = admissionYear;
            LastName = lastName;
            FirstName = firstName;
            Patronymic = patronymic;
            GroupIndex = groupIndex;
            SubgroupIndex = subgroupIndex;
            City = city;
            School = school;
            Concurs = concurs;
            Rating = rating;
            Telegram = telegram;
            Phone = phone;
            Email = email;
            Note = note;
            SecretNote = secretNote;
            TgId = tgId;
            Job = job;
            Type = type;
        }

        public int AdmissionYear;
        public string LastName;
        public string FirstName;
        public string Patronymic;
        public int GroupIndex;
        public int SubgroupIndex;
        public string City;
        public string School;
        public string Concurs;
        public string Rating;
        public string Telegram;
        public string Phone;
        public string Email;
        public string Note;
        public string SecretNote;
        public long TgId;
        public string Job;
        public ContactType Type;
        
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
            var id = new[] { "0801", "0802", "0809", "0810" }[GroupIndex - 1];
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

        public override string ToString()
        {
            return $"{FirstName} {LastName} {Telegram} {TgId}";
        }

    }

    public enum ContactType
    {
        Student,
        Administration,
        Teacher
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
