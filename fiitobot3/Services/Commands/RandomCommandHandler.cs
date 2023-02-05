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

        private const double ShowSenderChance = 1.0 / 10.0;

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
            var contact = GetPresentedContact(sender);
            await presenter.ShowContact(contact, fromChatId, contact.GetDetailsLevelFor(sender));
        }

        private Contact GetPresentedContact(Contact sender)
        {
            if (ShouldShowSender())
                return sender;
            var students = botDataRepo.GetData().Students
                .Where(s => s.Status.IsOneOf("Активный", ""))
                .ToList();
            return students[random.Next(students.Count)];
        }

        private bool ShouldShowSender() => random.NextDouble() < ShowSenderChance;
    }
}
