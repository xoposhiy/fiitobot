using System;
using System.Collections.Generic;
using System.Linq;
using fiitobot.GoogleSpreadsheet;

namespace fiitobot.Services
{
    public class SheetContactsRepository
    {
        private readonly GSheetClient sheetClient;
        private volatile Contact[] contacts;
        private volatile string[] admins;
        private volatile string[] otherSpreadsheets;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private readonly object locker = new object();
        private readonly string spreadsheetId;

        public SheetContactsRepository(GSheetClient sheetClient, string contactsSpreadsheetId)
        {
            this.sheetClient = sheetClient;
            spreadsheetId = contactsSpreadsheetId;
        }

        public Contact[] FindContacts(string query)
        {
            ReloadIfNeeded();
            return contacts!.Where(c => SameContact(c, query)).ToArray();
        }

        private void ReloadIfNeeded(bool force = false)
        {
            lock (locker)
            {
                if (DateTime.Now - lastUpdateTime <= TimeSpan.FromMinutes(1) && !force) return;
                contacts = LoadContacts();
                admins = LoadAdmins();
                otherSpreadsheets = LoadDetailSourceSpreadsheets();
                lastUpdateTime = DateTime.Now;
            }
        }

        public Contact[] GetAllContacts()
        {
            ReloadIfNeeded();
            return contacts!;
        }

        public string[] GetAllAdmins()
        {
            ReloadIfNeeded();
            return admins;
        }

        public string[] LoadDetailSourceSpreadsheets()
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var adminsSheet = spreadsheet.GetSheetByName("Details");
            return adminsSheet.ReadRange("A1:A").Select(row => row[0]).ToArray();
        }

        private bool SameContact(Contact contact, string query)
        {
            query = query.ToLower();
            var first = contact.FirstName.ToLower();
            var last = contact.LastName.ToLower();
            return first == query || last == query || last + ' ' + first == query || first + ' ' + last == query ||
                   query == contact.Telegram.ToLower() || ('@' + query) == contact.Telegram.ToLower();
        }

        public string[] LoadAdmins()
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var adminsSheet = spreadsheet.GetSheetByName("Admins");
            return adminsSheet.ReadRange("A1:A").Select(row => row[0]).ToArray();
        }

        public Contact[] LoadContacts()
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var studentsSheet = spreadsheet.GetSheetByName("Students");
            var data = studentsSheet.ReadRange("A1:O");
            var headers = data[0];
            return data.Skip(1).Select(row => ParseContactFromRow(row, headers)).ToArray();
        }

        private Contact ParseContactFromRow(List<string> row, List<string> headers)
        {
            string Get(string name)
            {
                try
                {
                    var index = headers.IndexOf(name);
                    return index < row.Count ? row[index] : "";
                }
                catch (Exception e)
                {
                    throw new Exception($"Bad {name}", e);
                }
            }

            return new Contact(
                int.Parse(Get("AdmissionYear")),
                Get("LastName"),
                Get("FirstName"),
                Get("Patronymic"),
                int.Parse(Get("GroupIndex")),
                int.Parse(Get("SubgroupIndex")),
                Get("City"),
                Get("School"),
                Get("Konkurs"),
                Get("Rating"),
                Get("Telegram"),
                Get("Phone"),
                Get("Email"),
                Get("Note"),
                long.TryParse(Get("TgId"), out var tgId) ? tgId : -1
            );
        }

        public bool IsAdmin(string username)
        {
            ReloadIfNeeded();
            return admins!.Any(a => a.Trim('@').Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public string[] GetOtherSpreadsheets()
        {
            return otherSpreadsheets!;
        }

        public void ForceReload()
        {
            ReloadIfNeeded(true);
        }
    }
}
