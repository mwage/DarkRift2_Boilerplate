using DarkRift;

namespace Rooms
{
    public class Player : IDarkRiftSerializable
    {
        public uint Id { get; private set; }
        public string Name { get; private set; }
        public bool IsHost { get; set; }
        public PlayerColor Color { get; private set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt32();
            Name = e.Reader.ReadString();
            IsHost = e.Reader.ReadBoolean();
            Color = (PlayerColor)e.Reader.ReadByte();
        }

        public void Serialize(SerializeEvent e)
        {
        }
    }
}
