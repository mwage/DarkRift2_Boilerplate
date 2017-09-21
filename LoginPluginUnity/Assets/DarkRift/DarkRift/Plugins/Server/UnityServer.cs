using UnityEngine;
using System.Collections;

using DarkRift.Server;
using DarkRift;
using System;
using System.IO;
using System.Net;

namespace DarkRift.Server.Unity
{
    [AddComponentMenu("DarkRift/Server")]
	public sealed class UnityServer : MonoBehaviour
	{
        #region Basic settings

        /// <summary>
        ///     The address the server listens on.
        /// </summary>
        public IPAddress Address
        {
            get { return address; }
            set { address = value; }
        }

        [SerializeField]
        [Tooltip("The address the server will listen on.")]
        IPAddress address = IPAddress.Any;

        /// <summary>
        ///     The port the server listens on.
        /// </summary>
        public ushort Port
        {
            get { return port; }
            set { port = value; }
        }

        [SerializeField]
        [Tooltip("The port the server will listen on.")]
        ushort port = 4296;

        /// <summary>
        ///     The IP protocol version the server listens on.
        /// </summary>
        public IPVersion IPVersion
        {
            get { return IPVersion; }
            set { IPVersion = value; }
        }

        [SerializeField]
        [Tooltip("The IP protocol version the server will listen on.")] //Declared in custom editor
        IPVersion ipVersion = IPVersion.IPv4;

        [SerializeField]
        [Tooltip("Indicates whether the server will be created in the OnEnable method.")]
        bool createOnEnable = true;

        #endregion

        #region Data settings

        /// <summary>
        ///     The location DarkRift will store persistant data.
        /// </summary>
        public string DataDirectory
        {
            get { return dataDirectory; }
            set { dataDirectory = value; }
        }
            
        [SerializeField]
        [Tooltip("The location DarkRift will store persistant data.")]
        string dataDirectory = "Data/";

        #endregion

        #region Logging settings

        /// <summary>
        ///     Whether the recommended logging settings are being used.
        /// </summary>
        public bool UseRecommended
        {
            get { return useRecommended; }
            set { useRecommended = value; }
        }

        [SerializeField]
        [Tooltip("Indicates whether the recommended logging settings will be used.")]
        bool useRecommended = true;

        /// <summary>
        ///     The location that log files will be placed when using recommended logging settings.
        /// </summary>
        public string LogFileString
        {
            get { return logFileString; }
            set { logFileString = value; }
        }

        [SerializeField]
        [Tooltip("The location log files will be written to.")]
        string logFileString = @"Logs/{0:d-M-yyyy}/{0:HH-mm-ss tt}.txt";

        /// <summary>
        ///     The log writers messages will be written to.
        /// </summary>
        public ServerSpawnData.LoggingSettings.LogWriterSettings[] LogWriters
        {
            get { return logWriters; }
            set { logWriters = value; }
        }

        [HideInInspector]
        ServerSpawnData.LoggingSettings.LogWriterSettings[] logWriters = new ServerSpawnData.LoggingSettings.LogWriterSettings[0];

        #endregion

        #region Plugin settings
        
        /// <summary>
        ///     The plugins that should be loaded.
        /// </summary>
        public Type[] PluginTypes
        {
            get { return pluginTypes; }
            set { pluginTypes = value; }
        }
        
        [HideInInspector]
        Type[] pluginTypes;
        
        #endregion

        #region Database settings

        /// <summary>
        ///     The databases that the server will connect to.
        /// </summary>
        public ServerSpawnData.DatabaseSettings.DatabaseConnectionData[] Databases
        {
            get { return databases; }
            set { databases = value; }
        }

        [HideInInspector]
        ServerSpawnData.DatabaseSettings.DatabaseConnectionData[] databases;

        #endregion

        /// <summary>
        ///     The actually server.
        /// </summary>
        public DarkRiftServer Server { get; private set; }

        void OnEnable()
        {
            //If createOnEnable is selected create a server
            if (createOnEnable)
                Create();
        }

        void Update()
        {
            //Execute all queued dispatcher tasks
            Server.ExecuteDispatcherTasks();
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create()
        {
            if (Server != null)
                throw new InvalidOperationException("The server has already been created! (Is CreateOnEnable enabled?)");

            ServerSpawnData spawnData = new ServerSpawnData(address, port, ipVersion);

            //Data settings
            spawnData.Data.Settings["directory"] = dataDirectory;

            //Logging settings
            spawnData.Plugins.PluginTypes.Add(typeof(UnityConsoleWriter));

            if (useRecommended)
            {
                ServerSpawnData.LoggingSettings.LogWriterSettings fileWriter = new ServerSpawnData.LoggingSettings.LogWriterSettings();
                fileWriter.Name = "FileWriter1";
                fileWriter.Type = "FileWriter";
                fileWriter.LogLevels = new LogType[] { LogType.Trace, LogType.Info, LogType.Warning, LogType.Error, LogType.Fatal };
                fileWriter.Settings["file"] = logFileString;
                spawnData.Logging.LogWriters.Add(fileWriter);

                ServerSpawnData.LoggingSettings.LogWriterSettings consoleWriter = new ServerSpawnData.LoggingSettings.LogWriterSettings();
                consoleWriter.Name = "UnityConsoleWriter1";
                consoleWriter.Type = "UnityConsoleWriter";
                consoleWriter.LogLevels = new LogType[] { LogType.Info, LogType.Warning, LogType.Error, LogType.Fatal };
                spawnData.Logging.LogWriters.Add(consoleWriter);

                ServerSpawnData.LoggingSettings.LogWriterSettings debugWriter = new ServerSpawnData.LoggingSettings.LogWriterSettings();
                debugWriter.Name = "DebugWriter1";
                debugWriter.Type = "DebugWriter";
                debugWriter.LogLevels = new LogType[] { LogType.Warning, LogType.Error, LogType.Fatal };
                spawnData.Logging.LogWriters.Add(debugWriter);
            }
            else
            {
                spawnData.Logging.LogWriters.AddRange(logWriters);
            }

            //Plugins
            if (pluginTypes != null)
                spawnData.Plugins.PluginTypes.AddRange(pluginTypes);
            
            //Databases
            if (databases != null)
                spawnData.Databases.Databases.AddRange(databases);

            Server = new DarkRiftServer(spawnData);
            Server.Start();
        }

        private void OnDisable()
        {
            Close();
        }

        /// <summary>
        ///     Closes the server.
        /// </summary>
        public void Close()
        {
            Server.Dispose();
        }
    }
}
