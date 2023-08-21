using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using fiitobot.GoogleSpreadsheet;

namespace fiitobot.Services
{
    public class MarksReloadResult
    {
        public MarksReloadResult(int updatedStudentsCount, string[] processedSheets)
        {
            UpdatedStudentsCount = updatedStudentsCount;
            ProcessedSheets = processedSheets;
        }

        public int UpdatedStudentsCount;
        public string[] ProcessedSheets;
    }

    public class MarksReloadService
    {
        private readonly BotDataRepository dataRepo;
        private readonly S3ContactsDetailsRepo detailsRepo;
        private readonly GSheetClient gSheetClient;
        private readonly Regex sheetNameRegex = new Regex(@"^(\d) семестр$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MarksReloadService(BotDataRepository dataRepo, S3ContactsDetailsRepo detailsRepo, GSheetClient gSheetClient)
        {
            this.dataRepo = dataRepo;
            this.detailsRepo = detailsRepo;
            this.gSheetClient = gSheetClient;
        }

        public async Task<MarksReloadResult> ReloadFrom(string spreadsheetUrl)
        {
            var spreadsheet = gSheetClient.GetSpreadsheet(spreadsheetUrl);
            var sheets = spreadsheet.GetSheets();
            var contacts = dataRepo.GetData().Students;
            var studentSemesters = new Dictionary<long, List<SemesterMarks>>();
            var processedSheets = new List<string>();
            foreach (var sheet in sheets)
            {
                var match = sheetNameRegex.Match(sheet.SheetName);
                if (match.Success)
                {
                    Console.WriteLine(sheet.SheetName);
                    var semesterNumber = int.Parse(match.Groups[1].Value);
                    var sheetRows = sheet.ReadRange("A1:Z");
                    if (!string.IsNullOrWhiteSpace(sheetRows[0][0]))
                        continue;
                    var rowsCount = sheetRows.Skip(2).TakeWhile(r => r.Count > 0 && !string.IsNullOrWhiteSpace(r[0])).Count();
                    sheetRows = sheetRows.Take(rowsCount + 2).ToList();
                    ParseMarksData(semesterNumber, sheetRows, contacts, studentSemesters);
                    processedSheets.Add(sheet.SheetName);
                }
            }
            var updatedCount = 0;

            foreach (var kv in studentSemesters)
            {
                var details = await detailsRepo.FindById(kv.Key) ?? new ContactDetails(kv.Key);
                var reloadedSemesters = studentSemesters[kv.Key];
                var oldSem = details.Semesters == null ? "" : details.Semesters.StrJoin("\n", s => $"{s.SemesterNumber}:\n{s.Marks.StrJoin("\n")}");
                var newSem = reloadedSemesters.StrJoin("\n", s => $"{s.SemesterNumber}:\n{s.Marks.StrJoin("\n")}");
                if (oldSem != newSem)
                {
                    details.Semesters = reloadedSemesters;
                    await detailsRepo.Save(details);
                    updatedCount++;
                }
            }

            return new MarksReloadResult(updatedCount, processedSheets.ToArray());
        }

        private void ParseMarksData(
            int semesterNumber,
            List<List<string>> sheetRows,
            Contact[] contacts,
            Dictionary<long, List<SemesterMarks>> studentSemesters)
        {
            var fioColIndex = sheetRows[1].IndexOf(cell => "ФИО".Equals(cell, StringComparison.OrdinalIgnoreCase));
            var groupColIndex = sheetRows[1].IndexOf(cell => "Группа".Equals(cell, StringComparison.OrdinalIgnoreCase));
            var zachColIndex = sheetRows[0].IndexOf(cell => "зачет".Equals(cell, StringComparison.OrdinalIgnoreCase));
            if (zachColIndex<0) zachColIndex = sheetRows[0].IndexOf(cell => "зачёт".Equals(cell, StringComparison.OrdinalIgnoreCase));
            var ekzColIndex = sheetRows[0].IndexOf(cell => "экзамен".Equals(cell, StringComparison.OrdinalIgnoreCase));
            if (fioColIndex < 0) throw new Exception($"Не найден столбец `ФИО` на листе семестра {semesterNumber}");
            if (groupColIndex < 0) throw new Exception($"Не найден столбец `Группа` на листе семестра {semesterNumber}");
            if (zachColIndex < 0) throw new Exception($"Не найден столбец 'зачет' на листе семестра {semesterNumber}");
            if (ekzColIndex < 0) throw new Exception($"Не найден столбец 'экзамен' на листе семестра {semesterNumber}");
            var disciplines = sheetRows[1]
                .Select((value, colIndex) => (value, col: colIndex, exam:colIndex >= ekzColIndex))
                .Skip(zachColIndex)
                .TakeWhile(cell => !cell.value.IsOneOf("", "з", "э", "З", "Э"))
                .ToList();
            Console.WriteLine($"{semesterNumber} {sheetRows.Count} {disciplines.StrJoin("; ", d => d.value)}");
            for (int i = 2; i < sheetRows.Count; i++)
            {
                var row = sheetRows[i];
                if (row.Count <= fioColIndex) continue;
                var fio = row[fioColIndex];
                var group = row[groupColIndex];
                var fitContacts = contacts.Where(c => c.SameContact(fio) && c.GroupIndex == Contact.ExtractGroupIndex(group)).ToList();
                if (fitContacts.Count == 1)
                {
                    var contact = fitContacts.Single();
                    var semesters = studentSemesters.GetOrCreate(contact.Id, id => new List<SemesterMarks>());
                    while (semesters.Count < semesterNumber)
                        semesters.Add(new SemesterMarks {SemesterNumber = semesters.Count+1});
                    var semester = semesters[semesterNumber - 1];
                    foreach (var (disciplineName, colIndex, isExam) in disciplines)
                    {
                        var disciplineMark = new DisciplineMark(disciplineName);
                        if (colIndex >= row.Count) continue;
                        disciplineMark.ParseAndSetMark(isExam, row[colIndex]);
                        semester.Marks.Add(disciplineMark);
                    }
                }
            }
        }
    }
}
