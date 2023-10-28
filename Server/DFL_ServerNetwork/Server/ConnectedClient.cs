using DFLCommonNetwork.GameEngine;
using System.Net;
using System.Net.Sockets;

namespace DFLNetwork
{
    public class ConnectedClient
    {
        public int ID { get; internal set; }
        public IPAddress ServerIP { get; internal set; }
        public TcpClient TCPClient { get; internal set; }
        public GameClient GameClient { get; internal set; }
    }
}