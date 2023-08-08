using System.Net;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Yandex.ObjectStorage;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class S3ContactsDetailsRepo : IContactDetailsRepo
    {
        private readonly YandexStorageService storageService;

        public S3ContactsDetailsRepo(YandexStorageService storageService)
        {
            this.storageService = storageService;
        }

        public async Task<ContactDetails> FindById(long contactId)
        {
            var response = await storageService.TryGetAsync(GetFilename(contactId));
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new System.Exception(response.Error);
            var contactState = JsonConvert.DeserializeObject<ContactDetails>(response.Result);
            return contactState;
        }

        public async Task Save(ContactDetails details)
        {
            var json = JsonConvert.SerializeObject(details);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var response = await storageService.PutObjectAsync(jsonBytes, GetFilename(details.ContactId));
            if (!response.IsSuccessStatusCode)
                throw new System.Exception(response.Error);
        }

        private string GetFilename(long contactId)
        {
            return "contact_" + contactId + ".json";
        }
    }
}
