using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Database;

namespace LoginPlugin
{
    public class Login : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;
        public override Command[] Commands => new[]
        {
            new Command("AllowAddUser", "Allow Users to be added to the Database [AllowAddUser on/off]", "",
                AllowAddUserCommand),
            new Command("AddUser", "Adds a User to the Database [AddUser name password]", "", AddUserCommand),
            new Command("DelUser", "Deletes a User from the Database [DelUser name]", "", DelUserCommand),
            new Command("LPDebug", "Enables Plugin Debug", "", DebugCommand),
            new Command("LoggedIn", "Logs number of online users", "", UsersLoggedInCommand),
            new Command("Online", "Logs number of online users", "", UsersOnlineCommand)
        };

        public ConcurrentDictionary<string, IClient> Clients { get; } = new ConcurrentDictionary<string, IClient>();
        public ConcurrentDictionary<IClient, string> UsersLoggedIn { get; } = new ConcurrentDictionary<IClient, string>();

        public delegate void LogoutEventHandler(string username);

        // Maximum number of Tags per Plugin
        public const ushort TagsPerPlugin = 256;

        // Tag
        private const byte LoginTag = 0;
        private const ushort Shift = LoginTag * TagsPerPlugin;

        // Subjects
        private const ushort LoginUser = 0 + Shift;
        private const ushort LogoutUser = 1 + Shift;
        private const ushort AddUser = 2 + Shift;
        private const ushort LoginSuccess = 3 + Shift;
        private const ushort LoginFailed = 4 + Shift;
        private const ushort LogoutSuccess = 5 + Shift;
        private const ushort AddUserSuccess = 6 + Shift;
        private const ushort AddUserFailed = 7 + Shift;

        private const string ConfigPath = @"Plugins/Login.xml";
        private const string PrivateKeyPath = @"Plugins/PrivateKey.xml";
        private static readonly object InitializeLock = new object();
        private bool _allowAddUser = true;
        private DatabaseProxy _database;
        private bool _debug = true;
        private string _privateKey;

