using AspNetCore.Yandex.ObjectStorage;
using AspNetCore.Yandex.ObjectStorage.Configuration;

namespace fiitobot.Services
{
    public static class FiitobotS3
    {
        public static YandexStorageService CreateYandexStorageService(this Settings settings)
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
}
