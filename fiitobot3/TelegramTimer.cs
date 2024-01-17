using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using fiitobot.Services;
using Telegram.Bot;
using Yandex.Cloud.Functions;

namespace fiitobot
{
    public class TelegramBotTimerFunction : YcFunction<string, Response>
    {
        public Response FunctionHandler(string request, Context context)
        {
            var settings = new Settings();
            var client = new TelegramBotClient(settings.TgToken);
            try
            {
                var botDataRepository = new BotDataRepository(settings);
                var formattedDateToday = DateTime.Today.ToString("dd.MM");
                var botData = botDataRepository.GetData();
                var contacts = botData.AllContacts.ToList();

                var contactsWithBirthDate = contacts
                        .Where(c => !string.IsNullOrEmpty(c.BirthDate) && c.BirthDate != "no")
                        .ToList();

                var contactsTodayBirth = contactsWithBirthDate.Where(contact =>
                    contact.BirthDate.StartsWith(formattedDateToday)).ToList();

                var chatIds = new Dictionary<Contact, List<Contact>>(); // для каждого именниника храним его одногруппников

                foreach (var contact in contactsTodayBirth)
                {
                    chatIds.Add(contact, contacts
                        .Where(c => contact.FormatMnemonicGroup(DateTime.Now, false)
                                    == c.FormatMnemonicGroup(DateTime.Now, false) && c != contact && c.ReceiveBirthdayNotifications)
                        .ToList());
                }

                foreach (var contact in chatIds)
                {
                    foreach (var receiver in contact.Value)
                    {
                        if (receiver.ReceiveBirthdayNotifications)
                        {
                            client.SendTextMessageAsync(receiver.TgId,
                                $"Сегодня свой день рождения отмечает {contact.Key.FirstLastName()} {contact.Key.Telegram}🥳" +
                                "\n\nМожешь написать оригинальное поздравление в личку или беседу своего курса)" +
                                "\n\nЧтобы перестать получать уведомления о др своих одногруппников, напиши /bd_notify_off");
                        }
                    }
                }

                return new Response(200, "ok");
            }
            catch (Exception e)
            {
                client.SendTextMessageAsync(settings.DevopsChatId, "Request:\n\n" + request + "\n\n" + e).Wait();
                return new Response(500, e.ToString());
            }
        }
    }
}
