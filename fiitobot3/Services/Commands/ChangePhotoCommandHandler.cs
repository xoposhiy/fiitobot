using System.IO;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
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

        public string Command => "/changephoto";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var photo = await photoRepository.TryGetPhotoForModeration(fromChatId);
            if (photo == null)
            {
                await presenter.SayUploadPhotoFirst(fromChatId);
                return;
            }
            await presenter.ShowPhotoForModeration(reviewerChatId, sender, new MemoryStream(photo));
            await presenter.SayPhotoGoesToModeration(fromChatId, new MemoryStream(photo));
        }
    }
}
