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
    public async Task FindPhoto()
    {
        var dir = new NamedPhotoDirectory(new Settings().PhotoListUrl);
        var photo = await dir.FindPhoto("Волкова", "Александра");
        Assert.IsNotNull(photo);
        Console.WriteLine(photo.PhotoUri);
    }
}
