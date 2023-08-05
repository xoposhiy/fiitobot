using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TL;

namespace fiitobot.Services
{

    public class TgRankGranter
    {
        /// <summary>
        /// В заданном чате раздает студентам ФИИТ админские права без особых разрешений, но с ранком Студент ФИИТ, чтобы остальным было видно, что это пишет студент.
        /// Количество админов в группе не может быть больше 50. Поэтому админские права раздаются 30 студентам ФИИТ, которые последними писали что-то в чат.
        /// Таким образом в последних сообщениях все студенты ФИИТ будут отмечены подписями.
        /// 
        /// Код вычисляет список админов, которых нужно разжаловать, а потом список тех, кого нужно произвести в админы. Остальных не трогает.
        /// В 2023 появились ещё и выпускники. Им даем статус Выпускник ФИИТ
        /// </summary>
        public async Task GrantStudentRanks(WTelegram.Client client, BotDataRepository dataRepository, string chatTitle = "спроси про ФИИТ", string studentRank = "Студент ФИИТ", string graduateRank = "Выпускник ФИИТ")
        {
            var _ = await client.LoginUserIfNeeded();
            var chats = await client.Messages_GetAllDialogs();
            ChatBase chat = chats.chats.FirstOrDefault(c => c.Value.Title.Contains(chatTitle)).Value;
            Console.WriteLine($"Found Chat {chat.Title} {chat.ID} {chat.ToInputPeer()} {chat.GetType()}");
            var channel = (InputPeerChannel)chat.ToInputPeer();
            var participants = await client.Channels_GetParticipants(channel, new ChannelParticipantsAdmins());
            var studAdmins = participants.participants.OfType<ChannelParticipantAdmin>().Where(a => a.rank == studentRank || a.rank == graduateRank).ToList();
            var anyAdminIds = participants.participants.OfType<ChannelParticipantAdmin>().Select(a => a.UserId).ToHashSet();
            foreach (var creator in participants.participants.OfType<ChannelParticipantCreator>())
            {
                anyAdminIds.Add(creator.UserId);
            }
            Console.WriteLine("All administrators: " + anyAdminIds.Count);
            Console.WriteLine($"Found {studAdmins.Count} student admins");

            var studentContacts = dataRepository.GetData().Students;
            var students = studentContacts.Where(s => s.AdmissionYear < 2023).Select(c => c.TgId).ToHashSet();
            Console.WriteLine($"Loaded {students.Count} students");

            Dictionary<long, PeerUser> lastAuthors = new Dictionary<long, PeerUser>();
            var lastMessageId = 0;
            while (lastAuthors.Count < 30)
            {
                var history = await client.Messages_GetHistory(channel, offset_id: lastMessageId);
                if (history.Messages.Length == 0) break;
                foreach (var m in history.Messages)
                {
                    if (m.From is PeerUser user && !lastAuthors.ContainsKey(user.ID) && students.Contains(user.ID))
                    {
                        lastAuthors.Add(user.ID, user);
                    }
                }
                lastMessageId = history.Messages.Last().ID;
                Console.WriteLine($"Last authors: {lastAuthors.Count}");
            }
            Console.WriteLine($"Final last authors: {lastAuthors.Count}");
            //    .Select(a => participants.users[a.UserId]).ToList();
            var adminsToRemove = studAdmins.Where(a => !lastAuthors.ContainsKey(a.UserId)).Select(a => participants.users[a.UserId]).ToList();
            Console.WriteLine($"Administrators to remove {adminsToRemove.Count}");

            foreach (var adminToRemove in adminsToRemove)
            {
                await client.Channels_EditAdmin(channel, adminToRemove, new ChatAdminRights { flags = 0 }, null);
                Console.WriteLine($"Remove admin {adminToRemove.username ?? adminToRemove.last_name}");
            }
            participants = await client.Channels_GetAllParticipants(channel);
            var chatUsers = participants.users.Values;
            Console.WriteLine($"Found {chatUsers.Count} participants");
            var adminsToAdd = chatUsers.Where(user => lastAuthors.ContainsKey(user.ID) && students.Contains(user.ID) && !anyAdminIds.Contains(user.ID)).ToList();
            Console.WriteLine($"Administrators to add {adminsToAdd.Count}");
            foreach (var user in adminsToAdd)
            {
                var contact = studentContacts.First(s => s.TgId == user.ID);
                var rank = contact.GraduationYear <= 2023 ? graduateRank : studentRank;
                await client.Channels_EditAdmin(channel, user,
                    new ChatAdminRights
                    {
                        flags = ChatAdminRights.Flags.post_messages | ChatAdminRights.Flags.edit_messages
                    },
                    rank);
                Console.WriteLine($"Make admin {user.first_name} {user.last_name} {user.ID} → {rank}");
            }
        }
    }
}
