using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class UpdateStudentStatusesFromItsCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IUrfuStudentsDownloader studentsDownloader;
        private readonly IBotDataRepository repo;
        private readonly SheetContactsRepository sheetsRepo;

        public UpdateStudentStatusesFromItsCommandHandler(IPresenter presenter, IUrfuStudentsDownloader studentsDownloader, IBotDataRepository repo, SheetContactsRepository sheetsRepo)
        {
            this.presenter = presenter;
            this.studentsDownloader = studentsDownloader;
            this.repo = repo;
            this.sheetsRepo = sheetsRepo;
        }
        public string Command => "/its";
        public ContactType[] AllowedFor => new[] { ContactType.Administration, };
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
        {
            await presenter.Say($"Получаю рейтинги и статусы студентов из ИТС УрФУ...", fromChatId);
            var groups = repo.GetData().Students.Where(s => s.GroupIndex > 0)
                .Select(s => s.FormatOfficialGroup(DateTime.Now).Substring(0, 8)).Distinct();
            var students = new List<UrfuStudent>();
            foreach (var group in groups)
            {
                var groupStudents = await studentsDownloader.Download(group);
                students.AddRange(groupStudents);
            }
            await presenter.Say($"Загрузил {students.Count.Pluralize("студента|студента|студентов")}", fromChatId);
            var result = sheetsRepo.UpdateStudentsActivity(students);
            if (result.updatedStudents.Any())
            {
                await presenter.Say("Смотри, у этих студентов поменялись статусы!\n" + string.Join("\n", result.updatedStudents), fromChatId);
                await presenter.Say("Не забудь /reload", fromChatId);
            }

            if (result.newStudents.Any())
            {
                await presenter.Say("Ого, у нас есть новые активные студенты!\n" + string.Join("\n", result.newStudents), fromChatId);
                await presenter.Say("Их нужно вручную внести в таблицу с контактами: для вышедших из академ-опуска поменяйте года в его старой записи, а новеньких придется добавить в конец таблицы. После этого не забудь /reload", fromChatId);
            }
            await presenter.Say("Готово!", fromChatId);
        }
    }
}
