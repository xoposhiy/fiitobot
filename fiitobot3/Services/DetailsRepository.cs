using System;
using System.Collections.Generic;
using System.Linq;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services.Commands;

namespace fiitobot.Services
{
    public class DetailsRepository
    {
        private readonly GSheetClient sheetClient;
        private readonly SheetContactsRepository contactsRepo;
        private readonly object locker = new object();
        private DateTime lastUpdateTime = DateTime.MinValue;
        private volatile List<SheetData> data = new List<SheetData>();
        private volatile string[] sources;

        public DetailsRepository(GSheetClient sheetClient, SheetContactsRepository contactsRepo)
        {
            this.sheetClient = sheetClient;
            this.contactsRepo = contactsRepo;
        }

        public void ReloadIfNeeded(bool force = false)
        {
            lock (locker)
            {
                if (DateTime.Now - lastUpdateTime <= TimeSpan.FromHours(24) && !force) return;
                sources = contactsRepo.GetOtherSpreadsheets();
                var newData =
                    from spreadsheet in sources.Select((url, sourceId) => (url, sourceId))
                    let sheet = sheetClient.GetSheetByUrl(spreadsheet.url)
                    let values = sheet.ReadRange("A1:ZZ")
                    select new SheetData(sheet.SheetName, spreadsheet.sourceId, spreadsheet.url, values);
                data = newData.ToList();
                lastUpdateTime = DateTime.Now;
            }
        }

        public ContactWithDetails[] EnrichWithDetails(Contact[] contacts)
        {
            ReloadIfNeeded();
            var people = contacts.Select(c => new ContactWithDetails(c, new List<ContactDetail>())).ToArray();
            var index = CreatePeopleIndex(people);

            foreach (var (rubric, sourceId, url, values) in data)
            {
                try
                {
                    if (values.Count <= 1) continue;
                    var headerRowsCount = 1;
                    var headers = values[0];
                    if (string.IsNullOrWhiteSpace(headers[0]))
                    {
                        headerRowsCount++;
                        var h0 = headers;
                        headers = values[1];
                        for (int i = 0; i < h0.Count; i++)
                        {
                            if (i < headers.Count && string.IsNullOrWhiteSpace(headers[i]))
                                headers[i] = h0[i];
                        }
                    }

                    Console.WriteLine("Рубрика: " + rubric + " " + url);
                    var count = 0;
                    foreach (var row in values.Skip(headerRowsCount))
                    {
                        var person = FindPerson(row, index);
                        if (person != null)
                        {
                            count++;
                            for (int i = 0; i < row.Count; i++)
                            {
                                var value = row[i];
                                if (string.IsNullOrWhiteSpace(value)) continue;
                                if (i >= headers.Count || string.IsNullOrWhiteSpace(headers[i])) continue;
                                var ignoredValues = GetContactValuesToIgnore(person.Contact);
                                if (ignoredValues.Any(ignoredValue =>
                                        value.StartsWith(ignoredValue, StringComparison.OrdinalIgnoreCase))) continue;
                                if (person.Details.Any(res =>
                                        res.Parameter.Equals(headers[i], StringComparison.OrdinalIgnoreCase)))
                                    continue;
                                var detail = new ContactDetail(rubric, headers[i].Replace("\n", " ").Replace("\r", " "), value,
                                    sourceId);
                                person.Details.Add(detail);
                            }
                        }
                    }
                    Console.WriteLine("Students found: " + count);
                }
                catch (Exception e)
                {
                    throw new Exception($"spreadsheet {rubric} at {url} error", e);
                }
            }

            return people;
        }

        private ContactWithDetails FindPerson(List<string> row, Dictionary<string, HashSet<ContactWithDetails>> peopleIndex)
        {
            string prevCellValue = null;
            foreach (var cell in row)
            {
                var parts = cell.Split(" ");
                var exactPerson = GetExactPerson(peopleIndex, parts[0], cell); // полное ФИ/ИФ в ячейке
                if (exactPerson != null) return exactPerson;
                if (prevCellValue != null)
                {
                    // ФИ/ИФ разбитое на две соседних ячейки
                    var possibleFullName = prevCellValue + " " + cell;
                    exactPerson = GetExactPerson(peopleIndex, prevCellValue, possibleFullName);
                    if (exactPerson != null) return exactPerson;
                }
                prevCellValue = cell;
            }
            return null;
        }

