using System;
using System.Text;
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
            var objectStorage = CreateYandexStorageService();
            var res = objectStorage.GetAsByteArrayAsync("data.json").Result!;
            var s = Encoding.UTF8.GetString(res);
            return JsonConvert.DeserializeObject<BotData>(s);
        }

        public void Save(BotData newBotData)
        {
            var objectStorage = CreateYandexStorageService();
            var s = JsonConvert.SerializeObject(newBotData);
            var data = Encoding.UTF8.GetBytes(s);
            var res = objectStorage.PutObjectAsync(data, "data.json").Result!;
            if (!res.IsSuccessStatusCode)
                throw new Exception(res.StatusCode + " " + res.Result);
            botData = newBotData;
        }

        private YandexStorageService CreateYandexStorageService()
        {
            var objectStorage = new YandexStorageService(new YandexStorageOptions
            {
                AccessKey = settings.YandexCloudStaticKeyId,
                SecretKey = settings.YandexCloudStaticKey,
                BucketName = "fiitobot-storage",
                Endpoint = "storage.yandexcloud.net",
                Location = "ru-central1-a",
                Protocol = "https"
            });
            return objectStorage;
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
