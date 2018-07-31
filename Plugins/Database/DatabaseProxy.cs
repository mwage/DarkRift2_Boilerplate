using System;
using DarkRift;
using DarkRift.Server;

namespace Database
{
    public class DatabaseProxy : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public IDataLayer DataLayer { get; private set; }

        private static readonly object InitializeLock = new object();

        public DatabaseProxy(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public void SetDatabase(IDataLayer dataLayer)
        {
            lock (InitializeLock)
            {
                if (DataLayer == dataLayer)
                {
                    WriteEvent($"Database: {dataLayer.Name} is already selected", LogType.Info);
                }

                if (DataLayer != null)
                {
                    WriteEvent($"Switching from Database: {DataLayer.Name} to Database: {dataLayer.Name}", LogType.Warning);
                }
                else
                {
                    WriteEvent("Selected Database: " + dataLayer.Name, LogType.Info);
                }

                DataLayer = dataLayer;
            }
        }

        #region ErrorHandling

        public void DatabaseError(IClient client, ushort tag, Exception e)
        {
            WriteEvent("Database Error: " + e.Message + " - " + e.StackTrace, LogType.Error);

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 2);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        #endregion
    }
}