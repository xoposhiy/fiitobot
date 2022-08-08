using System;
using System.Linq;
using fiitobot;
using fiitobot.Services;
using NUnit.Framework;

namespace tests;

[TestFixture]
public class BotDataRepoTests
{
    [Test]
    [Explicit]
    public void Test()
    {
        var data = new BotDataRepository(new Settings()).GetData();
        var me = data.AllContacts.Where(s => s.Contact.LastName == "Егоров").ToList();
        Console.WriteLine(me[0].Contact.Type);
    }

}