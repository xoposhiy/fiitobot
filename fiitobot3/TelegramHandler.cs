using System;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using fiitobot.Services.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Yandex.Cloud.Functions;

namespace fiitobot
{
    public class Response
    {
        public Response(int statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body;
        }

        public int StatusCode { get; set; }
        public string Body { get; set; }
    }

    public class TelegramBotHandlerFunction : YcFunction<string, Response>
    {
        public Response FunctionHandler(string request, Context context)
        {
            var settings = new Settings();
            var client = new TelegramBotClient(settings.TgToken);
            try
            {
                var body = JObject.Parse(request).GetValue("body")!.Value<string>();
                var update = JsonConvert.DeserializeObject<Update>(body);
                var sheetClient = new GSheetClient(settings.GoogleAuthJson);
                var botDataRepository = new BotDataRepository(settings);
                var detailsRepo = new S3ContactsDetailsRepo(settings.CreateFiitobotBucketService());
                var contactsRepo = new SheetContactsRepository(sheetClient, settings.SpreadSheetId, botDataRepository, detailsRepo);
                var presenter = new Presenter(client, settings);
                var marksReloadService = new MarksReloadService(botDataRepository, detailsRepo, sheetClient);
                var namedPhotoDirectory = new NamedPhotoDirectory(settings.PhotoListUrl);
                var photoRepo = new S3PhotoRepository(settings);
                var downloader = new TelegramFileDownloader(client);
                var studentsDownloader = new UrfuStudentsDownloader(settings);
                var demidovichService = new DemidovichService(settings.CreateDemidovichBucketService());
                var brsClient = new BrsClient(BrsClient.IsFiitOfficialGroup);
                var commands = new IChatCommandHandler[]
                {
                    new StartCommandHandler(presenter, botDataRepository),
                    new HelpCommandHandler(presenter),
                    new ContactsCommandHandler(botDataRepository, presenter),
                    new ShowGroupCommandHandler(botDataRepository, presenter),
                    new RandomCommandHandler(botDataRepository, detailsRepo, presenter, new Random(Guid.NewGuid().GetHashCode())),
                    new ReloadCommandHandler(presenter, contactsRepo, botDataRepository),
                    new ChangePhotoCommandHandler(presenter, photoRepo, settings.ModeratorsChatId),
                    new AcceptPhotoCommandHandler(presenter, botDataRepository, photoRepo, settings.ModeratorsChatId),
                    new RejectPhotoCommandHandler(presenter, botDataRepository, photoRepo, settings.ModeratorsChatId),
                    new TellToContactCommandHandler(presenter, botDataRepository),
                    new UpdateStudentStatusesFromItsCommandHandler(presenter, studentsDownloader, botDataRepository, contactsRepo),
                    new JoinCommandHandler(presenter, settings.ModeratorsChatId),
                    new DetailsCommandHandler(presenter, botDataRepository, detailsRepo),
                    new DemidovichCommandHandler(presenter, demidovichService),
                    new DownloadMarksFromBrsCommandHandler(presenter, botDataRepository, detailsRepo, brsClient),
                    new DownloadMarksFromSpreadsheetsCommandHandler(presenter, marksReloadService)
                };
                var updateService = new HandleUpdateService(botDataRepository, namedPhotoDirectory, photoRepo, demidovichService, downloader, presenter, detailsRepo, commands);
                updateService.Handle(update).Wait();
                if (GetSender(update) != settings.DevopsChatId)
                    client.SendTextMessageAsync(settings.DevopsChatId, presenter.FormatIncomingUpdate(update), null, parseMode: ParseMode.Html);
                return new Response(200, "ok");
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(settings.DevopsChatId, "Request:\n\n" + request + "\n\n" + e).Wait();
                return new Response(500, e.ToString());
            }
        }

        private long GetSender(Update update)
        {
            return update.Type switch
            {
                UpdateType.Message => update.Message!.From.Id,
                UpdateType.InlineQuery => update.InlineQuery!.From.Id,
                UpdateType.EditedMessage => update.EditedMessage!.Chat.Id,
                UpdateType.CallbackQuery => update.CallbackQuery!.From.Id,
                _ => 0
            };
        }
    }
}
