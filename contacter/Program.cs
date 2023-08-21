using System.Text;
using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using TL;
using WTelegram;
using Contact = fiitobot.Contact;

#pragma warning disable CS8321

//await AnalyzeFiitobotLogs("фиитобот");

//await ImportContacts(2023);
await AnalyzeStudentsChat(2023, "Чат ФИИТ 2023");

//await ActualizeContacts(true, "https://docs.google.com/spreadsheets/d/1VH_pZnYvTgQ-IzFVs5CYQWjbCo6YtUfip4w7K6GTK3U/edit#gid=0", "Чат ФИИТ 2023");
//await ReportActiveStudents("Чат ФИИТ 2023");
//await AddToChat("Чат ФИИТ 2023", "phones2023.csv");
//await ExtractChats();
//await ExtractStudents();

// Массовое добавление студентов в чат по их телефонам.
// phone csv - список телефонов зачисленных, который добыт во время приёмки. В каждой строке телефон, TAB, и что угодно ещё.
// В проекте есть пример файла.
async Task AddToChat(string chatSubstring, string phonesCsv)
{
    var settings = new Settings();
    using var client = new Client(settings.TgClientConfig);
    var defaultLogger = Helpers.Log;
    Helpers.Log = (level, message) =>
    {
        if (level >= 3) defaultLogger(level, message);
    };

    await client.LoginUserIfNeeded();
    var chats = await client.Messages_GetAllChats();
    var chat = chats.chats.First(c => c.Value.Title.Contains(chatSubstring)).Value;
    Console.WriteLine($"Found Chat {chat.Title} {chat.ID} {chat.ToInputPeer()} {chat.GetType()}");

    var lines = await File.ReadAllLinesAsync(phonesCsv);
    var count = 0;
    Console.WriteLine("Spreadsheet copy-paste friendly formatting:");
    foreach (var line in lines)
    {
        Console.Write(line + "\t");
        var phone = line.Split('\t', 1)[0];
        var user = await FindByPhone(client, phone);
        if (user != null)
        {
            Console.Write($"{user.MainUsername}\t{user.ID}\t");
            try
            {
                var _ = await client.AddChatUser(chat, user);
                Console.WriteLine("added");
                count++;
            }
            catch
            {
                // TODO: If error because of no access — send invite link via personal message
                //await client.SendMessageAsync(user, "TODO friendly text and invite link");
                Console.WriteLine("NO ACCESS");
            }
        }
        else
        {
            //skip username and tgId
            Console.Write("\t\t");
            Console.WriteLine("NOT FOUND");
        }
    }

    Console.WriteLine(count + " users added to chat");
}

async Task AnalyzeFiitobotLogs()
{
    var settings = new Settings();
    using var client = new Client(settings.TgClientConfig);
    var defaultLogger = Helpers.Log;
    Helpers.Log = (level, message) =>
    {
        if (level >= 3) defaultLogger(level, message);
    };

    await client.LoginUserIfNeeded();
    var topPeers = (Contacts_TopPeers)await client.Contacts_GetTopPeers(bots_pm: true, bots_inline: true);
    var fiitobot = topPeers.users.First(c => c.Value.username == "fiitobot").Value;
    Console.WriteLine($"Found User {fiitobot.MainUsername} {fiitobot.ID} {fiitobot.GetType()}");
    var lastMessageId = 0;
    var usersFreq = new Dictionary<string, int>();
    var requestsFreq = new Dictionary<string, int>();
    var messagesCount = 0;
    while (true)
    {
        var history = await client.Messages_GetHistory(fiitobot, lastMessageId);
        if (history.Messages.Length == 0) break;
        var texts = history.Messages.OfType<Message>().Select(m => m.message)
            .Where(t => t.StartsWith("From: "))
            .Select(t => t.Substring("From: ".Length)).ToList();
        foreach (var text in texts)
        {
            var parts = text.Split(" Message: ");
            if (parts.Length < 2) continue;
            var who = parts[0];
            var what = parts[1];
            usersFreq.Increment(who);
            requestsFreq.Increment(what.ToLower().Trim());
        }

        lastMessageId = history.Messages.Last().ID;
        messagesCount += history.Messages.Length;
        Console.WriteLine($"Read {messagesCount} messages. Last message date {history.Messages.Last().Date}");
        if (history.Messages.Last().Date < DateTime.Now - TimeSpan.FromDays(30)) break;
    }

    Console.WriteLine("Top Users:");
    Console.WriteLine(usersFreq.ToFrequencyString(20));
    File.WriteAllText("users.txt", usersFreq.ToFrequencyString());
    Console.WriteLine();
    Console.WriteLine("Top Requests:");
    Console.WriteLine(requestsFreq.ToFrequencyString(20));
    File.WriteAllText("requests.txt", requestsFreq.ToFrequencyString());
}


