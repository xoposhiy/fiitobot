using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class DemidovichCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly DemidovichService demidovichService;

        public DemidovichCommandHandler(IPresenter presenter, DemidovichService demidovichService)
        {
            this.presenter = presenter;
            this.demidovichService = demidovichService;
        }

        public string Command => "/demidovich";
        public ContactType[] AllowedFor => ContactTypes.All;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var exerciseNumber = text.Split(" ", 2)[1];
            var imageBytes = await demidovichService.TryGetImageBytes(exerciseNumber);
            if (imageBytes == null) return;
            await presenter.ShowDemidovichTask(imageBytes, exerciseNumber, fromChatId);
        }
    }
}
