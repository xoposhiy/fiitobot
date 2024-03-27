using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yandex.Cloud.Functions;

namespace tgnames
{
    public class Function : YcFunction<string, YandexFunctionResponse>
    {
        public YandexFunctionResponse FunctionHandler(string request, Context context)
        {
            var accessToken = JsonConvert.DeserializeObject<JObject>(context.TokenJson).GetValue("access_token")!.Value<string>();
            var settings = new Settings
            {
                AccessToken = accessToken
            };
            var req = JObject.Parse(request);
            var body = req.GetValue("body")!.Value<string>();
            var isBodyBase64 = req.GetValue("isBase64Encoded")!.Value<bool>();
            if (isBodyBase64)
                body = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(body));
            var tgNamesRequest = JsonConvert.DeserializeObject<TgNamesRequest>(body);
            if (!settings.ApiKeys.ContainsKey(tgNamesRequest.ApiKey))
                return new YandexFunctionResponse(403, "Unknown Api Key");
            var res = HandleRequest(tgNamesRequest, settings).Result;
            return new YandexFunctionResponse(200, JsonConvert.SerializeObject(res));
        }

        private async Task<TgNamesResponse> HandleRequest(TgNamesRequest request, Settings settings)
        {
            var repo = new NamesRepo(settings);

            var hasUsername = !string.IsNullOrWhiteSpace(request.Username);
            var hasTgId = request.TgId.HasValue;
            if (hasUsername && hasTgId)
                return FormatResponse(request, await repo.Save(request.TgId.Value, request.Username));
            if (hasUsername)
                return FormatResponse(request, await repo.SearchByUsername(request.Username));
            if (hasTgId)
                return FormatResponse(request, await repo.SearchByTgId(request.TgId.Value));
            return new TgNamesResponse()
            {
                TgId = request.TgId,
                Username = request.Username,
                Found = false,
                ErrorMessage = "Username or TgId must be specified",
                LastUpdateTimestamp = DateTime.MinValue
            };
        }

        private TgNamesResponse FormatResponse(TgNamesRequest request, UserEntry entry)
        {
            if (entry == null)
            {
                return new TgNamesResponse
                {
                    TgId = request.TgId,
                    Username = request.Username,
                    Found = false,
                    ErrorMessage = null,
                    LastUpdateTimestamp = DateTime.MinValue
                };
            }
            return new TgNamesResponse
            {
                TgId = entry.Id,
                Username = entry.Username,
                Found = true,
                ErrorMessage = null,
                LastUpdateTimestamp = entry.LastUpdate
            };
        }
    }
}
