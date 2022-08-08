using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using System.Text;
using TL;
using WTelegram;

//await ImportContacts();
//await ReportUnknowns("Чат ФИИТ 2022");
await ReportMissingInChat(2022, "Чат ФИИТ 2022");
//await ExtractChats();
//await ExtractStudents();
//await ActualizeContacts("https://docs.google.com/spreadsheets/d/1VH_pZnYvTgQ-IzFVs5CYQWjbCo6YtUfip4w7K6GTK3U/edit#gid=0", "Чат ФИИТ");


async Task ReportMissingInChat(int admissionYear, string chatName)
{
    var settings = new Settings();
    var repo = new BotDataRepository(settings);
    var data = repo.GetData();
    Console.WriteLine($"Loaded {data.AllContacts.Count()}");
    using var client = new Client(settings.TgClientConfig);
    var defaultLogger = Helpers.Log;
    Helpers.Log = (level, message) =>
    {
        if (level >= 3) defaultLogger(level, message);
    };

    await client.LoginUserIfNeeded();
    var extractor = new TgContactsExtractor();

    var users = (await extractor.ExtractUsersFromChatsAndChannels(client, chatName)).ToDictionary(u => u.ID, u => u);
    Console.WriteLine($"FOUND {users.Count} users in chat {chatName}");
    var missing = 0;
    foreach (var person in data.Students.Where(s => s.Contact.AdmissionYear == admissionYear))
    {
        var contact = person.Contact;
        var tgId = contact.TgId;
        if (tgId == -1)
        {
            missing++;
            Console.WriteLine($"No telegramId for {contact.FirstName} {contact.LastName} {contact.Concurs}");
        }
        else
        {
            if (!users.TryGetValue(tgId, out var u))
            {
                missing++;
                Console.WriteLine($"MISSING {contact.FirstName} {contact.LastName} {contact.Concurs}");
            }
        }
    }
    Console.WriteLine(missing);
}

async Task ReportUnknowns(string chatName)
{
    var settings = new Settings();
    var repo = new BotDataRepository(settings);
    var data = repo.GetData();
    Console.WriteLine($"Loaded {data.AllContacts.Count()}");
    using var client = new Client(settings.TgClientConfig);
    var defaultLogger = Helpers.Log;
    Helpers.Log = (level, message) =>
    {
        if (level >= 3) defaultLogger(level, message);
    };

    await client.LoginUserIfNeeded();
    var extractor = new TgContactsExtractor();

    var users = (await extractor.ExtractUsersFromChatsAndChannels(client, chatName));
    Console.WriteLine($"FOUND {users.Count} users in chat {chatName}");
    var countByYear = new Dictionary<int, int>();
    foreach (var user in users)
    {
        var match = data.AllContacts.FirstOrDefault(c => c.Contact.TgId == user.ID);
        if (match == null)
            Console.WriteLine($"{user.last_name};{user.first_name};;;;{user.phone};;{user.username};{user.ID}");
        else
        {
            var admissionYear = match.Contact.AdmissionYear;
            var newCount = countByYear.GetOrDefault(admissionYear) + 1;
            countByYear[admissionYear] = newCount;
        }
    }

    Console.WriteLine("По годам поступления:");
    foreach (var kv in countByYear.OrderBy(kv => kv.Key))
    {
        Console.WriteLine(kv.Key + "\t" + kv.Value + "\t из " + data.AllContacts.Count(c => c.Contact.AdmissionYear == kv.Key));
    }
}
async Task ActualizeContacts(string spreadsheetUrl, params string[] chatSubstrings)
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
                //rollingLog.Enqueue(message);
                //while (rollingLog.Count > 10) rollingLog.Dequeue();
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
    Dictionary<long, User> users = (await extractor.ExtractUsersFromChatsAndChannels(tgClient, chatSubstrings)).ToDictionary(u => u.ID, u => u);

    Console.WriteLine($"Tg Users Found: {users.Count}");
    Console.WriteLine($"Contacts from Google Sheet: {data.Count - 1}");
    var edit = sheet.Edit();
    var editsCount = 0;
    for (var rowIndex = 1; rowIndex < data.Count; rowIndex++)
    {
        var row = data[rowIndex];
        while (row.Count <= Math.Max(usernameColIndex, Math.Max(tgIdColIndex, phoneColIndex)))
            row.Add("");
        var username = row[usernameColIndex];
        var phone = row[phoneColIndex];
        var tgId = long.TryParse(row[tgIdColIndex], out var v) ? v : -1;
        //Console.WriteLine($"{string.Join(";", row)}...");
        var user = await FindByPhone(tgClient, phone) ?? 
                   (users.TryGetValue(tgId, out var u) ? u 
                     : await FindByUsername(tgClient, username));
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
                        edit = edit.WriteRangeNoCasts((rowIndex, colIndex), new List<List<object>> { new() { stringValue } });
                        editsCount++;
                    }
                    else
                    {
                        edit.Execute();
                        editsCount = 0;
                        edit = sheet.Edit();
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
        Console.Write("SEARCH BY USERNAME " + username + "... ");
        var res = await client.Contacts_ResolveUsername(username.Replace("@", ""));
        Console.WriteLine("FOUND!!!");
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
    finally
    {
        Console.WriteLine();
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
        Console.WriteLine("NO PHONE " + phone);
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
    var data = repo.GetData();
    Console.WriteLine($"Loaded {data.AllContacts.Count()}");
    using var client = new Client(settings.TgClientConfig);
    var defaultLogger = Helpers.Log;
    Helpers.Log = (level, message) =>
    {
        if (level >= 3) defaultLogger(level, message);
    };

    await client.LoginUserIfNeeded();
    var extractor = new TgContactsExtractor();

    Dictionary<long, User> users = (await extractor.ExtractUsersFromChatsAndChannels(client, "Чат ФИИТ", "спроси про ФИИТ", "Чат — Матмех, приём!")).ToDictionary(u => u.ID, u => u);
    var myContacts = await client.Contacts_GetContacts();

    foreach (var contact in data.Students.Where(c => c.Contact.AdmissionYear.IsOneOf(2022)).Select(p => p.Contact))
    {
        var suffix = contact.AdmissionYear <= 0 ? "" : (" фт" + contact.AdmissionYear % 100);
        if (myContacts.users.ContainsKey(contact.TgId))
        {
            var myTgContact = myContacts.users[contact.TgId];
            if (suffix == "")
            {
                if (!myTgContact.first_name.EndsWith(" фт22")) continue;
            }
            else
                if (myTgContact.first_name.EndsWith(suffix)) continue;
        } 
        //Console.WriteLine($"{contact}");

        var resolvedPeer = await FindByPhone(client, contact.Phone) ??
                           (users.TryGetValue(contact.TgId, out var u) ? u
                               : await FindByUsername(client, contact.Telegram));
        if (resolvedPeer == null)
            Console.WriteLine($"NotFound {contact}");
        else
        {
            Console.WriteLine("AddContact " + resolvedPeer.ID + "\t" + contact.FirstName + suffix + " " + contact.LastName + " @" + resolvedPeer.username);
            await client.Contacts_AddContact(resolvedPeer, contact.FirstName + suffix, contact.LastName,
                contact.Phone, true);
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