using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiitobot.Services
{
    public class BrsClient : AbstractBrsClient
    {
        private readonly Predicate<string> officialGroupPredicate;
        private readonly HttpClient httpClient;
        private string lastSessionId;
        private readonly CookieContainer cookieContainer;
        
        public BrsClient(Predicate<string> officialGroupPredicate)
        {
            this.officialGroupPredicate = officialGroupPredicate;
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
            var amountString = await httpClient.GetStringAsync($"https://brs.urfu.ru/mrd/mvc/mobile/discipline/amount?year={studyYear}&termType={yearPart}&course={courseNumber}");
            var url = $"https://brs.urfu.ru/mrd/mvc/mobile/discipline/fetch?year={studyYear}&termType={yearPart}&course={courseNumber}&total={amountString}&page=1&pageSize=1000&search=";
            var res = await httpClient.GetStringAsync(url);
            var ans = TryDeserialize(res);
            var content = ans["content"];
            var brsContainers = content.ToObject<List<BrsContainer>>()!;
            brsContainers = brsContainers.Where(c => officialGroupPredicate(c.Group)).ToList();
            return brsContainers;
        }

        private static JObject TryDeserialize(string res)
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(res);
            }
            catch
            {
                throw new FormatException(res);
            }
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
            var url = $"https://brs.urfu.ru/mrd/mvc/mobile/studentMarks/fetch?disciplineLoad={container.DisciplineLoad}&groupUuid={container.GroupHistoryId}&cardType=practice&hasTest=false&isTotal=true&intermediate=false&selectedTeachers=null&showActiveStudents=false";
            var res = await httpClient.GetStringAsync(url);
            var marks = JsonConvert.DeserializeObject<List<BrsStudentMark>>(res).Where(m => m.IsRealMark).ToList();
            foreach (var brsStudentMark in marks)
            {
                if (string.IsNullOrWhiteSpace(brsStudentMark.ModuleTitle))
                    brsStudentMark.ModuleTitle = container.Discipline;
                if (container.Discipline.StartsWith("Специальный курс"))
                    brsStudentMark.ContainerName = "ск" + container.Discipline.Split("№").Last();
            }
            return marks;
        }

        public static bool IsFiitOfficialGroup(string officialGroup)
        {
            //МЕН-490801
            //      ↑↑
            //0123456789
            return officialGroup.Substring(6, 2) == "08";
        }
    }
}