#pragma warning disable CS8321 // Local function is declared but never used


async Task ReportActiveStudents(string chatTitle)
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
    var chats = await client.Messages_GetAllChats();
    var chat = chats.chats.FirstOrDefault(c => c.Value.Title.Contains(chatTitle)).Value;
    Console.WriteLine($"Found Chat {chat.Title} {chat.ID} {chat.ToInputPeer()} {chat.GetType()}");
    var channel = (InputPeerChannel)chat.ToInputPeer();

    var lastMessageId = 0;
    var messagesCount = 0;
    var activity = new Dictionary<long, int>();
    while (true)
    {
        var history = await client.Messages_GetHistory(channel, lastMessageId);
        if (history.Messages.Length == 0) break;
        foreach (var m in history.Messages)
            if (m.From is PeerUser)
            {
                var id = m.From.ID;
                activity[id] = activity.GetOrDefault(id) + 1;
            }

        lastMessageId = history.Messages.Last().ID;
        messagesCount += history.Messages.Length;
        Console.WriteLine($"Read {messagesCount} messages. Last message date {history.Messages.Last().Date}");
        if (history.Messages.Last().Date < DateTime.Now - TimeSpan.FromDays(90)) break;
    }

    var activeStudents = data.Students
        .Select(p => (Contact: (Contact)p, MessagesCount: activity.GetOrDefault(((Contact)p).TgId)))
        .OrderByDescending(c => c.MessagesCount).ToList();
    foreach (var s in activeStudents)
        Console.WriteLine($"{s.MessagesCount},{s.Contact.AdmissionYear},{s.Contact.FirstLastName()}");
}

async Task AnalyzeStudentsChat(int admissionYear, string chatName)
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
    var countByYear = new Dictionary<int, int>();
    Console.WriteLine("# UNKNOWN USERS IN CHAT:");
    foreach (var user in users.Values)
    {
        var match = data.AllContacts.FirstOrDefault(c => ((Contact)c).TgId == user.ID);
        if (match == null)
        {
            Console.WriteLine($"UNKNOWN {user.last_name}; {user.first_name} {user.phone} {user.username} {user.ID}");
        }
        else
        {
            var year = match.AdmissionYear;
            var newCount = countByYear.GetOrDefault(year) + 1;
            countByYear[year] = newCount;
        }
    }

    Console.WriteLine();
    Console.WriteLine("# MISSING USERS IN CHAT:");
    foreach (var person in data.Students.Where(s =>
                 ((Contact)s).AdmissionYear == admissionYear && s.Status.IsOneOf("", "Активный")))
    {
        var contact = (Contact)person;
        var tgId = contact.TgId;
        if (tgId == -1)
        {
            Console.WriteLine($"NO TGID {contact.FirstName} {contact.LastName} {contact.Concurs}");
        }
        else
        {
            if (!users.TryGetValue(tgId, out var _))
            {
                missing++;
                Console.WriteLine($"MISSING {contact.FirstName} {contact.LastName} {contact.Concurs}");
            }
        }
    }

    Console.WriteLine("MISSING COUNT: " + missing);
    Console.WriteLine();
    Console.WriteLine("# ADMISSION YEAR STATISTICS:");
    foreach (var kv in countByYear.OrderBy(kv => kv.Key))
        Console.WriteLine(kv.Key + "\t" + kv.Value + "\t из " +
                          data.AllContacts.Count(c => ((Contact)c).AdmissionYear == kv.Key));
}

