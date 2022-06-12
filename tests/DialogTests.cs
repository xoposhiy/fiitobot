using System.Collections.Generic;
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
    [TestCase("Мизурова Анастасия Лишние Слова")]
    public async Task SearchesStudent(string query)
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
    public async Task AdminQuery_ShowsPhoto(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var photoRepo = A.Fake<IPhotoRepository>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter, photoRepo);

        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Admin);

        A.CallTo(() => photoRepo.FindRandomPhoto(A<Contact>.Ignored))
            .Returns(Task.FromResult(new PersonPhoto(null, null, null)));
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, AccessRight.Admin))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => contactsPresenter.ShowPhoto(
                A<Contact>.Ignored, A<PersonPhoto>.Ignored, 123, AccessRight.Admin))
            .MustHaveHappenedOnceExactly();
    }
    
    [TestCase("Досье Мизурова")]
    public async Task AdminDetailsQuery_ShowsDetails(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Admin);

        A.CallTo(() => contactsPresenter.ShowDetails(
            A<PersonData>.Ignored, data!.SourceSpreadsheets, 123))
            .MustHaveHappenedOnceExactly();
    }

    [TestCase("Досье Мизурова")]
    public async Task DoNotShowDetailsToStudents(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, AccessRight.Student);

        A.CallTo(() => contactsPresenter.ShowDetails(
                null, null, 123))
            .WithAnyArguments()
             .MustNotHaveHappened();
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

    [TestCase("Иван")]
    public async Task ShowOtherStudentsList_IfMoreThanOneResult(string firstName)
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

    [TestCase("Abracadabra")]
    public async Task ShowNoResults(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 42, AccessRight.Student);

        A.CallTo(() => contactsPresenter.SayNoResults(42))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    [TestCase("Иван")]
    public async Task ExternalUsers_HasNoAccess(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 42, AccessRight.External);

        A.CallTo(() => contactsPresenter.SayNoRights(42, AccessRight.External))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    [TestCase("Гимназия 9")]
    [TestCase("9")]
    [TestCase("Гимназия 99")]
    [TestCase("тагил")]
    [TestCase("Нижний Тагил")]
    public async Task SearchBySchoolOrCity(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 42, AccessRight.Student);

        A.CallTo(() => 
                contactsPresenter.ShowContactsBy(
                    A<string>.Ignored, 
                    A<IList<Contact>>.That.Matches(p => p.Count == 1), 
                    42, 
                    AccessRight.Student))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    private HandleUpdateService PrepareUpdateService(IPresenter presenter, IPhotoRepository? photoRepository = null)
    {
        return new HandleUpdateService(null, null, data, photoRepository, presenter);
    }
}