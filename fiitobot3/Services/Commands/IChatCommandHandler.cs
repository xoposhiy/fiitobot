using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public interface IChatCommandHandler
    {
        string Command { get; }
        ContactType[] AllowedFor { get; }

        //TODO Перейти на ContactWithDetail sender
        Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false);
    }
}