async Task ActualizeContacts(bool actualizeOnlyWithoutTgId, string spreadsheetUrl, params string[] chatSubstrings)
{
    var settings = new Settings();
    var gsClient = new GSheetClient(settings.GoogleAuthJson);
    var sheet = gsClient.GetSheetByUrl(spreadsheetUrl);
    var data = sheet.ReadRange("A1:V");
    var headersRow = data[0];
    var usernameColIndex = headersRow.IndexOf("Telegram");
    var phoneColIndex = headersRow.IndexOf("Phone");
    var tgIdColIndex = headersRow.IndexOf("TgId");
    var tgClient = new Client(settings.TgClientConfig);
    tgClient.FloodRetryThreshold = 120;
    var defaultLogger = Helpers.Log;
    Helpers.Log = (level, message) =>
    {
        if (level < 3) return;
        defaultLogger(level, message);
    };

    await tgClient.LoginUserIfNeeded();
    var extractor = new TgContactsExtractor();
    Dictionary<long, User> users =
        (await extractor.ExtractUsersFromChatsAndChannels(tgClient, chatSubstrings)).ToDictionary(u => u.ID, u => u);

    Console.WriteLine($"Tg Users Found: {users.Count}");
    Console.WriteLine($"Contacts from Google Sheet: {data.Count - 1}");
    var edit = sheet.Edit();
    var editsCount = 0;
    for (var rowIndex = 408; rowIndex < data.Count; rowIndex++)
    {
        var row = data[rowIndex];
        while (row.Count <= Math.Max(usernameColIndex, Math.Max(tgIdColIndex, phoneColIndex)))
            row.Add("");
        var username = row[usernameColIndex];
        var phone = row[phoneColIndex];
        var tgId = long.TryParse(row[tgIdColIndex], out var v) ? v : -1;
        if (actualizeOnlyWithoutTgId && tgId != -1) continue;
        //Console.WriteLine($"{string.Join(";", row)}...");
        var user = await FindByPhone(tgClient, phone) ??
                   (users.TryGetValue(tgId, out var u)
                       ? u
                       : await FindByUsername(tgClient, username));
        if (user == null)
        {
            Console.WriteLine("  no telegram user found");
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

                    edit = edit.WriteRangeNoCasts((rowIndex, colIndex),
                        new List<List<object>> { new() { stringValue } });
                    editsCount++;
                    if (editsCount > 20)
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
        Console.WriteLine("FOUND!!! " + res.User.username);
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

async Task ImportContacts(int year)
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

    var myContacts = await client.Contacts_GetContacts();
    Console.WriteLine("Contacts: " + myContacts.users.Count);
    Dictionary<long, User> users =
        (await extractor.ExtractUsersFromChatsAndChannels(client, $"Чат ФИИТ {year}")).ToDictionary(u => u.ID, u => u);

    foreach (var contact in data.Students.Where(c => ((Contact)c).AdmissionYear.IsOneOf(year)).Select(p => (Contact)p))
    {
        Console.WriteLine(contact.Telegram);
        if (myContacts.users.ContainsKey(contact.TgId)) continue;
        var suffix = contact.AdmissionYear <= 0 ? "" : " фт" + contact.AdmissionYear % 100;
        var resolvedPeer = await FindByUsername(client, contact.Telegram) ??
                           (users.TryGetValue(contact.TgId, out var u)
                               ? u
                               : await FindByPhone(client, contact.Phone));
        if (resolvedPeer == null)
        {
            Console.WriteLine($"NotFound {contact}");
        }
        else
        {
            Console.WriteLine("AddContact " + resolvedPeer.ID + "\t" + contact.FirstName + suffix + " " +
                              contact.LastName + " @" + resolvedPeer.username);
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