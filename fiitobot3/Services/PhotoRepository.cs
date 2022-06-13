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
        Task<PersonPhoto> FindRandomPhoto(Contact contact);
    }

    public class PhotoRepository : IPhotoRepository
    {
        private readonly Random random = new Random();
        private readonly string photoListUrl;

        public PhotoRepository(string photoListUrl)
        {
            this.photoListUrl = photoListUrl;
        }

        public async Task<PersonPhoto> FindRandomPhoto(Contact contact)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync($"https://cloud-api.yandex.net/v1/disk/public/resources?public_key={UrlEncoder.Default.Encode(photoListUrl)}&fields=name%2Ctype%2Cpublic_url%2C_embedded.items.name%2C_embedded.items.type%2C_embedded.items.public_url&limit=10000");
            var response = JsonConvert.DeserializeObject<YdResourcesResponse>(json);
            var people = response.Embedded.Items;
            var personDirectories = people.Where(p => p.Name.ContainsSameText(contact.LastName) && p.Type == "dir").ToList();
            if (personDirectories.Count > 1)
                personDirectories = personDirectories.Where(d => d.Name.ContainsSameText(contact.FirstName)).ToList();
            if (personDirectories.Count == 1)
            {
                var dir = personDirectories[0];
                var photoDirUrl = dir.PublicUrl;
                if (photoDirUrl == null) return null;
                json = await client.GetStringAsync(
                    $"https://cloud-api.yandex.net/v1/disk/public/resources?public_key={UrlEncoder.Default.Encode(photoDirUrl)}&fields=_embedded.items.preview&preview_size=800x600");
                response = JsonConvert.DeserializeObject<YdResourcesResponse>(json);
                var photos = response.Embedded.Items;
                if (photos.Count == 0)
                    return null;
                var randomPhoto = photos.SelectOne(random);
                return new PersonPhoto(new Uri(photoDirUrl), new Uri(randomPhoto.Preview), dir.Name);
            }
            return null;
        }
    }

    public class PersonPhoto
    {
        public PersonPhoto(Uri photosDirectory, Uri randomPhoto, string dirName)
        {
            PhotosDirectory = photosDirectory;
            RandomPhoto = randomPhoto;
            DirName = dirName;
        }

        public readonly Uri PhotosDirectory;
        public readonly Uri RandomPhoto;
        public readonly string DirName;
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
