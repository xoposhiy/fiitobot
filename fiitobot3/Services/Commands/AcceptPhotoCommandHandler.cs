using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class RejectPhotoCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;
        private readonly IPhotoRepository photoRepository;
        private readonly IContactDetailsRepo detailsRepo;
        private readonly long reviewerChatId;

        public RejectPhotoCommandHandler(IPresenter presenter, IBotDataRepository repo, IPhotoRepository photoRepository, long reviewerChatId, IContactDetailsRepo detailsRepo)
        {
            this.presenter = presenter;
            this.repo = repo;
            this.photoRepository = photoRepository;
            this.reviewerChatId = reviewerChatId;
            this.detailsRepo = detailsRepo;
        }

        public string Command => "/reject_photo";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
        {
            if (fromChatId != reviewerChatId) return;
            var parts = text.Split(" ");
            if (parts.Length != 2) return;
            if (!long.TryParse(parts[1], out var contactId)) return;
            var person = repo.GetData().AllContacts.FirstOrDefault(c => c.Id == contactId);
            if (person == null) return;
            person.UpdateFromDetails(await detailsRepo.GetById(contactId));
            var success = await photoRepository.RejectPhoto(person.TgId);
            if (success)
            {
                await presenter.SayPhotoRejected(person, sender.Contact, fromChatId);
                await presenter.SayPhotoRejected(person, null, person.TgId);
            }
        }
    }

    public class AcceptPhotoCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;
        private readonly IContactDetailsRepo detailsRepo;
        private readonly IPhotoRepository photoRepository;
        private readonly long reviewerChatId;

        public AcceptPhotoCommandHandler(IPresenter presenter, IBotDataRepository repo, IContactDetailsRepo detailsRepo, IPhotoRepository photoRepository, long reviewerChatId)
        {
            this.presenter = presenter;
            this.repo = repo;
            this.detailsRepo = detailsRepo;
            this.photoRepository = photoRepository;
            this.reviewerChatId = reviewerChatId;
        }

        public string Command => "/accept_photo";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
        {
            if (fromChatId != reviewerChatId) return;
            var parts = text.Split(" ");
            if (parts.Length != 2)
            {
                await presenter.Say("Неверная команда", fromChatId);
                return;
            }

            if (!long.TryParse(parts[1], out var contactId))
            {
                await presenter.Say("Неверная команда", fromChatId);
                return;
            }
            var person = repo.GetData().AllContacts.FirstOrDefault(c => c.Id == contactId);
            if (person == null)
            {
                await presenter.Say("Не найден контакт с Id = " + contactId, fromChatId);
                return;
            }
            person.UpdateFromDetails(await detailsRepo.GetById(contactId));
            var success = await photoRepository.AcceptPhoto(person.TgId);
            if (success)
            {
                await presenter.SayPhotoAccepted(person, sender.Contact, fromChatId);
                await presenter.SayPhotoAccepted(person, null, person.TgId);
            }
            else
            {
                await presenter.Say($"Что-то пошло не так... TgId = {person.TgId}", fromChatId);
            }
        }
    }
}
