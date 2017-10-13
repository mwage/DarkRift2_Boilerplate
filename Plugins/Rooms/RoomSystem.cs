using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using LoginPlugin;

namespace RoomSystemPlugin
{
    public class RoomSystem : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;
        public override Command[] Commands => new[]
        {
            new Command("Rooms", "Shows all rooms", "", GetRoomsCommand)
        };

        // Tag
        private const byte RoomTag = 3;

        // Subjects
        private const ushort Create = 0;
        private const ushort Join = 1;
        private const ushort Leave = 2;
        private const ushort GetOpenRooms = 3;
        private const ushort GetOpenRoomsFailed = 4;
        private const ushort CreateFailed = 5;
        private const ushort CreateSuccess = 6;
        private const ushort JoinFailed = 7;
        private const ushort JoinSuccess = 8;
        private const ushort PlayerJoined = 9;
        private const ushort LeaveSuccess = 10;
        private const ushort PlayerLeft = 11;

        private const string ConfigPath = @"Plugins\RoomSystem.xml";
        private Login _loginPlugin;
        private bool _debug = true;
        public Dictionary<ushort, Room> RoomList = new Dictionary<ushort, Room>();
        private readonly Dictionary<uint, Room> _playersInRooms = new Dictionary<uint, Room>();

        public RoomSystem(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig();
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        private void LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Settings for the RoomSystem Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    WriteEvent("Created /Plugins/RoomSystem.xml!", LogType.Warning);
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create RoomSystem.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                }
            }
            else
            {
                try
                {
                    document = XDocument.Load(ConfigPath);
                    _debug = document.Element("Variables").Attribute("Debug").Value == "true";
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to load Login.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                }
            }
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            // If you have DR2 Pro, use the Plugin.Loaded() method instead
            if (_loginPlugin == null)
            {
                _loginPlugin = PluginManager.GetPluginByType<Login>();
            }

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            LeaveRoom(e.Client);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!(e.Message is TagSubjectMessage message) || message.Tag != RoomTag)
                return;

            var client = (Client)sender;

            // Create Room Request
            if (message.Subject == Create)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, RoomTag, CreateFailed, "Create Room failed."))
                    return;

                string roomName;
                bool isVisible;

                try
                {
                    var reader = message.GetReader();
                    roomName = reader.ReadString();
                    isVisible = reader.ReadBoolean();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, RoomTag, CreateFailed, ex, "Room Create Failed!");
                    return;
                }

                roomName = AdjustRoomName(roomName, _loginPlugin.UsersLoggedIn[client]);
                var roomId = GenerateRoomId();

                var room = new Room(roomId, roomName, isVisible);
                var player = new Player(client.GlobalID, _loginPlugin.UsersLoggedIn[client], true);
                room.AddPlayer(player, client);
                RoomList.Add(roomId, room);
                _playersInRooms.Add(client.GlobalID, room);

                var wr = new DarkRiftWriter();
                wr.Write(room);
                wr.Write(player);
                client.SendMessage(new TagSubjectMessage(RoomTag, CreateSuccess, wr), SendMode.Reliable);

                if (_debug)
                {
                    WriteEvent("Creating Room " + roomId + ": " + room.Name, LogType.Info);
                }
            }

            // Join Room Request
            else if (message.Subject == Join)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, RoomTag, JoinFailed, "Join Room failed."))
                    return;

                ushort roomId;

                try
                {
                    var reader = message.GetReader();
                    roomId = reader.ReadUInt16();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, RoomTag, JoinFailed, ex, "Room Join Failed! ");
                    return;
                }

                if (!RoomList.ContainsKey(roomId))
                {
                    // Return Error 3 for Room doesn't exist anymore
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)3);
                    client.SendMessage(new TagSubjectMessage(RoomTag, JoinFailed, writer), SendMode.Reliable);

                    if (_debug)
                    {
                        WriteEvent("Room Join Failed! Room " + roomId + " doesn't exist anymore", LogType.Info);
                    }

                    return;
                }
                var room = RoomList[roomId];
                var newPlayer = new Player(client.GlobalID, _loginPlugin.UsersLoggedIn[client], false);

                // Check if player already is in an active room -> Send error 2
                if (_playersInRooms.ContainsKey(client.GlobalID))
                {
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)2);

                    client.SendMessage(new TagSubjectMessage(RoomTag, JoinFailed, writer), SendMode.Reliable);

                    if (_debug)
                    {
                        WriteEvent("User " + client.GlobalID + " couldn't join Room " + room.Id + ", since he already is in Room: " + _playersInRooms[client.GlobalID], LogType.Info);
                    }
                    return;
                }

                if (room.AddPlayer(newPlayer, client))
                {
                    _playersInRooms[client.GlobalID] = room;

                    var writer = new DarkRiftWriter();
                    writer.Write(room);
                    foreach (var player in room.PlayerList)
                    {
                        writer.Write(player);
                    }

                    client.SendMessage(new TagSubjectMessage(RoomTag, JoinSuccess, writer), SendMode.Reliable);

                    // Let the other clients know
                    writer = new DarkRiftWriter();
                    writer.Write(newPlayer);

                    foreach (var cl in room.Clients.Where(c => c.GlobalID != client.GlobalID))
                    {
                        cl.SendMessage(new TagSubjectMessage(RoomTag, PlayerJoined, writer), SendMode.Reliable);
                    }

                    if (_debug)
                    {
                        WriteEvent("User " + client.GlobalID + " joined Room " + room.Id, LogType.Info);
                    }
                }
                // Room full or has started -> Send error 2
                else
                {
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)2);

                    client.SendMessage(new TagSubjectMessage(RoomTag, JoinFailed, writer), SendMode.Reliable);

                    if (_debug)
                    {
                        WriteEvent("User " + client.GlobalID + " couldn't join, since Room " + room.Id + " was either full or had started!", LogType.Info);
                    }
                }
                // Try to join room
            }

            // Leave Room Request
            else if (message.Subject == Leave)
            {
                LeaveRoom(client);
            }

            // Get Open Rooms Request
            else if (message.Subject == GetOpenRooms)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, RoomTag, GetOpenRoomsFailed, "GetRoomRequest failed."))
                    return;

                // If he is, send back all available rooms
                var availableRooms = RoomList.Values.Where(r => r.IsVisible && !r.HasStarted).ToList();
                var writer = new DarkRiftWriter();
                foreach (var room in availableRooms)
                {
                    writer.Write(room);
                }
                client.SendMessage(new TagSubjectMessage(RoomTag, GetOpenRooms, writer), SendMode.Reliable);
            }
        }

        private ushort GenerateRoomId()
        {
            ushort i = 0;
            while (true)
            {
                if (!RoomList.ContainsKey(i))
                {
                    return i;
                }

                i++;
            }
        }

        private static string AdjustRoomName(string roomName, string playerName)
        {
            if (roomName == "")
            {
                return playerName + "'s Lobby";
            }

            return roomName;
        }

        private void LeaveRoom(Client client)
        {
            var id = client.GlobalID;
            if (!_playersInRooms.ContainsKey(id))
                return;

            var room = _playersInRooms[id];
            var leaverName = room.PlayerList.FirstOrDefault(p => p.Id == client.GlobalID)?.Name;
            _playersInRooms.Remove(id);

            if (room.RemovePlayer(client))
            {
                // Only message user if he's still connected (would cause error if LeaveRoom is called from Disconnect otherwise)
                if (client.IsConnected)
                {
                    client.SendMessage(new TagSubjectMessage(RoomTag, LeaveSuccess, new DarkRiftWriter()), SendMode.Reliable);
                }

                // Remove room if it's empty
                if (room.PlayerList.Count == 0)
                {
                    RoomList.Remove(RoomList.FirstOrDefault(r => r.Value == room).Key);
                    if (_debug)
                    {
                        WriteEvent("Room " + room.Id + " deleted!", LogType.Info);
                    }
                }
                // otherwise set a new host and let other players know
                else
                {
                    var newHost = room.PlayerList.First();
                    newHost.SetHost(true);

                    var writer = new DarkRiftWriter();
                    writer.Write(id);
                    writer.Write(newHost.Id);
                    writer.Write(leaverName);

                    foreach (var cl in room.Clients)
                    {
                        cl.SendMessage(new TagSubjectMessage(RoomTag, PlayerLeft, writer), SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    WriteEvent("User " + client.GlobalID + " left Room: " + room.Name,
                        LogType.Info);
                }
            }
            else
            {
                WriteEvent("Tried to remove player who wasn't in the room anymore.", LogType.Warning);
            }
        }

        private void GetRoomsCommand(object sender, CommandEventArgs e)
        {
            WriteEvent("Active Rooms:", LogType.Info);
            var rooms = RoomList.Values.ToList();
            foreach (var room in rooms)
            {
                WriteEvent(room.Name + " [" + room.Id + "] - " + room.PlayerList.Count + "/" + room.MaxPlayers, LogType.Info);
            }
        }
    }
}