using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class DownloadMarksFromSpreadsheetsCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly MarksReloadService reloadService;

        public DownloadMarksFromSpreadsheetsCommandHandler(IPresenter presenter, MarksReloadService reloadService)
        {
            this.presenter = presenter;
            this.reloadService = reloadService;
        }

        public string Command => "/gdoc_scores";
        public ContactType[] AllowedFor => new[] { ContactType.Administration };
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
        {
            var parts = text.Split(" ");
            if (parts.Length < 2)
            {
                await presenter.Say("Укажите URL гугл-таблицы с оценками вот так: /gdoc_scores [spreadsheet url]\n\nВ таблице должны быть листы вида '1 семестр', " +
                                    "в которых первые две строки − заголовки, в первой должны быть 'зачет' и 'экзамен', " +
                                    "а во второй − ФИО, Группа и, собственно, название предметов." +
                                    "После заголовка последнего предмета должна быть пустая ячейка", fromChatId);
                return;
            }

            var url = parts[1];
            await presenter.SayReloadStarted(fromChatId);
            var res = await reloadService.ReloadFrom(url);
            await presenter.Say($"Нашел такие листы с оценками: {res.ProcessedSheets.StrJoin(", ")}\nОбновил {res.UpdatedStudentsCount.Pluralize("студента|студента|студентов")}", fromChatId);
        }
    }
}
