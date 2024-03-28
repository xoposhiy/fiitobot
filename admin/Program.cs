using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;

// ReSharper disable HeuristicUnreachableCode

var settings = new Settings();

var sheetClient = new GSheetClient(settings.GoogleAuthJson);
var botDataRepo = new BotDataRepository(settings);
var detailsRepo = new S3ContactsDetailsRepo(settings.CreateFiitobotBucketService());
var contactsRepository = new SheetContactsRepository(sheetClient, settings.SpreadSheetId, botDataRepo, detailsRepo);

Console.WriteLine("Loading");
var data = botDataRepo.GetData();
foreach (var teacher in data.Teachers)
{
    var details = await detailsRepo.GetById(teacher.Id);
    if (details.LastUseTime > DateTime.Now.AddMonths(-30) && details.TelegramId == 0 && teacher.TgId == 0)
    {
        Console.WriteLine(teacher);
    }
}
