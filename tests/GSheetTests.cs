using System;
using NUnit.Framework;

namespace tests;

public class GSheetTests
{
    [Test]
    [Explicit]
    public void GetByUrl()
    {
        var client = new GSheetClientBuilder().Build();
        var sheet = client.GetSheetByUrl(
            "https://docs.google.com/spreadsheets/d/1XFKrFCScUD5APkZFALQp0XXgAuifHVxmwMSt1J2TqE8/edit#gid=419729714");
        var data = sheet.ReadRange("A:ZZ");
        foreach (var line in data)
        {
            Console.WriteLine(string.Join("; ", line));
        }

    }
}
