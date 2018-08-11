using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using LoginPlugin;
using RoomSystemPlugin;

namespace ChatPlugin
{
    public class Chat : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;
        public override Command[] Commands => new[]
        {
            new Command("groups", "Shows all chatgroups [groups username(optional]", "", GetChatGroupsCommand)
        };

        public ConcurrentDictionary<string, ChatGroup> ChatGroups { get; } = new ConcurrentDictionary<string, ChatGroup>();
        public ConcurrentDictionary<string, List<ChatGroup>> ChatGroupsOfPlayer { get; } = new ConcurrentDictionary<string, List<ChatGroup>>();

        // Tag
        private const byte ChatTag = 3;
        private const ushort Shift = ChatTag * Login.TagsPerPlugin;

        // Subjects
        private const ushort PrivateMessage = 0 + Shift;
        private const ushort SuccessfulPrivateMessage = 1 + Shift;
        private const ushort RoomMessage = 2 + Shift;
        private const ushort GroupMessage = 3 + Shift;
        private const ushort MessageFailed = 4 + Shift;
        private const ushort JoinGroup = 5 + Shift;
        private const ushort JoinGroupFailed = 6 + Shift;
        private const ushort LeaveGroup = 7 + Shift;
        private const ushort LeaveGroupFailed = 8 + Shift;
        private const ushort GetActiveGroups = 9 + Shift;
        private const ushort GetActiveGroupsFailed = 10 + Shift;

        private const string ConfigPath = @"Plugins/Chat.xml";
        private static readonly object InitializeLock = new object();
        private bool _debug = true;
        private Login _loginPlugin;
        private RoomSystem _roomSystem;

        public Chat(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig();
            ClientManager.ClientConnected += OnPlayerConnected;
        }

