using System;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using MongoDB.Driver;

namespace DbConnectorPlugin
{
    public class DbConnector : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        public IMongoCollection<Message> Messages;
        public IMongoCollection<User> Users;

        private const string ConfigPath = @"Plugins\DbConnector.xml";
        private readonly IMongoDatabase _database;

        public DbConnector(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            var connectionString = LoadConfig();

            try
            {
                var client = new MongoClient(connectionString);
                _database = client.GetDatabase("test");
                GetCollections();
            }
            catch (Exception ex)
            {
                WriteEvent("Failed to set up Database:" + ex.Message + " - " + ex.StackTrace, LogType.Fatal);
            }
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
                catch(Exception ex)
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
            Users = _database.GetCollection<User>("users");
        }
    }
}
