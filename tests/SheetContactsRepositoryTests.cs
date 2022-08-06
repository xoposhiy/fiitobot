using System.Linq;
using NUnit.Framework;

namespace tests;

public class SheetContactsRepositoryTests
{
    [Test]
    public void GetContacts()
    {
        var repo = new SheetContactsRepositoryBuilder().Build();
        var contacts = repo.FindContacts("Мизуро́ва");
        Assert.That(contacts.Select(c => c.ToString()), Is.EqualTo(new[]{ "Дарья Мизуро́ва @udarenienao 450998862" }));
    }
}