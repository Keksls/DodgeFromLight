using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Modal : MonoBehaviour
{
    public GameObject Modal;
    public TextMeshProUGUI Title;
    public TextMeshProUGUI Text;
    public Button BtnLeft;
    public Button BtnRight;
    private Action CallbackRight;
    private Action CallbackLeft;

    private void Awake()
    {
        DodgeFromLight.UI_Modal = this;
        Hide();
    }

    public void ClickBtnRight()
    {
        CallbackRight?.Invoke();
        Hide();
    }

    public void ClickBtnLeft()
    {
        CallbackLeft?.Invoke();
        Hide();
    }

    public UI_Modal SetTitle(string title)
    {
        Title.text = title;
        return this;
    }

    public UI_Modal SetButtonLeft(string text, Action Callback)
    {
        BtnLeft.gameObject.SetActive(true);
        BtnLeft.gameObject.GetComponentInChildren<TextMeshProUGUI>(true).text = text;
        CallbackLeft = Callback;
        return this;
    }

    public UI_Modal SetButtonRight(string text, Action Callback)
    {
        BtnRight.gameObject.GetComponentInChildren<TextMeshProUGUI>(true).text = text;
        CallbackRight = Callback;
        return this;
    }

    public UI_Modal HideButtonLeft()
    {
        BtnLeft.gameObject.SetActive(false);
        return this;
    }

    public void Show(string text)
    {
        Modal.SetActive(true);
        Text.text = text;
    }

    public void Hide()
    {
        SetButtonRight("OK", Hide);
        CallbackLeft = null;
        BtnLeft.gameObject.SetActive(false);
        Modal.SetActive(false);
    }
}