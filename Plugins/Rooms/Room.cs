using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace RoomSystemPlugin
{
    public class Room : IDarkRiftSerializable
    {
        public ushort Id { get; }
        public string Name { get; }
        public byte MaxPlayers { get; } = 10;
        public bool HasStarted { get; set; }
        public bool IsVisible { get; }
        public List<IClient> Clients = new List<IClient>();
        public List<Player> PlayerList = new List<Player>();

        public Room(ushort id, string name, bool isVisible)
        {
            Name = name;
            IsVisible = isVisible;
            Id = id;
            HasStarted = false;
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(MaxPlayers);
            e.Writer.Write((byte) PlayerList.Count);
        }

        public void Deserialize(DeserializeEvent e)
        {
        }

        internal bool AddPlayer(Player player, IClient client)
        {
            if (PlayerList.Count >= MaxPlayers || HasStarted)
                return false;

            PlayerList.Add(player);
            Clients.Add(client);
            return true;
        }

        internal bool RemovePlayer(IClient client)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;

            PlayerList.Remove(PlayerList.Find(p => p.Id == client.ID));
            Clients.Remove(client);
            return true;
        }
    }
}