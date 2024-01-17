using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Yandex.ObjectStorage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class S3FaqRepo: IFAQRepo
    {
        private readonly YandexStorageService storageService;
        private ILogger logger;

        private Timer _timer;

        public void StartUploading()
        {
            _timer = new Timer(Save, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public S3FaqRepo(YandexStorageService storageService)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            logger = factory.CreateLogger("faq service");
            this.storageService = storageService;
        }

        public async Task<List<Faq>> FindById()
        {
             var response = await storageService.TryGetAsync("file.json");
             if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
             if (!response.IsSuccessStatusCode)
                throw new Exception(response.Error);
             var faqs = JsonConvert.DeserializeObject<List<Faq>>(response.Result);
             return faqs;
        }

        public async void Save(object obj)
        {
            logger.LogInformation("SAAAVEEE");
            var faqs = await GetFaqsFromGoogleDoc();
            var json = JsonConvert.SerializeObject(faqs);
            logger.LogInformation(json);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var response = await storageService.PutObjectAsync(jsonBytes, "file.json");
            if (!response.IsSuccessStatusCode)
                throw new Exception(response.Error);
        }

        private static async Task<List<Faq>> GetFaqsFromGoogleDoc()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            var url = $@"https://www.googleapis.com/drive/v3/files/1u0o3GvZKJhSQNkdKKSIrUllu2d5-XnWY6SDNviv7W7E/export?mimeType=text/plain&key=AIzaSyBeZdXpOaL9tQwUwm0YjbHrTM9b4Nndan8";
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
                    logger.LogInformation(keyword);
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
