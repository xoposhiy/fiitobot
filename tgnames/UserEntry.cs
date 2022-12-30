using System;

namespace tgnames
{
    public class UserEntry
    {
        public UserEntry(long id, string username, DateTime lastUpdate)
        {
            Id = id;
            Username = username;
            LastUpdate = lastUpdate;
        }

        public long Id;
        public string Username;
        public DateTime LastUpdate;

    }
}
