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
        private const string Range = "A1:ZZ";
        private const string baseGoogleUrl = "https://calendar.google.com/calendar/embed?src=";
        private readonly object locker = new object();
        private readonly GSheetClient sheetClient;
        private readonly string spreadsheetId;
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo detailsRepo;
        public volatile Contact[] admins;
        private volatile Contact[] students;
        public volatile Contact[] teachers;

        public SheetContactsRepository(GSheetClient sheetClient, string contactsSpreadsheetId, IBotDataRepository botDataRepo, IContactDetailsRepo detailsRepo)
        {
            this.sheetClient = sheetClient;
            spreadsheetId = contactsSpreadsheetId;
            this.botDataRepo = botDataRepo;
            this.detailsRepo = detailsRepo;
        }

        public void Reload()
        {
            var botData = botDataRepo.GetData();
            lock (locker)
            {
                students = LoadContacts(ContactType.Student, StudentsSheetName, botData.Students);
                admins = LoadContacts(ContactType.Administration, AdminSheetName, botData.Administrators);
                teachers = LoadContacts(ContactType.Staff, StaffSheetName, botData.Teachers);
            }
        }

        public Contact[] GetStudents()
        {
            if (students == null) Reload();
            return students;
        }

        public Contact[] GetAdmins()
        {
            if (admins == null) Reload();
            return admins;
        }

        public Contact[] GetTeachers()
        {
            if (teachers == null) Reload();
            return teachers;
        }

        public string[] LoadDetailSourceSpreadsheets()
        {
            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var adminsSheet = spreadsheet.GetSheetByName("Details");
            return adminsSheet.ReadRange("A1:A").Select(row => row[0]).ToArray();
        }

        /// <summary>
        /// Загружает контакты из гугл-таблицы.
        /// Для всех контактов, у которых нет идентификатора (столбец Id), присваивает новые идентификаторы и
        /// сохраняет их в гугл-таблице.
        /// </summary>
        public Contact[] LoadContacts(ContactType contactType, string sheetName, Contact[] oldContacts)
        {
            var prevId = long.Parse((int)contactType + DateTime.Now.ToString("yyMMddhhmmssfff"));
            long GenerateNewId()
            {
                // ReSharper disable once AccessToModifiedClosure
                prevId++;
                return prevId;
            }

            var spreadsheet = sheetClient.GetSpreadsheet(spreadsheetId);
            var contactsSheet = spreadsheet.GetSheetByName(sheetName);
            var synchronizer = new GSheetSynchronizer<Contact, long>(contactsSheet, contact => contact.Id);
            var loadContacts = synchronizer.LoadSheetAndCreateIdsForNewRecords(() => new Contact { Type = contactType }, GenerateNewId);
            var changes = new List<Contact>();
            if (loadContacts.Any(c => c.Id == 0))
                throw new Exception("Why no Id?!?");
            var pairs = loadContacts.Join(
                    oldContacts ?? Array.Empty<Contact>(),
                    c => c.Id,
                    c => c.Id, (newContact, currentContact) => (newContact, currentContact));
            foreach (var (newContact, oldContact) in pairs)
            {
                //TODO обобщить: пометить часть полей как хранящиеся только в S3 и отсутствующие в гугл-таблице
                //Пока что такие BirthDate и ReceiveBirthdayNotifications - то, что пользователи указывают сами.
                newContact.BirthDate = oldContact.BirthDate;
                newContact.ReceiveBirthdayNotifications = oldContact.ReceiveBirthdayNotifications;

                // Мы поменяли username в Google Sheets → Надо перезаписать все в details, если там другой username
                if (newContact.Telegram != oldContact.Telegram)
                {
                    var details = detailsRepo.GetById(newContact.Id).Result;
                    if (details.TelegramUsername != newContact.Telegram)
                    {
                        details.TelegramId = 0;
                        details.TelegramUsername = newContact.TelegramUsername;
                        details.TelegramUsernameSource = TgUsernameSource.GoogleSheet;
                        detailsRepo.Save(details).Wait();
                    }
                }
                // Мы хотим получить актуальную инфу из бота, и для этого удалили TgId из таблички
                // → берем все, что есть из details
                // По хорошему обновить бы у всех, но для этого надо будет прочитать все details всех контактов, а это непозволительно дорого
                // поэтому делаем это только если явно заказали сделать, удалив TgId из таблички
                else if (newContact.TgId == 0 || newContact.Telegram == "")
                {
                    var details = detailsRepo.GetById(newContact.Id).Result;
                    if (details.TelegramId != 0)
                    {
                        newContact.TgId = details.TelegramId;
                        newContact.Telegram = details.TelegramUsernameWithSobachka;
                        changes.Add(newContact);
                    }
                }
            }

            Console.WriteLine("Changes of " + contactType);
            foreach (var contact in changes)
            {
                Console.WriteLine(contact);
            }
            synchronizer.UpdateSheet(() => new Contact { Type = contactType }, changes);
            foreach (var contact in loadContacts)
            {
                if (contact.Telegram.StartsWith("https://t.me/"))
                    contact.Telegram = contact.Telegram.Replace("https://t.me/", "@");
            }
            return loadContacts.ToArray();
        }

        public (IList<UrfuStudent> newStudents, IList<UrfuStudent> updatedStudents) UpdateStudentsActivity(IReadOnlyList<UrfuStudent> itsContacts)
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
                if (colIndex < 0)
                    throw new Exception("Unknown column " + colName + ". Headers: " + string.Join(", ", headers));
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
                var groupName = row.FormatOfficialGroup(DateTime.Now);
                var urfuStudent = map[key].FirstOrDefault(s => s.GroupName == groupName)
                                  ?? new UrfuStudent(groupName)
                                  {
                                      Name = row.FirstLastName(),
                                      Status = row.Status != Contact.ActiveStatus
                                          ? row.Status :
                                          (row.IsGraduated(DateTime.Now) ? "Закончил" : "Переведён"),
                                      Rating = row.CurrentRating
                                  };
                notUsed.Remove(urfuStudent);
                if (Update("Status", row.Status, urfuStudent.Status, rowIndex))
                    updatedStudents.Add(urfuStudent);
                Update("CurrentRating", row.CurrentRating, urfuStudent.Rating, rowIndex);
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

            return new Contact
            {
                Id = GetId(),
                Type = contactType,
                TgId = long.TryParse(Get("TgId"), out var tgId) ? tgId : -1,
                LastName = Get("LastName"),
                FirstName = Get("FirstName"),
                Patronymic = Get("Patronymic"),
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
                Google = Get("Google"),
                Notion = Get("Notion"),
                Note = Get("Note"),
                FiitJob = Get("FiitJob"),
                MainCompany = Get("MainCompany"),
                SecretNote = Get("SecretNote"),
                CurrentRating = UniversalParseDouble(Get("CurrentRating")),
                GoogleCalendarId = Get("GoogleCalendarId")
            };
        }

        private double? UniversalParseDouble(string s)
        {
            s = s.Replace(",", ".");
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? (double?)d : null;
        }

        private string GetGoogleCalendarLinkById(string googleCalendarId)
        {
            return googleCalendarId.Length < 1 ? "" : $"{baseGoogleUrl}{googleCalendarId}";
        }
    }
}
