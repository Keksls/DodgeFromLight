using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Loggin : MonoBehaviour
{
    public GameObject ConnectPanel;
    public GameObject RegisterPanel;
    public InputField Connect_Account;
    public InputField Connect_Password;
    public InputField Register_Account;
    public InputField Register_Password;
    public InputField Register_Mail;
    public Action<bool> OnConnect;

    private void Start()
    {
        Connect_Password.onSubmit.AddListener((val) => Click_Connection());
        Register_Mail.onSubmit.AddListener((val) => Click_CreateAccount());
    }

    public void Click_Connection()
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Connecting user...");
        DFLClient.ConnectAccount(Connect_Account.text, Connect_Password.text, (res, success) =>
        {
            if (success)
            {
                //DodgeFromLight.UI_Notifications.Notify("Connected as <color='#d97400'>" + DFLAPI.CurrentUser.Name + "</color>");
                DodgeFromLight.UI_WorkerNotifier.Hide();
            }
            else
            {
                string rep = "";
                res.Get(ref rep);
                DodgeFromLight.UI_Notifications.Notify("Error : <color=#8f1000>" + rep + "</color>");
                DodgeFromLight.UI_WorkerNotifier.Hide();
            }
            OnConnect?.Invoke(success);
        });
    }

    public void PlayWithoutAccount()
    {
        OnConnect?.Invoke(false);
    }

    public void Click_CreateAccount()
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Creating user...");
        DFLClient.CreateAccount(Register_Account.text, Register_Password.text, Register_Mail.text, (res) =>
        {
            bool success = false;
            res.Get(ref success);
            if (success)
            {
                DodgeFromLight.UI_Notifications.Notify("Account created as <color=#d97400>" + Register_Account.text + "</color>");
                DFLClient.ConnectAccount(Register_Account.text, Register_Password.text, (res, success) =>
                {
                    if (success)
                    {
                        Events.Fire_SaveGetted();
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                        OnConnect?.Invoke(success);
                    }
                    else
                    {
                        string msg = "";
                        res.Get(ref msg);
                        DodgeFromLight.UI_Notifications.Notify("Error : <color=#8f1000>" + msg + "</color>");
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                    }
                });
            }
            else
            {
                string msg = "";
                res.Get(ref msg);
                DodgeFromLight.UI_Notifications.Notify("Error : <color=#8f1000>" + msg + "</color>");
                DodgeFromLight.UI_WorkerNotifier.Hide();
            }
        });
    }

    public void ShowConnectionPanel()
    {
        ConnectPanel.SetActive(true);
        if (PlayerPrefs.HasKey("Username"))
        {
            Connect_Account.text = PlayerPrefs.GetString("Username");
            EventSystem.current.SetSelectedGameObject(Connect_Password.gameObject);
        }
        RegisterPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        ConnectPanel.SetActive(false);
        RegisterPanel.SetActive(true);
    }
}