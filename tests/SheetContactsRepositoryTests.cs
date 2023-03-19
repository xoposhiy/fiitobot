using System;
using NUnit.Framework;

namespace tests;

public class SheetContactsRepositoryTests
{
    [Test]
    [Explicit]
    public void TestGetAllContacts()
    {
        var contactsRepository = new SheetContactsRepositoryBuilder().Build();
        Console.WriteLine(contactsRepository.GetStudents().Length);
    }
}
