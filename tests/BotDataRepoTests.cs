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
    public void TestReadBotData()
    {
        var data = new BotDataRepository(new Settings()).GetData();
        var students = data.Students.Where(s => s.Status.IsOneOf("Активный", "")).ToList();
        Console.WriteLine(students.Count);
        Assert.Greater(students.Count, 0);
    }

}
