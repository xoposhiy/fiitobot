using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public abstract class AbstractBrsClient
    {
        public abstract Task<List<BrsContainer>> GetContainers(string sessionId, int studyYear, int courseNumber, int yearPart);
        public abstract Task<List<BrsStudentMark>> GetTotalMarks(string sessionId, BrsContainer container);

        public async Task<List<BrsStudentMark>> GetTotalMarks(string sessionId, int studyYear, int courseNumber, int yearPart)
        {
            var containers = await GetContainers(sessionId, studyYear, courseNumber, yearPart);
            var ans = new List<BrsStudentMark>();
            foreach (var container in containers)
                ans.AddRange(await GetTotalMarks(sessionId, container));
            return ans.OrderBy(x => x.StudentGroup).ThenBy(x => x.StudentFio).ThenBy(x => x.Total).ToList();
        }
    }
}