        public Login(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig();
            LoadRsaKey();
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        public event LogoutEventHandler onLogout;

        private void LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Settings for the Login Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true), new XAttribute("AllowAddUser", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    WriteEvent("Created /Plugins/Login.xml!", LogType.Info);
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create Login.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                }
            }
            else
            {
                try
                {
                    document = XDocument.Load(ConfigPath);
                    _debug = document.Element("Variables").Attribute("Debug").Value == "true";
                    _allowAddUser = document.Element("Variables").Attribute("AllowAddUser").Value == "true";
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to load Login.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                }
            }
        }

        private void LoadRsaKey()
        {
            try
            {
                _privateKey = File.ReadAllText(PrivateKeyPath);
            }
            catch (Exception ex)
            {
                WriteEvent("Failed to load PrivateKey.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Fatal);
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
                    }
                }
            }

            UsersLoggedIn[e.Client] = null;

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (UsersLoggedIn.ContainsKey(e.Client))
            {
                UsersLoggedIn.TryRemove(e.Client, out var username);

                if (username != null)
                {
                    Clients.TryRemove(username, out _);
                    onLogout?.Invoke(username);
                }
            }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag >= TagsPerPlugin * (LoginTag + 1)) return;

                var client = e.Client;

                switch (message.Tag)
                {
                    case LoginUser:
                    {
                        // If user is already logged in (shouldn't happen though)
                        if (UsersLoggedIn[client] != null)
                        {
                            using (var msg = Message.CreateEmpty(LoginSuccess))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }

                            return;
                        }

                        string username;
                        string password;

                        using (var reader = message.GetReader())
                        {
                            try
                            {
                                username = reader.ReadString();
                                password = Encryption.Decrypt(reader.ReadBytes(), _privateKey);
                            }
                            catch (Exception ex)
                            {
                                // Return Error 0 for Invalid Data Packages Recieved
                                InvalidData(client, LoginFailed, ex, "Failed to log in!");
                                return;
                            }
                        }

                        if (Clients.ContainsKey(username))
                        {
                            // Username is already in use -> return Error 3
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 3);

                                using (var msg = Message.Create(LoginFailed, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                            return;
                        }

                        try
                        {
                            _database.DataLayer.GetUser(username, user =>
                            {
                                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                                {
                                    UsersLoggedIn[client] = username;
                                    Clients[username] = client;

                                    using (var msg = Message.CreateEmpty(LoginSuccess))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }

                                    if (_debug)
                                    {
                                        WriteEvent("Successful login (" + client.ID + ").", LogType.Info);
                                    }
                                }
                                else
                                {
                                    if (_debug)
                                    {
                                        WriteEvent("User " + client.ID + " couldn't log in!", LogType.Info);
                                    }

                                    // Return Error 1 for "Wrong username/password combination"
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write((byte) 1);

                                        using (var msg = Message.Create(LoginFailed, writer))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, LoginFailed, ex);
                        }
                        break;
                    }

                    case LogoutUser:
                    {
                        var username = UsersLoggedIn[client];
                        UsersLoggedIn[client] = null;

                        if (username != null)
                        {
                            Clients.TryRemove(username, out _);
                        }

                        if (_debug)
                        {
                            WriteEvent("User " + client.ID + " logged out!", LogType.Info);
                        }

                        using (var msg = Message.CreateEmpty(LogoutSuccess))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }

                        onLogout?.Invoke(username);
                        break;
                    }

                    case AddUser:
                    {
                        if (!_allowAddUser) return;

                        string username;
                        string password;

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                username = reader.ReadString();

                                password = BCrypt.Net.BCrypt.HashPassword(
                                    Encryption.Decrypt(reader.ReadBytes(), _privateKey)
                                    , 10);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return Error 0 for Invalid Data Packages Recieved
                            InvalidData(client, AddUserFailed, ex, "Failed to add user!");
                            return;
                        }

                        try
                        {
                            _database.DataLayer.UsernameAvailable(username, isAvailable =>
                            {
                                if (isAvailable)
                                {
                                    _database.DataLayer.AddNewUser(username, password, () =>
                                    {
                                        if (_debug)
                                        {
                                            WriteEvent("New User: " + username, LogType.Info);
                                        }

                                        using (var msg = Message.CreateEmpty(AddUserSuccess))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                    });
                                }
                                else
                                {
                                    if (_debug)
                                    {
                                        WriteEvent("User " + client.ID + " failed to sign up!", LogType.Info);
                                    }

                                    // Return Error 1 for "Wrong username/password combination"
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write((byte) 1);

                                        using (var msg = Message.Create(AddUserFailed, writer))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // Return Error 2 for Database error
                            _database.DatabaseError(client, AddUserFailed, ex);
                        }
                        break;
                    }
                }
            }
        }

        #region Commands

        private void UsersLoggedInCommand(object sender, CommandEventArgs e)
        {
            WriteEvent(Clients.Count + " Users logged in", LogType.Info);
        }

        private void UsersOnlineCommand(object sender, CommandEventArgs e)
        {
            WriteEvent(ClientManager.GetAllClients().Length + " Users logged in", LogType.Info);
        }

        private void DebugCommand(object sender, CommandEventArgs e)
        {
            _debug = !_debug;
            WriteEvent("Debug is: " + _debug, LogType.Info);
        }

        private void AddUserCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }

            if (e.Arguments.Length != 2)
            {
                WriteEvent("Invalid arguments. Enter [AddUser name password].", LogType.Warning);
                return;
            }

            var username = e.Arguments[0];
            var password = BCrypt.Net.BCrypt.HashPassword(e.Arguments[1], 10);

            try
            {
                _database.DataLayer.UsernameAvailable(username, isAvailable =>
                {
                    if (isAvailable)
                    {
                        _database.DataLayer.AddNewUser(username, password, () =>
                        {
                            if (_debug)
                            {
                                WriteEvent("New User: " + username, LogType.Info);
                            }
                        });
                    }
                    else
                    {
                        WriteEvent("Username already in use.", LogType.Warning);
                    }
                });
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }

        private void DelUserCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }

            var username = e.Arguments[0];

            try
            {
                _database.DataLayer.DeleteUser(username, () =>
                {
                    if (_debug)
                    {
                        WriteEvent("Removed User: " + username, LogType.Info);
                    }
                });
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }

        private void AllowAddUserCommand(object sender, CommandEventArgs e)
        {
            switch (e.Arguments[0])
            {
                case "on":
                    _allowAddUser = true;
                    WriteEvent("Adding users allowed: True!", LogType.Info);
                    break;
                case "off":
                    _allowAddUser = false;
                    WriteEvent("Adding users allowed: False!", LogType.Info);
                    break;
                default:
                    WriteEvent("Please enter [AllowAddUser off] or [AllowAddUser on]", LogType.Info);
                    break;
            }
        }

        #endregion

        #region ErrorHandling

        public bool PlayerLoggedIn(IClient client, ushort tag, string error)
        {
            if (UsersLoggedIn[client] != null) return true;

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 1);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            WriteEvent(error + " Player wasn't logged in.", LogType.Warning);
            return false;
        }

        public void InvalidData(IClient client, ushort tag, Exception e, string error)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 0);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            WriteEvent(error + " Invalid data received: " + e.Message + " - " + e.StackTrace, LogType.Warning);
        }

        #endregion
    }
}