using System.Collections.Generic;
using System.Threading.Tasks;
using fiitobot.Services.Commands;
using Google.Apis.Docs.v1.Data;

namespace fiitobot.Services
{
    public interface IFAQRepo
    {
        Task<List<Faq>> FindById();
        void Save(object obj);
        Dictionary<string, Faq> GetKeyword2Faqs(List<Faq> faqs);
    }
}
