using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ydb.Sdk.Value.ResultSet;

namespace fiitobot.Services.Commands
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

        public string Command => "/contacts";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails sender, bool silentOnNoResults = false)
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
                var contacts = botDataRepo.GetData().Students.Select(p => p).ToList();
                if (year != "all")
                    contacts.RemoveAll(c => c.AdmissionYear.ToString() != year);

                string GetNameWithSuffix(Contact c)
                {
                    if (suffix == "ftYY") return c.FirstName + " фт" + c.AdmissionYear % 100;
                    if (suffix == "ft") return c.FirstName + " фт";
                    return c.FirstName;
                }
                string GetSecondNameWithSuffix(Contact c)
                {
                    if (suffix == "patronymic") return c.LastName + " ФТ" + c.AdmissionYear % 100;
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
                        "",
                        "*",
                        c.Email,
                        "Telegram",
                        c.TelegramWithSobachka,
                        "Mobile",
                        c.Phone,
                        "",
                        "School",
                        "",
                        "University",
                        "ФИИТ УрФУ " + c.AdmissionYear
                    })
                    .Select(row => string.Join(",", row))
                    .ToList();

                var contentText = string.Join(",", headers) + "\n" + string.Join("\n", rows.Select(cell => cell.Replace("\r\n", " ").Replace("\n", " ")));
                var content = Encoding.UTF8.GetBytes(contentText);
                await presenter.SendContacts(fromChatId, content, "contacts_" + year + "_" + suffix + ".csv");
            }
        }
    }
}
