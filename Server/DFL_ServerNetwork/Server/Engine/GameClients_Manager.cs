using DFLNetwork;
using System.Linq;
using DFLNetwork.Protocole;
using DFLCommonNetwork.GameEngine;

namespace DFLServer
{
    public static class GameClients_Engine
    {
        static GameClients_Engine()
        {
            Server.OnDisconnected += ClientDisconnected;
        }

        #region Network Methods
        public static void ConnectUser()
        {
            string account = null;
            Server.currentMessage.Get(ref account);
            string password = null;
            Server.currentMessage.Get(ref password);

            // client seem to be already connected
            if (Server.Clients.Select(c => c.Value.GameClient).Count(c => c.Name == account) > 0)
            {
                var client = Server.Clients.Select(c => c.Value).Where(c => c.GameClient.Name == account).FirstOrDefault();
                if (client != null)
                {
                    // disconnect the old client
                    Server.Server_ClientDisconnected(null, client.TCPClient);
                }
            }

            // connect this client
            GameClient gameClient = Database.getUser(account, password);
            if (gameClient != null)
            {
                ConnectedClient client = new ConnectedClient()
                {
                    GameClient = gameClient,
                    ID = gameClient.ID,
                    ServerIP = null,
                    TCPClient = Server.currentTCPClient
                };
                Server.Clients.Add(client.ID, client);
                gameClient.Pass = null;
                Server.Reply(new NetworkMessage().Set(true).SetObject(gameClient));
            }
            else
                Server.Reply(new NetworkMessage().Set(false).Set("Account or password incorrect."));
        }

        public static void CreateUser()
        {
            string name = null, pass = null, mail = null;
            Server.currentMessage.Get(ref name);
            Server.currentMessage.Get(ref pass);
            Server.currentMessage.Get(ref mail);
            PlayerSave playerSave = null;
            Server.currentMessage.GetObject<PlayerSave>(ref playerSave);

            if (name.Length < Server.config.AccountLenghtMin)
                Server.Reply(new NetworkMessage().Set(false).Set("Account must contains at least " + Server.config.AccountLenghtMin + " chars."));
            else if (pass.Length < Server.config.AccountLenghtMin)
                Server.Reply(new NetworkMessage().Set(false).Set("Password must contains at least " + Server.config.AccountLenghtMin + " chars."));
            else
            {
                if (Database.AddUser(name, pass, mail, playerSave))
                {
                    var user = Database.getUser(name, pass);
                    Lobbies_Manager.LoadHub(user);
                    Server.Reply(new NetworkMessage().Set(true).SetObject(user));
                }
                else
                    Server.Reply(new NetworkMessage().Set(false).Set("Account already exists."));
            }
        }

        public static void GetLiteHero()
        {
            int clientID = 0;
            Server.currentMessage.Get(ref clientID);
            GameClient gameClient = Database.getUser(clientID);
            if (gameClient == null)
            {
                Server.Reply(new NetworkMessage().Set(false).Set("fail to get client"));
                return;
            }
            LitePlayerSave client = new LitePlayerSave(gameClient, new CellPos());
            Server.Reply(new NetworkMessage().Set(true).SetObject(client));
        }

