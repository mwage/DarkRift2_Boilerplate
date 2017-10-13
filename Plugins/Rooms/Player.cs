using DarkRift;

namespace RoomSystemPlugin
{
    public class Player : IDarkRiftSerializable
    {
        public uint Id { get; }
        public string Name { get; }
        public bool IsHost { get; private set; }
        public PlayerColor Color { get; private set; }

        public Player(uint id, string name, bool isHost, PlayerColor color)
        {
            Id = id;
            Name = name;
            IsHost = isHost;
            Color = color;
        }

        public void SetNewColor(PlayerColor color)
        {
            Color = color;
        }

        public void SetHost(bool isHost)
        {
            IsHost = isHost;
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(IsHost);
            e.Writer.Write((byte)Color);
        }

        public void Deserialize(DeserializeEvent e)
        {
        }
    }
}