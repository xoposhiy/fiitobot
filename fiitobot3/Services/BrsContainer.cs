using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class BrsContainer
    {
        [JsonProperty("disciplineLoad")]
        public string DisciplineLoad { get; set; }

        [JsonProperty("groupId")]
        public string GroupId { get; set; }

        [JsonProperty("discipline")]
        public string Discipline { get; set; }

        [JsonProperty("groupHistoryId")]
        public string GroupHistoryId { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        public override string ToString()
        {
            return $"{Group} {Discipline} load: {DisciplineLoad} groupId:{GroupId}";
        }
    }
}
