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
foreach (var teacher in data.Administrators)
{
    var details = await detailsRepo.GetById(teacher.Id);
    if (teacher.TelegramWithSobachka != details.TelegramUsernameWithSobachka)
    {
        if (details.TelegramUsernameSource == TgUsernameSource.GoogleSheet)
        {
            Console.WriteLine($"Updating {teacher.Id} {teacher.FirstName} {teacher.LastName}");
            Console.WriteLine(details.TelegramUsername + " → " + teacher.TelegramUsername);
            details.TelegramUsername = teacher.TelegramUsername;
            await detailsRepo.Save(details);
        }
        else
        {
            Console.WriteLine($"Have better(?) username {details.TelegramUsername}");
        }
    }
}
