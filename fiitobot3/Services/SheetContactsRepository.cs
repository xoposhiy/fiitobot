using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiitobot.GoogleSpreadsheet;

namespace fiitobot.Services
{
    public class SheetContactsRepository
    {
        private readonly GSheetClient sheetClient;
        private volatile Contact[] contacts;
        private volatile Contact[] admins;
        private volatile Contact[] teachers;
        private volatile string[] otherSpreadsheets;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private readonly object locker = new object();
        private readonly string spreadsheetId;
        private string StudentsSheetName = "Students";
        private string Range = "A1:R";

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
                contacts = LoadContacts(ContactType.Student, StudentsSheetName);
                admins = LoadContacts(ContactType.Administration, "Administrators");
                teachers = LoadContacts(ContactType.Teacher, "Teachers");
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

        public Contact[] GetAllTeachers()
        {
            ReloadIfNeeded();
            return teachers;
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

        public Contact[] LoadContacts(ContactType contactType, string sheetName)
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var studentsSheet = spreadsheet.GetSheetByName(sheetName);
            var data = studentsSheet.ReadRange(Range);
            var headers = data[0].TakeWhile(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return data.Skip(1).Select(row => ParseContactFromRow(row, headers, contactType)).ToArray();
        }
        public (IList<UrfuStudent> newStudents, IList<UrfuStudent> updatedStudents) UpdateStudentsActivity(IReadOnlyList<UrfuStudent> updatedContacts)
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var studentsSheet = spreadsheet.GetSheetByName(StudentsSheetName);
            var data = studentsSheet.ReadRange(Range);
            var headers = data[0].TakeWhile(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var sheetStudents = data.Skip(1).Select(row => ParseContactFromRow(row, headers, ContactType.Student)).ToArray();
            var map = updatedContacts.ToLookup(s => s.Name.Canonize());
            var edit = studentsSheet.Edit();
            var editsCount = 0;
            bool Update(string colName, object oldValue, object newValue, int rowIndex)
            {
                if (oldValue == newValue || ""+oldValue == ""+newValue) return false;
                var colIndex = headers.IndexOf(colName);
                edit.WriteRangeNoCasts((rowIndex, colIndex),
                    new List<List<object>> { new List<object> { newValue } });
                Console.WriteLine($"Update google sheet: ({rowIndex} {colIndex}) {oldValue} → {newValue}");
                editsCount++;
                return true;
            }
            var rowIndex = 1;
            var notUsed = updatedContacts.ToHashSet();
            var updatedStudents = new List<UrfuStudent>();
            foreach (var row in sheetStudents)
            {
                var key = (row.LastName + " " + row.FirstName + " " + row.Patronymic).Canonize();
                var urfuStudent = map[key].FirstOrDefault(s => s.GroupName == row.FormatOfficialGroup(DateTime.Now));
                if (urfuStudent != null)
                {
                    notUsed.Remove(urfuStudent);
                    if (Update("Status", row.Status, urfuStudent.Status, rowIndex))
                        updatedStudents.Add(urfuStudent);
                    Update("CurrentRating", row.CurrentRating, urfuStudent.Rating, rowIndex);
                }
                rowIndex++;
            }
            if (editsCount > 0)
                edit.Execute();
            return (notUsed.Where(s => s.Status == "Активный").ToList(), updatedStudents);
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
                Get("EnrollRating"),
                Get("Telegram"),
                Get("Phone"),
                Get("Email"),
                Get("Note"),
                long.TryParse(Get("TgId"), out var tgId) ? tgId : -1,
                Get("Job"),
                contactType,
                Get("SecretNote"),
                Get("Status"),
                UniversalParseDouble(Get("CurrentRating"))
            );
        }

        private double? UniversalParseDouble(string s)
        {
            s = s.Replace(",", ".");
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? (double?)d : null;

        }

        public string[] GetOtherSpreadsheets()
        {
            return otherSpreadsheets!;
        }
    }
}
