using System;
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
        public override bool ThreadSafe => false;

        public override Command[] Commands => new[]
        {
            new Command("groups", "Shows all chatgroups [groups username(optional]", "", GetChatGroupsCommand)
        };

        // Tag
        private const byte ChatTag = 2;

        // Subjects
        private const ushort PrivateMessage = 0;
        private const ushort SuccessfulPrivateMessage = 1;
        private const ushort RoomMessage = 2;
        private const ushort GroupMessage = 3;
        private const ushort MessageFailed = 4;
        private const ushort JoinGroup = 5;
        private const ushort JoinGroupFailed = 6;
        private const ushort LeaveGroup = 7;
        private const ushort LeaveGroupFailed = 8;
        private const ushort GetActiveGroups = 9;
        private const ushort GetActiveGroupsFailed = 10;

        private const string ConfigPath = @"Plugins\Chat.xml";
        private Login _loginPlugin;
        private RoomSystem _roomSystem;
        private bool _debug = true;

        public Dictionary<string, ChatGroup> ChatGroups = new Dictionary<string, ChatGroup>();
        public Dictionary<string, List<ChatGroup>> ChatGroupsOfPlayer = new Dictionary<string, List<ChatGroup>>();

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
                    WriteEvent("Created /Plugins/Chat.xml!", LogType.Warning);
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
            // If you have DR2 Pro, use the Plugin.Loaded() method instead
            if (_loginPlugin == null)
            {
                _loginPlugin = PluginManager.GetPluginByType<Login>();
                _roomSystem = PluginManager.GetPluginByType<RoomSystem>();
                _loginPlugin.onLogout += RemovePlayerFromChatGroups;
                ChatGroups["General"] = new ChatGroup("General");
            }

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!(e.Message is TagSubjectMessage message) || message.Tag != ChatTag)
                return;

            var client = (Client)sender;

            // Private Message
            if (message.Subject == PrivateMessage)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTag, MessageFailed, "Private Message failed."))
                    return;

                var senderName = _loginPlugin.UsersLoggedIn[client];
                string receiver;
                string content;

                try
                {
                    var reader = message.GetReader();
                    receiver = reader.ReadString();
                    content = reader.ReadString();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, ChatTag, MessageFailed, ex, "Send Message failed! ");
                    return;
                }

                if (!_loginPlugin.Clients.ContainsKey(receiver))
                {
                    // If receiver isn't logged in -> return error 3
                    var wr = new DarkRiftWriter();
                    wr.Write((byte)3);
                    client.SendMessage(new TagSubjectMessage(ChatTag, MessageFailed, wr), SendMode.Reliable);

                    if (_debug)
                    {
                        WriteEvent("Send Message failed. Receiver wasn't logged in.", LogType.Info);
                    }
                    return;
                }

                var receivingClient = _loginPlugin.Clients[receiver];

                var writer = new DarkRiftWriter();
                writer.Write(senderName);
                writer.Write(receiver);
                writer.Write(content);
                client.SendMessage(new TagSubjectMessage(ChatTag, SuccessfulPrivateMessage, writer), SendMode.Reliable);

                writer = new DarkRiftWriter();
                writer.Write(senderName);
                writer.Write(content);
                receivingClient.SendMessage(new TagSubjectMessage(ChatTag, PrivateMessage, writer), SendMode.Reliable);
            }
            // Room Message
            else if (message.Subject == RoomMessage)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTag, MessageFailed, "Group/Room Message failed."))
                    return;

                var senderName = _loginPlugin.UsersLoggedIn[client];
                ushort roomId;
                string content;

                try
                {
                    var reader = message.GetReader();
                    roomId = reader.ReadUInt16();
                    content = reader.ReadString();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, ChatTag, MessageFailed, ex, "Send Message failed! ");
                    return;
                }

                var writer = new DarkRiftWriter();
                writer.Write(senderName);
                writer.Write(content);

                if (!_roomSystem.RoomList[roomId].Clients.Contains(client))
                {
                    // If player isn't actually in the room -> return error 2
                    var wr = new DarkRiftWriter();
                    wr.Write((byte)2);
                    client.SendMessage(new TagSubjectMessage(ChatTag, MessageFailed, wr), SendMode.Reliable);

                    WriteEvent("Send Message failed. Player wasn't part of the room.", LogType.Warning);
                    return;
                }

                foreach (var cl in _roomSystem.RoomList[roomId].Clients)
                {
                    cl.SendMessage(new TagSubjectMessage(ChatTag, RoomMessage, writer), SendMode.Reliable);
                }
            }
            // ChatGroup Message
            else if (message.Subject == GroupMessage)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTag, MessageFailed, "Group/Room Message failed."))
                    return;

                var senderName = _loginPlugin.UsersLoggedIn[client];
                string groupName;
                string content;

                try
                {
                    var reader = message.GetReader();
                    groupName = reader.ReadString();
                    content = reader.ReadString();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, ChatTag, MessageFailed, ex, "Send Message failed! ");
                    return;
                }

                if (!ChatGroups[groupName].Users.Values.Contains(client))
                {
                    // If player isn't actually in the chatgroup -> return error 2
                    var wr = new DarkRiftWriter();
                    wr.Write((byte)2);
                    client.SendMessage(new TagSubjectMessage(ChatTag, MessageFailed, wr), SendMode.Reliable);

                    WriteEvent("Send Message failed. Player wasn't part of the chat group.", LogType.Warning);
                    return;
                }

                var writer = new DarkRiftWriter();
                writer.Write(groupName);
                writer.Write(senderName);
                writer.Write(content);

                foreach (var cl in ChatGroups[groupName].Users.Values)
                {
                    cl.SendMessage(new TagSubjectMessage(ChatTag, GroupMessage, writer), SendMode.Reliable);
                }
            }
            // Join Chatgroup
            else if (message.Subject == JoinGroup)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTag, JoinGroupFailed, "Join ChatGroup failed."))
                    return;

                var playerName = _loginPlugin.UsersLoggedIn[client];
                string groupName;

                try
                {
                    var reader = message.GetReader();
                    groupName = reader.ReadString();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, ChatTag, JoinGroupFailed, ex, "Join Chatgroup failed! ");
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
                    var wr = new DarkRiftWriter();
                    wr.Write((byte)2);

                    client.SendMessage(new TagSubjectMessage(ChatTag, JoinGroupFailed, wr), SendMode.Reliable);
                    return;
                }
                if (!ChatGroupsOfPlayer.ContainsKey(playerName))
                {
                    ChatGroupsOfPlayer[playerName] = new List<ChatGroup>();
                }
                ChatGroupsOfPlayer[playerName].Add(chatGroup);

                var writer = new DarkRiftWriter();
                writer.Write(chatGroup);

                client.SendMessage(new TagSubjectMessage(ChatTag, JoinGroup, writer), SendMode.Reliable);

                if (_debug)
                {
                    WriteEvent("Player joined ChatGroup: " + groupName, LogType.Info);
                }
            }
            // Leave Chatgroup
            else if (message.Subject == LeaveGroup)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTag, JoinGroupFailed, "Leave ChatGroup failed."))
                    return;

                var playerName = _loginPlugin.UsersLoggedIn[client];
                string groupName;

                try
                {
                    var reader = message.GetReader();
                    groupName = reader.ReadString();
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    _loginPlugin.InvalidData(client, ChatTag, JoinGroupFailed, ex, "Leave ChatGroup failed! ");
                    return;
                }

                // get chatgroup if necessary and remove player from it
                var chatGroup = ChatGroups.FirstOrDefault(x =>
                    string.Equals(x.Key, groupName, StringComparison.CurrentCultureIgnoreCase)).Value;
                if (chatGroup == null)
                {
                    // No such Chatgroup -> return error 2
                    var wr = new DarkRiftWriter();
                    wr.Write((byte)2);

                    client.SendMessage(new TagSubjectMessage(ChatTag, LeaveGroupFailed, wr), SendMode.Reliable);
                    return;
                }
                chatGroup.RemovePlayer(playerName);

                // Remove Chatgroup if he was the last player in it
                if (chatGroup.Users.Count == 0 && chatGroup.Name != "General")
                {
                    ChatGroups.Remove(chatGroup.Name);
                }

                // Remove chatgroup from the players groups
                if (ChatGroupsOfPlayer[playerName].Count == 0)
                {
                    ChatGroupsOfPlayer.Remove(playerName);
                }
                else
                {
                    ChatGroupsOfPlayer[playerName].Remove(chatGroup);
                }

                var writer = new DarkRiftWriter();
                writer.Write(chatGroup.Name);

                client.SendMessage(new TagSubjectMessage(ChatTag, LeaveGroup, writer), SendMode.Reliable);

                if (_debug)
                {
                    WriteEvent("Player left ChatGroup: " + groupName, LogType.Info);
                }
            }
            // Get ChatGroup List
            else if (message.Subject == GetActiveGroups)
            {
                // If player isn't logged in -> return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTag, GetActiveGroupsFailed, "Get ChatGroups failed."))
                    return;

                var groupNames = ChatGroups.Values.Select(chatGroup => chatGroup.Name).ToArray();

                var writer = new DarkRiftWriter();
                writer.Write(groupNames);

                client.SendMessage(new TagSubjectMessage(ChatTag, GetActiveGroups, writer), SendMode.Reliable);
            }
        }

        private void RemovePlayerFromChatGroups(string username)
        {
            if (!ChatGroupsOfPlayer.ContainsKey(username))
                return;

            foreach (var chatGroup in ChatGroupsOfPlayer[username])
            {
                ChatGroups[chatGroup.Name].RemovePlayer(username);
                if (chatGroup.Users.Count == 0 && chatGroup.Name != "General")
                {
                    ChatGroups.Remove(chatGroup.Name);
                }
            }
            ChatGroupsOfPlayer.Remove(username);
        }

        private void GetChatGroupsCommand(object sender, CommandEventArgs e)
        {
            WriteEvent("Active Chatgroups:", LogType.Info);
            var chatGroups = ChatGroups.Values.ToList();
            if (e.Arguments.Length == 0)
            {
                foreach (var chatGroup in chatGroups)
                {
                    WriteEvent(chatGroup.Name + " - " + chatGroup.Users.Count, LogType.Info);
                }
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
                {
                    WriteEvent(chatGroup.Name, LogType.Info);
                }
            }
        }
    }
}
