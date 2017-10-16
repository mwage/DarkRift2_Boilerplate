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
    public class Login : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;
        public override Command[] Commands => new[]
        {
            new Command ("AllowAddUser", "Allow Users to be added to the Database [AllowAddUser on/off]", "", AllowAddUserCommand),
            new Command("AddUser", "Adds a User to the Database [AddUser name password]", "", AddUserCommand),
            new Command("DelUser", "Deletes a User from the Database [DelUser name]", "", DelUserCommand),
            new Command("LPDebug", "Enables Plugin Debug", "", DebugCommand),
            new Command("LoggedIn", "Logs number of online users", "", UsersLoggedInCommand),
            new Command("Online", "Logs number of online users", "", UsersOnlineCommand)
        };

        // Tag
        private const byte LoginTag = 0;

        // Subjects
        private const ushort LoginUser = 0;
        private const ushort LogoutUser = 1;
        private const ushort AddUser = 2;
        private const ushort LoginSuccess = 3;
        private const ushort LoginFailed = 4;
        private const ushort LogoutSuccess = 5;
        private const ushort AddUserSuccess = 6;
        private const ushort AddUserFailed = 7;

        // Connects the Client with his Username
        public Dictionary<Client, string> UsersLoggedIn = new Dictionary<Client, string>();
        public Dictionary<string, Client> Clients = new Dictionary<string, Client>();

        private const string ConfigPath = @"Plugins\Login.xml";
        private const string PrivateKeyPath = @"Plugins\PrivateKey.xml";
        private DbConnector _dbConnector;
        private string _privateKey;
        private bool _allowAddUser = true;
        private bool _debug = true;

        public delegate void LogoutEventHandler(string username);

        public event LogoutEventHandler onLogout;

        public Login(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig();
            LoadRsaKey();
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

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
                    WriteEvent("Created /Plugins/Login.xml!", LogType.Warning);
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
            // If you have DR2 Pro, use the Plugin.Loaded() method to get the DbConnector Plugin instead
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
            }

            UsersLoggedIn[e.Client] = null;

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (UsersLoggedIn.ContainsKey(e.Client))
            {
                var username = UsersLoggedIn[e.Client];
                UsersLoggedIn.Remove(e.Client);
                if (username != null)
                {
                    Clients.Remove(username);
                    onLogout?.Invoke(username);
                }
            }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!(e.Message is TagSubjectMessage message) || message.Tag != LoginTag)
                return;

            var client = (Client)sender;

            // Login Request
            if (message.Subject == LoginUser)
            {
                // If user is already logged in (shouldn't happen though)
                if (UsersLoggedIn[client] != null)
                {
                    client.SendMessage(new TagSubjectMessage(LoginTag, LoginSuccess, new DarkRiftWriter()), SendMode.Reliable);
                    return;
                }

                string username;
                string password;

                try
                {
                    var reader = message.GetReader();
                    username = reader.ReadString();
                    password = Encryption.Decrypt(reader.ReadBytes(), _privateKey);
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    InvalidData(client, LoginTag, LoginFailed, ex, "Failed to log in!");
                    return;
                }

                if (UsersLoggedIn.ContainsValue(username))
                {
                    // Username is already in use -> return Error 3
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)3);
                    client.SendMessage(new TagSubjectMessage(LoginTag, LoginFailed, writer), SendMode.Reliable);
                    return;
                }

                try
                {
                    var user = _dbConnector.Users.AsQueryable().FirstOrDefault(u => u.Username == username);

                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                    {
                        UsersLoggedIn[client] = username;
                        Clients[username] = client;

                        client.SendMessage(new TagSubjectMessage(LoginTag, LoginSuccess, new DarkRiftWriter()), SendMode.Reliable);

                        if (_debug)
                        {
                            WriteEvent("Successful login (" + client.GlobalID + ").", LogType.Info);
                        }
                    }
                    else
                    {
                        if (_debug)
                        {
                            WriteEvent("User " + client.GlobalID + " couldn't log in!", LogType.Info);
                        }

                        // Return Error 1 for "Wrong username/password combination"
                        var writer = new DarkRiftWriter();
                        writer.Write((byte)1);
                        client.SendMessage(new TagSubjectMessage(LoginTag, LoginFailed, writer), SendMode.Reliable);
                    }
                }
                catch (Exception ex)
                {
                    // Return Error 2 for Database error
                    _dbConnector.DatabaseError(client, LoginTag, LoginFailed, ex);
                }
            }

            // Logout Request
            if (message.Subject == LogoutUser)
            {
                var username = UsersLoggedIn[client];
                UsersLoggedIn[client] = null;
                if (username != null)
                {
                    Clients.Remove(username);
                }

                if (_debug)
                {
                    WriteEvent("User " + client.GlobalID + " logged out!", LogType.Info);
                }

                client.SendMessage(new TagSubjectMessage(LoginTag, LogoutSuccess, new DarkRiftWriter()), SendMode.Reliable);
                onLogout?.Invoke(username);
            }

            // Registration Request
            if (message.Subject == AddUser)
            {
                if (!_allowAddUser)
                    return;

                string username;
                string password;

                try
                {
                    var reader = message.GetReader();
                    username = reader.ReadString();

                    password = BCrypt.Net.BCrypt.HashPassword(
                        Encryption.Decrypt(reader.ReadBytes(), _privateKey)
                        , 10);
                }
                catch (Exception ex)
                {
                    // Return Error 0 for Invalid Data Packages Recieved
                    InvalidData(client, LoginTag, AddUserFailed, ex, "Failed to add user!");
                    return;
                }

                try
                {
                    if (UsernameAvailable(username))
                    {
                        AddNewUser(username, password);
                        client.SendMessage(new TagSubjectMessage(LoginTag, AddUserSuccess, new DarkRiftWriter()), SendMode.Reliable);
                    }
                    else
                    {
                        if (_debug)
                        {
                            WriteEvent("User " + client.GlobalID + " failed to sign up!", LogType.Info);
                        }

                        // Return Error 1 for "Wrong username/password combination"
                        var writer = new DarkRiftWriter();
                        writer.Write((byte)1);
                        client.SendMessage(new TagSubjectMessage(LoginTag, AddUserFailed, writer), SendMode.Reliable);
                    }
                }
                catch (Exception ex)
                {
                    // Return Error 2 for Database error
                    _dbConnector.DatabaseError(client, LoginTag, AddUserFailed, ex);
                }
            }
        }

        private bool UsernameAvailable(string username)
        {
            return !_dbConnector.Users.AsQueryable().Any(u => u.Username == username);
        }

        private void AddNewUser(string username, string password)
        {
            _dbConnector.Users.InsertOne(new User(username, password));

            if (_debug)
            {
                WriteEvent("New User: " + username, LogType.Info);
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
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
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
                if (UsernameAvailable(username))
                {
                    AddNewUser(username, password);
                }
                else
                {
                    WriteEvent("Username already in use.", LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
            }
        }

        private void DelUserCommand(object sender, CommandEventArgs e)
        {
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
            }

            var username = e.Arguments[0];

            try
            {
                _dbConnector.Users.DeleteOne(u => u.Username == username);

                if (_debug)
                {
                    WriteEvent("Removed User: " + username, LogType.Info);
                }
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

        public bool PlayerLoggedIn(Client client, byte tag, ushort subject, string error)
        {
            if (UsersLoggedIn[client] != null)
                return true;

            var writer = new DarkRiftWriter();
            writer.Write((byte)1);
            client.SendMessage(new TagSubjectMessage(tag, subject, writer), SendMode.Reliable);

            WriteEvent(error + " Player wasn't logged in.", LogType.Warning);
            return false;
        }

        public void InvalidData(Client client, byte tag, ushort subject, Exception e, string error)
        {
            var writer = new DarkRiftWriter();
            writer.Write((byte)0);
            client.SendMessage(new TagSubjectMessage(tag, subject, writer), SendMode.Reliable);

            WriteEvent(error + " Invalid data received: " + e.Message + " - " + e.StackTrace, LogType.Warning);
        }

        #endregion
    }
}
