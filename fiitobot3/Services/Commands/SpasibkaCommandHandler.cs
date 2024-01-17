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

        public SpasibkaCommandHandler(IPresenter presenter, IContactDetailsRepo contactDetailsRepo)
        {
            this.presenter = presenter;
            this.contactDetailsRepo = contactDetailsRepo;
        }

        public string Command => "/spasibka";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            var dialogState = senderDetails.DialogState;

            try
            {
                var callback = text.Split(' ');
                switch (string.Join(' ', callback.Take(2)))
                {
                    case "/spasibka clear":
                        senderDetails.Spasibki = new List<Spasibka>();
                        await presenter.Say("Спасибки очищены", fromChatId);
                        await contactDetailsRepo.Save(senderDetails);
                        return;

                    case "/spasibka cancel":
                        if (dialogState.CommandHandlerData.Length == 0) return;
                        senderDetails.DialogState = new DialogState();
                        await presenter.Say("Спасибка отменена", fromChatId);
                        await contactDetailsRepo.Save(senderDetails);
                        return;

                    case "/spasibka restart":
                        if (dialogState.CommandHandlerData.Length == 0) return;
                        var id = senderDetails.DialogState.CommandHandlerData.Split(' ').First();
                        senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                        senderDetails.DialogState.CommandHandlerData = $"{id}";
                        await presenter.Say("Напишите текст спасибки", fromChatId);
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
                        await ConfirmAndSendSpasibka(rcvId, sender, spska);
                        await presenter.Say("Спасибка отправлена получателю!", fromChatId);
                        senderDetails.DialogState = new DialogState();
                        await contactDetailsRepo.Save(senderDetails);
                        return;

                    case "/spasibka showAll":
                        var content = new StringBuilder();
                        ContactDetails details;
                        string errorMessage;

                        if (callback.Length == 3 && long.TryParse(callback[2], out var contactId))
                        {
                            details = await contactDetailsRepo.FindById(contactId);
                            errorMessage = "У этого пользователя пока нет спасибок :(\n" +
                                           "Но вы можете поблагодарить его за что-нибудь!";
                        }
                        else
                        {
                            details = await contactDetailsRepo.FindById(sender.Id);
                            errorMessage = "У вас пока нет спасибок :(\n" +
                                           "Но вы можете отправить их тому, кого есть за что благодарить!";
                        }

                        foreach (var group in
                                 details.Spasibki.GroupBy(spasibka => spasibka.Sender, new ContactComparer()))
                        {
                            content.Append($"От <code>{group.Key.FirstLastName()}</code> {group.Key.Telegram}:\n");
                            // content.Append($"От [{group.Key.FirstLastName()}]({group.Key.Telegram}):\n");

                            var i = 1;
                            foreach (var spasibka in group)
                            {
                                content.Append($"{i}. {spasibka.Content}\n");
                                i++;
                            }

                            content.Append("\n\n");
                        }

                        // TODO: помещать максимум по 15-20 спасибок в страницу + возможность листать строницы-
                        var toSend = content.ToString();

                        if (toSend.Length != 0)
                            await presenter.Say(toSend, fromChatId);
                        else
                            await presenter.Say(errorMessage, fromChatId);

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

                    // установить пустое поле, чтобы можно было делать новые запросы в бота на стадии подтверждения спасибки
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                senderDetails.DialogState = new DialogState();
                await contactDetailsRepo.Save(senderDetails);
                throw;
            }
        }

        private async Task ConfirmAndSendSpasibka(long receiverId, Contact sender, string spasibka)
        {
            var receiverDetails = contactDetailsRepo.FindById(receiverId).Result;
            receiverDetails.Spasibki.Add(new Spasibka(sender, spasibka));
            await contactDetailsRepo.Save(receiverDetails);

            await presenter.ShowSpasibkaToReceiver(
                $"Вам пришла спасибка от: <code>{sender.FirstLastName()}</code> {sender.Telegram}." +
                $" Вот что вам пишут:\n\n{spasibka}",
                receiverDetails.TelegramId);
        }
    }
}
