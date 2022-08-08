using System;
using System.Threading.Tasks;

namespace fiitobot.Services
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

        public string[] Synonyms => new[] { "/random" };
        public AccessRight[] AllowedFor => new[] { AccessRight.Admin, AccessRight.Staff, AccessRight.Student, };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var students = botDataRepo.GetData().Students;
            var contact = students[random.Next(students.Length)].Contact;
            await presenter.ShowContact(contact, fromChatId, contact.GetDetailsLevelFor(sender));
        }
    }
}