using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SpasibkaStartCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo contactDetailsRepo;

        public SpasibkaStartCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo,
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
                var inpt = text.Split(" ");
                if (inpt.Length > 1 && long.TryParse(inpt[1], out var receiverId))
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

                    else // callbacks
                    {
                        var query = text.Split(" ")[1];
                        switch (query)
                        {
                            case "cancel":
                                if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                                senderDetails.DialogState = new DialogState();
                                await presenter.Say("Спасибка отменена", fromChatId);
                                break;
                            case "confirm":
                                if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                                await presenter.Say("done", fromChatId);
                                senderDetails.DialogState = new DialogState();
                                break;
                            case "restart":
                                if (senderDetails.DialogState.CommandHandlerData.Length == 0) return;
                                var dt = senderDetails.DialogState.CommandHandlerData.Split(' ');
                                senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                                senderDetails.DialogState.CommandHandlerData = $"{dt.First()}";
                                await presenter.Say("Напишите текст спасибки", fromChatId);
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
    }
}
