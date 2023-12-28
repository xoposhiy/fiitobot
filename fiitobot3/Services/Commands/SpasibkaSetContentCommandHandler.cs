using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaSetContentCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo contactDetailsRepo;

        public SpasibkaSetContentCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo,
            IContactDetailsRepo contactDetailsRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
            this.contactDetailsRepo = contactDetailsRepo;
        }

        public string Command => "/SetSpasibkaContent";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var query = text.Split(" ").Skip(0).ToArray();
            var content = string.Join(' ', query);

            var senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            var receiver = senderDetails.DialogState.Receiver;

            senderDetails.DialogState.State = State.WaitingForApply;
            await presenter.Say($"Текст спасибки: {content}", fromChatId);
            senderDetails.DialogState.State = State.WaitingForApply;
            senderDetails.DialogState.CommandHandlerName = content;
            await contactDetailsRepo.Save(senderDetails);
        }
    }
}
