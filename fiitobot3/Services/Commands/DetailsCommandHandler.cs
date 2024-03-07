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
        public ContactType[] AllowedFor => new[] { ContactType.Staff, ContactType.Administration, ContactType.Student };
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
        {
            var query = text.Split(" ").Skip(1).StrJoin(" ");
            await ShowDetails(query, sender, fromChatId);
        }

        private async Task ShowDetails(string query, Contact sender, long fromChatId)
        {
            var botData = botDataRepo.GetData();
            var contacts = string.IsNullOrWhiteSpace(query) ? new[]{sender} : botData.SearchContacts(query);
            if (contacts.Length == 1)
            {
                var contact = contacts[0];
                if (sender.Type == ContactType.Student && contact.TgId != fromChatId)
                {
                    await presenter.SayNoRights(fromChatId, ContactType.Student);
                    return;
                }
                var details = await contactDetailsRepo.GetById(contact.Id);
                var contactWithDetails = new
                    ContactWithDetails(contact, details);
                await presenter.ShowDetails(contactWithDetails, fromChatId);
            }
            else
                await presenter.SayBeMoreSpecific(fromChatId);
        }
    }
}
