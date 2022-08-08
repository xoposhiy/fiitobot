using System;
using System.IO;
using System.Threading.Tasks;
using AspNetCore.Yandex.ObjectStorage;
using AspNetCore.Yandex.ObjectStorage.Configuration;

namespace fiitobot.Services
{
    public interface IPhotoRepository
    {
        Task<byte[]> TryGetModeratedPhoto(long tgId);
        Task<byte[]> TryGetPhotoForModeration(long tgId);
        Task SetPhotoForModeration(long tgId, byte[] photo);
        Task<bool> RejectPhoto(long tgId);
        Task<bool> AcceptPhoto(long tgId);
    }

    public class S3PhotoRepository : IPhotoRepository
    {
        private readonly Settings settings;
        public S3PhotoRepository(Settings settings)
        {
            this.settings = settings;
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

        public async Task<byte[]> TryGetModeratedPhoto(long tgId)
        {
            var client = CreateYandexStorageService();
            try
            {
                return await client.GetAsByteArrayAsync($"moderated-{tgId}.jpeg");
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]> TryGetPhotoForModeration(long tgId)
        {
            var client = CreateYandexStorageService();
            try
            {
                return await client.GetAsByteArrayAsync($"for-moderation-{tgId}.jpeg");
            }
            catch
            {
                return null;
            }
        }

        public async Task SetPhotoForModeration(long tgId, byte[] photo)
        {
            var client = CreateYandexStorageService();
            await client.PutObjectAsync(photo, $"for-moderation-{tgId}.jpeg");
        }

        public async Task<bool> RejectPhoto(long tgId)
        {
            var client = CreateYandexStorageService();
            var response = await client.DeleteObjectAsync($"for-moderation-{tgId}.jpeg");
            return response.IsSuccess;
        }

        public async Task<bool> AcceptPhoto(long tgId)
        {
            var client = CreateYandexStorageService();
            var photo = await TryGetPhotoForModeration(tgId);
            if (photo == null)
            {
                return false;
            }

            await client.PutObjectAsync(photo, $"moderated-{tgId}.jpeg");
            await client.DeleteObjectAsync($"for-moderation-{tgId}.jpeg");
            return true;
        }
    }
}