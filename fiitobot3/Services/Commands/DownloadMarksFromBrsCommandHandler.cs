using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class DownloadMarksFromBrsCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepository;
        private readonly IContactDetailsRepo contactDetailsRepo;
        private readonly AbstractBrsClient brsClient;

        public DownloadMarksFromBrsCommandHandler(IPresenter presenter, IBotDataRepository botDataRepository, IContactDetailsRepo contactDetailsRepo, AbstractBrsClient brsClient)
        {
            this.presenter = presenter;
            this.botDataRepository = botDataRepository;
            this.contactDetailsRepo = contactDetailsRepo;
            this.brsClient = brsClient;
        }

        public string Command => "/scores";
        public ContactType[] AllowedFor => new[] { ContactType.Administration };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var parts = text.Split(" ");
            if (parts.Length < 2)
            {
                await presenter.Say("Usage /scores brs_jsessionId course_number [term_type [year]]", fromChatId);
                return;
            }
            var sw = Stopwatch.StartNew();
            var sessionId = parts[1];
            var courseNumber = int.Parse(parts[2]);
            var defaultYearPart = DateTime.Now.Month < 5 ? 1 : 2;
            var defaultYear = DateTime.Now.Month < 9 ? DateTime.Now.Year-1 : DateTime.Now.Year;
            var yearPart = parts.Length > 3 ? int.Parse(parts[3]) : defaultYearPart;
            var studyYear = parts.Length > 4 ? int.Parse(parts[4]) : defaultYear;
            var yearPartName = new[] { "весенний", "осенний" }[yearPart % 2];
            var semester = $"Курс {courseNumber}, {yearPartName} семестр, учебный год {studyYear}/{studyYear + 1}";

            await presenter.Say($"{semester}. Загружаю данные из БРС — это может занять минутку другую. Напишу, как закончу...", fromChatId);
            var marks = await brsClient.GetTotalMarks(sessionId, studyYear, courseNumber, yearPart);
            var tsvFileContent = CreateTsv(marks);
            var filename = $"scores_{studyYear}-{yearPart}_{courseNumber}.csv";
            var caption = $"Успеваемость студентов ФИИТ ({marks.Count} оценок). {semester}";
            await presenter.SendFile(fromChatId, tsvFileContent, filename, caption);
            await presenter.Say($"Done in {sw.ElapsedMilliseconds} ms. Now updating student details...", fromChatId);
            await UpdateStudentDetails(marks, studyYear, yearPart, courseNumber);
            await presenter.Say($"Done in {sw.ElapsedMilliseconds} ms", fromChatId);
        }

        private async Task UpdateStudentDetails(List<BrsStudentMark> marks, int year, int yearPart, int courseNumber)
        {
            var groups = marks.GroupBy(m => (m.StudentGroup, m.StudentFio));
            var data = botDataRepository.GetData();
            var studyYearBeginning = new DateTime(year, 09, 01);
            foreach (var studentGroup in groups)
            {
                var fio = studentGroup.Key.StudentFio;
                var group = studentGroup.Key.StudentGroup;
                var students = data.Students.Where(s =>
                        s.SameContact(fio)
                        && s.FormatOfficialGroup(studyYearBeginning) == group)
                    .ToList();
                foreach (var student in students)
                {
                    await UpdateStudentDetails(studentGroup, student, year, yearPart, courseNumber);
                }
            }
        }

        private async Task UpdateStudentDetails(IEnumerable<BrsStudentMark> brsMarks, Contact student, int year, int yearPart, int courseNumber)
        {
            var details = await contactDetailsRepo.GetById(student.Id);
            foreach (var mark in brsMarks)
                details.UpdateOrAddMark(mark, year, yearPart, courseNumber);
            details.Details.RemoveAll(c => c.Value.Trim() == "()");
            await contactDetailsRepo.Save(details);
        }

        private byte[] CreateTsv(List<BrsStudentMark> marks)
        {
            var tsv = new StringBuilder();
            tsv.AppendLine(string.Join("\t", "Группа", "ФИО", "Дисциплина", "Оценка", "Баллы", "Активный", "Контейнер"));
            foreach (var mark in marks)
            {
                tsv.AppendLine(
                    string.Join(
                        "\t",
                        mark.StudentGroup, mark.StudentFio, mark.ModuleTitle, mark.Mark, mark.Total, mark.StudentStatus, mark.ContainerName));
            }
            return Encoding.UTF8.GetBytes(tsv.ToString());
        }
    }
}
