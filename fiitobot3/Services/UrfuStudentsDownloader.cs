using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CsQuery;
using Newtonsoft.Json;

namespace fiitobot.Services
{
    public interface IUrfuStudentsDownloader
    {
        Task<UrfuStudent[]> Download(string academicGroup);
    }

    public class UrfuStudent
    {
        public string GroupName;
        public string Id;
        public string Name;
        public double? Rating;
        public string Status;
        public override string ToString()
        {
            return $"{GroupName} {Name} {Status} {Rating}";
        }
    }

    public class UrfuStudentsDownloader : IUrfuStudentsDownloader
    {
        private class UrfuStudentsData
        {
            public UrfuStudent[] Data;
        }

        private class FilterItem
        {
            public readonly string property;
            public readonly string value;

            public FilterItem(string property, string value)
            {
                this.property = property;
                this.value = value;
            }
        }
        private readonly HttpClient client;
        private readonly HttpClientHandler messageHandler;
        private readonly Settings settings;

        public UrfuStudentsDownloader(Settings settings)
        {
            this.settings = settings;
            messageHandler = new HttpClientHandler();
            client = new HttpClient(messageHandler);
        }

        public async Task<UrfuStudent[]> Download(string academicGroup)
        {
            if (messageHandler.CookieContainer.Count == 0)
                await Login();
            var filter = JsonConvert.SerializeObject(new[]
            {
                new FilterItem("name", ""), new FilterItem("status", ""), new FilterItem("groupName", academicGroup)
            });
            var requestUri = $"https://its.urfu.ru/Students?_dc=1660072439988&page=1&start=0&limit=300&filter={HttpUtility.UrlEncode(filter)}";
            client.DefaultRequestHeaders.Remove("X-Requested-With");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            var res = await client.GetStringAsync(requestUri);
            try
            {
                var data = JsonConvert.DeserializeObject<UrfuStudentsData>(res);
                return data.Data;
            }
            catch
            {
                Console.WriteLine(res);
                throw;
            }
        }

        public async Task Login()
        {
            var html = await client.GetStringAsync("https://its.urfu.ru/Account/Login");
            var doc = CQ.Create(html);
            var csrfToken = doc["input[Name='__RequestVerificationToken']"].Attr("value");
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", csrfToken),
                new KeyValuePair<string, string>("UserName", settings.ItsLogin),
                new KeyValuePair<string, string>("Password", settings.ItsPassword),
                new KeyValuePair<string, string>("RememberMe", "false")
            });
            var response = await client.PostAsync("https://its.urfu.ru/Account/Login", requestContent);
            if (!response.IsSuccessStatusCode)
                throw new Exception(response.StatusCode + " " + await response.Content.ReadAsStringAsync());
        }
    }
}
