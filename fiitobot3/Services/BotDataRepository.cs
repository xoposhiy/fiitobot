using System;
using System.Text;
using AspNetCore.Yandex.ObjectStorage;
using AspNetCore.Yandex.ObjectStorage.Configuration;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class BotDataRepository
    {
        private readonly Settings settings;

        public BotDataRepository(Settings settings)
        {
            this.settings = settings;
        }

        public BotData Load()
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
            var res = objectStorage.GetAsByteArrayAsync("data.json").Result!;
            var s = Encoding.UTF8.GetString(res);
            var botData = JsonConvert.DeserializeObject<BotData>(s);
            return botData;
        }

        public void Save(BotData botData)
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
            var s = JsonConvert.SerializeObject(botData);
            var data = Encoding.UTF8.GetBytes(s);
            var res = objectStorage.PutObjectAsync(data, "data.json").Result!;
            if (!res.IsSuccessStatusCode)
                throw new Exception(res.StatusCode + " " + res.Result);
        }
    }
}
