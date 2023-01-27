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
        public Contact[] Students;
        [JsonIgnore]
        public IEnumerable<Contact> AllContacts => Students.Concat(Administrators).Concat(Teachers ?? Array.Empty<Contact>());
        public Contact[] Teachers;

        public Contact[] FindContact(string query)
        {
            var res = FindContactIn(query, AllContacts.Where(c => c.Status == Contact.ActiveStatus));
            if (res.Length == 0)
                res = FindContactIn(query, AllContacts.Where(c => c.Status != Contact.ActiveStatus));
            return res;
        }

        private static Contact[] FindContactIn(string query, IEnumerable<Contact> contacts)
        {
            var allSearchable = contacts.ToList();
            var exactResults = allSearchable
                .Where(c => ExactSameContact(c, query))
                .ToArray();
            if (exactResults.Length > 0)
                return exactResults;
            return allSearchable.Where(c => c.SameContact(query)).ToArray();
        }

        public Contact[] SearchContacts(string query)
        {
            var res = FindContact(query);
            if (res.Length > 0) return res;
            var parts = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(FindContact)
                       .Where(g => g.Length > 0)
                       .MinBy(g => g.Length)
                   ?? Array.Empty<Contact>();
        }

        public static bool ExactSameContact(Contact contact, string query)
        {
            var fullName = contact.LastName + " " + contact.FirstName;
            var tg = contact.Telegram?.TrimStart('@') ?? "";
            var fn = fullName.Canonize();
            return query.Canonize().Equals(fn, StringComparison.InvariantCultureIgnoreCase)
                   || query.Equals(tg, StringComparison.InvariantCultureIgnoreCase)
                   || query.Equals(""+contact.TgId);
        }

        public Contact FindContactByTgId(long id)
        {
            return AllContacts.FirstOrDefault(p => p.TgId == id);
        }

        public Contact FindContactByTelegramName(string username)
        {
            return AllContacts.FirstOrDefault(p => p.SameTelegramUsername(username));
        }
    }
}
