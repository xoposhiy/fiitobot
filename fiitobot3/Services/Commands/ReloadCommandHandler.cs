using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class ReloadCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IPresenter presenter;
        private readonly SheetContactsRepository contactsRepository;

        public ReloadCommandHandler(IPresenter presenter, SheetContactsRepository contactsRepository, IBotDataRepository botDataRepo)
        {
            this.botDataRepo = botDataRepo;
            this.presenter = presenter;
            this.contactsRepository = contactsRepository;
        }
        public string Command => "/reload";

        public ContactType[] AllowedFor => new[] { ContactType.Administration };
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
        {
            await presenter.SayReloadStarted(fromChatId);
            ReloadContactsFromSpreadsheet();
            await presenter.SayReloaded(botDataRepo.GetData(), fromChatId);
        }

        private void ReloadContactsFromSpreadsheet()
        {
            contactsRepository.Reload();
            var contacts = contactsRepository.GetStudents().ToArray();
            var botData = new BotData
            {
                Administrators = contactsRepository.GetAdmins().ToArray(),
                Teachers = contactsRepository.GetTeachers().ToArray(),
                Students = contacts
            };
            botDataRepo.Save(botData);
        }
    }
}
