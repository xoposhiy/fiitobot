using System;
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

        public string Command => "/setSpasibkaContent";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            try
            {
                var query = text.Split(" ").Skip(1).ToArray();
                var content = string.Join(' ', query);

                // var receiver = senderDetails.DialogState.Receiver;

                // senderDetails.DialogState.State = State.WaitingForApply;
                // presenter.ShowSpasibcaConfirmationMessage(sender, senderDetails, content, fromChatId);
                // senderDetails.DialogState.CommandHandlerName = content;

                await contactDetailsRepo.Save(senderDetails);
            }
            catch (Exception e)
            {
                senderDetails.DialogState = new DialogState();
                await contactDetailsRepo.Save(senderDetails);
                throw;
            }
        }
    }
}
