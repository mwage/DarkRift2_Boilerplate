using DarkRift;
using DarkRift.Server;
using Database;
using MongoDB.Driver;
using System;
using System.IO;
using System.Xml.Linq;

namespace MongoDbConnector
{
    public class MongoDbConnector : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;
        public override Command[] Commands => new[]
        {
            new Command ("LoadMongo", "Loads Mongo Db Database", "", LoadDbCommand),
        };

        public IMongoCollection<User> Users { get; private set; }

        private const string ConfigPath = @"Plugins\MongoDbConnector.xml";
        private readonly IMongoDatabase _mongoDatabase;
        private DatabaseProxy _database;


        public MongoDbConnector(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            var connectionString = LoadConfig();

            try
            {
                var client = new MongoClient(connectionString);
                _mongoDatabase = client.GetDatabase("test");
                GetCollections();
            }
            catch (Exception ex)
            {
                WriteEvent("Failed to connect to MongoDb: " + ex.Message + " - " + ex.StackTrace, LogType.Fatal);
            }

            ClientManager.ClientConnected += OnPlayerConnected;
        }

        // Get Connection String
        private string LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Insert your ConnectionString below!"),
                    new XElement("ConnectionString", "mongodb://localhost:27017"));
                try
                {
                    document.Save(ConfigPath);
                    WriteEvent("Created /Plugins/DbConnector.xml. Please adjust your connection string and restart the server!",
                        LogType.Warning);
                    return "mongodb://localhost:27017";
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create DbConnector.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                    return null;
                }
            }

            try
            {
                document = XDocument.Load(ConfigPath);

                return document.Element("ConnectionString").Value;

            }
            catch (Exception ex)
            {
                WriteEvent("Failed to load DbConnector.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                return null;
            }
        }

        private void GetCollections()
        {
            Users = _mongoDatabase.GetCollection<User>("users");
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            // If you have DR2 Pro, use the Plugin.Loaded() method to get the DbConnector Plugin instead
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
                _database.SetDatabase(new DataLayer("MongoDB", this));
            }
        }

        public void Log(string message, LogType logType)
        {
            WriteEvent(message, logType);
        }

        public void LoadDbCommand(object sender, CommandEventArgs e)
        {
            LoadDatabase();
        }
    }
}
