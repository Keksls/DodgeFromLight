using DFL.Utils;
using DFLCommonNetwork.GameEngine;
using DFLNetwork.Protocole;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DFLServer
{
    public static class Console_Manager
    {
        private static Dictionary<ChatCanal, char> CanalChar = new Dictionary<ChatCanal, char>();
        private static Dictionary<char, ChatCanal> CharCanal = new Dictionary<char, ChatCanal>();

        static Console_Manager()
        {
            CanalChar = new Dictionary<ChatCanal, char>();
            CanalChar.Add(ChatCanal.General, 's');
            CanalChar.Add(ChatCanal.PrivateMessage, 'w');

            CharCanal = new Dictionary<char, ChatCanal>();
            CharCanal.Add('s', ChatCanal.General);
            CharCanal.Add('w', ChatCanal.PrivateMessage);
        }

        #region Network Methods
        public static void ClientSpeak()
        {
            try
            {
                string text = null;
                Server.currentMessage.Get(ref text);
                string clientName = Server.GetClient(Server.currentClientID).GameClient.Name;

                if (text.StartsWith("/"))
                {
                    string[] args = text.Trim().Split(' ');
                    if (args[0].Length == 2 && !args[1].StartsWith("/")) // Speak to canal
                    {
                        ChatCanal canal = CharCanal[args[0][1]];
                        string message = "";
                        switch (canal)
                        {
                            case ChatCanal.General:
                                message = text.Trim().Substring(args[0].Length + 1);
                                // if I am in a lobby
                                int lobbyID = 0;
                                if (Lobbies_Manager.IsInLobby() && Lobbies_Manager.GetLobbyForClient(out lobbyID))
                                {
                                    List<TcpClient> clients = Lobbies_Manager.GetClientsOnLobby(lobbyID);
                                    SpeakToClients(ChatCanal.General, message, clients);
                                }
                                else
                                    SpeakToClient(ChatCanal.General, message, Server.currentClientID);
                                break;

                            case ChatCanal.PrivateMessage:
                                message = text.Trim().Substring(args[0].Length + 1 + args[1].Length);
                                int targetID = Server.GetClientIDByName(args[1]);
                                if (targetID == -1)
                                    SpeakToClient(ChatCanal.System, "<color=red>" + args[1] + " is not connected or does not exist.</color>", Server.currentClientID);
                                else
                                {
                                    SpeakToClient(ChatCanal.PrivateMessage, "[ <i>" + args[1] + "</i> ]" + message, Server.currentClientID);
                                    SpeakToClient(ChatCanal.PrivateMessage, message, targetID);
                                }
                                break;
                        }
                    }
                    else // Command
                    {
                        string cmd = args[1];
                        switch (cmd)
                        {
                            case "/unlock":
                                if (VerifyPrivileges())
                                    Unlockcmd(args);
                                break;

                            case "/system":
                                if (VerifyPrivileges())
                                    SystemSpeak(args);
                                break;

                            case "/join":
                                if (VerifyPrivileges())
                                    Join(args);
                                break;

                            case "/friend":
                                AddFriendCmd(args);
                                break;

                            case "/unfriend":
                                RemoveFriendCmd(args);
                                break;
                        }
                    }
                }
                else
                    Writer.Write(text, ConsoleColor.Red);
            }
            catch (Exception ex) { Writer.Write(ex.Message, ConsoleColor.Red); }
        }

        public static void ClientEmote()
        {
            byte emoteType = 0;
            short emoteID = 0;
            Server.currentMessage.Get(ref emoteType);
            Server.currentMessage.Get(ref emoteID);
            int lobbyID = 0;
            if (Lobbies_Manager.IsInLobby() && Lobbies_Manager.GetLobbyForClient(out lobbyID))
                Server.SendToClients(new NetworkMessage(HeadActions.toClient_Emote).Set(Server.currentClientID).Set(emoteType).Set(emoteID), Lobbies_Manager.GetClientsOnLobby(lobbyID));
            else
                Server.SendToClient(new NetworkMessage(HeadActions.toClient_Emote).Set(Server.currentClientID).Set(emoteType).Set(emoteID), Server.currentClientID);
        }
        #endregion

        #region Commands
        private static bool VerifyPrivileges()
        {
            if (!PhysicalPersistance.Admins.Contains(Server.currentClientID))
            {
                SpeakToClient(ChatCanal.System, "<color=red><b>You are not allowed to use this commands</b></color>", Server.currentClientID);
                return false;
            }
            else
                return true;
        }

        private static void Unlockcmd(string[] args)
        {
            try
            {
                string targetName = args[2];
                int targetID = Server.GetClientIDByName(targetName);
                if (targetID == -1)
                {
                    SpeakToClient(ChatCanal.System, "<color=orange>" + targetName + " is not connected into your current lobby</color>", Server.currentClientID);
                    return;
                }
                if (args[3] == "all")
                {
                    PlayerSave ps = GameClients_Engine.UnlockAll(targetID);
                    Server.SendToClient(new NetworkMessage(HeadActions.toClient_RefreshPlayerSave).SetObject(ps), targetID);
                    SpeakToClient(ChatCanal.System, "<b>" + targetName + "</b> get everything unlocked !", targetID);
                }
                else
                {
                    SkinType skinType = (SkinType)int.Parse(args[3]);
                    short skinID = short.Parse(args[4]);
                    HashSet<SkinType> unlockable = new HashSet<SkinType>() { SkinType.Pet, SkinType.Sword, SkinType.Wand, SkinType.Shield };
                    if (unlockable.Contains(skinType))
                    {
                        PlayerSave ps = GameClients_Engine.UnlockSkin(targetID, skinType, skinID, -1);
                        Server.SendToClient(new NetworkMessage(HeadActions.toClient_RefreshPlayerSave).SetObject(ps), targetID);
                        SpeakToClient(ChatCanal.System, "<b>" + targetName + "</b> get a new " + skinType.ToString(), targetID);
                    }
                    else
                    {
                        SpeakToClient(ChatCanal.System, "<color=red>you can only unlock Pet, Sword, Shield and Wand. for skin, set xp.</color>", Server.currentClientID);
                        return;
                    }
                }
            }
            catch
            {
                SpeakToClient(ChatCanal.System, "<color=red>wrong syntax : /unlock [playerName] [skinType] [skinID] (or all)</color>", Server.currentClientID);
                return;
            }
        }

        private static void SystemSpeak(string[] args)
        {
            try
            {
                string targetName = args[2];
                int targetID = Server.GetClientIDByName(targetName);
                string text = string.Join(" ", args);
                text = text.Remove(0, args[0].Length + 1 + args[1].Length + 1 + args[2].Length + 1);
                SpeakToClient(ChatCanal.System, text, targetID);

            }
            catch { SpeakToClient(ChatCanal.System, "Fail to execute cmd.", Server.currentClientID); }
        }

        private static void Join(string[] args)
        {
            try
            {
                string targetName = args[2];
                int targetID = Server.GetClientIDByName(targetName);
                string senderName = args[3];
                int senderID = Server.GetClientIDByName(senderName);
                Lobbies_Manager.JoinPlayer(targetID, senderID, true);
            }
            catch { SpeakToClient(ChatCanal.System, "Fail to execute cmd.", Server.currentClientID); }
        }

        private static void AddFriendCmd(string[] args)
        {
            string targetName = args[2];
            var target = Database.getUserByName(targetName);
            if (target == null)
            {
                SpeakToClient(ChatCanal.System, targetName + " does not exist", Server.currentClientID);
                return;
            }
            if (Database.AddFriend(Server.currentClientID, target.ID))
                SpeakToClient(ChatCanal.System, targetName + " is now your friend", Server.currentClientID);
            else
                SpeakToClient(ChatCanal.System, "Impossible to add " + targetName + " as a friend", Server.currentClientID);
        }

        private static void RemoveFriendCmd(string[] args)
        {
            string targetName = args[2];
            var target = Database.getUserByName(targetName);
            if (target == null)
            {
                SpeakToClient(ChatCanal.System, targetName + " does not exist", Server.currentClientID);
                return;
            }
            if (Database.RemoveFriend(Server.currentClientID, target.ID))
                SpeakToClient(ChatCanal.System, targetName + " is not your friend anymore", Server.currentClientID);
            else
                SpeakToClient(ChatCanal.System, "Impossible to remove " + targetName + " from your friends list", Server.currentClientID);
        }
        #endregion

        #region Utils
        public static void SpeakToClient(ChatCanal canal, string text, int clientID)
        {
            Server.SendToClient(new NetworkMessage(HeadActions.toClient_Speak)
                .Set((byte)canal)
                .Set(Server.currentClientID)
                .Set(Server.GetClient(Server.currentClientID).GameClient.Name)
                .Set(text),
                clientID);
        }

        public static void SpeakToClients(ChatCanal canal, string text, List<TcpClient> clients)
        {
            Server.SendToClients(new NetworkMessage(HeadActions.toClient_Speak)
                .Set((byte)canal)
                .Set(Server.currentClientID)
                .Set(Server.GetClient(Server.currentClientID).GameClient.Name)
                .Set(text),
                clients);
        }
        #endregion

        #region Connect / Disconnect
        public static void ClientConnected(int clientID)
        {

        }

        public static void ClientDisconnected(int clientID)
        {

        }
        #endregion
    }
}