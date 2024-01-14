using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo contactDetailsRepo;

        public SpasibkaCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo,
            IContactDetailsRepo contactDetailsRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
            this.contactDetailsRepo = contactDetailsRepo;
        }

        public string Command => "/spasibka";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            // из IContactDetailsRepo.FindBy(ID) можно найти ContactDetails человека
            // с помощью S3ContactsDetailsRepo.Save(ContactDetails) можно сохранить ContactDetails человека

            var senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            var dialogState = senderDetails.DialogState;

            try
            {
                switch (text)
                {
                    case "/spasibka cancel":
                        if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                        senderDetails.DialogState = new DialogState();
                        await presenter.Say("Спасибка отменена", fromChatId);
                        await contactDetailsRepo.Save(senderDetails);
                        return;

                    case "/spasibka restart":
                        if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                        var id = senderDetails.DialogState.CommandHandlerData.Split(' ').First();
                        senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                        senderDetails.DialogState.CommandHandlerData = $"{id}";
                        await presenter.Say("Напишите текст спасибки", fromChatId);
                        await contactDetailsRepo.Save(senderDetails);
                        return;

                    case "/spasibka confirm":
                        if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                        var rcvId = long.Parse(senderDetails.DialogState.CommandHandlerData.Split(' ')[0]);
                        var s = senderDetails.DialogState.CommandHandlerData
                            .Split(' ')
                            .Skip(1);
                        var spska = string.Join(' ', s);
                        await SendSpasibka(rcvId, sender, spska);
                        await presenter.Say("Спасибка отправлена получателю", fromChatId);
                        senderDetails.DialogState = new DialogState();
                        await contactDetailsRepo.Save(senderDetails);
                        return;

                    case "/spasibka showAll":
                        var details = await contactDetailsRepo.FindById(sender.Id);
                        var content = new StringBuilder();
                        // foreach (var spasibka in details.Spasibki)
                        // {
                        //     var from = spasibka.Sender;
                        //     content.Append($"От `{from.FirstLastName()}`:\n{spasibka.Content}\n\n");
                        // }

                        var ssw = details.Spasibki.GroupBy(spasibka => spasibka.Sender);
                        foreach (var group in details.Spasibki.GroupBy(spasibka => spasibka.Sender.FirstLastName()))
                        {
                            content.Append($"От <code>{group.Key}</code>:\n");
                            var i = 1;
                            foreach (var spasibka in group)
                            {
                                content.Append($"{i}. {spasibka.Content}\n");
                                i++;
                            }

                            content.Append("\n\n");
                        }

                        await presenter.Say(content.ToString(), fromChatId);

                        return;
                }


                if (dialogState.CommandHandlerLine.Length > 0)
                {
                    if (dialogState.CommandHandlerLine.Split(' ')[1] == "waitingForContent")
                    {
                        var data = dialogState.CommandHandlerData.Split(' ');
                        if (data.Length != 1)
                            throw new ArgumentException();
                        var rcvrId = long.Parse(data.First());
                        var content = text;
                        senderDetails.DialogState.CommandHandlerData = $"{rcvrId} {content}";
                        senderDetails.DialogState.CommandHandlerLine = $"{Command} waitingForApply";
                        await presenter.ShowSpasibkaConfirmationMessage(content, fromChatId);
                    }

                    // // callbacks
                    // else
                    // {
                    //     var query = text.Split(" ")[1];
                    //     switch (query)
                    //     {
                    //         case "cancel":
                    //             if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                    //             senderDetails.DialogState = new DialogState();
                    //             await presenter.Say("Спасибка отменена", fromChatId);
                    //             break;
                    //
                    //         case "restart":
                    //             if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                    //             var dt = senderDetails.DialogState.CommandHandlerData.Split(' ');
                    //             senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                    //             senderDetails.DialogState.CommandHandlerData = $"{dt.First()}";
                    //             await presenter.Say("Напишите текст спасибки", fromChatId);
                    //             break;
                    //
                    //         case "confirm":
                    //             if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                    //             var rcvId = long.Parse(senderDetails.DialogState.CommandHandlerData.Split(' ')[0]);
                    //             var s = senderDetails.DialogState.CommandHandlerData
                    //                 .Split(' ')
                    //                 .Skip(1);
                    //             var spska = string.Join(' ', s);
                    //             SendSpasibka(rcvId, sender, spska, fromChatId);
                    //             await presenter.Say("Спасибка отправлена получателю", fromChatId);
                    //             senderDetails.DialogState = new DialogState();
                    //             break;
                    //
                    //         case "showAll":
                    //             var details = await contactDetailsRepo.FindById(sender.Id);
                    //             if (details.Spasibki.Count == 0)
                    //             {
                    //             }
                    //             else
                    //             {
                    //                 var content = new StringBuilder();
                    //                 foreach (var spasibka in details.Spasibki)
                    //                 {
                    //                     var from = spasibka.Sender;
                    //                     content.Append($"От {from.FirstName} {from.LastName}:\n{spasibka.Content}\n\n");
                    //                 }
                    //
                    //                 await presenter.Say(content.ToString(), fromChatId);
                    //             }
                    //
                    //             break;
                    //
                    //         default:
                    //             senderDetails.DialogState = new DialogState();
                    //             break;
                    //     }
                    // }
                }

                // если зашли впервые
                var input = text.Split(" ");
                if (input.Length > 1 && long.TryParse(input[1], out var receiverId))
                {
                    senderDetails.DialogState = new DialogState
                    {
                        CommandHandlerLine = "/spasibka waitingForContent",
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

        private async Task SendSpasibka(long receiverId, Contact sender, string spasibka)
        {
            var receiverDetails = contactDetailsRepo.FindById(receiverId).Result;
            receiverDetails.Spasibki.Add(new Spasibka(sender, spasibka));
            await contactDetailsRepo.Save(receiverDetails);

            await presenter.ShowSpasibkaToReceiver(
                receiverDetails.TelegramId,
                $"Вам пришла спасибка от: {sender.FirstLastName()}. Вот что вам пишут:\n\n{spasibka}");
        }
    }
}
