using UnityEngine;

public class AutoConnect : MonoBehaviour
{
    public string account = "keks";
    public string pass = "a";
    public bool AutoEnterLobby = true;
    public bool AutoEnterMapCreator = false;
    public string mapID = "";

    private void Start()
    {
        if (DFLClient.OnlineState == OnlineState.NotChecked || DFLClient.LoginState == LoginState.LoggedOut)
        {
            DFLClient.CheckIfOnline((status) =>
            {
                if (status == OnlineState.Online)
                {
                    if (DFLClient.LoginState != LoginState.LoggedIn)
                    {
                        DFLClient.ConnectAccount(account, pass, (res, success) =>
                        {
                            if (success)
                            {
                                if (AutoEnterLobby)
                                    LobbyManager.Instance.
            JoinLobby(-DFLClient.CurrentUser.ID, (success) =>
            {
                if (!success)
                    DodgeFromLight.UI_Notifications.Notify("Fail enter Hub");
            });
                                else if (AutoEnterMapCreator)
                                {

                                }
                            }
                            else
                            {
                                // fail to connect
                            }
                        });
                    }
                    else
                    {
                        // no internet
                    }
                }
                else
                {
                    // already online
                }
            });
        }
    }
}