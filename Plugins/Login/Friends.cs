using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Database;

namespace LoginPlugin
{
    public class Friends : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("AddFriend", "Adds a User to the Database [AddFriend name friend]", "", AddFriendCommand),
            new Command("DelFriend", "Deletes a User from the Database [DelFriend name friend]", "", DelFriendCommand)
        };

        // Tag
        private const byte FriendsTag = 1;
        private const ushort Shift = FriendsTag * Login.TagsPerPlugin;

        // Subjects
        private const ushort FriendRequest = 0 + Shift;
        private const ushort RequestFailed = 1 + Shift;
        private const ushort RequestSuccess = 2 + Shift;
        private const ushort AcceptRequest = 3 + Shift;
        private const ushort AcceptRequestSuccess = 4 + Shift;
        private const ushort AcceptRequestFailed = 5 + Shift;
        private const ushort DeclineRequest = 6 + Shift;
        private const ushort DeclineRequestSuccess = 7 + Shift;
        private const ushort DeclineRequestFailed = 8 + Shift;
        private const ushort RemoveFriend = 9 + Shift;
        private const ushort RemoveFriendSuccess = 10 + Shift;
        private const ushort RemoveFriendFailed = 11 + Shift;
        private const ushort GetAllFriends = 12 + Shift;
        private const ushort GetAllFriendsFailed = 13 + Shift;
        private const ushort FriendLoggedIn = 14 + Shift;
        private const ushort FriendLoggedOut = 15 + Shift;

        private const string ConfigPath = @"Plugins/Friends.xml";
        private static readonly object InitializeLock = new object();
        private DatabaseProxy _database;
        private bool _debug = true;
        private Login _loginPlugin;

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
                    WriteEvent("Created /Plugins/Friends.xml!", LogType.Info);
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
            // If you have DR2 Pro, use the Loaded() method instead and spare yourself the locks
            if (_database == null)
            {
                lock (InitializeLock)
                {
                    if (_database == null)
                    {
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                        _loginPlugin = PluginManager.GetPluginByType<Login>();

                        _loginPlugin.onLogout += LogoutFriend;
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
                if (message.Tag < Login.TagsPerPlugin * FriendsTag || message.Tag >= Login.TagsPerPlugin * (FriendsTag + 1)) return;

                var client = e.Client;

                switch (message.Tag)
                {
                    case FriendRequest:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, RequestFailed, "Friend request failed.")) return;

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
                            _loginPlugin.InvalidData(client, RequestFailed, ex, "Friend Request Failed! ");
                            return;
                        }

                        try
                        {
                            _database.DataLayer.GetFriends(receiver, receiverUser =>
                            {
                                if (receiverUser == null)
                                {
                                    // No user with that name found -> return error 3
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write((byte) 3);

                                        using (var msg = Message.Create(RequestFailed, writer))
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
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write((byte) 4);

                                        using (var msg = Message.Create(RequestFailed, writer))
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
                                _database.DataLayer.AddRequest(senderName, receiver, () =>
                                {
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write(receiver);

                                        using (var msg = Message.Create(RequestSuccess, writer))
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

                                        using (var writer = DarkRiftWriter.Create())
                                        {
                                            writer.Write(senderName);

                                            using (var msg = Message.Create(FriendRequest, writer))
                                            {
                                                receivingClient.SendMessage(msg, SendMode.Reliable);
                                            }
                                        }
                                    }
                                });
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, RequestFailed, ex);
                        }
                        break;
                    }

                    case DeclineRequest:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, DeclineRequestFailed, "DeclineFriendRequest failed.")) return;

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
                            _loginPlugin.InvalidData(client, DeclineRequestFailed, ex, "Decline Request Failed!");
                            return;
                        }

                        try
                        {
                            // Delete the request from the database for both users
                            _database.DataLayer.RemoveRequest(senderName, receiver, () =>
                            {
                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(receiver);
                                    writer.Write(true);

                                    using (var msg = Message.Create(DeclineRequestSuccess, writer))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }
                                }

                                if (_debug)
                                {
                                    WriteEvent(senderName + " declined " + receiver + "'s friend request.", LogType.Info);
                                }

                                // If Receiver is currently logged in, let him know right away
                                if (_loginPlugin.Clients.ContainsKey(receiver))
                                {
                                    var receivingClient = _loginPlugin.Clients[receiver];

                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write(senderName);
                                        writer.Write(false);

                                        using (var msg = Message.Create(DeclineRequestSuccess, writer))
                                        {
                                            receivingClient.SendMessage(msg, SendMode.Reliable);
                                        }
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, DeclineRequestFailed, ex);
                        }
                        break;
                    }

                    case AcceptRequest:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, AcceptRequestFailed, "AcceptFriendRequest failed.")) return;

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
                            _loginPlugin.InvalidData(client, AcceptRequestFailed, ex, "Accept Request Failed!");
                            return;
                        }

                        try
                        {
                            // Delete the request from the database for both users and add their names to their friend list
                            _database.DataLayer.AddFriend(senderName, receiver, () =>
                            {
                                var receiverOnline = _loginPlugin.Clients.ContainsKey(receiver);

                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(receiver);
                                    writer.Write(receiverOnline);

                                    using (var msg = Message.Create(AcceptRequestSuccess, writer))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }
                                }

                                if (_debug)
                                {
                                    WriteEvent(senderName + " accepted " + receiver + "'s friend request.", LogType.Info);
                                }

                                // If Receiver is currently logged in, let him know right away
                                if (receiverOnline)
                                {
                                    var receivingClient = _loginPlugin.Clients[receiver];

                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write(senderName);
                                        writer.Write(true);

                                        using (var msg = Message.Create(AcceptRequestSuccess, writer))
                                        {
                                            receivingClient.SendMessage(msg, SendMode.Reliable);
                                        }
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, AcceptRequestFailed, ex);
                        }
                        break;
                    }

                    case RemoveFriend:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, RemoveFriendFailed, "RemoveFriend failed.")) return;

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
                            _loginPlugin.InvalidData(client, RemoveFriendFailed, ex, "Remove Friend Failed!");
                            return;
                        }

                        try
                        {
                            // Delete the names from the friendlist in the database for both users
                            _database.DataLayer.RemoveFriend(senderName, receiver, () =>
                            {
                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(receiver);
                                    writer.Write(true);

                                    using (var msg = Message.Create(RemoveFriendSuccess, writer))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }
                                }

                                if (_debug)
                                {
                                    WriteEvent(senderName + " removed " + receiver + " as a friend.", LogType.Info);
                                }

                                // If Receiver is currently logged in, let him know right away
                                if (_loginPlugin.Clients.ContainsKey(receiver))
                                {
                                    var receivingClient = _loginPlugin.Clients[receiver];

                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write(senderName);
                                        writer.Write(false);

                                        using (var msg = Message.Create(RemoveFriendSuccess, writer))
                                        {
                                            receivingClient.SendMessage(msg, SendMode.Reliable);
                                        }
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, RemoveFriendFailed, ex);
                        }
                        break;
                    }

                    case GetAllFriends:
                    {
                        // If player isn't logged in -> return error 1
                        if (!_loginPlugin.PlayerLoggedIn(client, GetAllFriendsFailed, "GetAllFriends failed.")) return;

                        var senderName = _loginPlugin.UsersLoggedIn[client];

                        try
                        {
                            _database.DataLayer.GetFriends(senderName, friendList =>
                            {
                                var onlineFriends = new List<string>();
                                var offlineFriends = new List<string>();

                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(senderName);

                                    foreach (var friend in friendList.Friends)
                                    {
                                        if (_loginPlugin.Clients.ContainsKey(friend))
                                        {
                                            onlineFriends.Add(friend);

                                            // let online friends know he logged in
                                            var cl = _loginPlugin.Clients[friend];

                                            using (var msg = Message.Create(FriendLoggedIn, writer))
                                            {
                                                cl.SendMessage(msg, SendMode.Reliable);
                                            }
                                        }
                                        else
                                        {
                                            offlineFriends.Add(friend);
                                        }
                                    }
                                }

                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(onlineFriends.ToArray());
                                    writer.Write(offlineFriends.ToArray());
                                    writer.Write(friendList.OpenFriendRequests.ToArray());
                                    writer.Write(friendList.UnansweredFriendRequests.ToArray());

                                    using (var msg = Message.Create(GetAllFriends, writer))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }
                                }

                                if (_debug)
                                {
                                    WriteEvent("Got friends for " + senderName, LogType.Info);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, GetAllFriendsFailed, ex);
                        }
                        break;
                    }
                }
            }
        }

        public void LogoutFriend(string username)
        {
            try
            {
                _database.DataLayer.GetFriends(username, friendList =>
                {
                    var friends = friendList.Friends;
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(username);

                        foreach (var friend in friends)
                        {
                            if (_loginPlugin.Clients.ContainsKey(friend))
                            {
                                // let online friends know he logged out
                                var client = _loginPlugin.Clients[friend];

                                using (var msg = Message.Create(FriendLoggedOut, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                WriteEvent($"Database Error. Failed to notify friends of Logout! \n\n{ex.Message}\n{ex.StackTrace}", LogType.Error);
            }
        }

        #region Commands

        private void AddFriendCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
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
                _database.DataLayer.AddFriend(username, friend, () =>
                {
                    if (_debug)
                    {
                        WriteEvent("Added " + friend + " as a friend of " + username, LogType.Info);
                    }
                });
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }

        private void DelFriendCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
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
                _database.DataLayer.RemoveFriend(username, friend, () =>
                {
                    if (_debug)
                    {
                        WriteEvent("Removed " + friend + " as a friend of " + username, LogType.Info);
                    }
                });
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }

        #endregion
    }
}