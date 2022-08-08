using System.IO;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public class ChangePhotoCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;
        private readonly IPhotoRepository photoRepository;
        private readonly long reviewerChatId;

        public ChangePhotoCommandHandler(IPresenter presenter, IBotDataRepository repo, IPhotoRepository photoRepository, long reviewerChatId)
        {
            this.presenter = presenter;
            this.repo = repo;
            this.photoRepository = photoRepository;
            this.reviewerChatId = reviewerChatId;
        }

        public string[] Synonyms => new[] { "/changephoto" };
        public AccessRight[] AllowedFor => new[] { AccessRight.Admin, AccessRight.Staff, AccessRight.Student, };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var photo = await photoRepository.TryGetPhotoForModeration(fromChatId);
            var person = repo.GetData().FindPersonByTgId(fromChatId);
            if (person == null)
                return;
            if (photo == null)
            {
                await presenter.SayUploadPhotoFirst(fromChatId);
                return;
            }
            await presenter.ShowPhotoForModeration(reviewerChatId, person.Contact, new MemoryStream(photo));
            await presenter.SayPhotoGoesToModeration(fromChatId, new MemoryStream(photo));
        }
    }
}