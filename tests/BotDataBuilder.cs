using System.IO;
using fiitobot;
using Newtonsoft.Json;

namespace tests;

public class BotDataBuilder
{
    public BotData Build()
    {
        return JsonConvert.DeserializeObject<BotData>(File.ReadAllText("botData.json"));
    }
}
