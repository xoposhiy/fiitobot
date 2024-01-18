using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaCommandHandler : IChatCommandHandler
    {
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
                senderDetails.DialogState.CommandHandlerLine = "";
                senderDetails.DialogState.CommandHandlerData = $"{storedReceiverId} {text}";
                await contactDetailsRepo.Save(senderDetails);
                await presenter.ShowSpasibkaConfirmationMessage(FormatSpasibkaNotification(sender, text), fromChatId);
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
            var canDelete = false;
            var zeroSpasibkas = "У этого пользователя пока нет спасибок :(\n" +
                               "Но вы можете поблагодарить его за что-нибудь!";

            var details = await contactDetailsRepo.FindById(contactId);
            if (contactId == sender.Id)
            {
                canDelete = true;
                zeroSpasibkas = "У вас пока нет спасибок :(\n" +
                               "Но вы можете отправить их тому, кого есть за что благодарить!";
            }

            var spasibkaLst = details.Spasibki;
            var i = 1;

            var content = new StringBuilder();
            for (var j = spasibkaLst.Count - 1; j >= 0; j--)
            {
                content.Append($"{i}) От {FormatSpasibka(spasibkaLst[j], "\n")}\n\n");
                i++;
            }

            // TODO: помещать максимум по 15-20 спасибок в страницу + возможность листать строницы
            if (content.Length != 0)
            {
                if (editMessage)
                {
                    if (senderDetails.DialogState.MessageId == null)
                        throw new Exception("DialogState.MessageId is null");
                    await presenter.EditMessage(content.ToString(), fromChatId, (int)senderDetails.DialogState.MessageId);
                }
                else
                    await presenter.ShowAllSpasibkaList(content.ToString(), fromChatId, canDelete);
            }
            else
                await presenter.Say(zeroSpasibkas, fromChatId);
        }

        private async Task ConfirmAndSendSpasibka(long receiverId, Contact sender, string content, long fromChatId)
        {
            var receiverDetails = contactDetailsRepo.FindById(receiverId).Result;
            receiverDetails.Spasibki.Add(new Spasibka(sender.Id, content, DateTime.UtcNow.AddHours(5)));
            await contactDetailsRepo.Save(receiverDetails);

            if (senderDetails.DialogState.MessageId == null)
                throw new Exception("DialogState.MessageId is null");

            await presenter.NotifyReceiverAboutNewSpasibka(
                FormatSpasibkaNotification(sender, content),
                receiverDetails.TelegramId);

            await presenter.EditMessage("Спасибка отправлена, получатель получил уведомление!",
                fromChatId,
                (int)senderDetails.DialogState.MessageId);
        }

        private static string FormatSpasibkaNotification(Contact sender, string content)
        {
            return $"Спасибо тебе от <code>{sender.FirstLastName()}</code> {sender.Telegram}." +
                   $" Вот что он пишет:\n\n«{content}»";
        }

        private async Task ShowOneSpasibkaToDelete(long fromChatId)
        {
            var spasibka = senderDetails.Spasibki[senderDetails.DialogState.ItemIndex];
            var content = FormatSpasibka(spasibka, "\n\n");

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

        private string FormatSpasibka(Spasibka spasibka, string lineSeparator)
        {
            var botData = botDataRepository.GetData();
            var sender = botData.AllContacts.FirstOrDefault(contact => contact.Id == spasibka.SenderContactId);

            var res = $"<code>{sender?.FirstLastName()}</code> {sender?.Telegram}:" +
                      $"{lineSeparator}«{spasibka.Content}»\n<code>{spasibka.PostDate:yyyy.MM.dd}</code>";
            return res;
        }
    }
}
