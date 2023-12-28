using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaContentCommandHandler : IChatCommandHandler
    {
        public string Command => "/spasibkaContent";
        public ContactType[] AllowedFor { get; }
        public Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
