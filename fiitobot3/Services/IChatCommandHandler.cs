using System.IO;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface IChatCommandHandler
    {
        string[] Synonyms { get; }
        AccessRight[] AllowedFor { get; }
        Task HandlePlainText(string text, long fromChatId, AccessRight accessRight, bool silentOnNoResults = false);
    }

    public class ChangePhotoCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;
        private readonly IS3PhotoRepository photoRepository;
        private readonly long reviewerChatId;

        public ChangePhotoCommandHandler(IPresenter presenter, IBotDataRepository repo, IS3PhotoRepository photoRepository, long reviewerChatId)
        {
            this.presenter = presenter;
            this.repo = repo;
            this.photoRepository = photoRepository;
            this.reviewerChatId = reviewerChatId;
        }

        public string[] Synonyms => new[] { "/changephoto" };
        public AccessRight[] AllowedFor => new[] { AccessRight.Admin, AccessRight.Staff, AccessRight.Student, };
        public async Task HandlePlainText(string text, long fromChatId, AccessRight accessRight, bool silentOnNoResults = false)
        {
            var photoStream = photoRepository.GetPhotoForModeration(fromChatId);
            var person = repo.GetData().FindPersonByTgId(fromChatId);
            if (person == null)
                return;
            if (photoStream == null)
            {
                await presenter.SayUploadPhotoFirst(fromChatId);
                return;
            }
            await presenter.ShowPhotoForModeration(reviewerChatId, person.Contact, photoStream);
            await presenter.SayPhotoGoesToModeration(fromChatId, photoStream);
        }
    }

    public interface IS3PhotoRepository
    {
        Stream GetModeratedPhoto(long tgId);
        Stream GetPhotoForModeration(long tgId);
    }
}