using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class JoinCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository repo;
        private readonly long reviewerChatId;

        public JoinCommandHandler(IPresenter presenter, IBotDataRepository repo, long reviewerChatId)
        {
            this.presenter = presenter;
            this.repo = repo;
            this.reviewerChatId = reviewerChatId;
        }

        public string Command => "/join";
        public ContactType[] AllowedFor => new[] { ContactType.External };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            if (sender.Type != ContactType.External)
            {
                await presenter.Say("Так у тебя же уже есть доступ!", fromChatId);
                return;
            }
            text = text.Replace("/join", "");
            await presenter.Say($"Кто-то ({sender}) хочет получить доступ боту. {text}\n\nЕсли это студент или преподаватель ФИИТ, добавьте его в таблицу контактов, выполните команду /reload в боте, после чего можно сообщить заявителю, что доступ появился.", reviewerChatId);
            await presenter.Say($"Модераторы получили ваш запрос. После того, как они убедятся, что вы действительно преподаватель или студент ФИИТ, они дадут доступ к этому боту. Осталось немного подождать!", fromChatId);
        }
    }
}
