using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using DbConnectorPlugin;
using MongoDB.Driver;

namespace LoginPlugin
{
    public class Friends : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        public override Command[] Commands => new[]
        {
            new Command("AddFriend", "Adds a User to the Database [AddFriend name friend]", "", AddFriendCommand),
            new Command("DelFriend", "Deletes a User from the Database [DelFriend name friend]", "", DelFriendCommand)
        };

        // Tag
        private const byte FriendsTag = 1;

        // Subjects
        private const ushort FriendRequest = 0;
        private const ushort RequestFailed = 1;
        private const ushort RequestSuccess = 2;
        private const ushort AcceptRequest = 3;
        private const ushort AcceptRequestSuccess = 4;
        private const ushort AcceptRequestFailed = 5;
        private const ushort DeclineRequest = 6;
        private const ushort DeclineRequestSuccess = 7;
        private const ushort DeclineRequestFailed = 8;
        private const ushort RemoveFriend = 9;
        private const ushort RemoveFriendSuccess = 10;
        private const ushort RemoveFriendFailed = 11;
        private const ushort GetAllFriends = 12;
        private const ushort GetAllFriendsFailed = 13;
        private const ushort FriendLoggedIn = 14;
        private const ushort FriendLoggedOut = 15;

        private const string ConfigPath = @"Plugins\Friends.xml";
        private DbConnector _dbConnector;
        private Login _loginPlugin;
        private bool _debug = true;

