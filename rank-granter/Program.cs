using fiitobot;
using fiitobot.Services;
using WTelegram;

var settings = new Settings();
using var client = new Client(settings.TgClientConfig);
client.FloodRetryThreshold = 120;
var defaultLogger = Helpers.Log;
Helpers.Log = (level, message) =>
{
    if (level < 3) return;
    defaultLogger(level, message);
};
var dataRepo = new BotDataRepository(settings);
await new TgRankGranter().GrantStudentRanks(client, dataRepo);
Console.WriteLine("Press any key to exit");
Console.ReadLine();
