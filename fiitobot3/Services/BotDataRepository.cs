using System;
using System.Collections.Generic;
using System.Text;
using AspNetCore.Yandex.ObjectStorage;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class BotDataRepository : IBotDataRepository
    {
        private BotData botData;
        private readonly Lazy<YandexStorageService> objectStorage;

        public BotDataRepository(Settings settings)
        {
            objectStorage = new Lazy<YandexStorageService>(settings.CreateFiitobotBucketService());
        }

        public BotData GetData()
        {
            if (botData != null) return botData;
            var res = objectStorage.Value.GetAsByteArrayAsync("data.json").Result!;
            var s = Encoding.UTF8.GetString(res);
            return JsonConvert.DeserializeObject<BotData>(s);
        }

        public void Save(BotData newBotData)
        {
            var s = JsonConvert.SerializeObject(newBotData);
            var data = Encoding.UTF8.GetBytes(s);
            var res = objectStorage.Value.PutObjectAsync(data, "data.json").Result!;
            if (!res.IsSuccessStatusCode)
                throw new Exception(res.StatusCode + " " + res.Result);
            botData = newBotData;
        }

        public ContactWithDetails GetDetails(Contact contact)
        {
            return new ContactWithDetails(contact);
        }

        public void SaveDetails(Contact contact, IReadOnlyCollection<ContactDetail> details)
        {
        }
    }

    public interface IBotDataRepository
    {
        BotData GetData();
        void Save(BotData newBotData);
        ContactWithDetails GetDetails(Contact contact);
        void SaveDetails(Contact contact, IReadOnlyCollection<ContactDetail> details);
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

        public ContactWithDetails GetDetails(Contact contact)
        {
            return new ContactWithDetails(contact);
        }

        public void SaveDetails(Contact contact, IReadOnlyCollection<ContactDetail> details)
        {
        }
    }
}
