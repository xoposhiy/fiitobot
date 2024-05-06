﻿using System;
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

        public async void UpdateBucketData()
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

        public async Task<List<Faq>> GetFaqs()
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
            var errorFormatting = "Formatting is broken";
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            var googleResponse = await client.GetStringAsync(settings.FaqGoogleDocUrl).ConfigureAwait(false);

            var googleFaqs = googleResponse.Split("###");
            if (googleFaqs.Length == 0)
                throw new Exception($"{errorFormatting}, faqs should be separated by ###");
            if (googleFaqs.Length < 2)
                throw new Exception("faqs not found");
            var resultFaqs = new List<Faq>();
            foreach (var faq in googleFaqs.Skip(1))
            {
                var lines = faq.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 5)
                    throw new Exception($"{errorFormatting}, faq must have format: Вопрос\nКлючевые слова:\nслово1, слово2\nОтвет:\nответ");
                var question = lines[0];
                var keywords = lines[2].Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.ToLower().Trim()).ToList();
                var answer = string.Join("\n", lines.Skip(4));
                resultFaqs.Add(new Faq(keywords, question, answer));
            }

            return resultFaqs;
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