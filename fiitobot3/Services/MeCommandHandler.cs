using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public class MeCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataHolder;
        private readonly IPresenter presenter;

        public MeCommandHandler(IBotDataRepository botDataHolder, IPresenter presenter)
        {
            this.botDataHolder = botDataHolder;
            this.presenter = presenter;
        }
        public string[] Synonyms => new []{"/me"};
        public AccessRight[] AllowedFor => new[] { AccessRight.Admin, AccessRight.Staff, AccessRight.Student, };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var contact = botDataHolder.GetData().AllContacts.FirstOrDefault(p => p.Contact.TgId == fromChatId);
            if (contact != null)
            {
                await SayCompliment(contact.Contact, fromChatId);
                await presenter.ShowContact(contact.Contact, fromChatId, contact.Contact.GetDetailsLevelFor(sender));
            }
        }

        private async Task SayCompliment(Contact contact, long fromChatId)
        {
            if (contact.Patronymic.EndsWith("вна"))
                await presenter.Say("Ты прекрасна, спору нет! ❤", fromChatId);
            else
                await presenter.Say("Ты прекрасен, спору нет! ✨", fromChatId);
        }

    }
}