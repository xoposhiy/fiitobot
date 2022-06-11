using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;

namespace tests
{
    public class SheetContactsRepositoryBuilder
    {
        public SheetContactsRepository Build()
        {
            var settings = new Settings();
            var client = new GSheetClient(settings.GoogleAuthJson);
            return new SheetContactsRepository(client, settings.SpreadSheetId);
        }
    }

    public class GSheetClientBuilder
    {
        public GSheetClient Build()
        {
            var settings = new Settings();
            return new GSheetClient(settings.GoogleAuthJson);
        }
    }
}