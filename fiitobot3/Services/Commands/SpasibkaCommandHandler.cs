using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaCommandHandler : IChatCommandHandler // self recovery: при ошибке скидывать в начальный стейт
    {
        public string Command => "/spasibka";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
