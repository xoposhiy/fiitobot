using System.Threading.Tasks;

namespace fiitobot.Services
{
    public class StartCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;

        public StartCommandHandler(IPresenter presenter)
        {
            this.presenter = presenter;
        }
        public string[] Synonyms => new[] { "/start", "/help" };

        public AccessRight[] AllowedFor => new[]
            { AccessRight.Admin, AccessRight.External, AccessRight.Staff, AccessRight.Student, };
        public async Task HandlePlainText(string text, long fromChatId, AccessRight accessRight, bool silentOnNoResults = false)
        {
            await presenter.ShowHelp(fromChatId, accessRight);
        }
    }
}