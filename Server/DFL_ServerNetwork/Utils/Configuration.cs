using DFLNetwork.Protocole.Compression;
using DFLNetwork.Protocole.Serialization;
using System.Reflection;
using System.Text;

namespace DFLServer.Utils
{
    public class Configuration
    {
        public int ProcessOffsetTime { get; set; }
        public int Port { get; set; }
        public bool LockConsole { get; set; }
        public string PersistancePath { get; set; }
        public string BlackListFilePath { get; set; }
        public int AccountLenghtMin { get; set; }
        public eCompression CompressionMethode { get; set; }
        public eSerializer SerializationMethode { get; set; }
        public string DatabaseFolder { get; set; }
        public int DatabaseSaveInterval { get; set; }
        public int DatabaseNbSaveToKeep { get; set; }
        public string CurrentDatabaseFile { get { return DatabaseFolder + @"\Database.db"; } }

        public Configuration()
        {
            ProcessOffsetTime = 10;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (PropertyInfo Info in GetType().GetProperties())
            {
                sb.Append(" - ");
                sb.Append(Info.Name);
                sb.Append(" : ");
                sb.AppendLine(GetType().GetProperty(Info.Name).GetValue(this).ToString());
            }
            return sb.ToString();
        }
    }
}
