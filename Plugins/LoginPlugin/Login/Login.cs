using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using DbConnectorPlugin;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

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
            new Command("LPDebug", "Enables Plugin Debug", "", DebugCommand),
            new Command("Online", "Logs number of online users", "", UsersLoggedInCommand),
            new Command("LoggedIn", "Logs number of online users", "", UsersOnlineCommand)
        };

        // Tag
        private const byte LoginTag = 0;

        // Subjects
        private const ushort Keys = 0;
        private const ushort LoginUser = 1;
        private const ushort LogoutUser = 2;
        private const ushort AddUser = 3;
        private const ushort LoginSuccess = 4;
        private const ushort LoginFailed = 5;
        private const ushort LogoutSuccess = 6;
        private const ushort AddUserSuccess = 7;
        private const ushort AddUserFailed = 8;

        // Connects the clients Global ID with his username
        public Dictionary<uint, string> UsersLoggedIn = new Dictionary<uint, string>();
        private Dictionary<uint, RSAParameters> _keys = new Dictionary<uint, RSAParameters>();

        private const string ConfigPath = @"Plugins\Login.xml";
        private DbConnector _dbConnector;
        private bool _allowAddUser = true;
        private bool _debug = true;

        public Login(PluginLoadData pluginLoadData) : base(pluginLoadData)
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

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            UsersLoggedIn[e.Client.GlobalID] = "";
            _keys[e.Client.GlobalID] = Encryption.GenerateKeys(out var publicKey);

            var writer = new DarkRiftWriter();
            writer.Write(publicKey.Exponent);
            writer.Write(publicKey.Modulus);

            e.Client.SendMessage(new TagSubjectMessage(LoginTag, Keys, writer), SendMode.Reliable);
            e.Client.MessageReceived += OnMessageReceived;

            // If you have DR2 Pro, use the Plugin.Loaded() method to get the DbConnector Plugin instead
            if (_dbConnector == null)
            {
                _dbConnector = PluginManager.GetPluginByType<DbConnector>();
            }
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (UsersLoggedIn.ContainsKey(e.Client.GlobalID))
            {
                UsersLoggedIn.Remove(e.Client.GlobalID);
            }
            if (_keys.ContainsKey(e.Client.GlobalID))
            {
                _keys.Remove(e.Client.GlobalID);
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
                if (UsersLoggedIn[client.GlobalID] != "")
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
                    password = Encryption.Decrypt(reader.ReadBytes(), _keys[client.GlobalID]);
                }
                catch (Exception ex)
                {
                    WriteEvent("LoginPlugin: Invalid Login data received: " + ex.Message + " - " + ex.StackTrace, LogType.Warning);

                    // Return Error 0 for Invalid Data Packages Recieved
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)0);
                    client.SendMessage(new TagSubjectMessage(LoginTag, LoginFailed, writer), SendMode.Reliable);
                    return;
                }
                
                try
                {
                    var user = _dbConnector.Users.AsQueryable().FirstOrDefault(u => u.Username == username);
                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                    {
                        UsersLoggedIn[client.GlobalID] = username;

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
                        writer.Write((byte) 1);
                        client.SendMessage(new TagSubjectMessage(LoginTag, LoginFailed, writer), SendMode.Reliable);
                    }
                }
                catch (Exception ex)
                {
                    WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);

                    // Return Error 2 for Database error
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)2);
                    client.SendMessage(new TagSubjectMessage(LoginTag, LoginFailed, writer), SendMode.Reliable);
                }
            }

            // Logout Request
            if (message.Subject == LogoutUser)
            {
                UsersLoggedIn[client.GlobalID] = "";

                if (_debug)
                {
                    WriteEvent("User " + client.GlobalID + " logged out!", LogType.Info);
                }
                client.SendMessage(new TagSubjectMessage(LoginTag, LogoutSuccess, new DarkRiftWriter()), SendMode.Reliable);
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
                        Encryption.Decrypt(reader.ReadBytes(), _keys[client.GlobalID])
                        , 10);
                }
                catch (Exception ex)
                {
                    WriteEvent("LoginPlugin: Invalid AddUser data received: " + ex.Message + " - " + ex.StackTrace, LogType.Warning);

                    // Return Error 0 for Invalid Data Recieved
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)0);
                    client.SendMessage(new TagSubjectMessage(LoginTag, AddUserFailed, writer), SendMode.Reliable);
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
                    WriteEvent("Database Error: " + ex.Message + " - " + ex.StackTrace, LogType.Error);

                    // Return Error 2 for Database error
                    var writer = new DarkRiftWriter();
                    writer.Write((byte)2);
                    client.SendMessage(new TagSubjectMessage(LoginTag, AddUserFailed, writer), SendMode.Reliable);
                }
            }
        }

        private bool UsernameAvailable(string username)
        {
            return _dbConnector.Users.AsQueryable().FirstOrDefault(u => u.Username == username) == null;
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
            WriteEvent(UsersLoggedIn.Count + " Users logged in", LogType.Info);
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
    }
}
