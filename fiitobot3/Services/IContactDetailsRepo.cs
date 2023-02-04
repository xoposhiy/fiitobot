using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface IContactDetailsRepo
    {
        Task<ContactDetails> FindById(long contactId);
        Task Save(ContactDetails details);
    }
}
