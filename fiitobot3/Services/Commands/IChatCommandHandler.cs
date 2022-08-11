using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using fiitobot.GoogleSpreadsheet;
using WTelegram;

namespace fiitobot.Services.Commands
{
    public interface IChatCommandHandler
    {
        string[] Synonyms { get; }
        AccessRight[] AllowedFor { get; }
        Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false);
    }
}