using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class HelpCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;

        public HelpCommandHandler(IPresenter presenter)
        {
            this.presenter = presenter;
        }
        public virtual string Command => "/help";

        public ContactType[] AllowedFor => ContactTypes.All;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            await presenter.ShowHelp(fromChatId, sender.Type);
        }
    }
}
