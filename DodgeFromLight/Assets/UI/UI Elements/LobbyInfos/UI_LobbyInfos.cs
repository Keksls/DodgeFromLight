using UnityEngine;
using TMPro;
using DFLCommonNetwork.GameEngine;

public class UI_LobbyInfos : MonoBehaviour
{
    public static UI_LobbyInfos Instance;
    public TextMeshProUGUI LobbyNameText;
    public TextMeshProUGUI NbPlayersText;
    private int nbPlayers;
    private int maxPlayers;

    private void Awake()
    {
        Instance = this;
        HideInfos();
        Events.EnterHub += Events_EnterHub;
        Events.LeaveHub += Events_LeaveHub;
        Events.EnterLobby += Events_EnterLobby;
        Events.LeaveLobby += Events_LeaveLobby;
    }

    private void Events_LeaveLobby()
    {
        HideInfos();
    }

    private void Events_EnterLobby(LitePlayerSave player, string name, int maxClients, int nbCLients)
    {

        EnterLobby(name, maxClients, nbCLients);
    }

    private void Events_LeaveHub()
    {
        HideInfos();
    }

    private void Events_EnterHub()
    {
        EnterHub();
    }

    private void OnDestroy()
    {
        Events.EnterHub -= Events_EnterHub;
        Events.LeaveHub -= Events_LeaveHub;
        Events.EnterLobby -= Events_EnterLobby;
        Events.LeaveLobby -= Events_LeaveLobby;
    }

    public void EnterLobby(string _name, int _maxPlayers, int _nbPlayers)
    {
        gameObject.SetActive(true);
        LobbyNameText.text = _name;
        nbPlayers = _nbPlayers;
        maxPlayers = _maxPlayers;
        NbPlayersText.text = "Players : " + nbPlayers + " / " + maxPlayers;
    }

    public void ClientEnterLobby()
    {
        nbPlayers++;
        NbPlayersText.text = "Players : " + nbPlayers + " / " + maxPlayers;
    }

    public void ClientLeaveLobby()
    {
        nbPlayers--;
        NbPlayersText.text = "Players : " + nbPlayers + " / " + maxPlayers;
    }

    public void EnterHub()
    {
        gameObject.SetActive(true);
        LobbyNameText.text = DFLClient.CurrentUser.Name + "'s Hub";
        nbPlayers = 1;
        maxPlayers = 8;
        NbPlayersText.text = "Players : " + nbPlayers + " / " + maxPlayers;
    }

    public void HideInfos()
    {
        gameObject.SetActive(false);
    }
}