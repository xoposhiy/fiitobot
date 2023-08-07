using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class RandomCommandHandler : IChatCommandHandler
    {
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo detailsRepo;
        private readonly IPresenter presenter;
        private readonly Random random;
        private readonly string[] randomFacts = {
            "Установи себе фотку в фиитоботе! Её будут видеть фиитовцы, которые ищут тебя тут. Чтобы установить фотку, просто пришли ее боту!",
            "Тут можно искать преподавателей и администраторов ФИИТа",
            "Я могу поискать людей по городу или номеру школы!",
            "Мой исходный код открыт. https://github.com/xoposhiy/fiitobot/tree/main/fiitobot3",
            "Меня хостят в Яндекс облаке с помощью бессерверных технологий, поэтому хостинг ничего не стоит! Научись делать так же с помощью этого шаблона телеграм бота: https://github.com/BasedDepartment1/cloud-function-bot",
            "Вот классный курс по командной строке: https://ru.hexlet.io/courses/cli-basics",
            "Вот коротенький курс по основам [не совсем] публичных выступлений для разработчиков: https://ulearn.me/course/speaker",
            "Этика студентов! Слышал про такую? https://docs.google.com/document/d/19VobSaJxbIWweIhx3b_XFcakjvqtGVSAISq4A-XfbDI/edit",
            "А вы знаете, как общаться с инопланетянами по научному? https://www.youtube.com/watch?v=Hk0HhaBi-zY",
            "Раньше курс Операционные системы был другим. Неплохие лекции про ОС остались в записи: https://www.youtube.com/playlist?list=PL4UhuTnu1z7vZbza0LfZNWOwdKChijOhX",
            "Не хотите послушать ликбез про то, что такое DevOps? https://youtu.be/R0XeuJIBRsU",
            "Не хотите узнать, как белые хакеры зарабатывают на поиске дыр в безопасности? https://youtu.be/mUhPugKjSBg",
            "Вот отличный доклад про геймдев во временя 8-битных компьютеров. Милые хаки и трюки! https://www.youtube.com/watch?v=TPbroUDHG0s&t=1164s",
            "Уже слышали свежие новости из мира слабо полостью антисимметричных квазигрупп десятого порядка? https://www.youtube.com/watch?v=cwKnHHRutUs",
            "Сыграйте лучше в clash of code с кем-нибудь! https://www.codingame.com/multiplayer/clashofcode",
            "А вы уже играли в Untrusted? https://alexnisnevich.github.io/untrusted/",
            "Поиграйте https://nandgame.com/",
            "Ни за что не смотри этот видос! https://www.youtube.com/watch?v=Lhz_de3aMNc",
            "Про алгоритмы играющие в игры можно узнать отсюда: https://www.youtube.com/watch?v=Z2RRwwmNQ_U",
            "Вы знали, что есть формула всего? https://www.youtube.com/watch?v=_s5RFgd59ao",
            "Напишите бота для гоночек! https://www.codingame.com/multiplayer/bot-programming/mad-pod-racing",
            "Ты слушал все выпуски подкаста от студентов ФИИТ? https://fiitpostupitt.mave.digital/",
            "Знаешь что такое reverse engineering? https://www.youtube.com/watch?v=LRaGOGKnpqA",
            "Знаешь что такое продуктовый дизайн? https://www.youtube.com/watch?v=5wXZzYEX_vE",
            "Это же задача о разборчивой невесте! https://www.youtube.com/watch?v=_IeneHj0QYs",
            "А вы знали, что есть функция ι, через которую выражаются все остальные функции во вселенной? https://en.wikipedia.org/wiki/Iota_and_Jot",
            "Обычный текст сложнее, чем вы думали! https://www.youtube.com/watch?v=gd5uJ7Nlvvo",
            "Наш мир сложнее, чем кажется программистам на первый взгляд! https://www.youtube.com/watch?v=Z8SHvJnGUCM",
            "Изучи, что такое декартово дерево! https://www.youtube.com/watch?v=0IDaKrPosdA",
            "Посмотри лекцию про Unicode! https://www.youtube.com/watch?v=mYhBS_4DoMA",
            "Что каждый программист должен знать про числа с плавающей точкой? https://habr.com/ru/articles/112953/",
            "Что каждый программист должен знать про память? https://rus-linux.net/lib.php?name=/MyLDP/hard/memory/memory.html",
            "Порешайте кроссворд с регулярными выражениями! https://regexcrossword.com/",
            "Сыграйте в Clean Code Game https://cleancodegame.github.io/",
            "Напиши отзыв про ФИИТ на https://tabiturient.ru/vuzu/urfu/",
            "Напиши отзыв про ФИИТ на https://vuzopedia.ru/vuz/1848",
            "Логотип и фирменный стиль ФИИТ бесплатно сделала компания JetStyle :) https://jetstyle.ru/portfolio/case/logo-and-brand-identity-for-fiit",
            "Если знаете случайный факт, достойный включения в этот список, пришлите его https://t.me/xoposhiy",
            "Подпишись на канал Технологии в Контуре. Там публикуют ссылки на доклады и статьи про техномясо! https://t.me/KonturTech",
        };

        private const double ShowSenderChance = 1.0 / 10.0;
        private const double ShowRandomFactChance = 3.0 / 10.0;

        public RandomCommandHandler(IBotDataRepository botDataRepo, IContactDetailsRepo detailsRepo, IPresenter presenter, Random random)
        {
            this.botDataRepo = botDataRepo;
            this.detailsRepo = detailsRepo;
            this.presenter = presenter;
            this.random = random;
        }

        public string Command => "/random";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var chance = random.NextDouble();
            if (chance < ShowSenderChance)
            {
                await presenter.ShowContact(sender, fromChatId, sender.GetDetailsLevelFor(sender));
                return;
            }

            if (chance < ShowSenderChance + ShowRandomFactChance)
            {
                var randomFact = randomFacts[random.Next(randomFacts.Length)];
                await presenter.Say($"Кстати!\n\n{randomFact}", fromChatId);
                return;
            }
            var contact = GetPresentedContact(sender);
            await presenter.ShowContact(contact, fromChatId, contact.GetDetailsLevelFor(sender));
        }

        private Contact GetPresentedContact(Contact sender)
        {
            var chance = random.NextDouble();
            if (chance < ShowSenderChance)
                return sender;
            var students = botDataRepo.GetData().Students
                .Where(s => s.Status.IsOneOf("Активный", ""))
                .ToList();
            var randomContact = students[random.Next(students.Count)];
            var details = detailsRepo.FindById(randomContact.Id).Result;
            randomContact.UpdateFromDetails(details);
            return randomContact;
        }
    }
}
