using System.Linq;
using NUnit.Framework;

namespace tests;

public class SheetContactsRepositoryTests
{
    [Test]
    public void GetContacts()
    {
        var repo = new SheetContactsRepositoryBuilder().Build();
        var contacts = repo.FindContacts("Мизурова");
        Assert.That(contacts.Select(c => c.ToString()), Is.EqualTo(new[]{ "Дарья Мизурова @udarenienao 450998862" }));
    }
}