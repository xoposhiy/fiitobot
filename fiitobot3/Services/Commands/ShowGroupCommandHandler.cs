using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class ShowGroupCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository dataRepo;
        private readonly IPresenter presenter;

        public ShowGroupCommandHandler(IBotDataRepository dataRepo, IPresenter presenter)
        {
            this.dataRepo = dataRepo;
            this.presenter = presenter;
        }

        public string Command => "/show_group";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, ContactWithDetails senderWithDetails, bool silentOnNoResults = false)
        {
            var students = dataRepo.GetData().Students;
            string groupName;
            var sender = senderWithDetails.Contact;
            if (sender.Type == ContactType.Student)
            {
                students = students.Where(p => p.GroupIndex == sender.GroupIndex && p.GraduationYear == sender.GraduationYear).ToArray();
                groupName = sender.FormatMnemonicGroup(DateTime.Now, false);
            }
            else
            {
                var parts = text.Split(" ");
                if (parts.Length < 2)
                {
                    await presenter.Say($"Укажите группу: {Command} 101", fromChatId);
                    return;
                }
                var group = parts[1].ToUpper().Replace("ФТ-", "").Replace("ФТ", "").Replace("ФИИТ-", "").Replace("ФИИТ", "");
                students = students.Where(p => p.FormatMnemonicGroup(DateTime.Now, false).Replace("ФТ-", "") == group || p.FormatOfficialGroup(DateTime.Now) == group).ToArray();
                groupName = students.First().FormatMnemonicGroup(DateTime.Now, false);
            }

            var headers = new[] { "Surname", "Name", "Patronymic", "Status" };
            var rows = students.Select(s => s.LastName + "\t" + s.FirstName + "\t" + s.Patronymic + "\t" + s.Status);
            var contentText = string.Join("\t", headers) + "\n" + string.Join("\n", rows);
            var content = Encoding.UTF8.GetBytes(contentText);
            await presenter.SendFile(fromChatId, content, groupName + ".txt", $"Список группы {groupName}");
        }
    }
}
