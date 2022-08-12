using fiitobot.Services.Commands;
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
        public virtual string Command => "/start";

        public ContactType[] AllowedFor => ContactTypes.All;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            await presenter.ShowHelp(fromChatId, sender?.Type ?? ContactType.External);
        }
    }

    public class HelpCommandHandler : StartCommandHandler
    {
        public HelpCommandHandler(IPresenter presenter) : base(presenter)
        {
        }
        public override string Command => "/help";
    }
}
