using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using TL;
using Yandex.Cloud.Mdb.Elasticsearch.V1;

namespace fiitobot.Services.Commands
{
    public class
        SpasibkaStartCommandHandler : IChatCommandHandler // self recovery: при ошибке скидывать в начальный стейт
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

            // await presenter.Say(text, fromChatId);

            var senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            var dialogState = senderDetails.DialogState;

            try
            {
                if (dialogState.CommandHandlerLine.Length == 0)
                {
                    var query = text.Split(" ")[1];
                    if (!long.TryParse(query, out var receiverId))
                    {
                        senderDetails.DialogState = new DialogState();
                        throw new ArgumentException();
                    }
                    senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                    senderDetails.DialogState.CommandHandlerData = $"{receiverId}";
                    await presenter.Say("Напишите текст спасибки", fromChatId);
                    await contactDetailsRepo.Save(senderDetails);
                    return;
                }

                switch (dialogState.CommandHandlerLine.Split(' ')[1])
                {
                    case "waitingForContent":
                        var data = dialogState.CommandHandlerData.Split(' ');
                        if (data.Length != 1)
                            throw new ArgumentException();
                        var rcvrId = long.Parse(data.First());
                        var content = text.Skip(2).ToString();
                        senderDetails.DialogState.CommandHandlerData = $"{rcvrId} {content}";
                        senderDetails.DialogState.CommandHandlerLine = $"{Command} waitingForApply";
                        await presenter.ShowSpasibcaConfirmationMessage(content, fromChatId);
                        break;
                    // case "waitingForApply":
                    //     break;
                    // case "confirm":
                    //     await presenter.Say("done", fromChatId);
                    //     break;
                    // case "restart":
                    //     var d = senderDetails.DialogState.CommandHandlerData.Split(' ');
                    //     senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                    //     senderDetails.DialogState.CommandHandlerData = $"{d.First()}";
                    //     await presenter.Say("Напишите текст спасибки", fromChatId);
                    //     break;

                    default:
                        var query = text.Split(" ")[1];
                        switch (query)
                        {
                            case "confirm":
                                await presenter.Say("done", fromChatId);
                                senderDetails.DialogState = new DialogState();
                                break;
                            case "restart":
                                var dt = senderDetails.DialogState.CommandHandlerData.Split(' ');
                                senderDetails.DialogState.CommandHandlerLine = "/spasibka waitingForContent";
                                senderDetails.DialogState.CommandHandlerData = $"{dt.First()}";
                                await contactDetailsRepo.Save(senderDetails);
                                await presenter.Say("Напишите текст спасибки", fromChatId);
                                break;
                            default:
                                senderDetails.DialogState = new DialogState();
                                break;
                        }

                        break;
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
