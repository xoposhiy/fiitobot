using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;

namespace fiitobot.Services
{
    public class TelegramFileDownloader : ITelegramFileDownloader
    {
        private readonly ITelegramBotClient botClient;

        public TelegramFileDownloader(ITelegramBotClient botClient)
        {
            this.botClient = botClient;
        }

        public async Task<byte[]> GetFileAsync(string fileId)
        {
            var file = await botClient.GetFileAsync(fileId);
            var memoryStream = new MemoryStream();
            await botClient.DownloadFileAsync(file.FilePath!, memoryStream);
            return memoryStream.ToArray();
        }
    }
}