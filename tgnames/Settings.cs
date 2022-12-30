using System.Collections.Generic;

namespace tgnames
{
    // Create Settings.Production.cs file with concrete values set in default constructor.
    public partial class Settings : IYdbSettings
    {
        public Dictionary<string, string> ApiKeys = new Dictionary<string, string>();
        public string YdbEndpoint { get; }
        public string YdbDatabase { get; }
        public string YandexCloudKeyFile { get; }
        public string AccessToken { get; set; }
    }
}
