using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface IChatCommandHandler
    {
        string[] Synonyms { get; }
        AccessRight[] AllowedFor { get; }
        Task HandlePlainText(string text, long fromChatId, AccessRight accessRight, bool silentOnNoResults = false);
    }
}