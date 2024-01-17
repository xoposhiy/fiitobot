using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using fiitobot.Services.Commands;

// ReSharper disable HeuristicUnreachableCode

var settings = new Settings();

var sheetClient = new GSheetClient(settings.GoogleAuthJson);
var botDataRepo = new BotDataRepository(settings);
var detailsRepo = new S3ContactsDetailsRepo(settings.CreateFiitobotBucketService());
var contactsRepository = new SheetContactsRepository(sheetClient, settings.SpreadSheetId, botDataRepo, detailsRepo);

Console.WriteLine("Loading");
var contacts = contactsRepository.GetStudents().ToArray();
Console.WriteLine($"Loaded {contacts.Length} students");
var botData = new BotData
{
    Administrators = contactsRepository.GetAdmins(),
    Teachers = contactsRepository.GetTeachers(),
    Students = contacts
};
Console.WriteLine("Saving data to S3");
botDataRepo.Save(botData);
Console.WriteLine("Done");


