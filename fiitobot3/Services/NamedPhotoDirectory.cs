using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public interface INamedPhotoDirectory
    {
        Task<PersonPhoto> FindPhoto(Contact contact);
    }

    public class NamedPhotoDirectory : INamedPhotoDirectory
    {
        private readonly Random random = new Random();
        private readonly string photoListUrl;

        public NamedPhotoDirectory(string photoListUrl)
        {
            this.photoListUrl = photoListUrl;
        }

        public async Task<PersonPhoto> FindPhoto(Contact contact)
        {
            var firstName = contact.FirstName;
            var lastName = contact.LastName;
            return await FindPhoto(lastName, firstName);
        }

        public async Task<PersonPhoto> FindPhoto(string lastName, string firstName)
        {
            using var client = new HttpClient();
            var requestUri = $"https://cloud-api.yandex.net/v1/disk/public/resources?public_key={UrlEncoder.Default.Encode(photoListUrl)}&fields=_embedded.items.name%2C_embedded.items.type%2C_embedded.items.preview&preview_size=800x1200&limit=5000";
            Console.WriteLine(requestUri);
            var json = await client.GetStringAsync(
                requestUri);
            
            var response = JsonConvert.DeserializeObject<YdResourcesResponse>(json);
            var people = response.Embedded.Items.Where(item =>
                item.Name != null && item.Type == "file" && item.Name.ContainsSameText(lastName) &&
                item.Name.ContainsSameText(firstName)).ToList();
            if (people.Count > 0)
            {
                var photo = people.SelectOne(new Random());
                return new PersonPhoto(new Uri(photoListUrl), new Uri(photo.Preview), photo.Name);
            }

            return null;
        }
    }

    public class PersonPhoto
    {
        public PersonPhoto(Uri photosDirectory, Uri photoUri, string name)
        {
            PhotosDirectory = photosDirectory;
            PhotoUri = photoUri;
            Name = name;
        }

        public readonly Uri PhotosDirectory;
        public readonly Uri PhotoUri;
        public readonly string Name;
    }

    public class YdResourcesEmbedded
    {
        [JsonProperty("items")]
        public List<YdResourcesItem> Items { get; set; }
    }

    public class YdResourcesItem
    {
        [JsonProperty("public_url")]
        public string PublicUrl { get; set; }

        [JsonProperty("public_key")]
        public string PublicKey { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }
    }

    public class YdResourcesResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("public_url")]
        public string PublicUrl { get; set; }

        [JsonProperty("_embedded")]
        public YdResourcesEmbedded Embedded { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }
}
