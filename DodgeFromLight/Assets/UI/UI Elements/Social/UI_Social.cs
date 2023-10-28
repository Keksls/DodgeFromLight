using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Social : MonoBehaviour
{
    public GameObject FriendLinePrefab;
    public Transform FriendsContainer;
    public TMP_InputField SearchField;
    public Color AvailableColor;
    public Color BusyColor;
    public Color NotConnectedColor;

    public void SearchFriend()
    {

    }

    public void BindFriends()
    {
        foreach (Transform t in FriendsContainer)
            Destroy(t.gameObject);

        DFLClient.GetFriends((success, friends) =>
        {
            if (!success)
            {
                DodgeFromLight.UI_Notifications.Notify("Fail getting friends list");
                return;
            }

            foreach (var friend in friends)
            {
                GameObject line = Instantiate(FriendLinePrefab);
                line.transform.SetParent(FriendsContainer, false);

                // name
                line.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = friend.Player.Name;
                // state
                line.transform.GetChild(1).GetComponent<Image>().color = GetColorOverState(friend.State);
                line.transform.GetChild(1).GetComponent<UITooltipSetter>().Message = GetTextOverState(friend.State);
                // btn Join
                Button btnJoin = line.transform.GetChild(2).GetComponent<Button>();
                btnJoin.interactable = friend.State == PlayerState.InLobby || friend.State == PlayerState.InHub;
                int id = friend.Player.ID;
                btnJoin.onClick.AddListener(() =>
                {
                    DFLClient.TryJoinPlayer(id, DFLClient.CurrentUser.ID, false);
                });
                // btn Visit Hub
                Button btnVisitHub = line.transform.GetChild(3).GetComponent<Button>();
                btnVisitHub.onClick.AddListener(() =>
                {
                    LobbyManager.Instance.VisitHub(-id, friend.Player.Name);
                });
            }
        });
    }

    private Color GetColorOverState(PlayerState state)
    {
        switch (state)
        {
            default:
            case PlayerState.NotConnected:
                return NotConnectedColor;
            case PlayerState.InHub:
            case PlayerState.InLobby:
                return AvailableColor;
            case PlayerState.InMapCreator:
            case PlayerState.InGame:
            case PlayerState.Playing:
                return BusyColor;
        }
    }

    private string GetTextOverState(PlayerState state)
    {
        switch (state)
        {
            default:
            case PlayerState.NotConnected:
                return "Player not connected";
            case PlayerState.InHub:
                return "Player in his Hub";
            case PlayerState.InLobby:
                return "Player in Lobby";
            case PlayerState.InMapCreator:
                return "Player in Map Creator";
            case PlayerState.InGame:
                return "Player connected AFK";
            case PlayerState.Playing:
                return "Player in a middle of something";
        }
    }
}