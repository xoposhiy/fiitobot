using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiitobot.Services
{
    public class ContactsCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IPresenter presenter;

        public ContactsCommandHandler(IBotDataRepository botDataRepo, IPresenter presenter)
        {
            this.botDataRepo = botDataRepo;
            this.presenter = presenter;
        }

        public string[] Synonyms => new[] { "/contacts" };
        public AccessRight[] AllowedFor => new[] { AccessRight.Admin, AccessRight.Staff, AccessRight.Student, };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var parts = text.Split("_");
            if (parts.Length == 1)
                await presenter.ShowDownloadContactsYearSelection(fromChatId);
            else if (parts.Length == 2)
            {
                var year = parts[1];
                await presenter.ShowDownloadContactsSuffixSelection(fromChatId, year);
            }
            else if (parts.Length == 3)
            {
                var year = parts[1];
                var suffix = parts[2];
                var contacts = botDataRepo.GetData().Students.Select(p => p.Contact).ToList();
                if (year != "all")
                    contacts.RemoveAll(c => c.AdmissionYear.ToString() != year);

                string GetNameWithSuffix(Contact c)
                {
                    if (suffix == "ftYY") return c.FirstName + " ôò" + (c.AdmissionYear % 100);
                    if (suffix == "ft") return c.FirstName + " ôò";
                    return c.FirstName;
                }
                string GetSecondNameWithSuffix(Contact c)
                {
                    if (suffix == "patronymic") return c.LastName + " ÔÒ" + (c.AdmissionYear % 100);
                    return c.LastName;
                }

                var headers = new[]{
                    "Name",
                    "Given Name",
                    "Additional Name",
                    "Family Name",
                    "Notes",
                    "E-mail 1 - Type",
                    "E-mail 1 - Value",
                    "IM 1 - Service",
                    "IM 1 - Value",
                    "Phone 1 - Type",
                    "Phone 1 - Value",
                    "Address 1 - City",
                    "Organization 1 - Type",
                    "Organization 1 - Name",
                    "Organization 2 - Type",
                    "Organization 2 - Name"};
                var rows = contacts.Select(c => new[]
                    {
                        GetNameWithSuffix(c) + " " + c.LastName,
                        GetNameWithSuffix(c),
                        suffix == "patronymic" ? c.Patronymic : "",
                        GetSecondNameWithSuffix(c),
                        c.Note,
                        "*",
                        c.Email,
                        "Telegram",
                        c.Telegram,
                        "Mobile",
                        c.Phone,
                        c.City,
                        "School",
                        c.School,
                        "University",
                        "ÔÈÈÒ ÓðÔÓ " + c.AdmissionYear
                    })
                    .Select(row => string.Join(",", row))
                    .ToList();

                var contentText = string.Join(",", headers) + "\n" + string.Join("\n", rows);
                var content = Encoding.UTF8.GetBytes(contentText);
                await presenter.SendContacts(fromChatId, content, "contacts_" + year + "_" + suffix + ".csv");
            }
        }
    }
}