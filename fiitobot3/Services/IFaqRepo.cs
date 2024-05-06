using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface IFaqRepo
    {
        Task<List<Faq>> GetFaqs();
        Dictionary<string, Faq> GetKeyword2Faqs(List<Faq> faqs);
    }
}
