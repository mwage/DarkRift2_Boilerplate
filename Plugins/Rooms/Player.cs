using DarkRift;

namespace RoomSystemPlugin
{
    public class Player : IDarkRiftSerializable
    {
        public uint Id { get; }
        public string Name { get; }
        public bool IsHost { get; private set; }

        public Player(uint id, string name, bool isHost)
        {
            Id = id;
            Name = name;
            IsHost = isHost;
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(IsHost);
        }

        public void Deserialize(DeserializeEvent e)
        {
        }

        public void SetHost(bool isHost)
        {
            IsHost = isHost;
        }
    }
}