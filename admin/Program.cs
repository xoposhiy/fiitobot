using fiitobot;
using fiitobot.Services;
using fiitobot.Services.Commands;

// ReSharper disable HeuristicUnreachableCode

var settings = new Settings();

var demidovich = new DemidovichService(settings.CreateDemidovichBucketService());
var count = 0;
foreach (var file in Directory.EnumerateFiles(@"c:\work\DemidovichBot\images\Demidovich", "*.gif"))
{
    var exerciseNumber = Path.GetFileNameWithoutExtension(file);
    var exists = await demidovich.HasImage(Path.GetFileNameWithoutExtension(file));
    if (!exists)
        Console.WriteLine(exerciseNumber);
    count++;
    if (count % 100 == 0)
        Console.WriteLine($"processed {count}");
}