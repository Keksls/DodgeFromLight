using DFL.Utils;
using DFLNetwork.Protocole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace DFLNetwork
{
    public class TcpServer
    {
        public TcpServer() { }
        public List<ServerListener> Listeners = new List<ServerListener>();
        public event EventHandler<TcpClient> ClientConnected;
        public event EventHandler<TcpClient> ClientDisconnected;
        public event EventHandler<NetworkMessage> DataReceived;

        public IEnumerable<IPAddress> GetIPAddresses()
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();
            IEnumerable<NetworkInterface> enabledNetInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up);
            foreach (NetworkInterface netInterface in enabledNetInterfaces)
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                    if (!ipAddresses.Contains(addr.Address))
                        ipAddresses.Add(addr.Address);
            }
            var ipSorted = ipAddresses.OrderByDescending(ip => RankIpAddress(ip)).ToList();
            return ipSorted;
        }

        public List<IPAddress> GetListeningIPs()
        {
            List<IPAddress> listenIps = new List<IPAddress>();
            foreach (var l in Listeners)
                if (!listenIps.Contains(l.IPAddress))
                    listenIps.Add(l.IPAddress);
            return listenIps.OrderByDescending(ip => RankIpAddress(ip)).ToList();
        }

        #region Sending
        public void Broadcast(HeadActions head, byte[] msg)
        {
            foreach (var listener in Listeners)
                foreach (var client in listener.ConnectedClients)
                    client.GetStream().Write(msg, 0, msg.Length);
            Writer.Write(head + " <<<= Message Broadcasted. " + msg.Length + " bytes ", ConsoleColor.Magenta);
        }

        public void Reply(HeadActions head, byte[] msg, TcpClient _tcpClient)
        {
            _tcpClient.GetStream().Write(msg, 0, msg.Length);
            Writer.Write(head + " <=> Reply " + msg.Length + " bytes ", ConsoleColor.DarkYellow);
        }

        public void SendToClient(HeadActions head, byte[] msg, TcpClient _tcpClient)
        {
            _tcpClient.GetStream().Write(msg, 0, msg.Length);
            Writer.Write(head + " <= Message Sended. " + msg.Length + " bytes", ConsoleColor.DarkCyan);
        }

        public void SendToClients(HeadActions head, byte[] msg, List<TcpClient> _tcpClient)
        {
            if (_tcpClient.Count > 0)
            {
                foreach (var client in _tcpClient)
                    client.GetStream().Write(msg, 0, msg.Length);
                Writer.Write(head + " <<= Message Sended on map.  [" + _tcpClient.Count + "] - " + msg.Length + " bytes", ConsoleColor.Cyan);
            }
        }
        #endregion

        private int RankIpAddress(IPAddress addr)
        {
            int rankScore = 1000;
            if (IPAddress.IsLoopback(addr))
                rankScore = 300;
            else if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                rankScore += 100;
                if (addr.GetAddressBytes().Take(2).SequenceEqual(new byte[] { 169, 254 }))
                    rankScore = 0;
            }
            if (rankScore > 500)
                foreach (var nic in TryGetCurrentNetworkInterfaces())
                {
                    var ipProps = nic.GetIPProperties();
                    if (ipProps.GatewayAddresses.Any())
                    {
                        if (ipProps.UnicastAddresses.Any(u => u.Address.Equals(addr)))
                            rankScore += 1000;
                        break;
                    }
                }
            return rankScore;
        }

        private static IEnumerable<NetworkInterface> TryGetCurrentNetworkInterfaces()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces().Where(ni => ni.OperationalStatus == OperationalStatus.Up);
            }
            catch (NetworkInformationException)
            {
                return Enumerable.Empty<NetworkInterface>();
            }
        }

        public TcpServer Start(int port, bool ignoreNicsWithOccupiedPorts = true)
        {
            var ipSorted = GetIPAddresses();
            bool anyNicFailed = false;
            foreach (var ipAddr in ipSorted)
            {
                try
                {
                    if (ipAddr.ToString() != "127.0.0.1" && ipAddr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Start(ipAddr, port);
                        Writer.Write_Server("  - " + ipAddr.ToString(), ConsoleColor.Yellow);
                    }
                    else
                        Writer.Write_Server("  - Switch IP : " + ipAddr.ToString(), ConsoleColor.DarkGray);
                }
                catch (SocketException ex)
                {
                    Writer.Write(ex.ToString(), ConsoleColor.Red);
                    anyNicFailed = true;
                }
            }

            if (!IsStarted)
                throw new InvalidOperationException("Port was already occupied for all network interfaces");

            if (anyNicFailed && !ignoreNicsWithOccupiedPorts)
            {
                Stop();
                throw new InvalidOperationException("Port was already occupied for one or more network interfaces.");
            }

            return this;
        }

        public TcpServer Start(int port, AddressFamily addressFamilyFilter)
        {
            var ipSorted = GetIPAddresses().Where(ip => ip.AddressFamily == addressFamilyFilter);
            foreach (var ipAddr in ipSorted)
            {
                try
                {
                    Start(ipAddr, port);
                }
                catch { }
            }

            return this;
        }

        public bool IsStarted { get { return Listeners.Any(l => l.Listener.Active); } }

        public TcpServer Start(IPAddress ipAddress, int port)
        {
            ServerListener listener = new ServerListener(this, ipAddress, port);
            Listeners.Add(listener);
            return this;
        }

        public void Stop()
        {
            Listeners.All(l => l.QueueStop = true);
            while (Listeners.Any(l => l.Listener.Active))
            {
                Thread.Sleep(100);
            };
            Listeners.Clear();
        }

        public int ConnectedClientsCount
        {
            get
            {
                return Listeners.Sum(l => l.ConnectedClientsCount);
            }
        }

        internal void NotifyMessageRx(TcpClient client, byte[] msg)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new NetworkMessage(msg, client));
            }
        }

        internal void NotifyClientConnected(ServerListener listener, TcpClient newClient)
        {
            if (ClientConnected != null)
                ClientConnected(this, newClient);
        }

        internal void NotifyClientDisconnected(ServerListener listener, TcpClient disconnectedClient)
        {
            if (ClientDisconnected != null)
                ClientDisconnected(this, disconnectedClient);
        }
    }
}