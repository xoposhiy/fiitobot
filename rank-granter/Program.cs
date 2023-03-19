using fiitobot;
using fiitobot.Services;
using WTelegram;

var settings = new Settings();
using var client = new Client(settings.TgClientConfig);
var dataRepo = new BotDataRepository(settings);
await new TgRankGranter().GrantStudentRanks(client, dataRepo);
