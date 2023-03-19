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
            var dataRepo = new BotDataRepository(settings);
            var detailsRepo = new S3ContactsDetailsRepo(settings.CreateFiitobotBucketService());
            return new SheetContactsRepository(client, settings.SpreadSheetId, dataRepo, detailsRepo);
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
