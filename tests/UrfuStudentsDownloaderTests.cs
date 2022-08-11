using System;
using System.Threading.Tasks;
using FakeItEasy;
using fiitobot;
using fiitobot.GoogleSpreadsheet;
using fiitobot.Services;
using fiitobot.Services.Commands;
using NUnit.Framework;

namespace tests;

public class UrfuStudentsDownloaderTests
{
    [Test]
    public async Task TestDownloader()
    {
        var downloader = new UrfuStudentsDownloader(new Settings());
        var students = await downloader.Download("МЕН-2108");
        foreach (var s in students)
        {
            Console.WriteLine($@"{s.Name} {s.GroupName} {s.Status}");
        }
        Assert.IsNotEmpty(students);
    }

    [Test]
    public async Task TestCommandHandler()
    {
        var settings = new Settings();
        var downloader = new UrfuStudentsDownloader(settings);
        var presenter = A.Fake<IPresenter>();
        A.CallTo(() => presenter.Say(null, 0))
            .WithAnyArguments()
            .Invokes(call =>
            {
                Console.WriteLine(call.Arguments[0]);
            });
        var command = new UpdateStudentStatusesFromItsCommandHandler(presenter, downloader, new BotDataRepository(settings), new SheetContactsRepository(new GSheetClient(settings.GoogleAuthJson), settings.SpreadSheetId));
        await command.HandlePlainText("", 0, null);
    }

}
