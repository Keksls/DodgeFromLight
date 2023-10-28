using DFL.Utils;
using DFLNetwork.Protocole;
using DFLNetwork;
using DFLServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

namespace DFLServer
{
    public static class Server
    {
        #region Variables
        private static Dictionary<HeadActions, Action> headToAction;
        private static ConcurrentQueue<NetworkMessage> MessagesQueue = new ConcurrentQueue<NetworkMessage>();
        private static TcpServer server;
        public static Configuration config = null;
        public static NetworkMessage currentMessage = null;
        public static int currentClientID { get { return currentMessage.ClientID; } }
        public static TcpClient currentTCPClient { get { return currentMessage.TcpClient; } }
        public static Dictionary<int, ConnectedClient> Clients = new Dictionary<int, ConnectedClient>(); // ID Client => ConnectedClient
        public static event Action<int> OnConnected;
        public static event Action<int> OnDisconnected;
        #endregion

        #region Start and Init
        public static void Initialize()
        {
            Writer.Write_Server("Loading Network Methods...", ConsoleColor.DarkYellow, false);
            headToAction = new Dictionary<HeadActions, Action>();
            // User
            headToAction.Add(HeadActions.toServer_CreateUser, GameClients_Engine.CreateUser);
            headToAction.Add(HeadActions.toServer_ConnectUser, GameClients_Engine.ConnectUser);
            headToAction.Add(HeadActions.toServer_GetLiteHero, GameClients_Engine.GetLiteHero);
            headToAction.Add(HeadActions.toServer_SetXP, GameClients_Engine.SetXP);
            headToAction.Add(HeadActions.toServer_EquipSkin, GameClients_Engine.EquipSkin);
            headToAction.Add(HeadActions.toServer_UnlockSkin, GameClients_Engine.UnlockSkin);
            headToAction.Add(HeadActions.toServer_SetState, GameClients_Engine.SetState);
            headToAction.Add(HeadActions.toServer_AddFriend, Social_Manager.AddFriend);
            headToAction.Add(HeadActions.toServer_RemoveFriend, Social_Manager.RemoveFriend);
            headToAction.Add(HeadActions.toServer_GetFriends, Social_Manager.GetFriends);
            // Lobby
            headToAction.Add(HeadActions.toServer_SendMeClientsOnLobby, Lobbies_Manager.SendClientsOnLobby);
            headToAction.Add(HeadActions.toServer_EnterLobby, Lobbies_Manager.ClientEnterLobby);
            headToAction.Add(HeadActions.toServer_LeaveLobby, Lobbies_Manager.ClientLeaveLobby);
            headToAction.Add(HeadActions.toServer_ChangeCell, Lobbies_Manager.ClientChangeCell);
            headToAction.Add(HeadActions.toServer_GiveMeLobbiesList, Lobbies_Manager.ClientGiveMeLobbiesList);
            headToAction.Add(HeadActions.toServer_SetOrientation, Lobbies_Manager.SetOrientation);
            headToAction.Add(HeadActions.toServer_TryJoinPlayer, Lobbies_Manager.TryJoinPlayer);
            headToAction.Add(HeadActions.toServer_AwnserEnterHub, Lobbies_Manager.AwnnserEnterHub);
            headToAction.Add(HeadActions.toServer_PlayAnimation, Lobbies_Manager.PlayAnimation);
            // Console
            headToAction.Add(HeadActions.toServer_Speak, Console_Manager.ClientSpeak);
            headToAction.Add(HeadActions.toServer_Emote, Console_Manager.ClientEmote);

            Writer.Write(headToAction.Count.ToString(), ConsoleColor.Green);
        }

        public static bool Start(int port)
        {
            Writer.Write_Server("Starting server on port " + port.ToString() + "...", ConsoleColor.DarkYellow);
            server = new TcpServer();
            server.Start(port);
            if (server.IsStarted)
            {
                Writer.Write_Server("started Success (" + server.Listeners.Count + " IP)", ConsoleColor.Green);
                server.ClientConnected += Server_ClientConnected;
                server.ClientDisconnected += Server_ClientDisconnected;
                server.DataReceived += Server_DataReceived;
                return true;
            }
            else
            {
                Writer.Write("FAIL", ConsoleColor.Red);
                return false;
            }
        }
        #endregion

