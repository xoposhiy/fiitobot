using fiitobot;
using Newtonsoft.Json;
using tests.Properties;

namespace tests;

public class BotDataBuilder
{
    public BotData Build()
    {
        return JsonConvert.DeserializeObject<BotData>(Resources.botData);
    }
}
