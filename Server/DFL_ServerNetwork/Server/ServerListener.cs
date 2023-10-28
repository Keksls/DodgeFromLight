using DFL.Utils;
using DFLCommonNetwork.Protocole;
using DFLServer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DFLNetwork
{
    public class ServerListener
    {
        private TcpListenerEx _listener = null;
        private List<TcpClient> _connectedClients = new List<TcpClient>();
        private List<TcpClient> _verifyingClients = new List<TcpClient>();
        private List<TcpClient> _verifiedClients = new List<TcpClient>();
        private List<TcpClient> _disconnectedClients = new List<TcpClient>();
        private TcpServer _parent = null;
        private Thread _rxThread = null;
        internal bool QueueStop { get; set; }
        internal IPAddress IPAddress { get; private set; }
        internal int Port { get; private set; }
        internal int ReadLoopIntervalMs { get; set; }
        internal TcpListenerEx Listener { get { return _listener; } }
        public int ConnectedClientsCount
        {
            get { return _connectedClients.Count; }
        }
        public int VerifyingClientsCount
        {
            get { return _verifyingClients.Count; }
        }
        public IEnumerable<TcpClient> ConnectedClients { get { return _connectedClients; } }
        private int currentLenght = -1;

        internal ServerListener(TcpServer parentServer, IPAddress ipAddress, int port)
        {
            QueueStop = false;
            _parent = parentServer;
            IPAddress = ipAddress;
            Port = port;
            ReadLoopIntervalMs = 10;
            _listener = new TcpListenerEx(ipAddress, port);
            _listener.Start();
            ThreadPool.QueueUserWorkItem(ListenerLoop);
        }

        private void StartThread()
        {
            if (_rxThread != null) { return; }
            _rxThread = new Thread(ListenerLoop);
            _rxThread.IsBackground = true;
            _rxThread.Start();
        }

        private void ListenerLoop(object state)
        {
            while (!QueueStop)
            {
                try { RunLoopStep(); }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(ex.ToString());
                }
                Thread.Sleep(1);
            }
            _listener.Stop();
        }

        bool IsSocketConnected(Socket s)
        {
            // https://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected)
                return false;
            else
                return true;
        }

        private void RunLoopStep()
        {
            // HANDLE Disconnection
            lock (_disconnectedClients)
            {
                if (_disconnectedClients.Count > 0)
                {
                    var disconnectedClients = _disconnectedClients.ToArray();
                    _disconnectedClients.Clear();
                    foreach (var disC in disconnectedClients)
                    {
                        _connectedClients.Remove(disC);
                        _parent.NotifyClientDisconnected(this, disC);
                    }
                    Server.UpdateTitle();
                }
            }

            // Handle conenctions
            if (_listener.Pending())
            {
                Thread t = new Thread(() =>
                {
                    var newClient = _listener.AcceptTcpClient();
                    Server.UpdateTitle();
                    if (BlackListManager.IsBlackListed(newClient))
                        newClient.Close();
                    else
                    {
                        lock (_verifyingClients)
                        {
                            _verifyingClients.Add(newClient);
                        }
                        ValidateClient(newClient);
                    }
                });
                t.Start();
            }

            // Handle Adding verified
            lock (_verifiedClients)
            {
                if (_verifiedClients.Count > 0)
                {
                    _connectedClients.AddRange(_verifiedClients);
                    _connectedClients.Clear();
                    Server.UpdateTitle();
                }
            }

            // Handle clients packets
            lock (_connectedClients)
            {
                byte[] bytesReceived = new byte[0];
                foreach (var c in _connectedClients)
                {
                    if (!IsSocketConnected(c.Client))
                    {
                        lock (_disconnectedClients)
                        {
                            _disconnectedClients.Add(c);
                        }
                        continue;
                    }

                    // get size
                    if (currentLenght == -1 && c.Available > 4)
                    {
                        byte[] nextByte = new byte[4];
                        c.Client.Receive(nextByte, 0, 4, SocketFlags.None);
                        currentLenght = BitConverter.ToInt32(nextByte, 0);
                        bytesReceived = new byte[currentLenght];
                    }

                    int i = 0;
                    while (c.Available > 0 && c.Connected && currentLenght != -1)
                    {
                        c.Client.Receive(bytesReceived, i, 1, SocketFlags.None);
                        i++;

                        // all message recieved
                        if (i == currentLenght)
                        {
                            currentLenght = -1;
                            _parent.NotifyMessageRx(c, bytesReceived);
                        }
                    }
                }
            }
        }

        private void ValidateClient(TcpClient client)
        {
            Thread t = new Thread(() =>
            {
                long timeEnd = DateTime.Now.AddSeconds(30).Ticks;

                // send handShake
                int rnd1 = 0;
                int rnd2 = 0;
                int key = 0;
                byte[] handShake = HandShake.GetRandomHandShake(out rnd1, out rnd2, out key);
                client.GetStream().Write(handShake, 0, handShake.Length);
                bool isClientOK = false;
                Writer.Write("HandShake client " + rnd1 + " " + rnd2 + " " + key, ConsoleColor.Cyan);

                // wait for client renspond correct hash
                while (client.Connected && DateTime.Now.Ticks < timeEnd)
                {
                    // client disconnect
                    if (!IsSocketConnected(client.Client))
                    {
                        Writer.Write("Client disconected before end handshake. Close his connection", ConsoleColor.Red);
                        break;
                    }

                    // get awnser
                    if (client.Available >= 4)
                    {
                        byte[] array = new byte[4];
                        client.Client.Receive(array, 0, 4, SocketFlags.None);
                        int clientKey = BitConverter.ToInt32(array, 0);
                        if (clientKey == key)
                            isClientOK = true;
                        else
                            Writer.Write("Client awnser wrong handshake key.", ConsoleColor.Red);
                        break;
                    }
                    Thread.Sleep(1);
                }

                // client awnser good
                if (isClientOK)
                {
                    Writer.Write("Client awnser good handshake key. Accept it.", ConsoleColor.Green);
                    lock (_verifyingClients)
                    {
                        _verifyingClients.Remove(client);
                    }
                    lock (_connectedClients)
                    {
                        _connectedClients.Add(client);
                    }
                    _parent.NotifyClientConnected(this, client);
                    client.GetStream().Write(BitConverter.GetBytes(key), 0, 4);
                }
                // no awnser, awnser error, disconnected or timeout
                else
                {
                    _verifyingClients.Remove(client);
                    client.Close();
                    client.Dispose();
                }
            });
            t.Start();
        }
    }
}