        public static void SetXP()
        {
            try
            {
                int clientID = 0, XP = 0;
                Server.currentMessage.Get(ref clientID);
                Server.currentMessage.Get(ref XP);
                SetXp(clientID, XP);
                Server.Reply(new NetworkMessage().Set(true).Set("Set XP to " + XP));
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false).Set("Fail to set XP"));
            }
        }

        public static void UnlockSkin()
        {
            try
            {
                int clientID = 0;
                short SkinID = 0, OnlineLastRewardIndex = 0;
                byte SkinType = 0;
                Server.currentMessage.Get(ref clientID);
                Server.currentMessage.Get(ref SkinType);
                Server.currentMessage.Get(ref SkinID);
                Server.currentMessage.Get(ref OnlineLastRewardIndex);

                UnlockSkin(clientID, (SkinType)SkinType, SkinID, OnlineLastRewardIndex);
                Server.Reply(new NetworkMessage().Set(true).Set("Unlocked skin : " + SkinID));
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false).Set("Fail to unlock skin"));
            }
        }

        public static void EquipSkin()
        {
            try
            {
                int clientID = 0;
                byte SkinType = 0;
                short SkinID = 0;
                Server.currentMessage.Get(ref clientID);
                Server.currentMessage.Get(ref SkinType);
                Server.currentMessage.Get(ref SkinID);

                // set to local if connected
                if (Server.IsClientConnected(clientID))
                    Server.GetClient(clientID).GameClient.PlayerSave.SetCurrentPart((SkinType)SkinType, SkinID);
                // set to database
                GameClient user = Database.getUser(clientID);
                user.PlayerSave.SetCurrentPart((SkinType)SkinType, SkinID);
                Database.UpdateUser(user);

                // if in lobby, send change skin to other clients of the lobby
                int lobbyID = 0;
                if (!Lobbies_Manager.GetLobbyForClient(out lobbyID))
                    Server.SendToClients(new NetworkMessage(HeadActions.toClient_EquipSkin).Set(clientID).Set((byte)SkinType).Set(SkinID), Lobbies_Manager.GetClientsOnLobby(lobbyID));
                else
                    Server.SendToClient(new NetworkMessage(HeadActions.toClient_EquipSkin).Set(clientID).Set((byte)SkinType).Set(SkinID), Server.currentClientID);
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false).Set("Fail to equip skin"));
            }
        }

        public static void SetState()
        {
            byte state = 0;
            Server.currentMessage.Get(ref state);
            Database.SetState(Server.currentClientID, (PlayerState)state);
        }
        #endregion

        #region Utils
        private static void SetXp(int clientID, int XP)
        {
            // set to local if connected
            if (Server.IsClientConnected(clientID))
                Server.GetClient(clientID).GameClient.PlayerSave.XP = XP;
            // set to database
            GameClient user = Database.getUser(clientID);
            user.PlayerSave.XP = XP;
            Database.UpdateUser(user);
        }

        public static PlayerSave UnlockSkin(int clientID, SkinType SkinType, short SkinID, short OnlineLastRewardIndex)
        {
            // set to local if connected
            if (Server.IsClientConnected(clientID))
            {
                Server.GetClient(clientID).GameClient.PlayerSave.Unlock(SkinType, SkinID);
                if (OnlineLastRewardIndex > -1)
                    Server.GetClient(clientID).GameClient.PlayerSave.OnlineLastReward = OnlineLastRewardIndex;
            }
            // set to database
            GameClient user = Database.getUser(clientID);
            user.PlayerSave.Unlock(SkinType, SkinID);
            if (OnlineLastRewardIndex > -1)
                user.PlayerSave.OnlineLastReward = OnlineLastRewardIndex;
            Database.UpdateUser(user);
            return user.PlayerSave;
        }

        public static PlayerSave UnlockAll(int clientID)
        {
            PlayerSave skins = PhysicalPersistance.GetFullyUnlockedSave();
            // set to local if connected
            if (Server.IsClientConnected(clientID))
            {
                Server.GetClient(clientID).GameClient.PlayerSave.Unlocked = skins.Unlocked;
                Server.GetClient(clientID).GameClient.PlayerSave.OnlineLastReward = skins.OnlineLastReward;
                Server.GetClient(clientID).GameClient.PlayerSave.XP = skins.XP;
            }
            // set to database
            GameClient user = Database.getUser(clientID);
            user.PlayerSave.Unlocked = skins.Unlocked;
            user.PlayerSave.OnlineLastReward = skins.OnlineLastReward;
            user.PlayerSave.XP = skins.XP;
            Database.UpdateUser(user);
            return user.PlayerSave;
        }
        #endregion

        #region Connect / Disconnect
        public static void ClientConnected(int clientID)
        {

        }

        public static void ClientDisconnected(int clientID)
        {
            Database.SetState(clientID, PlayerState.NotConnected);
        }
        #endregion
    }
}