using System;
using Newtonsoft.Json;

namespace tgnames
{
    public class TgNamesResponse
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string Username;
        public long? TgId;
        public bool Found;
        public string ErrorMessage;
        public DateTime LastUpdateTimestamp;
    }
}
