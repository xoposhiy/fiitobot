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
            var callback = text.Split(' ');

            switch (string.Join(' ', callback.Take(2)))
            {
                // case "/spasibka clearAll":
                //     var botData = botDataRepository.GetData();
                //     foreach (var contact in botData.AllContacts)
                //     {
                //         var contactDetails = await contactDetailsRepo.FindById(contact.Id);
                //         if (contactDetails == null) continue;
                //         contactDetails.Spasibki = new List<Spasibka>();
                //         await contactDetailsRepo.Save(contactDetails);
                //     }
                //
                //     await presenter.Say("Все спасибки удалены", fromChatId);
                //     return;

                case "/spasibka clear":
                    senderDetails.Spasibki = new List<Spasibka>();
                    await presenter.Say("Ваши спасибки очищены", fromChatId);
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "/spasibka delete":
                    senderDetails.Spasibki.RemoveAt(senderDetails.DialogState.IdxSpasibkaToDelete);
                    await ShowMessageAboutDeletedSpasibka(fromChatId);
                    senderDetails.DialogState.MessageId = null;
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "/spasibka showToDelete":
                    senderDetails.DialogState.IdxSpasibkaToDelete = senderDetails.Spasibki.Count - 1;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowOneSpasibkaToDelete(fromChatId);
                    return;

                case "/spasibka cancelDelete":
                    if (senderDetails.DialogState.MessageId == null)
                    {
                        throw new NullReferenceException();
                    }

                    await ShowAll(callback, sender, fromChatId, true);
                    return;

                case "/spasibka next":
                    if (dialogState.IdxSpasibkaToDelete - 1 < 0) return;
                    senderDetails.DialogState.IdxSpasibkaToDelete -= 1;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowOneSpasibkaToDelete(fromChatId);
                    return;

                case "/spasibka previous":
                    if (dialogState.IdxSpasibkaToDelete + 1 > senderDetails.Spasibki.Count - 1) return;
                    senderDetails.DialogState.IdxSpasibkaToDelete += 1;
                    await contactDetailsRepo.Save(senderDetails);
                    await ShowOneSpasibkaToDelete(fromChatId);
                    return;

                case "/spasibka cancel":
                    if (dialogState.CommandHandlerData.Length == 0) return;
                    if (senderDetails.DialogState.MessageId == null)
                        throw new NullReferenceException();
                    await presenter.EditMessage("Спасибка отменена", fromChatId,
                        (int)senderDetails.DialogState.MessageId);
                    senderDetails.DialogState = new DialogState();
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "/spasibka restart":
                    if (dialogState.CommandHandlerData.Length == 0) return;
                    var id = senderDetails.DialogState.CommandHandlerData.Split(' ').First();
                    senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                    senderDetails.DialogState.CommandHandlerData = $"{id}";
                    if (senderDetails.DialogState.MessageId == null)
                        throw new NullReferenceException();

                    await presenter.EditMessage("Напишите текст спасибки", fromChatId,
                        (int)senderDetails.DialogState.MessageId);
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "/spasibka confirm":
                    if (dialogState.CommandHandlerData.Length == 0) return;
                    var rcvId = long.Parse(senderDetails.DialogState.CommandHandlerData.Split(' ')[0]);
                    var s = senderDetails.DialogState.CommandHandlerData
                        .Split(' ')
                        .Skip(1)
                        .ToArray();
                    if (s.Length == 0) return;
                    var spska = string.Join(' ', s);
                    await ConfirmAndSendSpasibka(rcvId, sender, spska, fromChatId);

                    // if (senderDetails.DialogState.MessageId == null)
                    //     throw new NullReferenceException();
                    // await presenter.HideInlineKeyboard(fromChatId, (int)senderDetails.DialogState.MessageId);
                    senderDetails.DialogState = new DialogState();
                    await contactDetailsRepo.Save(senderDetails);
                    return;

                case "/spasibka showAll":
                    await ShowAll(callback, sender, fromChatId);
                    return;
            }

            // пришёл текст спасибки
            if (dialogState.CommandHandlerLine.Length > 0 &&
                dialogState.CommandHandlerLine.Split(' ')[1] == "waitingForContent")
            {
                var data = dialogState.CommandHandlerData.Split(' ');
                if (data.Length != 1)
                    throw new ArgumentException();
                var rcvrId = long.Parse(data.First());
                var content = text;

                senderDetails.DialogState.CommandHandlerLine = $"{Command} waitingForApply";
                senderDetails.DialogState.CommandHandlerData = $"{rcvrId} {content}";

                await presenter.ShowSpasibkaConfirmationMessage(content, fromChatId);
            }

            var input = text.Split(" ");

            // зашли впервые
            if (input.Length > 1 && long.TryParse(input[1], out var receiverId))
            {
                senderDetails.DialogState = new DialogState
                {
                    CommandHandlerLine = $"{Command} waitingForContent",
                    CommandHandlerData = $"{receiverId}"
                };
                await presenter.Say("Напишите текст спасибки", fromChatId);
            }

            await contactDetailsRepo.Save(senderDetails);
        }

        private async Task ShowAll(string[] callback, Contact sender, long fromChatId, bool editMessage = false)
        {
            var content = new StringBuilder();
            ContactDetails details;
            string errorMessage;
            var canEdit = false;

            if (callback.Length == 3 && long.TryParse(callback[2], out var contactId))
            {
                details = await contactDetailsRepo.FindById(contactId);
                errorMessage = "У этого пользователя пока нет спасибок :(\n" +
                               "Но вы можете поблагодарить его за что-нибудь!";
            }
            else
            {
                canEdit = true;
                details = await contactDetailsRepo.FindById(sender.Id);
                errorMessage = "У вас пока нет спасибок :(\n" +
                               "Но вы можете отправить их тому, кого есть за что благодарить!";
            }

            var spasibkaLst = details.Spasibki;
            var i = 1;

            for (var j = spasibkaLst.Count - 1; j >= 0; j--)
            {
                content.Append($"{i}) От {FormatSpasibka(spasibkaLst[j], "\n")}\n\n");
                i++;
            }

            // TODO: помещать максимум по 15-20 спасибок в страницу + возможность листать строницы
            var toSend = content.ToString();

            if (toSend.Length != 0)
            {
                if (editMessage)
                {
                    if (senderDetails.DialogState.MessageId == null)
                        throw new NullReferenceException();
                    await presenter.EditMessage(toSend, fromChatId, (int)senderDetails.DialogState.MessageId);
                }
                else
                    await presenter.ShowAllSpasibkaList(toSend, fromChatId, canEdit);
            }
            else
                await presenter.Say(errorMessage, fromChatId);
        }

        private async Task ConfirmAndSendSpasibka(long receiverId, Contact sender, string content, long fromChatId)
        {
            var receiverDetails = contactDetailsRepo.FindById(receiverId).Result;
            receiverDetails.Spasibki.Add(new Spasibka(sender.Id, content, DateTime.UtcNow.AddHours(5)));
            await contactDetailsRepo.Save(receiverDetails);

            if (senderDetails.DialogState.MessageId == null)
                throw new NullReferenceException();

            await presenter.NotifyReceiverAboutNewSpasibka(
                $"Вам пришла спасибка от <code>{sender.FirstLastName()}</code> {sender.Telegram}." +
                $" Вот что вам пишут:\n\n«{content}»",
                receiverDetails.TelegramId);

            await presenter.EditMessage("Спасибка отправлена, получатель получил уведомление!",
                fromChatId,
                (int)senderDetails.DialogState.MessageId);
        }

        private async Task ShowOneSpasibkaToDelete(long fromChatId)
        {
            var spasibka = senderDetails.Spasibki[senderDetails.DialogState.IdxSpasibkaToDelete];
            var content = FormatSpasibka(spasibka, "\n\n");

            if (senderDetails.DialogState.MessageId == null)
                throw new NullReferenceException();

            var messageId = (int)senderDetails.DialogState.MessageId;
            await presenter.ShowOneSpasibkaFromList(content, fromChatId, messageId,
                previous: senderDetails.DialogState.IdxSpasibkaToDelete + 1 < senderDetails.Spasibki.Count,
                next: senderDetails.DialogState.IdxSpasibkaToDelete - 1 >= 0);
        }

        private async Task ShowMessageAboutDeletedSpasibka(long fromChatId)
        {
            if (senderDetails.DialogState.MessageId == null)
                throw new NullReferenceException();

            await presenter.EditMessage("Спасибка удалена!", fromChatId,
                (int)senderDetails.DialogState.MessageId);
        }

        private string FormatSpasibka(Spasibka spasibka, string lineSeparator)
        {
            var botData = botDataRepository.GetData();
            var sender = botData.AllContacts.FirstOrDefault(contact => contact.Id == spasibka.SenderContactId);

            var res = $"<code>{sender?.FirstLastName()}</code> {sender?.Telegram}:" +
                      $"{lineSeparator}«{spasibka.Content}»\n<code>{spasibka.PostDate:yyyy.MM.dd-hh:mm}</code>";
            return res;
        }
    }
}
