using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiitobot.Services
{
    public class BrsClient : AbstractBrsClient
    {
        private readonly HttpClient httpClient;
        private string lastSessionId;
        private readonly CookieContainer cookieContainer;
        
        public BrsClient()
        {
            cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
            };
            httpClient = new HttpClient(handler);
        }

        public override async Task<List<BrsContainer>> GetContainers(string sessionId, int studyYear, int courseNumber,
            int yearPart)
        {
            AddSessionCookie(sessionId);
            var url = $"https://brs.urfu.ru/mrd/mvc/mobile/discipline/fetch?year={studyYear}&termType={yearPart}&course={courseNumber}&total=1000&page=1&pageSize=1000&search={UrlEncoder.Default.Encode("Специальный курс")}";
            var res = await httpClient.GetStringAsync(url);
            var ans = JsonConvert.DeserializeObject<JObject>(res);
            var content = ans["content"];
            return content.ToObject<List<BrsContainer>>();
        }

        private void AddSessionCookie(string sessionId)
        {
            if (sessionId == lastSessionId) return;
            cookieContainer.Add(new Cookie
            {
                Name = "JSESSIONID",
                Value = sessionId,
                Domain = "brs.urfu.ru",
                Path = "/mrd"
            });
            lastSessionId = sessionId;
        }

        public override async Task<List<BrsStudentMark>> GetTotalMarks(string sessionId, BrsContainer container)
        {
            AddSessionCookie(sessionId);
            var url = $"https://brs.urfu.ru/mrd/mvc/mobile/studentMarks/fetch?disciplineLoad={container.DisciplineLoad}&groupUuid={container.GroupHistoryId}&cardType=practice&hasTest=false&isTotal=true&intermediate=false&selectedTeachers=null&showActiveStudents=true";
            Console.WriteLine(url);
            var res = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<BrsStudentMark>>(res).Where(m => m.IsRealMark).ToList();
        }
    }
}
