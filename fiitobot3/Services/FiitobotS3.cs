using AspNetCore.Yandex.ObjectStorage;
using AspNetCore.Yandex.ObjectStorage.Configuration;

namespace fiitobot.Services
{
    public static class FiitobotS3
    {
        public static YandexStorageService CreateFiitobotBucketService(this Settings settings)
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

        public static YandexStorageService CreateDemidovichBucketService(this Settings settings)
        {
            var objectStorage = new YandexStorageService(new YandexStorageOptions
            {
                AccessKey = settings.YandexCloudStaticKeyId,
                SecretKey = settings.YandexCloudStaticKey,
                BucketName = "demidovich-storage",
                Endpoint = "storage.yandexcloud.net",
                Location = "ru-central1-a",
                Protocol = "https"
            });
            return objectStorage;
        }

        public static YandexStorageService CreateFaqBucketService(this Settings settings)
        {
            var objectStorage = new YandexStorageService(new YandexStorageOptions
            {
                AccessKey = "YCAJEmPJoNy6oMO7U3LRFK4Uc",
                SecretKey = "YCOpvS20fis42WY2yCv1_Q6HzNRxq8Qkp7sBsycH",
                BucketName = "faq-storage",
                Endpoint = "storage.yandexcloud.net",
                Location = "ru-central1-a",
                Protocol = "https"
            });
            return objectStorage;
        }
    }
}
