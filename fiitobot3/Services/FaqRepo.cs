using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface FaqRepo
    {
        Task<List<Faq>> FindById();
        Dictionary<string, Faq> GetKeyword2Faqs(List<Faq> faqs);
    }
}
