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
            var parts = text.Split(" ", 2);
            if (parts.Length < 2)
            {
                await presenter.Say($"Укажите номер задачи, вот так: {Command} 123\n\nИли просто пришлите номер задачи без указания команды", fromChatId);
                return;
            }
            var exerciseNumber = parts[1];
            var imageBytes = await demidovichService.TryGetImageBytes(exerciseNumber);
            if (imageBytes == null) return;
            await presenter.ShowDemidovichTask(imageBytes, exerciseNumber, fromChatId);
        }
    }
}
