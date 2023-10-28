using DFLCommonNetwork.GameEngine;
using DFLServer;
using System.Collections.Generic;

namespace DFLServerNetwork.DAO.DAOClasses
{
    public class SocialRelation
    {
        public int ID { get; set; }
        public int ClientID { get; set; }
        public List<int> FriendsID { get; set; }

        public SocialRelation()
        {
            FriendsID = new List<int>();
        }

        public List<SocialPlayer> GetFriends()
        {
            List<SocialPlayer> friends = new List<SocialPlayer>();
            foreach (int id in FriendsID)
            {
                SocialPlayer friend = new SocialPlayer();
                GameClient client = Database.getUser(id);
                friend.Player = new LitePlayerSave(client);
                friend.State = DFLServer.Server.Clients.ContainsKey(client.ID) ? client.State : PlayerState.NotConnected;
                friend.Connected = friend.State != PlayerState.NotConnected;
                friends.Add(friend);
            }
            return friends;
        }

        public bool AddFriend(int id)
        {
            if (id == ClientID)
                return false;
            if (FriendsID.Contains(id))
                return false;
            FriendsID.Add(id);
            return true;
        }

        public bool RemoveFriend(int id)
        {
            return FriendsID.Remove(id);
        }
    }
}