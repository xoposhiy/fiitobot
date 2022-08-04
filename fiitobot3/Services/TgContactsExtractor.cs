using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TL;

namespace fiitobot.Services
{
    public class TgContactsExtractor
    {
        /// <summary>
        /// Получает всех участников ФИИТочатов, чтобы можно было сохранить соответствие юзернейма и неизменного в будущем userID.
        /// </summary>
        public async Task<List<User>> ExtractUsersFromChatsAndChannels(WTelegram.Client client, params string[] chatTitleSubstrings)
        {
            var me = await client.LoginUserIfNeeded();
            Messages_Chats chats = await client.Messages_GetAllChats();
            var fiitChats = chats.chats.Where(c => chatTitleSubstrings.Any(substr => c.Value.Title.Contains(substr))).ToList();
            var allUsers = new Dictionary<long, User>();
            foreach (var fiitChat in fiitChats)
            {
                Console.WriteLine(fiitChat.Value);
                try
                {
                    var channels = await client.Channels_GetChannels(new InputChannel(fiitChat.Key, me.access_hash));
                    foreach (var c in channels.chats)
                    {
                        var chat = c.Value;
                        var res = await client.Channels_GetAllParticipants((InputPeerChannel)chat.ToInputPeer());
                        var users = res.users.Values;
                        foreach (var user in users)
                        {
                            if (!allUsers.ContainsKey(user.id))
                                allUsers.Add(user.id, user);
                        }
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        var fullChat = await client.Messages_GetFullChat(fiitChat.Key);
                        Console.WriteLine("Chat with " + fullChat.users.Count + " members and " + fullChat.chats.Count + " chats count");

                        foreach (var user in fullChat.users.Values)
                        {
                            if (!allUsers.ContainsKey(user.id))
                                allUsers.Add(user.id, user);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            return allUsers.Values.ToList();
        }

    }
}