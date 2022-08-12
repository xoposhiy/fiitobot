using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class RandomCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IPresenter presenter;
        private readonly Random random;

        public RandomCommandHandler(IBotDataRepository botDataRepo, IPresenter presenter, Random random)
        {
            this.botDataRepo = botDataRepo;
            this.presenter = presenter;
            this.random = random;
        }

        public string Command => "/random";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var students = botDataRepo.GetData().Students.Where(s => s.Contact.Status.IsOneOf("Активный", "")).ToList();
            var contact = students[random.Next(students.Count)].Contact;
            await presenter.ShowContact(contact, fromChatId, contact.GetDetailsLevelFor(sender));
        }
    }
}
