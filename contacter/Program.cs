using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using System.Text;
using WTelegram;

var settings = new Settings();

using var client = new Client(settings.TgClientConfig);

var extractor = new TgContactsExtractor();
var users = await extractor.ExtractUsersFromChatsAndChannels("ФИИТ 20", client);

var sb = new StringBuilder();
foreach (var user in users)
    sb.AppendLine($"{user.username ?? "-"};{user.id};{user.first_name};{user.last_name};{user.phone}");

File.WriteAllText("contacts.csv", sb.ToString());
Console.WriteLine(sb.ToString());