        private static ContactWithDetails GetExactPerson(Dictionary<string, HashSet<ContactWithDetails>> peopleIndex, string part1, string cell)
        {
            var candidates = peopleIndex.GetOrDefault(part1);
            if (candidates == null || candidates.Count == 0) return null;
            if (candidates.Count == 1)
                return candidates.First();
            var exactCandidates = candidates.Where(c => BotData.ExactSameContact(c.Contact, cell)).ToList();
            return exactCandidates.Count == 1 ? candidates.First() : null;
        }

        private static Dictionary<string, HashSet<ContactWithDetails>> CreatePeopleIndex(ContactWithDetails[] people)
        {
            var index = new Dictionary<string, HashSet<ContactWithDetails>>(StringComparer.OrdinalIgnoreCase);

            void Add(string token, ContactWithDetails person)
            {
                var bucket = index.GetOrCreate(token, t => new HashSet<ContactWithDetails>());
                bucket.Add(person);
            }

            foreach (var person in people)
            {
                var contact = person.Contact;
                Add(contact.FirstName, person);
                Add(contact.LastName, person);
                if (!string.IsNullOrWhiteSpace(contact.Patronymic))
                    Add(contact.Patronymic, person);
            }
            return index;
        }

        public ContactDetail[] GetPersonDetails(Contact contact)
        {
            ReloadIfNeeded();
            var result = new List<ContactDetail>();
            foreach (var (name, sourceId, url, values) in data)
            {
                try
                {
                    if (values.Count <= 1) continue;
                    var headerRowsCount = 1;
                    var headers = values[0];
                    if (string.IsNullOrWhiteSpace(headers[0]))
                    {
                        headerRowsCount++;
                        var h0 = headers;
                        headers = values[1];
                        for (int i = 0; i < h0.Count; i++)
                        {
                            if (i < headers.Count && string.IsNullOrWhiteSpace(headers[i]))
                                headers[i] = h0[i];
                        }
                    }

                    foreach (var row in values.Skip(headerRowsCount))
                    {
                        if (RowContains(row, contact))
                        {
                            for (int i = 0; i < row.Count; i++)
                            {
                                var value = row[i];
                                if (string.IsNullOrWhiteSpace(value)) continue;
                                if (i >= headers.Count || string.IsNullOrWhiteSpace(headers[i])) continue;
                                var ignoredValues = GetContactValuesToIgnore(contact);
                                if (ignoredValues.Any(ignoredValue =>
                                        value.StartsWith(ignoredValue, StringComparison.OrdinalIgnoreCase))) continue;
                                if (result.Any(res =>
                                        res.Parameter.Equals(headers[i], StringComparison.OrdinalIgnoreCase)))
                                    continue;
                                var detail = new ContactDetail(name, headers[i].Replace("\n", " ").Replace("\r", " "), value,
                                    sourceId);
                                result.Add(detail);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"spreadsheet {name} at {url} error", e);
                }
            }

            return result.ToArray();
        }

        private string[] GetContactValuesToIgnore(Contact contact)
        {
            return new[]
            {
                contact.FirstName,
                contact.LastName,
                contact.Patronymic,
                contact.FirstName + " " + contact.LastName,
                contact.LastName + " " + contact.FirstName + " " + contact.Patronymic,
                contact.City,
                contact.Concurs,
                contact.City,
                contact.Email,
                contact.Phone,
                contact.Email,
                contact.School,
                contact.Telegram,
                contact.FormatOfficialGroup(DateTime.Now),
                contact.FormatOfficialGroup(DateTime.Now.Subtract(TimeSpan.FromDays(365))),
                contact.FormatMnemonicGroup(DateTime.Now),
                contact.FormatMnemonicGroup(DateTime.Now.Subtract(TimeSpan.FromDays(365)))
            }.Where(v => v.Length > 1).ToArray();
        }

        private bool RowContains(List<string> row, Contact contact)
        {
            if (row.Any(v =>
                    v.StartsWith(contact.LastName + " " + contact.FirstName, StringComparison.OrdinalIgnoreCase)))
                return true;
            var firstNameIndex = row.IndexOf(contact.FirstName);
            var lastNameIndex = row.IndexOf(contact.LastName);
            if (Math.Abs(firstNameIndex - lastNameIndex) == 1)
                return true;
            return false;
        }

        public void ForceReload()
        {
            ReloadIfNeeded(true);
        }
    }
}
