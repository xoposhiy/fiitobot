using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using System.Text;
using TL;
using WTelegram;

await ExtractChats();
//await ExtractStudents();

async Task ExtractChats()
{
    var settings = new Settings();
    using var client = new Client(settings.TgClientConfig);
    Console.WriteLine("start!");
    var chats = (await client.Messages_GetAllDialogs()).chats;
    Console.WriteLine(chats.Count());
    var sb = new StringBuilder();
    foreach (var chat in chats.Values)
        sb.AppendLine($"{chat.ID};{chat.Title}");

    File.WriteAllText("chats.csv", sb.ToString());
    Console.WriteLine(sb.ToString());
}

async Task ExtractStudents()
{
    var settings = new Settings();
    using var client = new Client(settings.TgClientConfig);
    var extractor = new TgContactsExtractor();
    var users = await extractor.ExtractUsersFromChatsAndChannels("ФИИТ 20", client);

    var sb = new StringBuilder();
    foreach (var user in users)
        sb.AppendLine($"{user.username ?? "-"};{user.id};{user.first_name};{user.last_name};{user.phone}");

    File.WriteAllText("contacts.csv", sb.ToString());
    Console.WriteLine(sb.ToString());
}