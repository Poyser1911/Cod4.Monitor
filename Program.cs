using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Cod4.Core.Master;
using Cod4.Core.Messages;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Cod4.Core.Models;
using Cod4.Core.Parse;
using Cod4.Monitor.Util;
using Cod4MasterBot;

namespace Cod4.Monitor
{
    class Program
    {

        #region Private Members
        private static UdpClient _udp;
        private static DateTime _lastupdated;
        private static Cod4DbManager _cdbm;
        private static int _receivedCount = 0;
        private static Timer _crashTimer;

        private static Cod4MasterQuery _master;

        private static List<ServerInfo> _servers = new List<ServerInfo>();
        private static int _serversParsed = 0;
        private static readonly Config Config = Config.GetInstance;
        private static Stopwatch _timer;
        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.Title = "Cod4.Monitor by Poyser";
            _cdbm = new Cod4DbManager(Config);
            UdpReceiver();
            Run();
            Thread.Sleep(Timeout.Infinite);                                                                                                                                      
        }

        private static void Run()
        {
            _master = new Cod4MasterQuery();
            _master.OnGetServersRequestError += OnError;
            _master.OnError += OnError;
            if (!_master.Init())
                return;
            _master.OnGetServersCompleted += Q_OnQueryCompleted;
            Refresh();
        }

        private static void Refresh()
        {
            _master.GetServers();
            _crashTimer = new Timer((o) => { Common.ColourWrite("\n^1Master Server Did not respond in time.."); Refresh(); }, null, Config.GetInstance.Network.MasterServerResponseTimeOut, Timeout.Infinite);
        }

        private static void OnError(string errormessage)
        {
            Common.ColourWrite($"^1{errormessage}^7\n");
            for (int i = Config.Network.RetryDelay; i >= 0;i--)
            {
                Console.Write($"Retrying..{i}\r");
                Thread.Sleep(1000);
            }
            Run();
        }
        #endregion

        #region OnMasterComplete
        private static void Q_OnQueryCompleted(List<Core.Models.Server> servers)
        {
            _timer = new Stopwatch();
            _timer.Start();
            _crashTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Console.WriteLine($"\nMaster Returned: {servers.Count} valid servers.");
            foreach (Server server in servers)
            {
                //Console.WriteLine($"{server.IP}:{server.Port}");
                _udp.Send(MessageFactory.GetStatus, MessageFactory.GetStatus.Length, new IPEndPoint(IPAddress.Parse(server.IP), Int32.Parse(server.Port)));
                Thread.Sleep(1);
            }
        }
        #endregion

        #region Receiver
        private static void UdpReceiver()
        {
            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, Config.Network.QueryPort));
            Task.Run(() =>
            {
                byte[] data = new byte[Config.Network.GameServerResponseMaxBufferLength];
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    while (true)
                    {
                        try
                        {
                            data = _udp.Receive(ref sender);
                            _lastupdated = DateTime.Now;
                            if (_receivedCount == 0)
                            {
                                _receivedCount++;
                                Console.WriteLine("Monitoring..");
                                WatchForCompletion();
                            }
                            // Console.WriteLine($"Packet #{count++} Received with [{data.Length} bytes] from { sender.Address}");

                            LogServer(new Source() { IP = sender.Address.ToString(), Port = sender.Port, Status = Encoding.Default.GetString(data) }, true, true);
                        }
                        catch (Exception) { }
                    }

                }
                catch(Exception e){ OnError(e.Message); }
            });
        }
        #endregion

        #region Parse and Store Server
        public static void LogServer(Source source, bool getdvars = true, bool getplayers = true)
        {
            Task.Run(() =>
            {
                ServerInfo result = new ServerInfo();
                try
                {
                    if (Parser.GetServerInfo(source, getdvars, getplayers, out result))
                    {
                        try
                        {
                            lock (_servers)
                            {
                                _servers.Add(result);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            return;
                        }
                        _lastupdated = DateTime.Now;
                        _serversParsed++;
                        Console.Write($"\rServers Prepared: {_serversParsed}");
                    }
                    else
                        Console.WriteLine($"Error Getting Server Info For: {source.IP}:{source.Port}");

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unexpected Error: {e.StackTrace}");
                }
            });
        }

        #endregion

        #region WatchForParseCompletion
        private static void WatchForCompletion()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if ((DateTime.Now - _lastupdated).Seconds > Config.Network.GameServerResponseTimeout)
                    {
                        _cdbm.Insert(_servers);
                        _timer.Stop();
                        _receivedCount = 0;
                        _serversParsed = 0;

                        lock (_servers)
                        {
                            _servers = new List<ServerInfo>();
                        }
                        Common.ColourWrite($"Finshed in ^2{_timer.Elapsed.Seconds}^7.{_timer.Elapsed.Milliseconds}s");
                        Refresh();
                        return;
                    }
                    Thread.Sleep(10);
                }
            });
        }
        #endregion

    }
}
