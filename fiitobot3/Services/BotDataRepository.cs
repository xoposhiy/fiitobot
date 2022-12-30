using System;
using System.IO;
using System.Text;
using System.Threading.Channels;
using AspNetCore.Yandex.ObjectStorage;
using AspNetCore.Yandex.ObjectStorage.Configuration;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class BotDataRepository : IBotDataRepository
    {
        private readonly Settings settings;
        private BotData botData;

        public BotDataRepository(Settings settings)
        {
            this.settings = settings;
        }

        public BotData GetData()
        {
            if (botData != null) return botData;
            var objectStorage = settings.CreateYandexStorageService();
            var res = objectStorage.GetAsByteArrayAsync("data.json").Result!;
            var s = Encoding.UTF8.GetString(res);
            return JsonConvert.DeserializeObject<BotData>(s);
        }

        public void Save(BotData newBotData)
        {
            var objectStorage = settings.CreateYandexStorageService();
            var s = JsonConvert.SerializeObject(newBotData);
            var data = Encoding.UTF8.GetBytes(s);
            var res = objectStorage.PutObjectAsync(data, "data.json").Result!;
            if (!res.IsSuccessStatusCode)
                throw new Exception(res.StatusCode + " " + res.Result);
            botData = newBotData;
        }
    }

    public interface IBotDataRepository
    {
        BotData GetData();
        void Save(BotData newBotData);
    }

    public class MemoryBotDataRepository : IBotDataRepository
    {
        private BotData botData;

        public MemoryBotDataRepository(BotData botData)
        {
            this.botData = botData;
        }

        public BotData GetData()
        {
            return botData;
        }

        public void Save(BotData newBotData)
        {
            botData = newBotData;
        }
    }
}
