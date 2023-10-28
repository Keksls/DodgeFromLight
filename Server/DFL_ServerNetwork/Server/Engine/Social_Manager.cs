using DFLCommonNetwork.GameEngine;
using DFLNetwork.Protocole;
using System.Collections.Generic;

namespace DFLServer
{
    public static class Social_Manager
    {
        public static void AddFriend()
        {
            int friendID = 0;
            Server.currentMessage.Get(ref friendID);
            try
            {
                Database.AddFriend(Server.currentClientID, friendID);
                Server.Reply(new NetworkMessage().Set(true));
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false));
            }
        }

        public static void RemoveFriend()
        {
            int friendID = 0;
            Server.currentMessage.Get(ref friendID);
            try
            {
                Database.RemoveFriend(Server.currentClientID, friendID);
                Server.Reply(new NetworkMessage().Set(true));
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false));
            }
        }

        public static void GetFriends()
        {
            try
            {
                List<SocialPlayer> friends = Database.GetFriends(Server.currentClientID);
                Server.Reply(new NetworkMessage().Set(true).SetObject(friends));
            }
            catch
            {
                Server.Reply(new NetworkMessage().Set(false));
            }
        }
    }
}