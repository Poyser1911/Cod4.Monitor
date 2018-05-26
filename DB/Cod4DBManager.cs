using System;
using System.Collections.Generic;
using System.Linq;
using Cod4.Core.Models;
using Cod4.Monitor.Util;
using Cod4MasterBot;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Cod4.Monitor
{
    class Cod4DbManager
    {
        #region Private Fields
        private readonly Config _config;
        private MySqlConnection _connection;
        private MySqlTransaction _transaction;

        #endregion

        #region Constructor
        public Cod4DbManager(Config config)
        {
            _config = config;
            Connect();
        }
        #endregion

        #region Connect to Database
        private void Connect()
        {
            try
            {
                _connection = new MySqlConnection($"server={_config.Database.Host};uid={_config.Database.User};pwd={_config.Database.Password};database={_config.Database.DataBase};default command timeout=600");
                _connection.Open();
                Util.Common.ColourWrite("^2Database Connection Established\n");
            }
            catch (MySqlException ex)
            {
                Util.Common.FatalError("Error While Connecting to Database", ex.Message);
            }
        }
        #endregion

        #region Update to Database
        public void Insert(List<ServerInfo> servers)
        {
            servers = servers.ToList();
            #region Update Servers      
            using (MySqlTransaction trans = _connection.BeginTransaction())
            {
                MySqlCommand AddServerCmd = new MySqlCommand(
                    "INSERT INTO servers(ip,port,connect,hostname,gametype,mapname,players,dvarlist,playerlist) " +
                    "VALUES(@ip,@port,@connect,@hostname,@gametype,@mapname,@players,@dvarlist,@playerlist) " +
                    "ON DUPLICATE KEY UPDATE hostname=VALUES(hostname),gametype=VALUES(gametype),mapname=VALUES(mapname)," +
                    "players=VALUES(players),dvarlist=VALUES(dvarlist),playerlist=VALUES(playerlist);SELECT id FROM servers where connect=@connect", _connection, trans);

                try
                {
                    int playersCount = 0;
                    int serversCount = 0;
                    Console.WriteLine("\nPreparing Updates..");
                    for (int i = 0; i < servers.Count; i++)
                    {
                        if (servers[i] == null)
                            continue;
                        AddServerCmd.Parameters.Clear();
                        AddServerCmd.Parameters.AddWithValue("@ip", servers[i].IP);
                        AddServerCmd.Parameters.AddWithValue("@port", servers[i].Port);
                        AddServerCmd.Parameters.AddWithValue("@connect", $"{servers[i].IP}:{servers[i].Port}");
                        AddServerCmd.Parameters.AddWithValue("@hostname", Common.RemoveColours(servers[i].Dvars.GetVal("sv_hostname")));
                        AddServerCmd.Parameters.AddWithValue("@gametype", servers[i].Dvars.GetVal("g_gametype"));
                        AddServerCmd.Parameters.AddWithValue("@mapname", servers[i].Dvars.GetVal("mapname"));
                        AddServerCmd.Parameters.AddWithValue("@players", servers[i].Players.Count);
                        AddServerCmd.Parameters.AddWithValue("@dvarlist", JsonConvert.SerializeObject(servers[i].Dvars));
                        AddServerCmd.Parameters.AddWithValue("@playerlist", JsonConvert.SerializeObject(servers[i].Players));
                        MySqlDataReader rdr = AddServerCmd.ExecuteReader();
                        if (rdr.Read())
                        {
                            long id = rdr.GetInt32(0);
                            rdr.Close();
                            MySqlCommand AddPlayerCmd = new MySqlCommand(
                                "INSERT INTO players(server_id,name,info,servers_visited) " +
                                "VALUES(@server_id,@name,@info,JSON_ARRAY(@servers_visited)) " +
                                "ON DUPLICATE KEY UPDATE server_id=VALUES(server_id),name=VALUES(name),info=VALUES(info)," +
                                "servers_visited=CASE WHEN !JSON_CONTAINS(servers_visited,VALUES(servers_visited),'$')THEN JSON_ARRAY_APPEND(servers_visited,'$',JSON_EXTRACT(VALUES(servers_visited),'$[0]'))ELSE servers_visited END ;", _connection, trans);
                            if (servers[i].Players.Count != 0)
                                for (int j = 0; j < servers[i].Players.Count; j++)
                                {
                                    if (servers[i].Players[j].name.Contains("bot")) continue;
                                    AddPlayerCmd.Parameters.Clear();
                                    AddPlayerCmd.Parameters.AddWithValue("@server_id", id);
                                    AddPlayerCmd.Parameters.AddWithValue("@name", servers[i].Players[j].name);
                                    AddPlayerCmd.Parameters.AddWithValue("@info", JsonConvert.SerializeObject(servers[i].Players[j]));
                                    AddPlayerCmd.Parameters.AddWithValue("@servers_visited", id);
                                    AddPlayerCmd.ExecuteNonQuery();
                                    playersCount++;
                                }
                        }
                        serversCount++;
                        Console.Write($"\rBuilding Mysql Transaction: {((serversCount * 100) / servers.Count).ToString("#.##")}%");
                    }
                    Console.WriteLine("\nCommiting Transaction Changes..");
                    trans.Commit();
                    Console.WriteLine($"Committed {serversCount} Servers.");
                    Console.WriteLine($"Committed {playersCount} Players.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}\nTrace:{e.StackTrace}");
                }
            }
            #endregion
        }
        #endregion
    }
}
