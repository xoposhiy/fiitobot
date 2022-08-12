using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class DetailsCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IPresenter presenter;

        public DetailsCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
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
            var contacts = botData.SearchPeople(query);
            if (contacts.Length == 1)
                await presenter.ShowDetails(contacts[0], botData.SourceSpreadsheets, fromChatId);
            else
                await presenter.SayBeMoreSpecific(fromChatId);
        }
    }
}
