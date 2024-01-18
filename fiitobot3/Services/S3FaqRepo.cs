using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Yandex.ObjectStorage;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class S3FaqRepo: IFaqRepo
    {
        private readonly YandexStorageService storageService;
        private readonly Settings settings;

        public async void Save()
        {
            var faqs = await GetFaqsFromGoogleDoc();
            if (faqs is null)
                return;
            var json = JsonConvert.SerializeObject(faqs);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var response = await storageService.PutObjectAsync(jsonBytes, "faq.json");
            if (!response.IsSuccessStatusCode)
                throw new Exception(response.Error);
        }

        public S3FaqRepo(YandexStorageService storageService, Settings settings)
        {
            this.storageService = storageService;
            this.settings = settings;
        }

        public async Task<List<Faq>> FindById()
        {
             var response = await storageService.TryGetAsync("faq.json");
             if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
             if (!response.IsSuccessStatusCode)
                throw new Exception(response.Error);
             var faqs = JsonConvert.DeserializeObject<List<Faq>>(response.Result);
             return faqs;
        }

        private async Task<List<Faq>> GetFaqsFromGoogleDoc()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            var url = $@"https://www.googleapis.com/drive/v3/files/1u0o3GvZKJhSQNkdKKSIrUllu2d5-XnWY6SDNviv7W7E/export?mimeType=text/plain&key={settings.GoogleApiKey}";
            var result = await client.GetStringAsync(url).ConfigureAwait(false);

            var separation = result.Split("###");
            var list = (from text in separation.Skip(1)
                select text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) into lines
                let question = lines[0]
                let keywords = lines[2].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(w => w.ToLower().Trim()).ToList()
                let answer = string.Join("\n", lines.Skip(4))
                select new Faq(keywords, question, answer)).ToList();
            return list;
        }

        public Dictionary<string, Faq> GetKeyword2Faqs(List<Faq> faqs)
        {
            var dict = new Dictionary<string, Faq>();
            foreach (var faq in faqs)
            {
                foreach (var keyword in faq.Keywords)
                {
                    dict[keyword] = faq;
                }
            }

            return dict;
        }
    }

    public class Faq
    {
        public readonly List<string> Keywords;
        public readonly string Question;
        public readonly string Answer;

        public Faq(List<string> keywords, string question, string answer)
        {
            Keywords = keywords;
            Question = question;
            Answer = answer;
        }
    }
}
