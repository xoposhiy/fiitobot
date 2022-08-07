using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface ITelegramFileDownloader
    {
        Task<byte[]> GetFileAsync(string fileId);
    }
}