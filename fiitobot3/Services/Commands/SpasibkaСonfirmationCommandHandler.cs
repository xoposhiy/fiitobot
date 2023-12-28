using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaСonfirmationCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo contactDetailsRepo;

        public SpasibkaСonfirmationCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo,
            IContactDetailsRepo contactDetailsRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
            this.contactDetailsRepo = contactDetailsRepo;
        }

        public string Command => "/spasibkaСonfirmation";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
