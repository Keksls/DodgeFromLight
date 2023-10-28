using System;
using UnityEngine;
using UnityEngine.UI;

public class UIToggle : MonoBehaviour
{
    public Text leftText;
    public Text rightText;
    public Color SelectedTextColor;
    public Color NormalTextColor;
    public ToogleSide Side;
    public Transform btn;
    public event Action<ToogleSide> OnSwitchSide;

    void Start()
    {
        SetSide(Side);
    }

    public void SwitchSide()
    {
        if (Side == ToogleSide.Left)
            Side = ToogleSide.Right;
        else
            Side = ToogleSide.Left;
        SetSide(Side);
    }

    public void SetSide(ToogleSide side)
    {
        Side = side;
        if(Side == ToogleSide.Left)
        {
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16, 0);
            leftText.color = SelectedTextColor;
            rightText.color = NormalTextColor;
            leftText.fontSize = 24;
            rightText.fontSize = 22;
        }
        else
        {
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(16, 0);
            leftText.color = NormalTextColor;
            rightText.color = SelectedTextColor;
            leftText.fontSize = 22;
            rightText.fontSize = 24;
        }
        OnSwitchSide?.Invoke(side);
    }
}

public enum ToogleSide
{
    Left, Right
}