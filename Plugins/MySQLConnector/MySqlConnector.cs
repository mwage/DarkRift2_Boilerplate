using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Database;
using MySql.Data.MySqlClient;

namespace MySQLConnector
{
    public class MySqlConnector : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        private const string ConfigPath = @"Plugins/MySQLConnector.xml";
        private readonly string _connectionString;
        private DatabaseProxy _database;
        private readonly DataLayer _dataLayer;
        private static readonly object InitializeLock = new object();

        public MySqlConnector(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            _connectionString = LoadConfig();
            CreateTables();
            _dataLayer = new DataLayer("MySQL", this);

            ClientManager.ClientConnected += OnPlayerConnected;
        }

        // Get Connection String
        private string LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Insert your ConnectionString below."),
                    new XElement("ConnectionString", "SERVER=localhost;PORT=3306;DATABASE=myDataBase;UID=myUser;PASSWORD=myPass"));
                try
                {
                    document.Save(ConfigPath);
                    WriteEvent("Created /Plugins/MySQLConnector.xml. Adjust your connection string and restart the server.",
                        LogType.Info);
                    return "SERVER=localhost;PORT=3306;DATABASE=myDataBase;UID=myUser;PASSWORD=myPass";
                }
                catch (Exception ex)
                {
                    WriteEvent("Failed to create MySQLConnector.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
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
                WriteEvent("Failed to read MySQLConnector.xml: " + ex.Message + " - " + ex.StackTrace, LogType.Error);
                return null;
            }
        }

        public void Log(string message)
        {
            WriteEvent(message, LogType.Warning);
        }

        public void CreateTables()
        {
            const string userTable = "CREATE TABLE IF NOT EXISTS Users(" +
                                           "id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, " +
                                           "username VARCHAR(60) NOT NULL, " +
                                           "password VARCHAR(255) NOT NULL ) ";
            const string friendsTable = "CREATE TABLE IF NOT EXISTS Friends(" +
                                     "id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, " +
                                     "user VARCHAR(60) NOT NULL, " +
                                     "friend VARCHAR(60), " +
                                     "request VARCHAR(60)) ";

            ExecuteNonQuery(userTable);
            ExecuteNonQuery(friendsTable);
        }

        public object ExecuteNonQuery(string query, params QueryParameter[] parameters)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter.ParameterName, parameter.FieldType, parameter.Size, parameter.Column).Value = parameter.Value;
                        }

                        connection.Open();
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException e)
            {
                WriteEvent($"Failed to execute NonQuery! \n{e.Message}\n{e.StackTrace}", LogType.Error);
                return null;
            }
        }

        public object ExecuteScalar(string query, params QueryParameter[] parameters)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter.ParameterName, parameter.FieldType, parameter.Size, parameter.Column).Value = parameter.Value;
                        }

                        connection.Open();
                        return command.ExecuteScalar();
                    }
                }
            }
            catch (MySqlException e)
            {
                WriteEvent($"Failed to execute ScalarQuery! \n{e.Message}\n{e.StackTrace}", LogType.Error);
                return null;
            }
        }

        public DatabaseRow[] ExecuteQuery(string query, params QueryParameter[] parameters)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter.ParameterName, parameter.FieldType, parameter.Size, parameter.Column).Value = parameter.Value;
                        }

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            var fieldCount = reader.FieldCount;
                            var rows = new List<DatabaseRow>();

                            while (reader.Read())
                            {
                                //For each row create a DatabaseRow
                                var row = new DatabaseRow();

                                //And add each field to it
                                for (var i = 0; i < fieldCount; i++)
                                {
                                    row.Add(
                                        reader.GetName(i),
                                        reader.GetValue(i)
                                    );
                                }

                                //Add it to the rows
                                rows.Add(row);
                            }

                            return rows.ToArray();
                        }
                    }
                }
            }
            catch (MySqlException e)
            {
                WriteEvent($"Failed to execute Query! \n{e.Message}\n{e.StackTrace}", LogType.Error);
                return null;
            }
        }

        public string EscapeString(string s)
        {
            return MySqlHelper.EscapeString(s);
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