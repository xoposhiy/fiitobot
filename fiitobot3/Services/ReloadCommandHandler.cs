using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public class ReloadCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IPresenter presenter;
        private readonly SheetContactsRepository contactsRepository;
        private readonly DetailsRepository detailsRepository;

        public ReloadCommandHandler(IPresenter presenter, SheetContactsRepository contactsRepository, DetailsRepository detailsRepository, IBotDataRepository botDataRepo)
        {
            this.botDataRepo = botDataRepo;
            this.presenter = presenter;
            this.contactsRepository = contactsRepository;
            this.detailsRepository = detailsRepository;
        }
        public string[] Synonyms => new[] { "/reload" };

        public AccessRight[] AllowedFor => new[]
            { AccessRight.Admin };
        public async Task HandlePlainText(string text, long fromChatId, AccessRight accessRight, bool silentOnNoResults = false)
        {
            await presenter.SayReloadStarted(fromChatId);
            ReloadDataFromSpreadsheets();
            await presenter.SayReloaded(botDataRepo.GetData().AllContacts.Count(), fromChatId);
        }
        private void ReloadDataFromSpreadsheets()
        {
            var contacts = contactsRepository.GetAllContacts();
            var students = detailsRepository.EnrichWithDetails(contacts);
            var botData = new BotData
            {
                Administrators = contactsRepository.GetAllAdmins(),
                Teachers = contactsRepository.GetAllTeachers(),
                SourceSpreadsheets = contactsRepository.GetOtherSpreadsheets(),
                Students = students
            };
            botDataRepo.Save(botData);
        }
    }
}