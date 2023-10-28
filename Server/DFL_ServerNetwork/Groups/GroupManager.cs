using DFLEngine;
using DFLEngine.GameEngine;
using DFLNetwork;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace DFLServer
{
    public class GroupManager
    {
        //public Group Group;
        //public Dictionary<int, TcpClient> TCPClients = new Dictionary<int, TcpClient>();

        //public GroupManager(Group _Group)
        //{
        //    Group = _Group;
        //}

        //public void AddFightTeam(FightTeam Team)
        //{
        //    Group.Allies.Add(Team);
        //    TCPClients.Add(Team.ClientID, ServerEngine.TCPClients[Team.ClientID]);
        //}

        //public void RemoveFightTeam(int ClientID)
        //{
        //    foreach (var allie in Group.Allies)
        //        if (allie.ClientID == ClientID)
        //            Group.Allies.Remove(allie);
        //    TCPClients.Remove(ClientID);
        //}

        //public void Broadcast(HeadActions Action, ViewMessage Message)
        //{
        //    Server.SendToClients(Action, Message, TCPClients.Select(c => c.Value).ToList());
        //}

        //public List<Perso[]> GetTypedPersos()
        //{
        //    List<Perso[]> Persos = new List<Perso[]>();
        //    foreach(var team in Group.Allies)
        //        Persos.Add(team.Entities.Select(c => (Perso)c).ToArray());
        //    return Persos;
        //}
    }
}