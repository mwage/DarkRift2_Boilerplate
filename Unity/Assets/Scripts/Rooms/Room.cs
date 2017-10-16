using DarkRift;

namespace Rooms
{
    public class Room : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public string Name { get; private set; }
        public byte MaxPlayers { get; private set; }
        public byte CurrentPlayers { get; private set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Name = e.Reader.ReadString();
            MaxPlayers = e.Reader.ReadByte();
            CurrentPlayers = e.Reader.ReadByte();
        }

        public void Serialize(SerializeEvent e)
        {
        }
    }
}