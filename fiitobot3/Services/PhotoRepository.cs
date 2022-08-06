using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public interface IPhotoRepository
    {
        Task<PersonPhoto> FindPhoto(Contact contact);
    }

    public class PhotoRepository : IPhotoRepository
    {
        private readonly Random random = new Random();
        private readonly string photoListUrl;

        public PhotoRepository(string photoListUrl)
        {
            this.photoListUrl = photoListUrl;
        }

        public async Task<PersonPhoto> FindPhoto(Contact contact)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(
                $"https://cloud-api.yandex.net/v1/disk/public/resources?public_key={UrlEncoder.Default.Encode(photoListUrl)}&fields=_embedded.items.name%2C_embedded.items.type%2C_embedded.items.preview&preview_size=800x1200&limit=5000");
            var response = JsonConvert.DeserializeObject<YdResourcesResponse>(json);
            var people = response.Embedded.Items.Where(item => item.Type == "file" && item.Name.ContainsSameText(contact.LastName)).ToList();
            if (people.Count > 1)
                people = people.Where(d => d.Name.ContainsSameText(contact.FirstName)).ToList();
            if (people.Count == 1)
            {
                var photo = people.Single();
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

        [JsonProperty("name")]
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

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
