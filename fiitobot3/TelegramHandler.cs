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
                var contactsRepo = new SheetContactsRepository(sheetClient, settings.SpreadSheetId);
                var detailsRepo = new DetailsRepository(sheetClient, contactsRepo);
                var presenter = new Presenter(client, settings);
                var botDataRepository = new BotDataRepository(settings);
                var namedPhotoDirectory = new NamedPhotoDirectory(settings.PhotoListUrl);
                var photoRepo = new S3PhotoRepository(settings);
                var downloader = new TelegramFileDownloader(client);
                var studentsDownloader = new UrfuStudentsDownloader(settings);

                var commands = new IChatCommandHandler[]
                {
                    new StartCommandHandler(presenter, botDataRepository),
                    new HelpCommandHandler(presenter),
                    new ContactsCommandHandler(botDataRepository, presenter),
                    new RandomCommandHandler(botDataRepository, presenter, new Random()), 
                    new ReloadCommandHandler(presenter, contactsRepo, detailsRepo, botDataRepository),
                    new ChangePhotoCommandHandler(presenter, botDataRepository, photoRepo, settings.ModeratorsChatId),
                    new AcceptPhotoCommandHandler(presenter, botDataRepository, photoRepo, settings.ModeratorsChatId),
                    new RejectPhotoCommandHandler(presenter, botDataRepository, photoRepo, settings.ModeratorsChatId),
                    new TellToContactCommandHandler(presenter, botDataRepository),
                    new UpdateStudentStatusesFromItsCommandHandler(presenter, studentsDownloader, botDataRepository, contactsRepo),
                    new JoinCommandHandler(presenter, botDataRepository, settings.ModeratorsChatId),
                    new DetailsCommandHandler(presenter, botDataRepository)
                };
                var updateService = new HandleUpdateService(botDataRepository, namedPhotoDirectory, photoRepo, downloader, presenter, commands);
                updateService.Handle(update).Wait();
                client.SendTextMessageAsync(settings.DevopsChatId, presenter.FormatIncomingUpdate(update), ParseMode.Html);
                return new Response(200, "ok");
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(settings.DevopsChatId, "Request:\n\n" + request + "\n\n" + e).Wait();
                return new Response(500, e.ToString());
            }
        }
    }
}
