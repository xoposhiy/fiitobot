using System;
using System.Threading.Tasks;
using fiitobot;
using fiitobot.Services;
using NUnit.Framework;

namespace tests;

public class MarksReloadTests
{
    [Explicit]
    [TestCase("https://docs.google.com/spreadsheets/d/19TEU4kx6zzFn8iDyESndz-s9NWwOgxECGuLo64z5Eh4/edit#gid=1721109003", TestName="2019")]
    [TestCase("https://docs.google.com/spreadsheets/d/1XFKrFCScUD5APkZFALQp0XXgAuifHVxmwMSt1J2TqE8/edit#gid=1127095955", TestName="2020")]
    [TestCase("https://docs.google.com/spreadsheets/d/13J4JMOVKMfNSQpurVOWx6cvIBBw2mjQkV9F5HnK2KhY/edit#gid=1367147904", TestName="2021")]
    [TestCase("https://docs.google.com/spreadsheets/d/1zzNnJkLK-OQrUhkFHkLa2vmX1C90HxI1xaVLJ9t26WA/edit#gid=282300073", TestName="2022")]
    [TestCase("https://docs.google.com/spreadsheets/d/1-DtIMeBQw-NiR8BweulVoDHDyAL4JLGY3PXucEW5F0U/edit#gid=1305473930", TestName="2023")]
    public async Task Reload(string spreadsheetUrl)
    {
        var settings = new Settings();
        var detailsRepo = new S3ContactsDetailsRepo(settings.CreateFiitobotBucketService());
        var dataRepo = new BotDataRepository(settings);
        var reloadService = new MarksReloadService(dataRepo, detailsRepo, new GSheetClientBuilder().Build());
        var updatedCount = await reloadService.ReloadFrom(spreadsheetUrl);
        Console.WriteLine(updatedCount + " updates");
    }
}
