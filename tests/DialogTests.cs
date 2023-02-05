using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using fiitobot;
using fiitobot.Services;
using fiitobot.Services.Commands;
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
    
    [TestCase("Мизурова", "Мизуро́ва")]
    [TestCase("Мизуро́ва", "Мизуро́ва")]
    [TestCase("Анастасия Мизурова", "Мизуро́ва")]
    [TestCase("Мизурова Анастасия", "Мизуро́ва")]
    [TestCase("username42", "Мизуро́ва")]
    [TestCase("@username42", "Мизуро́ва")]
    [TestCase("Мизурова Анастасия Лишние Слова", "Мизуро́ва")]
    [TestCase("Семёнов Иван", "Семёнов")]
    [TestCase("Мавунгу", "Мавунгу Иса Исмагил")]
    public async Task SearchesStudent(string query, string expectedLastNameOfSingleResult)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);
        
        await handleUpdateService.HandlePlainText(query, 123, sender);
        Console.WriteLine(expectedLastNameOfSingleResult);
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.That.Matches(c => c.LastName == expectedLastNameOfSingleResult), 
                123, ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => contactsPresenter.ShowOtherResults(null, 0))
            .WithAnyArguments()
            .MustNotHaveHappened();
    }

    [TestCase("/random")]
    public async Task Random_ShowsSingleContact(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, sender);
        //TODO Проверить, что НЕ одногруппник НЕ видит контакты
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, A<ContactDetailsLevel>.Ignored))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    [TestCase("Мизурова", ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts)]
    [TestCase("Егоров Павел", ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks)]
    [TestCase("Петров Пётр", ContactDetailsLevel.Minimal)]
    [TestCase("Иванов", ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks)]
    public async Task StudentQuery_ShowsPhotoAndContacts_IfSameYear(string query, ContactDetailsLevel expectedDetailsLevel)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var photoRepo = A.Fake<INamedPhotoDirectory>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter, photoRepo);

        await handleUpdateService.HandlePlainText(query, 123, sender);

        A.CallTo(() => photoRepo.FindPhoto(A<Contact>.Ignored))
            .Returns(Task.FromResult(new PersonPhoto(null, null, null)));
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, expectedDetailsLevel))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => contactsPresenter.ShowPhoto(
                A<Contact>.Ignored, A<byte[]>.Ignored, 123))
            .MustHaveHappenedOnceExactly();
    }
    
    [TestCase("/details Мизурова")]
    public async Task AdminDetailsQuery_ShowsDetails(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AnAdmin();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, sender);

        A.CallTo(() => contactsPresenter.ShowDetails(
            A<ContactWithDetails>.Ignored, 123))
            .MustHaveHappenedOnceExactly();
    }

    [TestCase("/details Мизурова")]
    public async Task StudentDetailsQuery_NoRights(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, sender);

        A.CallTo(() => contactsPresenter.SayNoRights(123, ContactType.Student))
            .MustHaveHappenedOnceExactly();
    }

    [TestCase("/details Мизурова")]
    public async Task DoNotShowDetailsToStudents(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, sender);

        A.CallTo(() => contactsPresenter.ShowDetails(null, 123))
            .WithAnyArguments()
            .MustNotHaveHappened();
    }

    [TestCase("/as_student Мизурова")]
    public async Task AsStudent_DowngradeAdminRights(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AnAdmin();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 123, sender);

        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.Ignored,
                123, ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts))
            .MustHaveHappenedOnceExactly();
    }

    private ContactWithDetails AStudent(long tgId = 123123123)
    {
        return new ContactWithDetails(data!.Students.First(c => c.TgId == tgId));
    }

    private ContactWithDetails AnAdmin()
    {
        return new ContactWithDetails(data!.Administrators.First());
    }

    [TestCase("Иван")]
    public async Task ShowOtherStudentsList_IfMoreThanOneResult(string firstName)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);
        
        await handleUpdateService.HandlePlainText(firstName, 42, sender);
        
        A.CallTo(() => contactsPresenter.ShowContact(
                A<Contact>.That.Matches(c => c.FirstName == firstName),
                42, A<ContactDetailsLevel>.Ignored))
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
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 42, sender);

        A.CallTo(() => contactsPresenter.SayNoResults(42))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    [TestCase("Иван")]
    public async Task ExternalUsers_HasNoAccess(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);
        var externalUser = AGuest();

        await handleUpdateService.HandlePlainText(query, 42, externalUser);

        A.CallTo(() => contactsPresenter.SayNoRights(42, ContactType.External))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    private ContactWithDetails AGuest()
    {
        return new ContactWithDetails(
            new Contact(1, ContactType.External, 555, "Некто", "Нектович")
            {
                Telegram = "@guest"
            });
    }

    [TestCase("Я Гриша!")]
    public async Task ExternalUsers_CanJoin(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText("/join " + query, 555, AGuest());

        A.CallTo(() => contactsPresenter.Say(A<string>.Ignored, 111)) // Кто-то хочет доступ!
            .MustHaveHappenedOnceExactly(); 
        A.CallTo(() => contactsPresenter.Say(A<string>.Ignored, 555)) // Модераторы получили твой запрос
            .MustHaveHappenedOnceExactly(); 
        Assert.AreEqual(2, Fake.GetCalls(contactsPresenter).Count());
    }


    [TestCase("Гимназия 9")]
    [TestCase("9")]
    [TestCase("Гимназия 99")]
    [TestCase("тагил")]
    [TestCase("Нижний Тагил")]
    public async Task SearchBySchoolOrCity(string query)
    {
        var contactsPresenter = A.Fake<IPresenter>();
        var sender = AStudent();
        var handleUpdateService = PrepareUpdateService(contactsPresenter);

        await handleUpdateService.HandlePlainText(query, 42, sender);

        A.CallTo(() => 
                contactsPresenter.ShowContactsBy(
                    A<string>.Ignored, 
                    A<IList<Contact>>.That.Matches(p => p.Count == 1), 
                    42))
            .MustHaveHappenedOnceExactly();
        Assert.AreEqual(1, Fake.GetCalls(contactsPresenter).Count());
    }

    private HandleUpdateService PrepareUpdateService(IPresenter presenter, INamedPhotoDirectory? namedPhotoDirectory = null)
    {
        namedPhotoDirectory ??= A.Fake<INamedPhotoDirectory>();
        var photoRepo = A.Fake<IPhotoRepository>();
        var downloader = A.Fake<ITelegramFileDownloader>();
        var repo = new MemoryBotDataRepository(data);
        var detailsRepo = A.Fake<IContactDetailsRepo>();
        var commands = new IChatCommandHandler[]
        {
            new StartCommandHandler(presenter, repo),
            new HelpCommandHandler(presenter),
            new DetailsCommandHandler(presenter, repo, detailsRepo),
            new ContactsCommandHandler(repo, presenter),
            new RandomCommandHandler(repo, presenter, new Random()),
            new JoinCommandHandler(presenter, 111),
        };
        return new HandleUpdateService(repo, namedPhotoDirectory, photoRepo, null, downloader, presenter, detailsRepo, commands);
    }
}
