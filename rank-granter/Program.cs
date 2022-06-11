using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using System.Text;
using WTelegram;

var settings = new Settings();

using var client = new Client(settings.TgClientConfig);
var sheetClient = new GSheetClient(settings.GoogleAuthJson);
var contactsRepo = new SheetContactsRepository(sheetClient, settings.SpreadSheetId);

await new TgRankGranter().GrantStudentRanks(client, contactsRepo);
