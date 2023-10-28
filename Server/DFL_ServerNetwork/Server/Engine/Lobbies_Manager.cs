using DFL.Utils;
using DFLCommonNetwork.GameEngine;
using DFLNetwork.Protocole;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DFLServer
{
    public static class Lobbies_Manager
    {
        public static Dictionary<int, Lobby> Lobbies = new Dictionary<int, Lobby>(); // lobbyID => lobby
        public static Dictionary<int, int> ClientLobby = new Dictionary<int, int>(); // clientID => lobbyID
        private static List<string> LobbyEnterMessages = new List<string>() {
        "<b>%name%</b> just enter the lobby.",
        "Say hello to our new friend <b>%name%</b>.",
        "Oh my god, it's <b>%name%</b> !",
        "The sweet <b>%name%</b> is here.",
        "So happy to see <b>%name%</b> here again."
        };
        static Random rand = new Random();

        static Lobbies_Manager()
        {
            Server.OnDisconnected += ClientDisconnected;
        }

        public static void LoadLobbies(List<Lobby> lobbies)
        {
            foreach (Lobby lobby in lobbies)
                Lobbies.Add(lobby.ID, lobby);
        }

        public static void LoadHubs(List<GameClient> users)
        {
            foreach (GameClient user in users)
            {
                Lobby lobby = new Lobby()
                {
                    Clients = new Dictionary<int, CellPos>(),
                    HubOwner = user.ID,
                    IsHub = true,
                    MaxClients = 8,
                    Name = user.Name + "'s Hub",
                    ID = -user.ID
                };
                Lobbies.Add(lobby.ID, lobby);
            }
        }

        public static void LoadHub(GameClient user)
        {
            Lobby lobby = new Lobby()
            {
                Clients = new Dictionary<int, CellPos>(),
                HubOwner = user.ID,
                IsHub = true,
                MaxClients = 8,
                Name = user.Name + "'s Hub",
                ID = -user.ID
            };
            Lobbies.Add(lobby.ID, lobby);
        }

        #region Network Methods
        public static void SendClientsOnLobby()
        {
            try
            {
                int lobbyID = 0;
                Server.currentMessage.Get(ref lobbyID);
                List<LitePlayerSave> Persos = new List<LitePlayerSave>();
                foreach (var client in GetLobby(lobbyID).Clients)
                    if (Server.currentClientID != client.Key)
                        Persos.Add(new LitePlayerSave(Server.Clients[client.Key].GameClient, client.Value));
                Server.Reply(new NetworkMessage().Set(true).SetObject(Persos));
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false).Set("Fail to get clients on this lobby."));
            }
        }

        public static void ClientEnterLobby()
        {
            int lobbyID = 0;
            short posX = 0, posY = 0;
            Server.currentMessage.Get(ref lobbyID);
            Server.currentMessage.Get(ref posX);
            Server.currentMessage.Get(ref posY);

            CellPos pos = new CellPos(posX, posY);
            if (EnterLobby(lobbyID, pos))
            {
                Database.SetState(Server.currentClientID, PlayerState.InLobby);
                LitePlayerSave client = new LitePlayerSave(getClient(), pos);
                // Préviens les autre joueurs que le client entre dans le lobby
                Server.SendToClients(new NetworkMessage(HeadActions.toClient_EnterLobby).SetObject(client), GetClientsOnLobbyButMe());
                Server.Reply(new NetworkMessage()
                    .Set(true)
                    .SetObject(client)
                    .Set(Lobbies[lobbyID].Name)
                    .Set(Lobbies[lobbyID].MaxClients)
                    .Set(Lobbies[lobbyID].Clients.Count));
                string welcomeMessage = LobbyEnterMessages[rand.Next(0, LobbyEnterMessages.Count)].Replace("%name%", Server.GetClient(Server.currentClientID).GameClient.Name);
                int lId = 0;
                if (GetLobbyForClient(out lId))
                    Console_Manager.SpeakToClients(ChatCanal.System, welcomeMessage, GetClientsOnLobby(lId));
            }
            else
                Server.Reply(new NetworkMessage().Set(false).Set("Fail enter lobby"));
        }

        public static void ClientLeaveLobby()
        {
            var clients = GetClientsOnLobbyButMe();
            if (LeaveLobby())
            {
                Database.SetState(Server.currentClientID, PlayerState.InGame);
                // Préviens les autre joueurs que le client entre dans le lobby
                Server.SendToClients(new NetworkMessage(HeadActions.toClient_LeaveLobby).Set(Server.currentClientID), clients);
                Server.Reply(new NetworkMessage().Set(true));
            }
            else
                Server.Reply(new NetworkMessage().Set(false));
        }

        public static void ClientChangeCell()
        {
            short posX = 0, posY = 0;
            Server.currentMessage.Get(ref posX);
            Server.currentMessage.Get(ref posY);
            CellPos pos = new CellPos(posX, posY);
            if (!IsInLobby())
                return;
            ChangeCell(pos);
            // Préviens les autre joueurs que le client change de cell
            Server.SendToClients(new NetworkMessage(HeadActions.toClient_ChangeCell).Set(Server.currentClientID).Set((short)pos.X).Set((short)pos.Y), GetClientsOnLobbyButMe());
        }

        public static void ClientGiveMeLobbiesList()
        {
            List<ViewLobby> lobbies = new List<ViewLobby>();
            foreach (var pair in Lobbies)
                if (!pair.Value.IsHub)
                    lobbies.Add(new ViewLobby(pair.Value));
            Server.Reply(new NetworkMessage().SetObject(lobbies));
        }

        public static void SetOrientation()
        {
            byte dir = 0;
            Server.currentMessage.Get(ref dir);
            int lobbyID = 0;
            if (GetLobbyForClient(out lobbyID))
                Server.SendToClients(new NetworkMessage(HeadActions.toClient_SetOrientation).Set(Server.currentClientID).Set(dir), GetClientsOnLobby(lobbyID));
        }

        public static void TryJoinPlayer()
        {
            int targetID = 0, senderID = 0;
            bool force = false;
            Server.currentMessage.Get(ref targetID);
            Server.currentMessage.Get(ref senderID);
            Server.currentMessage.Get(ref force);
            JoinPlayer(targetID, senderID, force);
        }

        public static void AwnnserEnterHub()
        {
            int senderID = 0;
            bool awnser = false;
            Server.currentMessage.Get(ref senderID);
            Server.currentMessage.Get(ref awnser);
            if (awnser)
                Server.SendToClient(new NetworkMessage(HeadActions.toClient_ForceEnterLobby).Set(-Server.currentClientID), senderID);
            else
                Server.SendToClient(new NetworkMessage(HeadActions.toClient_Notify).Set(Server.GetClient(Server.currentClientID).GameClient.Name + " Refused to let you in.."), senderID);
        }

        public static void PlayAnimation()
        {
            string animationName = "";
            Server.currentMessage.Get(ref animationName);
            int lobbyID = 0;
            if (GetLobbyForClient(out lobbyID))
                Server.SendToClients(new NetworkMessage(HeadActions.toClient_PlayAnimation).Set(Server.currentClientID).Set(animationName), GetClientsOnLobby(lobbyID));
        }
        #endregion

        #region Connect / Disconnect
        public static void ClientConnected(int clientID)
        {

        }

        public static void ClientDisconnected(int clientID)
        {
            if (ClientLobby.ContainsKey(clientID))
            {
                int lobbyID = ClientLobby[clientID];
                if (LeaveLobby())
                    Server.SendToClients(new NetworkMessage(HeadActions.toClient_LeaveLobby).Set(clientID), GetClientsOnLobby(lobbyID));
            }
        }
        #endregion

        #region Utils
        public static void JoinPlayer(int targetID, int senderID, bool force)
        {
            if (ClientLobby.ContainsKey(targetID))
            {
                int lobbyID = ClientLobby[targetID];
                // target is in hub
                if (lobbyID < 0 && !force)
                {
                    int hubOwner = -lobbyID;
                    if (ClientLobby.ContainsKey(hubOwner) && ClientLobby[hubOwner] == lobbyID) // check if owner of this hub is in hub
                    {
                        // ask to owner if allow
                        Server.SendToClient(new NetworkMessage(HeadActions.toClient_AskEnterHub).Set(senderID).Set(Server.GetClient(senderID).GameClient.Name), hubOwner);
                    }
                    else // owner of the hub is not in his hub
                        Server.SendToClient(new NetworkMessage(HeadActions.toClient_Notify).Set("Player is in someone else Hub, but this owner is not available."), Server.currentClientID);
                }
                else // target is in lobby
                    Server.SendToClient(new NetworkMessage(HeadActions.toClient_ForceEnterLobby).Set(lobbyID), senderID);
            }
            else
                Server.SendToClient(new NetworkMessage(HeadActions.toClient_Notify).Set("This player is currently not joinable."), Server.currentClientID);
        }


        private static Lobby GetLobby(int lobbyID)
        {
            return Lobbies[lobbyID];
        }

        public static bool IsInLobby()
        {
            return ClientLobby.ContainsKey(Server.currentClientID);
        }

        private static bool EnterLobby(int lobbyID, CellPos Cell)
        {
            if (!Lobbies.ContainsKey(lobbyID))
                return false;
            if (Lobbies[lobbyID].Clients.ContainsKey(Server.currentClientID))
                return false;
            if (Lobbies[lobbyID].Clients.Count >= Lobbies[lobbyID].MaxClients)
            {
                Console_Manager.SpeakToClient(ChatCanal.System, "<color=red>Sorry, the lobby <b>" + Lobbies[lobbyID].Name + "</b> is full.", Server.currentClientID);
                return false;
            }

            Lobbies[lobbyID].Clients.Add(Server.currentClientID, Cell);
            if (!ClientLobby.ContainsKey(Server.currentClientID))
                ClientLobby.Add(Server.currentClientID, lobbyID);
            else
                ClientLobby[Server.currentClientID] = lobbyID;

            return true;
        }

        private static bool LeaveLobby()
        {
            int lobbyID = 0;
            if (!GetLobbyForClient(out lobbyID))
                return false;
            if (!Lobbies.ContainsKey(lobbyID))
                return false;
            if (!Lobbies[lobbyID].Clients.ContainsKey(Server.currentClientID))
                return false;

            Lobbies[lobbyID].Clients.Remove(Server.currentClientID);
            ClientLobby.Remove(Server.currentClientID);

            return true;
        }

        public static List<TcpClient> GetClientsOnLobbyButMe()
        {
            List<TcpClient> clients = new List<TcpClient>();

            int lobbyID = 0;
            if (!GetLobbyForClient(out lobbyID))
                return clients;
            if (Lobbies.ContainsKey(lobbyID))
                foreach (var client in Lobbies[lobbyID].Clients)
                    if (client.Key != Server.currentClientID)
                        clients.Add(Server.Clients[client.Key].TCPClient);

            return clients;
        }

        public static List<TcpClient> GetClientsOnLobby(int lobbyID)
        {
            List<TcpClient> clients = new List<TcpClient>();

            if (Lobbies.ContainsKey(lobbyID))
                foreach (var client in Lobbies[lobbyID].Clients)
                    clients.Add(Server.Clients[client.Key].TCPClient);

            return clients;
        }

        public static bool GetLobbyForClient(out int lobbyID)
        {
            lobbyID = 0;
            if (ClientLobby.ContainsKey(Server.currentClientID))
            {
                lobbyID = ClientLobby[Server.currentClientID];
                return true;
            }
            return false;
        }

        private static bool ChangeCell(CellPos pos)
        {
            int lobbyID = 0;
            if (!GetLobbyForClient(out lobbyID))
                return false;
            if (!Lobbies.ContainsKey(lobbyID))
                return false;
            if (!Lobbies[lobbyID].Clients.ContainsKey(Server.currentClientID))
                return false;

            Lobbies[lobbyID].Clients[Server.currentClientID] = pos;
            return true;
        }

        private static GameClient getClient()
        {
            return Server.Clients[Server.currentMessage.ClientID].GameClient;
        }
        #endregion
    }
}