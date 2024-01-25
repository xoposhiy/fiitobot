using System;
using fiitobot.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace tests;

public class ContactTests
{
    [Test]
    public void CollectionsAreNotNull_AfterDeserialization()
    {
        var dd = JsonConvert.DeserializeObject<ContactDetails>("{\"ContactId\":42}");
        Assert.IsNotNull(dd!.Spasibki);
        Assert.IsNotNull(dd.DialogState);
        Assert.IsNotNull(dd.Semesters);
        Assert.IsNotNull(dd.Details);
    }
}
