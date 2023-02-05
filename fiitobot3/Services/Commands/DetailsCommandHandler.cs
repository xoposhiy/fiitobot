using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class DetailsCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo contactDetailsRepo;
        private readonly IPresenter presenter;

        public DetailsCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo, IContactDetailsRepo contactDetailsRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
            this.contactDetailsRepo = contactDetailsRepo;
        }

        public string Command => "/details";
        public ContactType[] AllowedFor => new[] { ContactType.Staff, ContactType.Administration };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            await ShowDetails(text.Split(" ").Skip(1).StrJoin(" "), fromChatId);
        }

        private async Task ShowDetails(string query, long fromChatId)
        {
            var botData = botDataRepo.GetData();
            var contacts = botData.SearchContacts(query);
            if (contacts.Length == 1)
            {
                var contact = contacts[0];
                var details = await contactDetailsRepo.FindById(contact.Id);
                var contactWithDetails = new
                    ContactWithDetails(contact, details);
                await presenter.ShowDetails(contactWithDetails, fromChatId);
            }
            else
                await presenter.SayBeMoreSpecific(fromChatId);
        }
    }
}
