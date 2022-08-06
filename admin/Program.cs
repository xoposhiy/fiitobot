using System.Diagnostics;
using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using Newtonsoft.Json;

var settings = new Settings();
var sheetClient = new GSheetClient(settings.GoogleAuthJson);
var repo = new SheetContactsRepository(sheetClient, settings.SpreadSheetId);
var detailsRepo = new DetailsRepository(sheetClient, repo);
var sw = Stopwatch.StartNew();
var contacts = repo.GetAllContacts();
Console.WriteLine("GetData contacts " + sw.Elapsed);
detailsRepo.ReloadIfNeeded();
Console.WriteLine("GetData details " + sw.Elapsed);
var people = detailsRepo.EnrichWithDetails(contacts);
Console.WriteLine("Enrich " + sw.Elapsed);
var botData = new BotData
{
    Administrators = repo.GetAllAdmins(),
    SourceSpreadsheets = repo.GetOtherSpreadsheets(),
    Students = people
};
var content = JsonConvert.SerializeObject(botData, Formatting.Indented);
File.WriteAllText("data.json", content);
Console.WriteLine($"Finished in {sw.Elapsed}");