        public Friends(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
                    new XComment("Settings for the Friends Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    WriteEvent("Created /Plugins/Friends.xml!", LogType.Warning);
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create Friends.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
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
                    WriteEvent("Failed to load Friends.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                }
            }
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            // If you have DR2 Pro, use the Plugin.Loaded() method to get the DbConnector Plugin instead
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
                _loginPlugin = PluginManager.GetPluginByType<Login>();

                _loginPlugin.onLogout += LogoutFriend;
            }

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                var client = e.Client;

                // Friend Request
                if (message.Tag == GetTag(FriendRequest))
                {
                    // If player isn't logged in -> return error 1
                    if (!_loginPlugin.PlayerLoggedIn(client, FriendsTag, RequestFailed, "Friend request failed."))
                        return;

                    var senderName = _loginPlugin.UsersLoggedIn[client];
                    string receiver;

                    try
                    {
                        using (var reader = message.GetReader())
                        {
                            receiver = reader.ReadString();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return Error 0 for Invalid Data Packages Recieved
                        _loginPlugin.InvalidData(client, FriendsTag, RequestFailed, ex, "Friend Request Failed! ");
                        return;
                    }

                    try
                    {
                        var receiverUser = _dbConnector.Users.AsQueryable().FirstOrDefault(u => u.Username == receiver);
                        if (receiverUser == null)
                        {
                            // No user with that name found -> return error 3
                            using (var wr = DarkRiftWriter.Create())
                            {
                                wr.Write((byte) 3);

                                using (var msg = Message.Create(GetTag(RequestFailed), wr))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }

                            if (_debug)
                            {
                                WriteEvent("No user named " + receiver + " found!", LogType.Info);
                            }
                            return;
                        }

                        if (receiverUser.Friends.Contains(senderName) ||
                            receiverUser.OpenFriendRequests.Contains(senderName))
                        {
                            // Users are already friends or have an open request -> return error 4
                            using (var wr = DarkRiftWriter.Create())
                            {
                                wr.Write((byte) 4);

                                using (var msg = Message.Create(GetTag(RequestFailed), wr))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }

                            if (_debug)
                            {
                                WriteEvent("Request failed, " + senderName + " and " + receiver +
                                           " were already friends or had an open friend request!", LogType.Info);
                            }
                            return;
                        }

                        // Save the request in the database to both users
                        AddRequests(senderName, receiver);

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(receiver);

                            using (var msg = Message.Create(GetTag(RequestSuccess), writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        if (_debug)
                        {
                            WriteEvent(senderName + " wants to add " + receiver + " as a friend!", LogType.Info);
                        }

                        // If Receiver is currently logged in, let him know right away
                        if (_loginPlugin.Clients.ContainsKey(receiver))
                        {
                            var receivingClient = _loginPlugin.Clients[receiver];

                            using (var wr = DarkRiftWriter.Create())
                            {
                                wr.Write(senderName);

                                using (var msg = Message.Create(GetTag(FriendRequest), wr))
                                {
                                    receivingClient.SendMessage(msg,SendMode.Reliable);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return Error 2 for Database error
                        _dbConnector.DatabaseError(client, FriendsTag, RequestFailed, ex);
                    }
                }

                // Friend Request Declined
                if (message.Subject == DeclineRequest)
                {
                    // If player isn't logged in -> return error 1
                    if (!_loginPlugin.PlayerLoggedIn(client, FriendsTag, DeclineRequestFailed,
                        "DeclineFriendRequest failed."))
                        return;

                    var senderName = _loginPlugin.UsersLoggedIn[client];
                    string receiver;

                    try
                    {
                        var reader = message.GetReader();
                        receiver = reader.ReadString();
                    }
                    catch (Exception ex)
                    {
                        // Return Error 0 for Invalid Data Packages Recieved
                        _loginPlugin.InvalidData(client, FriendsTag, DeclineRequestFailed, ex,
                            "Decline Request Failed!");
                        return;
                    }

                    try
                    {
                        // Delete the request from the database for both users
                        RemoveRequests(senderName, receiver);

                        var writer = new DarkRiftWriter();
                        writer.Write(receiver);
                        writer.Write(true);

                        client.SendMessage(new TagSubjectMessage(FriendsTag, DeclineRequestSuccess, writer),
                            SendMode.Reliable);

                        if (_debug)
                        {
                            WriteEvent(senderName + " declined " + receiver + "'s friend request.", LogType.Info);
                        }

                        // If Receiver is currently logged in, let him know right away
                        if (_loginPlugin.Clients.ContainsKey(receiver))
                        {
                            var receivingClient = _loginPlugin.Clients[receiver];
                            var wr = new DarkRiftWriter();
                            wr.Write(senderName);
                            wr.Write(false);

                            receivingClient.SendMessage(new TagSubjectMessage(FriendsTag, DeclineRequestSuccess, wr),
                                SendMode.Reliable);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return Error 2 for Database error
                        _dbConnector.DatabaseError(client, FriendsTag, DeclineRequestFailed, ex);
                    }
                }

                // Friend Request Accepted
                if (message.Subject == AcceptRequest)
                {
                    // If player isn't logged in -> return error 1
                    if (!_loginPlugin.PlayerLoggedIn(client, FriendsTag, AcceptRequestFailed,
                        "AcceptFriendRequest failed."))
                        return;

                    var senderName = _loginPlugin.UsersLoggedIn[client];
                    string receiver;

                    try
                    {
                        var reader = message.GetReader();
                        receiver = reader.ReadString();
                    }
                    catch (Exception ex)
                    {
                        // Return Error 0 for Invalid Data Packages Recieved
                        _loginPlugin.InvalidData(client, FriendsTag, AcceptRequestFailed, ex, "Accept Request Failed!");
                        return;
                    }

                    try
                    {
                        // Delete the request from the database for both users and add their names to their friend list
                        RemoveRequests(senderName, receiver);
                        AddFriends(senderName, receiver);

                        var receiverOnline = _loginPlugin.Clients.ContainsKey(receiver);

                        var writer = new DarkRiftWriter();
                        writer.Write(receiver);
                        writer.Write(receiverOnline);

                        client.SendMessage(new TagSubjectMessage(FriendsTag, AcceptRequestSuccess, writer),
                            SendMode.Reliable);

                        if (_debug)
                        {
                            WriteEvent(senderName + " accepted " + receiver + "'s friend request.", LogType.Info);
                        }

                        // If Receiver is currently logged in, let him know right away
                        if (receiverOnline)
                        {
                            var receivingClient = _loginPlugin.Clients[receiver];
                            var wr = new DarkRiftWriter();
                            wr.Write(senderName);
                            wr.Write(true);

                            receivingClient.SendMessage(new TagSubjectMessage(FriendsTag, AcceptRequestSuccess, wr),
                                SendMode.Reliable);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return Error 2 for Database error
                        _dbConnector.DatabaseError(client, FriendsTag, AcceptRequestFailed, ex);
                    }
                }

                // Remove Friend
                if (message.Subject == RemoveFriend)
                {
                    // If player isn't logged in -> return error 1
                    if (!_loginPlugin.PlayerLoggedIn(client, FriendsTag, RemoveFriendFailed, "RemoveFriend failed."))
                        return;

                    var senderName = _loginPlugin.UsersLoggedIn[client];
                    string receiver;

                    try
                    {
                        var reader = message.GetReader();
                        receiver = reader.ReadString();
                    }
                    catch (Exception ex)
                    {
                        // Return Error 0 for Invalid Data Packages Recieved
                        _loginPlugin.InvalidData(client, FriendsTag, RemoveFriendFailed, ex, "Remove Friend Failed!");
                        return;
                    }

                    try
                    {
                        // Delete the names from the friendlist in the database for both users
                        RemoveFriends(senderName, receiver);

                        var writer = new DarkRiftWriter();
                        writer.Write(receiver);
                        writer.Write(true);

                        client.SendMessage(new TagSubjectMessage(FriendsTag, RemoveFriendSuccess, writer),
                            SendMode.Reliable);

                        if (_debug)
                        {
                            WriteEvent(senderName + " removed " + receiver + " as a friend.", LogType.Info);
                        }

                        // If Receiver is currently logged in, let him know right away
                        if (_loginPlugin.Clients.ContainsKey(receiver))
                        {
                            var receivingClient = _loginPlugin.Clients[receiver];
                            var wr = new DarkRiftWriter();
                            wr.Write(senderName);
                            wr.Write(false);

                            receivingClient.SendMessage(new TagSubjectMessage(FriendsTag, RemoveFriendSuccess, wr),
                                SendMode.Reliable);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return Error 2 for Database error
                        _dbConnector.DatabaseError(client, FriendsTag, RemoveFriendFailed, ex);
                    }
                }

                // Get all friends and their status
                if (message.Subject == GetAllFriends)
                {
                    // If player isn't logged in -> return error 1
                    if (!_loginPlugin.PlayerLoggedIn(client, FriendsTag, GetAllFriendsFailed, "GetAllFriends failed."))
                        return;

                    var senderName = _loginPlugin.UsersLoggedIn[client];

                    try
                    {
                        var user = _dbConnector.Users.AsQueryable().First(u => u.Username == senderName);
                        var onlineFriends = new List<string>();
                        var offlineFriends = new List<string>();

                        var writer = new DarkRiftWriter();
                        writer.Write(senderName);

                        foreach (var friend in user.Friends)
                        {
                            if (_loginPlugin.Clients.ContainsKey(friend))
                            {
                                onlineFriends.Add(friend);

                                // let online friends know he logged in
                                var cl = _loginPlugin.Clients[friend];
                                cl.SendMessage(new TagSubjectMessage(FriendsTag, FriendLoggedIn, writer),
                                    SendMode.Reliable);
                            }
                            else
                            {
                                offlineFriends.Add(friend);
                            }
                        }

                        var wr = new DarkRiftWriter();
                        wr.Write(onlineFriends.ToArray());
                        wr.Write(offlineFriends.ToArray());
                        wr.Write(user.OpenFriendRequests.ToArray());
                        wr.Write(user.UnansweredFriendRequests.ToArray());

                        client.SendMessage(new TagSubjectMessage(FriendsTag, GetAllFriends, wr), SendMode.Reliable);

                        if (_debug)
                        {
                            WriteEvent("Got friends for " + senderName, LogType.Info);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return Error 2 for Database error
                        _dbConnector.DatabaseError(client, FriendsTag, GetAllFriendsFailed, ex);
                    }
                }
            }
        }

        public void LogoutFriend(string username)
        {
            var friends = _dbConnector.Users.AsQueryable().First(u => u.Username == username).Friends;
            var writer = new DarkRiftWriter();
            writer.Write(username);

            foreach (var friend in friends)
            {
                if (_loginPlugin.Clients.ContainsKey(friend))
                {
                    // let online friends know he logged out
                    var cl = _loginPlugin.Clients[friend];
                    cl.SendMessage(new TagSubjectMessage(FriendsTag, FriendLoggedOut, writer),
                        SendMode.Reliable);
                }
            }
        }

        private ushort GetTag(ushort subject)
        {
            return _dbConnector.GetTag(FriendsTag, subject);
        }

        #region DbHelpers

        private void AddRequests(string sender, string receiver)
        {
            var updateReceiving = Builders<User>.Update.AddToSet(u => u.OpenFriendRequests, sender);
            _dbConnector.Users.UpdateOne(u => u.Username == receiver, updateReceiving);
            var updateSender = Builders<User>.Update.AddToSet(u => u.UnansweredFriendRequests, receiver);
            _dbConnector.Users.UpdateOne(u => u.Username == sender, updateSender);
        }

        private void RemoveRequests(string sender, string receiver)
        {
            var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
            _dbConnector.Users.UpdateOne(u => u.Username == sender, updateSender);
            var updateReceiving = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            _dbConnector.Users.UpdateOne(u => u.Username == receiver, updateReceiving);
        }

        private void AddFriends(string sender, string receiver)
        {
            var updateReceiving = Builders<User>.Update.AddToSet(u => u.Friends, sender);
            _dbConnector.Users.UpdateOne(u => u.Username == receiver, updateReceiving);
            var updateSending = Builders<User>.Update.AddToSet(u => u.Friends, receiver);
            _dbConnector.Users.UpdateOne(u => u.Username == sender, updateSending);
        }

        private void RemoveFriends(string sender, string receiver)
        {
            var senderUser = _dbConnector.Users.AsQueryable().First(u => u.Username == sender);
            var receiverUser = _dbConnector.Users.AsQueryable().First(u => u.Username == receiver);

            // Update sender
            if (senderUser.Friends.Contains(receiver))
            {
                var updateSender = Builders<User>.Update.Pull(u => u.Friends, receiver);
                _dbConnector.Users.UpdateOne(u => u.Username == sender, updateSender);
            }
            if (senderUser.OpenFriendRequests.Contains(receiver))
            {
                var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
                _dbConnector.Users.UpdateOne(u => u.Username == sender, updateSender);
            }
            if (senderUser.UnansweredFriendRequests.Contains(receiver))
            {
                var updateSender = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, receiver);
                _dbConnector.Users.UpdateOne(u => u.Username == sender, updateSender);
            }

            //Update receiver
            if (receiverUser.Friends.Contains(sender))
            {
                var updateReceiver = Builders<User>.Update.Pull(u => u.Friends, sender);
                _dbConnector.Users.UpdateOne(u => u.Username == receiver, updateReceiver);
            }
            if (receiverUser.OpenFriendRequests.Contains(sender))
            {
                var updateReceiver = Builders<User>.Update.Pull(u => u.OpenFriendRequests, sender);
                _dbConnector.Users.UpdateOne(u => u.Username == receiver, updateReceiver);
            }
            if (receiverUser.UnansweredFriendRequests.Contains(sender))
            {
                var updateReceiver = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
                _dbConnector.Users.UpdateOne(u => u.Username == receiver, updateReceiver);
            }
        }

        #endregion

        #region Commands

        private void AddFriendCommand(object sender, CommandEventArgs e)
        {
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
            }

            if (e.Arguments.Length != 2)
            {
                WriteEvent("Invalid arguments. Enter [AddFríend name friend].", LogType.Warning);
                return;
            }

            var username = e.Arguments[0];
            var friend = e.Arguments[1];

            try
            {
                AddFriends(username, friend);

                if (_debug)
                {
                    WriteEvent("Added " + friend + " as a friend of " + username, LogType.Info);
                }
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }

        private void DelFriendCommand(object sender, CommandEventArgs e)
        {
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
            }

            if (e.Arguments.Length != 2)
            {
                WriteEvent("Invalid arguments. Enter [AddFríend name friend].", LogType.Warning);
                return;
            }

            var username = e.Arguments[0];
            var friend = e.Arguments[1];

            try
            {
                RemoveFriends(username, friend);

                if (_debug)
                {
                    WriteEvent("Removed " + friend + " as a friend of " + username, LogType.Info);
                }
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }
        #endregion
    }
}
