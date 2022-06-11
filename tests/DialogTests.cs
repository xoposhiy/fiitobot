using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using fiitobot;
using fiitobot.Services;
using NUnit.Framework;

namespace tests;

public class DialogTests
{
    private BotData? data;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        data = new BotDataBuilder().Build();
    }
    
    [TestCase("Мизурова")]
    [TestCase("Мизуро́ва")]
    [TestCase("Анастасия Мизурова")]
    [TestCase("Мизурова Анастасия")]
    [TestCase("username42")]
    [TestCase("@username42")]
    public async Task PlainTextMessage_SearchesStudent(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);
        
        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Student);
        
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.That.Matches(c => c.LastName == "Мизуро́ва"), 
                123, AccessRight.Student))
            .MustHaveHappenedOnceExactly();
    }

    [TestCase("/random")]
    public async Task Random_ShowsSingleContact(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Student);

        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, AccessRight.Student))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    [TestCase("/me")]
    public async Task Me_ShowsSingleContact(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123123123, AccessRight.Student);

        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.That.Matches(c => c.ToString() == "Иван Иванов @username 123123123"),
                123123123, AccessRight.Student))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => contactsPresenter.Say(A<string>.Ignored, 123123123)).MustHaveHappenedOnceExactly();
        Assert.AreEqual(2, Fake.GetCalls(contactsPresenter).Count());
    }

    [TestCase("Мизурова")]
    [Ignore("Нужен тестовый PhotoRepo")]
    public async Task AdminQuery_ShowsPhoto(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Admin);

        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, AccessRight.Admin))
            .MustHaveHappenedOnceExactly();
    }

    [TestCase("asstudent Мизурова")]
    public async Task AsStudent_DowngradeAdminRights(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Admin);

        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, AccessRight.Student))
            .MustHaveHappenedOnceExactly();
    }


    [TestCase("Мизурова Анастасия Неизвестнокакоевна")]
    public async Task PlainTextMessage_SearchesStudent_IgnoringSomeParts(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);
        
        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Student);
        
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.That.Matches(c => c.LastName == "Мизуро́ва"), 
                123, AccessRight.Student))
            .MustHaveHappenedOnceExactly();
    }
    
    [TestCase("Иван")]
    public async Task PlainTextMessage_SearchesManyStudents(string firstName)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);
        
        await handleUpdateService.HandlePlainText(firstName, 42, AccessRight.Student);
        
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.That.Matches(c => c.FirstName == firstName), 
                42, AccessRight.Student))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => contactsPresenter.ShowOtherResults(
                A<Contact[]>.That.Matches(c => c.Length == 1),
                42))
            .MustHaveHappenedOnceExactly();
    }
    
    private HandleUpdateService PrepareUpdateService(IPresenter presenter)
    {
        return new HandleUpdateService(null, null, data, null, presenter);
    }
}