using System.Diagnostics;
using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using fiitobot.Services.Commands;
using Newtonsoft.Json;
using tests;
// ReSharper disable HeuristicUnreachableCode

var settings = new Settings();

var demidovich = new DemidovichService(settings.CreateDemidovichBucketService());
var count = 0;
foreach (var file in Directory.EnumerateFiles(@"c:\work\DemidovichBot\images\Demidovich", "*.gif"))
{
    var exerciseNumber = Path.GetFileNameWithoutExtension(file);
    var exists = await demidovich.HasImage(Path.GetFileNameWithoutExtension(file));
    if (!exists)
        Console.WriteLine(exerciseNumber);
    count++;
    if (count % 100 == 0)
        Console.WriteLine($"processed {count}");
}

return;
    
#pragma warning disable CS0162

var sheetClient = new GSheetClient(settings.GoogleAuthJson);
var repo = new SheetContactsRepository(sheetClient, settings.SpreadSheetId);
var namesClient = new TgNamesClient(new tgnames.Settings().ApiKeys.First().Key, new Uri("https://functions.yandexcloud.net/d4ek1oph2qq118htfcp3"));
var sw = Stopwatch.StartNew();
var contacts = repo.GetAllContacts();
foreach (var contact in contacts)
{
    namesClient.Request(contact.Telegram.TrimStart('@'), contact.TgId);
}

return;
Console.WriteLine("GetData contacts " + sw.Elapsed);
Console.WriteLine("GetData details " + sw.Elapsed);
Console.WriteLine("Enrich " + sw.Elapsed);
var botData = new BotData
{
    Administrators = repo.GetAllAdmins(),
    SourceSpreadsheets = repo.GetOtherSpreadsheets(),
    Students = contacts
};
var content = JsonConvert.SerializeObject(botData, Formatting.Indented);
File.WriteAllText("data.json", content);
Console.WriteLine($"Finished in {sw.Elapsed}");
#pragma warning restore CS0162
