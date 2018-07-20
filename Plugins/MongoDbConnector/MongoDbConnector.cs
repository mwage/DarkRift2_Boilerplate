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
        public override Version Version => new Version(2, 0, 0);
        public override bool ThreadSafe => false;
        public override Command[] Commands => new[]
        {
            new Command ("LoadMongo", "Loads Mongo Db Database", "", LoadDbCommand),
        };

        private const string ConfigPath = @"Plugins\MongoDbConnector.xml";
        private DatabaseProxy _database;
        private readonly DataLayer _dataLayer;

        public IMongoCollection<User> Users { get; private set; }
        private readonly IMongoDatabase _mongoDatabase;

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
                    WriteEvent("Created /Plugins/DbConnector.xml. Please adjust your connection string and restart the server!",
                        LogType.Info);
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

        //Set up MongoDB Schemas
        private void GetCollections()
        {
            Users = _mongoDatabase.GetCollection<User>("users");
        }

        //If you have DR2 Pro, use the Plugin.Loaded() method instead of this
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            //Register the database at initial startup
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
                LoadDatabase();
            }
        }

        //Register database
        private void LoadDatabase()
        {
            _database.SetDatabase(_dataLayer);
        }

        //Command for setting MongoDB as active database
        public void LoadDbCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }
            LoadDatabase();
        }
    }
}
