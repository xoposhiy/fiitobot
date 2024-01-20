using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaCommandHandler : IChatCommandHandler
    {
        private const int MaxSpasibkaLength = 2000;
        private const int MaxMessageLength = 4096;
        private readonly IPresenter presenter;
        private readonly IContactDetailsRepo contactDetailsRepo;
        private readonly BotDataRepository botDataRepository;
        private ContactDetails senderDetails;

        public SpasibkaCommandHandler(IPresenter presenter, IContactDetailsRepo contactDetailsRepo,
            BotDataRepository botDataRepository)
        {
            this.presenter = presenter;
            this.contactDetailsRepo = contactDetailsRepo;
            this.botDataRepository = botDataRepository;
        }

        public string Command => "/spasibka";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            var dialogState = senderDetails.DialogState;
            var storedData = dialogState.CommandHandlerData?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            var storedReceiverId = storedData.Length > 0 ? long.Parse(storedData[0]) : -1;
            var storedText = storedData.Length > 1 ? storedData[1] : "";

            if (!string.IsNullOrEmpty(dialogState.CommandHandlerLine) && !text.StartsWith(Command))
            {
                // пришёл текст спасибки
                if (text.Length > MaxSpasibkaLength)
                {
                    await presenter.Say("Ой, это слишком длинно! Давай ещё раз, но короче 2000 символов.", fromChatId);
                    return;
                }
                senderDetails.DialogState.CommandHandlerLine = "";
                senderDetails.DialogState.CommandHandlerData = $"{storedReceiverId} {text}";
                await contactDetailsRepo.Save(senderDetails);
                var spasibka = new Spasibka(sender.Id, text, DateTime.UtcNow);
                await presenter.ShowSpasibkaConfirmationMessage(FormatSpasibkaNotificationHtml(spasibka), fromChatId);
                return;
            }

            var parameters = text.Split(' ');
            var subcommand = parameters[1];

            switch (subcommand)
            {
                case "clear":
                    senderDetails.Spasibki.Clear();
                    await presenter.Say("Спасибок больше нет :(", fromChatId);
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "delete":
                    senderDetails.Spasibki.RemoveAt(senderDetails.DialogState.ItemIndex);
                    await ShowMessageAboutDeletedSpasibka(fromChatId);
                    senderDetails.DialogState.MessageId = null;
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "showToDelete":
                    senderDetails.DialogState.ItemIndex = senderDetails.Spasibki.Count - 1;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowOneSpasibkaToDelete(fromChatId);
                    return;

                case "cancelDelete":
                    if (senderDetails.DialogState.MessageId == null)
                    {
                        throw new Exception("DialogState.MessageId is null");
                    }
                    senderDetails.DialogState.ItemIndex = senderDetails.Spasibki.Count - 1;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowAll(sender.Id, sender, fromChatId, true);
                    return;

                case "next":
                    dialogState.ItemIndex -= 1;
                    if (dialogState.ItemIndex < 0)
                        dialogState.ItemIndex = 0;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowOneSpasibkaToDelete(fromChatId);
                    return;

                case "previous":
                    dialogState.ItemIndex += 1;
                    if (dialogState.ItemIndex >= senderDetails.Spasibki.Count)
                        dialogState.ItemIndex = senderDetails.Spasibki.Count-1;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowOneSpasibkaToDelete(fromChatId);
                    return;

                case "cancel":
                    if (storedReceiverId == -1) return;
                    if (senderDetails.DialogState.MessageId == null)
                        throw new Exception("DialogState.MessageId is null");
                    await presenter.EditMessage("Спасибка отменена", fromChatId,
                        (int)senderDetails.DialogState.MessageId);
                    senderDetails.DialogState = new DialogState();
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "start":
                    var receiverId = parameters[2];
                    senderDetails.DialogState = new DialogState
                    {
                        CommandHandlerLine = $"{Command}",
                        CommandHandlerData = $"{receiverId}"
                    };
                    await contactDetailsRepo.Save(senderDetails);
                    await presenter.AskForSpasibkaText(fromChatId);
                    return;

                case "restart":
                    if (storedReceiverId == -1) return;
                    senderDetails.DialogState.CommandHandlerLine = $"{Command}";
                    senderDetails.DialogState.CommandHandlerData = $"{storedReceiverId}";
                    await contactDetailsRepo.Save(senderDetails);
                    if (senderDetails.DialogState.MessageId == null)
                        throw new Exception("DialogState.MessageId is null");
                    await presenter.AskForSpasibkaText(fromChatId, (int)senderDetails.DialogState.MessageId);
                    return;

                case "confirm":
                    await ConfirmAndSendSpasibka(storedReceiverId, sender, storedText, fromChatId);
                    senderDetails = await contactDetailsRepo.FindById(senderDetails.ContactId);
                    senderDetails.DialogState = new DialogState();
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "showAll":
                    long? contactId = null;
                    if (parameters.Length > 2) contactId = long.Parse(parameters[2]);
                    await ShowAll(contactId ?? sender.Id, sender, fromChatId);
                    return;
            }
        }

        private async Task ShowAll(long contactId, Contact sender, long fromChatId, bool editMessage = false)
        {
            var details = await contactDetailsRepo.FindById(contactId);
            var spasibkas = details.Spasibki.OrderByDescending(s => s.PostDate).ToList();
            var content = "";
            var count = 0;
            foreach (var spasibka in spasibkas)
            {
                var formatted = FormatSpasibkaHtml(spasibka);
                if (content.Length + formatted.Length > MaxMessageLength - 500)
                {
                    content += $"\n\n<i>и ещё {(spasibkas.Count - count).Pluralize("спасибка|спасибки|спасибок")}</i>";
                    //TODO Показывать кнопку "Показать следующие" и следующие спасибки.
                    break;
                }
                if (count > 0) content += "\n\n";
                content += formatted;
                count++;
            }
            if (spasibkas.Count != 0)
            {
                if (editMessage)
                {
                    if (senderDetails.DialogState.MessageId == null)
                        throw new Exception("DialogState.MessageId is null");
                    await presenter.EditMessage(content, fromChatId, (int)senderDetails.DialogState.MessageId);
                }
                else
                    await presenter.ShowAllSpasibkaList(content, fromChatId, contactId == sender.Id);
            }
            else
            {
                var zeroSpasibkas = "Тут пока нет спасибок :(\n" +
                                    "Есть за что поблагодарить? Так сделай это — получить спасибку всегда приятно!";

                if (contactId == sender.Id)
                {
                    zeroSpasibkas = "Тут пока нет спасибок :(\n" +
                                    "Отправь сам кому-нибудь спасибку, и она к тебе не раз ещё вернется!";
                }
                await presenter.Say(zeroSpasibkas, fromChatId);
            }
        }

        private async Task ConfirmAndSendSpasibka(long receiverId, Contact sender, string content, long fromChatId)
        {
            var receiverDetails = contactDetailsRepo.FindById(receiverId).Result;
            var spasibka = new Spasibka(sender.Id, content, DateTime.UtcNow);
            receiverDetails.Spasibki.Add(spasibka);
            await contactDetailsRepo.Save(receiverDetails);

            if (senderDetails.DialogState.MessageId == null)
                throw new Exception("DialogState.MessageId is null");

            await presenter.NotifyReceiverAboutNewSpasibka(
                FormatSpasibkaNotificationHtml(spasibka),
                receiverDetails.TelegramId);

            await presenter.EditMessage("Спасибка отправлена, получатель получил уведомление!",
                fromChatId,
                (int)senderDetails.DialogState.MessageId);
        }

        private async Task ShowOneSpasibkaToDelete(long fromChatId)
        {
            var spasibka = senderDetails.Spasibki[senderDetails.DialogState.ItemIndex];
            var content = FormatSpasibkaHtml(spasibka);

            if (senderDetails.DialogState.MessageId == null)
                throw new Exception("DialogState.MessageId is null");

            var messageId = (int)senderDetails.DialogState.MessageId;
            await presenter.ShowOneSpasibkaFromList(content, fromChatId, messageId,
                previous: senderDetails.DialogState.ItemIndex + 1 < senderDetails.Spasibki.Count,
                next: senderDetails.DialogState.ItemIndex - 1 >= 0);
        }

        private async Task ShowMessageAboutDeletedSpasibka(long fromChatId)
        {
            if (senderDetails.DialogState.MessageId == null)
                throw new Exception("DialogState.MessageId is null");

            await presenter.EditMessage("Спасибка удалена!", fromChatId,
                (int)senderDetails.DialogState.MessageId);
        }

        private  string FormatSpasibkaNotificationHtml(Spasibka spasibka)
        {
            var botData = botDataRepository.GetData();
            var sender = botData.AllContacts.FirstOrDefault(contact => contact.Id == spasibka.SenderContactId);
            return $"Спасибо тебе от <code>{sender?.FirstLastName() ?? "НЛО"}</code> {sender?.Telegram}." +
                   $" Вот что он пишет:\n\n«{spasibka.Content.EscapeForTgHtml()}»";
        }

        private string FormatSpasibkaHtml(Spasibka spasibka)
        {
            var botData = botDataRepository.GetData();
            var sender = botData.AllContacts.FirstOrDefault(contact => contact.Id == spasibka.SenderContactId);
            var res = $"{spasibka.PostDate.AddHours(5):dd MMMM yyyy} ";
            if (sender != null) res += $"<code>{sender.FirstLastName()}</code> {sender.Telegram}";
            res += $":\n{spasibka.Content.EscapeForTgHtml()}";
            return res;
        }
    }
}
