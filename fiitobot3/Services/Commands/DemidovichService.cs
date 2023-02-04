using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Yandex.ObjectStorage;
using static System.Net.Mime.MediaTypeNames;

namespace fiitobot.Services.Commands
{
    public class DemidovichService
    {
        private readonly YandexStorageService storage;

        public DemidovichService(YandexStorageService storage)
        {
            this.storage = storage;
        }

        public async Task<bool> HasImage(string exerciseNumber)
        {
            if (!exerciseNumber.All(c => char.IsDigit(c) || c == '.'))
                return false;
            var response = await storage.TryGetAsync(GetFilename(exerciseNumber));
            return response.IsSuccessStatusCode;
        }

        public async Task<byte[]> TryGetImageBytes(string exerciseNumber)
        {
            if (!exerciseNumber.All(c => char.IsDigit(c) || c == '.'))
                return null;
            try
            {
                return await storage.GetAsByteArrayAsync(GetFilename(exerciseNumber));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetFilename(string exerciseNumber)
        {
            return exerciseNumber + ".gif";
        }
    }
}
