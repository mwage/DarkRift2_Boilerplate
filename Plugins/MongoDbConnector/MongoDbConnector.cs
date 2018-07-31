using System;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Database;
using MongoDB.Driver;

namespace MongoDbConnector
{
    public class MongoDbConnector : Plugin
    {
        public override Version Version => new Version(2, 0, 0);
        public override bool ThreadSafe => true;
        public override Command[] Commands => new[]
        {
            new Command("LoadMongo", "Loads Mongo Db Database", "", LoadDbCommand)
        };

        public IMongoCollection<User> Users { get; private set; }

        private const string ConfigPath = @"Plugins\MongoDbConnector.xml";
        private static readonly object InitializeLock = new object();
        private readonly DataLayer _dataLayer;
        private readonly IMongoDatabase _mongoDatabase;
        private DatabaseProxy _database;

        public MongoDbConnector(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            //Read Connectionstring from Config
            var connectionString = LoadConfig();

            //Set up MongoDB
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

            //GetDataLayer
            _dataLayer = new DataLayer("MongoDB", this);

            ClientManager.ClientConnected += OnPlayerConnected;
        }

        //Get Connection String
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
                    WriteEvent(
                        "Created /Plugins/DbConnector.xml. Please adjust your connection string and restart the server!",
                        LogType.Info);
                    return "mongodb://localhost:27017";
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create DbConnector.xml: " + ex.Message + " - " + ex.StackTrace,
                        LogType.Error);
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

        //Set up MongoDB Schemas
        private void GetCollections()
        {
            Users = _mongoDatabase.GetCollection<User>("users");
        }

        //If you have DR2 Pro, use the Plugin.Loaded() method instead of this
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_database == null)
            {
                lock (InitializeLock)
                {
                    if (_database == null)
                    {
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                        _database.SetDatabase(_dataLayer);
                    }
                }
            }
        }

        //Command for setting MongoDB as active database
        public void LoadDbCommand(object sender, CommandEventArgs e)
        {
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
            _database.SetDatabase(_dataLayer);
        }
    }
}