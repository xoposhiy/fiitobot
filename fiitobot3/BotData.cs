using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace fiitobot
{
    public class BotData
    {
        public Contact[] Administrators;
        public string[] SourceSpreadsheets;
        public PersonData[] Students;
        [JsonIgnore]
        public IEnumerable<PersonData> AllContacts => Students.Concat(Administrators.Select(PersonData.FromContact)).Concat(Teachers?.Select(PersonData.FromContact) ?? Array.Empty<PersonData>());
        public Contact[] Teachers;

        public PersonData[] FindPerson(string query)
        {
            var allSearchable = AllContacts.ToList();
            var exactResults = allSearchable
                .Where(c => ExactSameContact(c.Contact, query))
                .ToArray();
            if (exactResults.Length > 0)
                return exactResults;
            return allSearchable.Where(c => c.Contact.SameContact(query)).ToArray();
        }

        public static bool ExactSameContact(Contact contact, string query)
        {
            var fullName = contact.LastName + " " + contact.FirstName;
            var tg = contact.Telegram?.TrimStart('@') ?? "";
            var fn = fullName.Canonize();
            return query.Canonize().Equals(fn, StringComparison.InvariantCultureIgnoreCase)
                   || query.Equals(tg, StringComparison.InvariantCultureIgnoreCase);
        }
        public bool IsAdmin(long userId, string username)
        {
            return Administrators.Any(a => a.TgId == userId) || username != null && Administrators.Any(a => a.Telegram.Trim('@').Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        public bool IsTeacher(long userId, string username)
        {
            return Teachers.Any(a => a.TgId == userId) || username != null && Teachers.Any(a => a.Telegram.Trim('@').Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        public bool IsStudent(long userId, string username)
        {
            return Students.Any(a => a.Contact.TgId == userId) || username != null && Students.Any(a => a.Contact.Telegram.Trim('@').Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public PersonData FindPersonByTgId(long id)
        {
            return AllContacts.FirstOrDefault(p => p.Contact.TgId == id);
        }

        public PersonData FindPersonByTelegramName(string username)
        {
            return AllContacts.FirstOrDefault(p => p.Contact.Telegram.Trim('@').Equals(username.Trim('@'), StringComparison.OrdinalIgnoreCase));
        }
    }

    public class PersonData
    {
        public static PersonData FromContact(Contact contact)
        {
            return new PersonData
            {
                Contact = contact,
                Details = new List<Detail>()
            };
        }
        public Contact Contact;
        public List<Detail> Details;
    }
}
