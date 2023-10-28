using System;
using System.Collections.Generic;

namespace DFLCommonNetwork.GameEngine
{
    public class Lobby
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<int, CellPos> Clients { get; set; }
        public int MaxClients { get; set; }
        public bool IsHub { get; set; }
        public int HubOwner { get; set; }

        public Lobby()
        {
            Clients = new Dictionary<int, CellPos>();
        }
    }

    [Serializable]
    public class ViewLobby
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int NbClients { get; set; }
        public int MaxClients { get; set; }
        public bool IsHub { get; set; }
        public int HubOwner { get; set; }

        public ViewLobby(Lobby lobby)
        {
            ID = lobby.ID;
            Name = lobby.Name;
            Description = lobby.Description;
            NbClients = lobby.Clients.Count;
            MaxClients = lobby.MaxClients;
            IsHub = lobby.IsHub;
            HubOwner = lobby.HubOwner;
        }

        public ViewLobby() { }
    }
}