using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cod4.Monitor.Util;
using Newtonsoft.Json;

namespace Cod4MasterBot
{
    public class Config
    {
        private const string ConfigFileName = "config.json";

        [JsonIgnore]
        public static Config GetInstance { get; set; } = Common.GetConfig(ConfigFileName);
        public Network Network { get; set; } = new Network();
        public Database Database { get; set; } = new Database();
        public List<string> BlackList { get; set; }
    }
    public class Database
    {
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DataBase { get; set; }
    }
    public class Network
    {
        public int QueryPort { get; set; }
        public int MasterServerResponseMaxBufferLength { get; set; }
        public int GameServerResponseMaxBufferLength { get; set; }
        public int MasterServerResponseTimeOut { get; set; }
        public int GameServerResponseTimeout { get; set; }
        public int RetryDelay { get; set; }
    }
}
