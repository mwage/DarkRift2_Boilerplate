using System;
using DarkRift;
using DarkRift.Server;

namespace Database
{
    public class DatabaseProxy : Plugin
    {
        public DatabaseProxy(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        public IDataLayer DataLayer { get; private set; }

        public void SetDatabase(IDataLayer dataLayer)
        {
            if (DataLayer != null)
            {
                WriteEvent($"Database Error: Database {dataLayer.Name} is already active.", LogType.Error);
                return;
            }

            DataLayer = dataLayer;
            WriteEvent("Added Database: " + dataLayer.Name, LogType.Info);
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