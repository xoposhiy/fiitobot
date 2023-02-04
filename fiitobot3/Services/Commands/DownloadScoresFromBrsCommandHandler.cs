using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class DownloadScoresFromBrsCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly AbstractBrsClient brsClient;

        public DownloadScoresFromBrsCommandHandler(IPresenter presenter, AbstractBrsClient brsClient)
        {
            this.presenter = presenter;
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
            var defaultYearPart = DateTime.Now.Month < 5 ? 0 : 1;
            var defaultYear = DateTime.Now.Month < 9 ? DateTime.Now.Year-1 : DateTime.Now.Year;
            var yearPart = parts.Length > 3 ? int.Parse(parts[3]) : defaultYearPart;
            var studyYear = parts.Length > 4 ? int.Parse(parts[4]) : defaultYear;
            await presenter.Say("Загружаю данные из БРС — это может занять минутку другую. Напишу, как закончу...", fromChatId);
            var marks = await brsClient.GetTotalMarks(sessionId, studyYear, courseNumber, yearPart);
            var tsvFileContent = CreateTsv(marks);
            var yearPartName = new[]{ "весенний", "осенний" }[yearPart % 2];
            var filename = $"scores_{studyYear}-{yearPart}_{courseNumber}.csv";
            var caption = $"Успеваемость студентов ФИИТ {courseNumber} курса за {yearPartName} семестр {studyYear}/{studyYear+1} учебного года ({marks.Count} оценок)";
            await presenter.SendFile(tsvFileContent, filename, caption, fromChatId);
            await presenter.Say($"Done in {sw.ElapsedMilliseconds} ms", fromChatId);
        }

        private byte[] CreateTsv(List<BrsStudentMark> marks)
        {
            var tsv = new StringBuilder();
            tsv.AppendLine(string.Join("\t", "Группа", "ФИО", "Дисциплина", "Оценка", "Баллы"));
            foreach (var mark in marks)
            {
                tsv.AppendLine(
                    string.Join(
                        "\t",
                        mark.StudentGroup, mark.StudentFio, mark.ModuleTitle, mark.Mark, mark.Total));
            }
            return Encoding.UTF8.GetBytes(tsv.ToString());
        }
    }
}
