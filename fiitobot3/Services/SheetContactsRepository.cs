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
        private volatile Contact[] admins;
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

        public Contact[] GetAllAdmins()
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

        public Contact[] LoadAdmins()
        {
            return LoadContacts(ContactType.Administration, "Administrators");
        }

        public Contact[] LoadContacts()
        {
            return LoadContacts(ContactType.Student, "Students");
        }
        public Contact[] LoadContacts(ContactType contactType, string sheetName)
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var studentsSheet = spreadsheet.GetSheetByName(sheetName);
            var data = studentsSheet.ReadRange("A1:O");
            var headers = data[0].TakeWhile(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return data.Skip(1).Select(row => ParseContactFromRow(row, headers, contactType)).ToArray();
        }

        private Contact ParseContactFromRow(List<string> row, List<string> headers, ContactType contactType)
        {
            string Get(string name)
            {
                try
                {
                    var index = headers.IndexOf(name);
                    return index >= 0 && index < row.Count ? row[index] : "";
                }
                catch (Exception e)
                {
                    throw new Exception($"Bad {name}", e);
                }
            }

            return new Contact(
                int.TryParse(Get("AdmissionYear"), out var admissionYear) ? admissionYear : -1,
                Get("LastName"),
                Get("FirstName"),
                Get("Patronymic"),
                int.TryParse(Get("GroupIndex"), out var groupIndex) ? groupIndex : -1,
                int.TryParse(Get("SubgroupIndex"), out var subgroupIndex) ? subgroupIndex : -1,
                Get("City"),
                Get("School"),
                Get("Konkurs"),
                Get("Rating"),
                Get("Telegram"),
                Get("Phone"),
                Get("Email"),
                Get("Note"),
                long.TryParse(Get("TgId"), out var tgId) ? tgId : -1,
                Get("Job"),
                contactType

            );
        }

        public string[] GetOtherSpreadsheets()
        {
            return otherSpreadsheets!;
        }
    }
}
