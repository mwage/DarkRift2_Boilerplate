using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace ChatPlugin
{
    public class ChatGroup : IDarkRiftSerializable
    {
        public string Name { get; }
        public Dictionary<string, IClient> Users = new Dictionary<string, IClient>();

        public ChatGroup(string name)
        {
            Name = name;
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Name);
            e.Writer.Write(Users.Keys.ToArray());
        }

        public void Deserialize(DeserializeEvent e)
        {
        }

        internal bool AddPlayer(string username, IClient client)
        {
            if (Users.ContainsKey(username))
                return false;

            Users[username] = client;
            return true;
        }

        internal void RemovePlayer(string username)
        {
            Users.Remove(username);
        }
    }
}