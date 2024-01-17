using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class SaveBirthdayCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;

        public SaveBirthdayCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
        }

        public string Command => "/bd_save";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var date = text.Split(" ")[1];

            if (DateUtils.IsValidDate(date))
            {
                botDataRepo.UpdateContact(sender.Id, c => c.BirthDate = date);

                await presenter.Say("Отлично! Теперь твой ДР отображается в профиле!" +
                                    "\n\nКстати! Если прислать мне слово \"др\", я покажу дни рождения одногруппников. А если прислать название месяца — всех именинников в этом месяце", fromChatId);
                return;
            }

            await presenter.Say("Неверный формат или значение даты(" +
                                "\nУкажите допустимую дату в формате ДД.ММ или ДД.ММ.ГГГГ", fromChatId);
        }
    }

    public class FindBirthdayCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;

        public FindBirthdayCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
        }

        public string Command => "/bd_find";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var date = text.Split(" ")[1];

            if (!await ShowContactsListBy(date, c => c.BirthDate, fromChatId))
                await presenter.SayNoResults(fromChatId);
        }

        //TODO Дублирование с HandleUpdateService
        private async Task<bool> ShowContactsListBy(string text, Func<Contact, string> getProperty, long chatId)
        {
            var botData = botDataRepo.GetData();
            var contacts = botData.AllContacts.Select(p => p).ToList();

            var res = contacts.Where(c => SmartContains(getProperty(c) ?? "", text))
                .ToList();
            if (res.Count == 0) return false;
            var bestGroup = res.GroupBy(getProperty).MaxBy(g => g.Count());

            await presenter.ShowContactsBy(bestGroup.Key, res, chatId);
            return true;
        }

        //TODO Дублирование с HandleUpdateService.
        private bool SmartContains(string value, string query)
        {
            return new Regex($@"\b{Regex.Escape(query)}\b", RegexOptions.IgnoreCase).IsMatch(value);
        }
    }

    public class StatBirthdayCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;

        public StatBirthdayCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
        }

        public string Command => "/bd_stats";
        public ContactType[] AllowedFor => new[] { ContactType.Administration };
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var statCount = botDataRepo.GetData().AllContacts.Count(s => !string.IsNullOrEmpty(s.BirthDate) && s.BirthDate != "no");

            await presenter.Say($"{statCount} людей указали когда у них день рождения.", fromChatId);
        }
    }

    public class RemoveBirthdayCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;

        public RemoveBirthdayCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
        }

        public string Command => "/bd_remove";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            botDataRepo.UpdateContact(sender.Id, c => c.BirthDate = "no");

            await presenter.Say("Данные о твоем др теперь недоступны" +
                                "\n\nТы в любой момент можешь добавить др, написав нужную дату в формате ДД.ММ или ДД.ММ.ГГГГ", fromChatId);
        }
    }

    public class NotifyOffBirthdayCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;

        public NotifyOffBirthdayCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
        }

        public string Command => "/bd_notify_off";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            botDataRepo.UpdateContact(sender.Id, c => c.ReceiveBirthdayNotifications = false);

            await presenter.Say("Уведомления больше не будут приходить тебе в личные сообщения." +
                                "\n\nТы в любой момент можешь вернуть их, написав /bd_notify_on", fromChatId);
        }
    }

    public class NotifyOnBirthdayCommandHandler : IChatCommandHandler
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;

        public NotifyOnBirthdayCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
        }

        public string Command => "/bd_notify_on";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;
        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            botDataRepo.UpdateContact(sender.Id, c => c.ReceiveBirthdayNotifications = true);

            await presenter.Say("Теперь ты будешь получать уведомления о др своих одногруппников!" +
                                "\n\nТы в любой момент можешь отключить их, написав /bd_notify_off", fromChatId);
        }
    }

    public static class DateUtils
    {
        private static readonly Dictionary<string, string> MonthNames = new Dictionary<string, string>
        {
            {"01", "Январь"},
            {"02", "Февраль"},
            {"03", "Март"},
            {"04", "Апрель"},
            {"05", "Май"},
            {"06", "Июнь"},
            {"07", "Июль"},
            {"08", "Август"},
            {"09", "Сентябрь"},
            {"10", "Октябрь"},
            {"11", "Ноябрь"},
            {"12", "Декабрь"}
        };

        public static bool TryParseMonthName(string text, out string monthNumber)
        {
            monthNumber = MonthNames
                .Where(kv => kv.Value.Equals(text, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .FirstOrDefault();
            return monthNumber != null;
        }

        public static bool TryParseMonthNumber(string monthString, out string monthName) =>
            MonthNames.TryGetValue(monthString, out monthName);

        public static bool IsValidDate(string dateString)
        {
            string[] formats = { "dd.MM", "dd.MM.yyyy" };
            return DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
    }
}
