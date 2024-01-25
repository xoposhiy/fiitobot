using System.Threading.Tasks;

namespace fiitobot.Services
{
    public interface IContactDetailsRepo
    {
        Task<ContactDetails> GetById(long contactId);
        Task Save(ContactDetails details);
    }
}
