using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiitobot;
using fiitobot.Services;
using NUnit.Framework;

namespace tests;

[TestFixture]
public class BrsClientTests
{
    [Test]
    [Explicit]
    public async Task GetContainers()
    {
        AbstractBrsClient client = new BrsClient();
        var sessionId = "04D858DFD28FEAE7C75B9968CF3B5256";
        var containers = await client.GetContainers(sessionId, 2022, 3, 1);
        foreach (var brsContainer in containers)
        {
            Console.WriteLine(brsContainer);
        }
        Assert.AreEqual(28, containers.Count);

        var container = containers[0];
        var marks = await client.GetTotalMarks(sessionId, container);
        foreach (var studentMark in marks)
        {
            Console.WriteLine(studentMark);
        }
        Assert.AreEqual(16, marks.Count);
    }
    [Test]
    [Explicit]
    public async Task GetAllMarks()
    {
        AbstractBrsClient client = new BrsClient();
        var sessionId = "04D858DFD28FEAE7C75B9968CF3B5256";
        var marks = await client.GetTotalMarks(sessionId, 2022, 3, 1);
        var students = marks.GroupBy(m => (Fio:m.StudentFio, Group:m.StudentGroup));
        var tsv = new StringBuilder();
        foreach (var group in students.GroupBy(s => s.Key.Group))
        {
            Console.WriteLine(group.Key);
            foreach (var student in group)
            {
                Console.WriteLine("  " + student.Key.Fio);
                foreach (var mark in student)
                {
                    if (mark.Mark is "Не должен сдавать" or "Не выбрана")
                        continue;
                    Console.WriteLine($"    * {mark.Total} {mark.ModuleTitle}");
                    tsv.AppendLine(
                        string.Join(
                            "\t",
                            mark.StudentGroup, mark.StudentFio, mark.ModuleTitle, mark.Mark, mark.Total));
                }
            }
        }
        await File.WriteAllTextAsync("marks.tsv", tsv.ToString());
    }
}


[TestFixture]
public class BotDataRepoTests
{
    [Test]
    [Explicit]
    public void Test()
    {
        var data = new BotDataRepository(new Settings()).GetData();
        var students = data.Students.Where(s => s.Status.IsOneOf("Активный", "")).ToList();
        Console.WriteLine(students.Count);
    }

}
