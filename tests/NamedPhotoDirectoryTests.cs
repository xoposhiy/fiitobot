using System;
using System.Threading.Tasks;
using fiitobot;
using fiitobot.Services;
using NUnit.Framework;

namespace tests;

[TestFixture]
public class NamedPhotoDirectoryTests
{
    [Test]
    [Explicit]
    public async Task Test()
    {
        var dir = new NamedPhotoDirectory(new Settings().PhotoListUrl);
        var photo = await dir.FindPhoto("Волкова", "Александра");
        Console.WriteLine(photo.PhotoUri);
    }
}
