using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiitobot.GoogleSpreadsheet;

namespace fiitobot.Services
{
    public class SheetContactsRepository
    {
        private const string StaffSheetName = "Staff";
        private const string StudentsSheetName = "Students";
        private const string AdminSheetName = "Administrators";
        private const string Range = "A1:T";
        private readonly object locker = new object();
        private readonly GSheetClient sheetClient;
        private readonly string spreadsheetId;
        private volatile Contact[] admins;
        private volatile Contact[] contacts;
        private volatile string[] otherSpreadsheets;
        private volatile Contact[] staff;
        private DateTime lastUpdateTime = DateTime.MinValue;

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
                admins = LoadContacts(ContactType.Administration, AdminSheetName);
                staff = LoadContacts(ContactType.Staff, StaffSheetName);
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
            return staff;
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
                   query == contact.Telegram.ToLower() || '@' + query == contact.Telegram.ToLower();
        }

        /// <summary>
        /// Загружает контакты из гугл-таблицы.
        /// Для всех контактов, у которых нет идентификатора (столбец Id), присваивает новые идентификаторы и
        /// сохраняет их в гугл-таблице.
        /// </summary>
        public Contact[] LoadContacts(ContactType contactType, string sheetName)
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var studentsSheet = spreadsheet.GetSheetByName(sheetName);
            var data = studentsSheet.ReadRange(Range);
            var headers = data[0].TakeWhile(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var loadContacts = data.Skip(1).Select(row => ParseContactFromRow(row, headers, contactType)).ToArray();
            var newContacts = loadContacts.Where(c => c.Id == -1).ToList();
            if (newContacts.Any())
            {
                var now = DateTime.Now;
                var timeId = long.Parse((int)contactType + now.ToString("yyMMddhhmmssfff"));
                var prevId = Math.Max(timeId, loadContacts.Max(c => c.Id));
                var edit = studentsSheet.Edit();
                var idColumnIndex = headers.IndexOf("Id");
                foreach (var contact in newContacts)
                {
                    contact.Id = ++prevId;
                    var row = new List<object> { contact.Id };
                    var contactIndex = loadContacts.IndexOf(contact);
                    edit.WriteRangeNoCasts((contactIndex+1, idColumnIndex), new List<List<object>> { row });
                }
                edit.Execute();
            }
            return loadContacts;
        }

        public (IList<UrfuStudent> newStudents, IList<UrfuStudent> updatedStudents) UpdateStudentsActivity(
            IReadOnlyList<UrfuStudent> itsContacts)
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var studentsSheet = spreadsheet.GetSheetByName(StudentsSheetName);
            var data = studentsSheet.ReadRange(Range);
            var headers = data[0].TakeWhile(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var sheetStudents = data.Skip(1).Select(row => ParseContactFromRow(row, headers, ContactType.Student))
                .ToArray();
            var map = itsContacts.ToLookup(s => s.Name.Canonize());
            var edit = studentsSheet.Edit();
            var editsCount = 0;

            bool Update(string colName, object oldValue, object newValue, int rowIndex)
            {
                if (oldValue == newValue || "" + oldValue == "" + newValue) return false;
                var colIndex = headers.IndexOf(colName);
                edit.WriteRangeNoCasts((rowIndex, colIndex),
                    new List<List<object>> { new List<object> { newValue } });
                Console.WriteLine($"Update google sheet: ({rowIndex} {colIndex}) {oldValue} → {newValue}");
                editsCount++;
                return true;
            }

            var rowIndex = 1;
            var notUsed = itsContacts.ToHashSet();
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

            long GetId() => long.TryParse(Get("Id"), out var id) ? id : -1;

            return new Contact(
                GetId(),
                contactType,
                long.TryParse(Get("TgId"), out var tgId) ? tgId : -1,
                Get("LastName"),
                Get("FirstName"),
                Get("Patronymic"))
            {
                AdmissionYear = int.TryParse(Get("AdmissionYear"), out var admYear) ? admYear : -1,
                GraduationYear = int.TryParse(Get("GraduationYear"), out var gradYear) ? gradYear : -1,
                Status = Get("Status"),
                GroupIndex = int.TryParse(Get("GroupIndex"), out var groupIndex) ? groupIndex : -1,
                SubgroupIndex = int.TryParse(Get("SubgroupIndex"), out var subgroupIndex) ? subgroupIndex : -1,
                City = Get("City"),
                School = Get("School"),
                Concurs = Get("Concurs"),
                EnrollRating = Get("EnrollRating"),
                Telegram = Get("Telegram"),
                Phone = Get("Phone"),
                Email = Get("Email"),
                Note = Get("Note"),
                FiitJob = Get("FiitJob"),
                MainCompany = Get("MainCompany"),
                SecretNote = Get("SecretNote"),
                CurrentRating = UniversalParseDouble(Get("CurrentRating"))
            };
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
