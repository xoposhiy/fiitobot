using System;
using System.Linq;
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
                // если зашли впервые
                var input = text.Split(" ");
                if (input.Length > 1 && long.TryParse(input[1], out var receiverId))
                {
                    senderDetails.DialogState = new DialogState();
                    senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                    senderDetails.DialogState.CommandHandlerData = $"{receiverId}";
                    await contactDetailsRepo.Save(senderDetails);
                    await presenter.Say("Напишите текст спасибки", fromChatId);
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
                        await presenter.ShowSpasibcaConfirmationMessage(content, fromChatId);
                    }

                    // callbacks
                    else
                    {
                        var query = text.Split(" ")[1];
                        switch (query)
                        {
                            case "cancel":
                                if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                                senderDetails.DialogState = new DialogState();
                                await presenter.Say("Спасибка отменена", fromChatId);
                                break;

                            case "restart":
                                if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                                var dt = senderDetails.DialogState.CommandHandlerData.Split(' ');
                                senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                                senderDetails.DialogState.CommandHandlerData = $"{dt.First()}";
                                await presenter.Say("Напишите текст спасибки", fromChatId);
                                break;

                            case "confirm":
                                if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                                var rcvId = long.Parse(senderDetails.DialogState.CommandHandlerData.Split(' ')[0]);
                                var s = senderDetails.DialogState.CommandHandlerData
                                    .Split(' ')
                                    .Skip(1);
                                var spasibka = string.Join(' ', s);
                                SendSpasibka(rcvId, sender, spasibka);
                                await presenter.Say("Спасибка отправлена получателю", fromChatId);
                                senderDetails.DialogState = new DialogState();
                                break;

                            default:
                                senderDetails.DialogState = new DialogState();
                                break;
                        }
                    }
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

        private void SendSpasibka(long receiverId, Contact sender, string spasibka)
        {
            var receiver = contactDetailsRepo.FindById(receiverId).Result;
            presenter.Say(
                $"Вам пришла спасибка от: {sender.FirstName} {sender.LastName}. Вот что вам пишут:\n\n{spasibka}",
                sender.TgId);
        }
    }
}
