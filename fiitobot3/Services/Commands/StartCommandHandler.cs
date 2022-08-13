using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class StartCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;

        public StartCommandHandler(IPresenter presenter, IBotDataRepository repo)
        {
            this.presenter = presenter;
            this.repo = repo;
        }
        public virtual string Command => "/start";

        public ContactType[] AllowedFor => ContactTypes.All;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            if (text == "/start")
            {
                await presenter.ShowHelp(fromChatId, sender.Type);
                return;
            }
            var rest = text.Split(new[] { ' ' }, 2)[1].Trim();
            if (long.TryParse(rest, out var tgId))
            {
                var contact = repo.GetData().FindPersonByTgId(tgId)?.Contact;
                if (contact == null) return;
                await presenter.ShowContact(contact, fromChatId, contact.GetDetailsLevelFor(sender));
            }
            {
                var contact = repo.GetData().FindPersonByTelegramName(rest)?.Contact;
                if (contact == null) return;
                await presenter.ShowContact(contact, fromChatId, contact.GetDetailsLevelFor(sender));
            }

        }
    }
}