        private void LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Settings for the Chat Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    WriteEvent("Created /Plugins/Chat.xml!", LogType.Info);
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create Chat.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
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
                    WriteEvent("Failed to load Chat.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                }
            }
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            // If you have DR2 Pro, use the Loaded() method instead and spare yourself the locks
            if (_loginPlugin == null)
            {
                lock (InitializeLock)
                {
                    if (_loginPlugin == null)
                    {
                        _loginPlugin = PluginManager.GetPluginByType<Login>();
                        _roomSystem = PluginManager.GetPluginByType<RoomSystem>();
                        _loginPlugin.onLogout += RemovePlayerFromChatGroups;
                        ChatGroups["General"] = new ChatGroup("General");
                    }
                }
            }

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag < Login.TagsPerPlugin * ChatTag || message.Tag >= Login.TagsPerPlugin * (ChatTag + 1))
                {
                    return;
                }

                var client = e.Client;

                // Private Message
                switch (message.Tag)
                {
                    case PrivateMessage:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, MessageFailed, "Private Message failed.")) return;

                        var senderName = _loginPlugin.UsersLoggedIn[client];
                        string receiver;
                        string content;

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                receiver = reader.ReadString();
                                content = reader.ReadString();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return Error 0 for Invalid Data Packages Recieved
                            _loginPlugin.InvalidData(client, MessageFailed, ex, "Send Message failed! ");
                            return;
                        }

                        if (!_loginPlugin.Clients.ContainsKey(receiver))
                        {
                            // If receiver isn't logged in -> return error 3
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 3);

                                using (var msg = Message.Create(MessageFailed, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }

                            if (_debug)
                            {
                                WriteEvent("Send Message failed. Receiver wasn't logged in.", LogType.Info);
                            }
                            return;
                        }

                        var receivingClient = _loginPlugin.Clients[receiver];

                        // Let sender know message got transmitted
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(senderName);
                            writer.Write(receiver);
                            writer.Write(content);

                            using (var msg = Message.Create(SuccessfulPrivateMessage, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        // Let receiver know about the new message
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(senderName);
                            writer.Write(content);

                            using (var msg = Message.Create(PrivateMessage, writer))
                            {
                                receivingClient.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        break;
                    }

                    case RoomMessage:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, MessageFailed, "Group/Room Message failed.")) return;

                        var senderName = _loginPlugin.UsersLoggedIn[client];
                        ushort roomId;
                        string content;

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                roomId = reader.ReadUInt16();
                                content = reader.ReadString();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return Error 0 for Invalid Data Packages Recieved
                            _loginPlugin.InvalidData(client, MessageFailed, ex, "Send Message failed! ");
                            return;
                        }

                        if (!_roomSystem.RoomList[roomId].Clients.Contains(client))
                        {
                            // If player isn't actually in the room -> return error 2
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);

                                using (var msg = Message.Create(MessageFailed, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }

                            WriteEvent("Send Message failed. Player wasn't part of the room.", LogType.Warning);
                            return;
                        }

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(senderName);
                            writer.Write(content);

                            using (var msg = Message.Create(RoomMessage, writer))
                            {
                                foreach (var cl in _roomSystem.RoomList[roomId].Clients)
                                    cl.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        break;
                    }

                    case GroupMessage:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, MessageFailed, "Group/Room Message failed.")) return;

                        var senderName = _loginPlugin.UsersLoggedIn[client];
                        string groupName;
                        string content;

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                groupName = reader.ReadString();
                                content = reader.ReadString();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return Error 0 for Invalid Data Packages Recieved
                            _loginPlugin.InvalidData(client, MessageFailed, ex, "Send Message failed! ");
                            return;
                        }

                        if (!ChatGroups[groupName].Users.Values.Contains(client))
                        {
                            // If player isn't actually in the chatgroup -> return error 2
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);

                                using (var msg = Message.Create(MessageFailed, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }

                            WriteEvent("Send Message failed. Player wasn't part of the chat group.", LogType.Warning);
                            return;
                        }

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(groupName);
                            writer.Write(senderName);
                            writer.Write(content);

                            using (var msg = Message.Create(GroupMessage, writer))
                            {
                                foreach (var cl in ChatGroups[groupName].Users.Values)
                                    cl.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        break;
                    }

                    case JoinGroup:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, JoinGroupFailed, "Join ChatGroup failed.")) return;

                        var playerName = _loginPlugin.UsersLoggedIn[client];
                        string groupName;

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                groupName = reader.ReadString();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return Error 0 for Invalid Data Packages Recieved
                            _loginPlugin.InvalidData(client, JoinGroupFailed, ex, "Join Chatgroup failed! ");
                            return;
                        }

                        // Create chatgroup if necessary and add player to it
                        var chatGroup = ChatGroups.FirstOrDefault(x =>
                            string.Equals(x.Key, groupName, StringComparison.CurrentCultureIgnoreCase)).Value;
                        if (chatGroup == null)
                        {
                            chatGroup = new ChatGroup(groupName);
                            ChatGroups[groupName] = chatGroup;
                        }

                        if (!chatGroup.AddPlayer(playerName, client))
                        {
                            // Already in Chatgroup -> return error 2
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);

                                using (var msg = Message.Create(JoinGroupFailed, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                            return;
                        }
                        if (!ChatGroupsOfPlayer.ContainsKey(playerName))
                        {
                            ChatGroupsOfPlayer[playerName] = new List<ChatGroup>();
                        }
                        ChatGroupsOfPlayer[playerName].Add(chatGroup);

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(chatGroup);

                            using (var msg = Message.Create(JoinGroup, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        if (_debug)
                        {
                            WriteEvent("Player joined ChatGroup: " + groupName, LogType.Info);
                        }
                        break;
                    }

                    case LeaveGroup:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, JoinGroupFailed, "Leave ChatGroup failed.")) return;

                        var playerName = _loginPlugin.UsersLoggedIn[client];
                        string groupName;

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                groupName = reader.ReadString();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return Error 0 for Invalid Data Packages Recieved
                            _loginPlugin.InvalidData(client, JoinGroupFailed, ex, "Leave ChatGroup failed! ");
                            return;
                        }

                        // get chatgroup if necessary and remove player from it
                        var chatGroup = ChatGroups.FirstOrDefault(x =>
                            string.Equals(x.Key, groupName, StringComparison.CurrentCultureIgnoreCase)).Value;
                        if (chatGroup == null)
                        {
                            // No such Chatgroup -> return error 2
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);

                                using (var msg = Message.Create(LeaveGroupFailed, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                            return;
                        }
                        chatGroup.RemovePlayer(playerName);

                        // Remove Chatgroup if he was the last player in it
                        if (chatGroup.Users.Count == 0 && chatGroup.Name != "General")
                        {
                            ChatGroups.TryRemove(chatGroup.Name, out _);
                        }

                        // Remove chatgroup from the players groups
                        if (ChatGroupsOfPlayer[playerName].Count == 0)
                        {
                            ChatGroupsOfPlayer.TryRemove(playerName, out _);
                        }
                        else
                        {
                            ChatGroupsOfPlayer[playerName].Remove(chatGroup);
                        }

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(chatGroup.Name);

                            using (var msg = Message.Create(LeaveGroup, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        if (_debug)
                        {
                            WriteEvent("Player left ChatGroup: " + groupName, LogType.Info);
                        }
                        break;
                    }
                    case GetActiveGroups:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, GetActiveGroupsFailed, "Get ChatGroups failed.")) return;

                        var groupNames = ChatGroups.Values.Select(chatGroup => chatGroup.Name).ToArray();

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(groupNames);

                            using (var msg = Message.Create(GetActiveGroups, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        break;
                    }
                }
            }
        }

        private void RemovePlayerFromChatGroups(string username)
        {
            if (!ChatGroupsOfPlayer.ContainsKey(username)) return;

            foreach (var chatGroup in ChatGroupsOfPlayer[username])
            {
                ChatGroups[chatGroup.Name].RemovePlayer(username);
                if (chatGroup.Users.Count == 0 && chatGroup.Name != "General")
                {
                    ChatGroups.TryRemove(chatGroup.Name, out _);
                }
            }
            ChatGroupsOfPlayer.TryRemove(username, out _);
        }

        private void GetChatGroupsCommand(object sender, CommandEventArgs e)
        {
            WriteEvent("Active Chatgroups:", LogType.Info);
            var chatGroups = ChatGroups.Values.ToList();
            if (e.Arguments.Length == 0)
            {
                foreach (var chatGroup in chatGroups)
                    WriteEvent(chatGroup.Name + " - " + chatGroup.Users.Count, LogType.Info);
            }
            else
            {
                var username = e.Arguments[0];
                if (!ChatGroupsOfPlayer.ContainsKey(username))
                {
                    WriteEvent(username + " doesn't exist in any chatgroups.", LogType.Info);
                    return;
                }
                foreach (var chatGroup in ChatGroupsOfPlayer[username])
                    WriteEvent(chatGroup.Name, LogType.Info);
            }
        }
    }
}