using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public class TellToContactCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;

        public TellToContactCommandHandler(IPresenter presenter, IBotDataRepository repo)
        {
            this.presenter = presenter;
            this.repo = repo;
        }

        public string[] Synonyms => new[] { "/tell" };
        public AccessRight[] AllowedFor => new[] { AccessRight.Admin, };
        public async Task HandlePlainText(string text, long fromChatId, Contact contact, bool silentOnNoResults = false)
        {
            var args = text.Split(new []{' '}, 3);
            var toWhom = args[1];
            var candidates = repo.GetData().Students.Where(s =>
                    toWhom == s.Contact.Telegram || toWhom == s.Contact.Telegram.Trim('@') || toWhom == s.Contact.TgId.ToString() || toWhom == s.Contact.LastName)
                .ToList();
            if (candidates.Count > 1)
                await presenter.Say("Не понял кому... Слишком много кандидатов", fromChatId);
            else if (candidates.Count == 0)
                await presenter.Say("Не понял кому... Никого не нашел", fromChatId);
            else
            {
                var message = args[2];
                await presenter.Say(message, candidates.Single().Contact.TgId);
            }
        }
    }
}