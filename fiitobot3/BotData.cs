using System;
using System.Collections.Generic;
using System.Linq;

namespace fiitobot
{
    public class BotData
    {
        public Contact[] Administrators;
        public string[] SourceSpreadsheets;
        public PersonData[] Students;
        public IEnumerable<PersonData> AllContacts => Students.Concat(Administrators.Select(PersonData.FromContact));

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

        public bool IsAdmin(string username)
        {
            return Administrators.Any(a => a.Telegram.Trim('@').Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsAdmin(long userId)
        {
            return Administrators.Any(a => a.TgId == userId);
        }

        public PersonData FindPersonByTgId(long id)
        {
            return AllContacts.FirstOrDefault(p => p.Contact.TgId == id);
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
