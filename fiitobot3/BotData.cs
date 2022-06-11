using System;
using System.Collections.Generic;
using System.Linq;

namespace fiitobot
{
    public class BotData
    {
        public string[] Admins;
        public string[] SourceSpreadsheets;
        public PersonData[] People;

        public PersonData[] FindPerson(string query)
        {
            var exactResults = People.Where(c => ExactSameContact(c.Contact, query)).ToArray();
            if (exactResults.Length > 0)
                return exactResults;
            return People.Where(c => c.Contact.SameContact(query)).ToArray();
        }

        public static bool ExactSameContact(Contact contact, string query)
        {
            var fullName = contact.LastName + " " + contact.FirstName;
            return query.Canonize().Equals(fullName.Canonize(), StringComparison.InvariantCultureIgnoreCase)
                   || query.Equals(contact.Telegram.TrimStart('@'), StringComparison.InvariantCultureIgnoreCase);
        }

        public bool IsAdmin(string username)
        {
            return Admins.Any(a => a.Trim('@').Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public PersonData FindPersonByTgId(long id)
        {
            return People.FirstOrDefault(p => p.Contact.TgId == id);
        }
    }

    public class PersonData
    {
        public Contact Contact;
        public List<Detail> Details;
    }
}
