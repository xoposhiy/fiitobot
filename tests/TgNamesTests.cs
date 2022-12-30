using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using tgnames;

namespace tests;

public class TgNamesTests
{
    [Test]
    public async Task NamesRepo()
    {
        var settings = new Settings();
        var repo = new NamesRepo(settings);
        await repo.Save(123, "me");
        var user = await repo.SearchByTgId(123);
        Assert.That(user.Username, Is.EqualTo("me"));
        var user2 = await repo.SearchByUsername("me");
        Assert.That(user.Id, Is.EqualTo(123));
    }

    [Test]
    public void Request_WithUnknownApiKey_ShouldGiveError()
    {
        var client = CreateClient_WithBadApiKey();
        Assert.That(() => client.Request("asdad", null), Throws.Exception.With.Message.EqualTo("Unknown Api Key"));
    }

    [Test]
    public void Request_UnknownUsername_ShouldGiveFoundFalse()
    {
        var client = CreateClient();
        var res = client.Request("unknownUsername_asfasfas", null);
        Console.WriteLine(res);
        Assert.That(res.Found, Is.False);
    }

    [Test]
    public void Request_KnownUsername_ShouldGiveFoundTrue()
    {
        var client = CreateClient();
        var saveRes = client.Request("xoposhiy", 123123123);
        Assert.That(saveRes.ErrorMessage, Is.Null);

        var res = client.Request(null, 123123123);
        Assert.That(res.ToString(), Is.EqualTo(saveRes.ToString()));

        res = client.Request("xoposhiy", null);
        Assert.That(res.ToString(), Is.EqualTo(saveRes.ToString()));

    }

    private static TgNamesClient CreateClient_WithBadApiKey()
    {
        return new TgNamesClient("unknownApiKey", new Uri("https://functions.yandexcloud.net/d4ek1oph2qq118htfcp3"));
    }

    private static TgNamesClient CreateClient()
    {
        return new TgNamesClient(new tgnames.Settings().ApiKeys.First().Key, new Uri("https://functions.yandexcloud.net/d4ek1oph2qq118htfcp3"));
    }
}