        // handle the Recieving Queue
        public static void processMessagesQueue(int delay)
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    if (MessagesQueue.Count > 0)
                    {
                        if (!MessagesQueue.TryDequeue(out currentMessage))
                            Writer.Write("NullQuedMessageException", ConsoleColor.Red);
                        Writer.Write(currentMessage.Head.ToString() + " => [" + currentMessage.ClientID + "] " + currentMessage.Data.Length + " bytes ", ConsoleColor.DarkGray);

                        if (headToAction.ContainsKey(currentMessage.Head))
                            headToAction[currentMessage.Head]();
                        else
                        {
                            Writer.Write("Trying to Process message with head '" + currentMessage.Head.ToString() + "' but no action related... Message skipped.", ConsoleColor.DarkMagenta);
                            Reply(new NetworkMessage(HeadActions.None, currentClientID).Set(false));
                        }
                        currentMessage = null;
                    }
                    Thread.Sleep(1);
                    //continue;
                }
            });
            t.Start();
        }

        public static void UpdateTitle()
        {
            string title = "DFL Server - ";
            int nbConnected = 0;
            int nbVerifying = 0;
            foreach(var listner in server.Listeners)
            {
                nbConnected += listner.ConnectedClientsCount;
                nbVerifying += listner.VerifyingClientsCount;
            }
            title += "listner : " + server.Listeners.Count + " - Con. : " + nbConnected + " - Ver. : " + nbVerifying;
            Console.Title = title;
        }

        #region Sending and Rep
        public static void Reply(NetworkMessage msg)
        {
            msg.Head = currentMessage.Head;
            msg.ReplyTo(currentMessage.ReplyID);
            server.Reply(msg.Head, msg.Serialize(), currentMessage.TcpClient);
        }

        public static void SendToClient(NetworkMessage msg, TcpClient Client)
        {
            server.SendToClient(msg.Head, msg.Serialize(), Client);
        }

        public static void SendToClient(NetworkMessage msg, int ClientID)
        {
            server.SendToClient(msg.Head, msg.Serialize(), Clients[ClientID].TCPClient);
        }

        public static void SendToClients(NetworkMessage msg, List<TcpClient> Client)
        {
            server.SendToClients(msg.Head, msg.Serialize(), Client);
        }

        public static void Broadcast(NetworkMessage msg)
        {
            server.Broadcast(msg.Head, msg.Serialize());
        }
        #endregion

        #region ServerEvent
        public static void Server_ClientDisconnected(object sender, TcpClient e)
        {
            int clientID = -1;
            foreach (var client in Clients)
                if (client.Value.TCPClient == e)
                {
                    clientID = client.Key;
                    break;
                }

            if (clientID > 0)
            {
                Database.SetState(clientID, PlayerState.NotConnected);
                var message = currentMessage;
                currentMessage = new NetworkMessage(clientID);
                OnDisconnected?.Invoke(clientID);
                // supprime des clients connectés
                Clients.Remove(clientID);
                currentMessage = message;
                Writer.Write("Client disconnected Good!", ConsoleColor.Green);
            }
            else
                Writer.Write("Not connected client leave server.", ConsoleColor.Yellow);
        }

        private static void Server_ClientConnected(object sender, TcpClient e)
        {
            Writer.Write("New client connected !", ConsoleColor.Green);
        }

        private static void Server_DataReceived(object sender, NetworkMessage e)
        {
            if (e == null)
                Writer.Write("le message est null.", ConsoleColor.Red);
            else
                MessagesQueue.Enqueue(e);
        }
        #endregion

        #region Utils
        public static int GetClientIDByName(string clientName)
        {
            foreach (var client in Clients)
                if (GetClient(client.Key).GameClient.Name == clientName)
                    return client.Key;
            return -1;
        }

        public static bool IsClientConnected(int clientID)
        {
            return Clients.ContainsKey(clientID);
        }

        public static ConnectedClient GetClient(int clientID)
        {
            return Clients[clientID];
        }

        public static void AddClient(ConnectedClient client)
        {
            Clients.Add(client.ID, client);
            OnConnected?.Invoke(client.ID);
        }
        #endregion
    }
}