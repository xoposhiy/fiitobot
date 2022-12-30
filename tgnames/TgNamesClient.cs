using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using tgnames;

namespace tests
{

    public class TgNamesClient
    {
        private readonly string apiKey;
        private readonly Uri serviceUri;
        private readonly HttpClient client;

        public TgNamesClient(string apiKey, Uri serviceUri)
        {
            this.apiKey = apiKey;
            this.serviceUri = serviceUri;
            client = new HttpClient();
        }

        public TgNamesResponse Request(string username, long? tgId)
        {
            var tgNamesRequest = new TgNamesRequest
            {
                ApiKey = apiKey,
                Username = username,
                TgId = tgId
            };
            var response = client.PostAsync(serviceUri,
                new StringContent(JsonConvert.SerializeObject(tgNamesRequest))).Result;
            var content = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode)
                throw new Exception(content);
            return JsonConvert.DeserializeObject<TgNamesResponse>(content);
        }
    }
}
