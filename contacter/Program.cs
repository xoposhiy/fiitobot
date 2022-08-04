using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using System.Text;
using TL;
using WTelegram;

//await ImportContacts();
//await ExtractChats();
//await ExtractStudents();
await ActualizeContacts(
    "https://docs.google.com/spreadsheets/d/1VH_pZnYvTgQ-IzFVs5CYQWjbCo6YtUfip4w7K6GTK3U/edit#gid=0");


async Task ActualizeContacts(string spreadsheetUrl)
{
    var settings = new Settings();
    var gsClient = new GSheetClient(settings.GoogleAuthJson);
    var sheet = gsClient.GetSheetByUrl(spreadsheetUrl);
    var data = sheet.ReadRange("A1:O");
    var headersRow = data[0];
    var usernameColIndex = headersRow.IndexOf("Telegram");
    var phoneColIndex = headersRow.IndexOf("Phone");
    var tgIdColIndex = headersRow.IndexOf("TgId");
    var tgClient = new Client(settings.TgClientConfig);
    tgClient.FloodRetryThreshold = 120;
    var defaultLogger = Helpers.Log;
    var rollingLog = new Queue<string>();
    Helpers.Log = (level, message) =>
    {
        lock (rollingLog)
        {
            if (level < 3)
            {
                rollingLog.Enqueue(message);
                while (rollingLog.Count > 10) rollingLog.Dequeue();
            }
            else
            {
                foreach (var prevMessage in rollingLog)
                    defaultLogger(2, prevMessage);
                defaultLogger(level, message);
            }
        }
    };
    
    await tgClient.LoginUserIfNeeded();
    var extractor = new TgContactsExtractor();
    Dictionary<long, User> users = (await extractor.ExtractUsersFromChatsAndChannels(tgClient, "Чат ФИИТ 2022", "спроси про ФИИТ", "Чат — Матмех, приём!")).ToDictionary(u => u.ID, u => u);

    Console.WriteLine($"Tg Users Found: {users.Count}");
    Console.WriteLine($"Contacts from Google Sheet: {data.Count - 1}");
    var edit = sheet.Edit();
    var editsCount = 0;
    for (var rowIndex = 278; rowIndex < data.Count; rowIndex++)
    {
        var row = data[rowIndex];
        while (row.Count <= Math.Max(usernameColIndex, Math.Max(tgIdColIndex, phoneColIndex)))
            row.Add("");
        var username = row[usernameColIndex];
        var phone = row[phoneColIndex];
        var tgId = long.TryParse(row[tgIdColIndex], out var v) ? v : -1;
        Console.WriteLine($"{string.Join(";", row)}...");
        var user = //await FindByUsername(tgClient, username) ??
                   await FindByPhone(tgClient, phone) ?? (users.TryGetValue(tgId, out var u) ? u : null);
        if (user == null)
        {
            Console.WriteLine($"  no telegram user found");
        }
        else
        {
            void Update(int colIndex, string prefix, object newValue, string what)
            {
                var stringValue = "" + newValue;
                if (stringValue == "") return;
                stringValue = prefix + stringValue;
                if (row[colIndex] != stringValue && stringValue != "")
                {
                    Console.WriteLine($"  {what}: {row[colIndex]} → {stringValue}");
                    row[colIndex] = stringValue;

                    if (editsCount < 20)
                    {
                        edit.WriteRange((rowIndex, colIndex), new List<List<string>> { new() { stringValue } });
                        editsCount++;
                    }
                    else
                    {
                        edit.Execute();
                        editsCount = 0;
                    }
                }
            }
            Update(usernameColIndex, "@", user.username, "username");
            Update(phoneColIndex, "+ ", user.phone, "phone");
            Update(tgIdColIndex, "", user.id, "ID");
        }
    }
    if (editsCount > 0)
        edit.Execute();
}

async Task<User?> FindByUsername(Client client, string username)
{
    if (string.IsNullOrWhiteSpace(username)) return null;
    try
    {
        var res = await client.Contacts_ResolveUsername(username.Replace("@", ""));
        return res.User;
    }
    catch (RpcException e)
    {
        if (e.Code == 420)
        {
            Console.WriteLine("Sleep 5 sec...");
            Thread.Sleep(5000);
            return await FindByUsername(client, username);
        }
        return null;
    }
}

async Task<User?> FindByPhone(Client client, string phone)
{
    if (string.IsNullOrWhiteSpace(phone)) return null;
    try
    {
        var res = await client.Contacts_ResolvePhone(phone);
        return res.User;
    }
    catch (RpcException e)
    {
        if (e.Code == 420)
        {
            Console.WriteLine("Sleep 5 sec...");
            Thread.Sleep(5000);
            return await FindByPhone(client, phone);
        }

        return null;
    }
}

async Task ImportContacts()
{
    var settings = new Settings();
    var repo = new BotDataRepository(settings);
    var data = repo.Load();
    Console.WriteLine($"Loaded {data.AllContacts.Count()}");
    using var client = new Client(settings.TgClientConfig);
    await client.LoginUserIfNeeded();

    foreach (var person in data.AllContacts.Where(c => c.Contact.AdmissionYear == 2022))
    {
        var contact = person.Contact;
        if (string.IsNullOrWhiteSpace(contact.Phone)) continue;
        Console.WriteLine($"Phone: {contact.Phone}");
        try
        {
            var resolvedPeer = await client.Contacts_ResolvePhone(contact.Phone);
            Console.WriteLine("ID " + resolvedPeer.User.ID);
            await client.Contacts_AddContact(resolvedPeer.User, contact.FirstName + " фт22", contact.LastName,
                contact.Phone, true);
            Console.WriteLine($"Resolved: {resolvedPeer.User.username} {contact}");
        }
        catch (TL.RpcException)
        {
            Console.WriteLine("NotFound");
        }
    }
}

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
    var users = await extractor.ExtractUsersFromChatsAndChannels(client, "ФИИТ 20");

    var sb = new StringBuilder();
    foreach (var user in users)
        sb.AppendLine($"{user.username ?? "-"};{user.id};{user.first_name};{user.last_name};{user.phone}");

    File.WriteAllText("contacts.csv", sb.ToString());
    Console.WriteLine(sb.ToString());
}