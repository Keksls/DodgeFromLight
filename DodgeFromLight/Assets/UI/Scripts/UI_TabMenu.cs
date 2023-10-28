using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UI_TabMenu : MonoBehaviour
{
    public TextMeshProUGUI Title;
    Button[] Headers;
    GameObject[] Bodies;
    public Transform HeaderContainer;
    public Transform BodyContainer;
    public Color DisableColor;
    public Color NormalColor;
    public Color DisableTextColor;
    public Color NormalTextColor;
    public float SelectedHeight = 46f;
    public float UnselectedHeight = 42f;
    public int DefaultSelectedTabIndex = 0;
    private float height;
    public event Action<int> OnSelectTab;

    private void Awake()
    {
        Headers = new Button[HeaderContainer.childCount];
        Bodies = new GameObject[BodyContainer.childCount];
        for (int i = 0; i < Headers.Length; i++)
        {
            Headers[i] = HeaderContainer.GetChild(i).GetComponentInChildren<Button>();
            int index = i;
            Headers[i].onClick.AddListener(() =>
            {
                SelectTab(index);
            });
        }
        height = Headers[1].GetComponent<RectTransform>().sizeDelta.y;
        for (int i = 0; i < Bodies.Length; i++)
            Bodies[i] = BodyContainer.GetChild(i).gameObject;

        SelectTab(DefaultSelectedTabIndex);
    }

    public void SelectTab(int index)
    {
        for (int i = 0; i < Bodies.Length; i++)
            Bodies[i].SetActive(false);
        Bodies[index].SetActive(true);

        for (int i = 0; i < Headers.Length; i++)
        {
            ColorBlock cb = Headers[i].colors;
            cb.normalColor = DisableColor;
            Headers[i].colors = cb;
            Headers[i].GetComponentInChildren<TextMeshProUGUI>().color = DisableTextColor;
            Headers[i].GetComponent<RectTransform>().sizeDelta = new Vector2(Headers[i].GetComponent<RectTransform>().sizeDelta.x, height);
        }
        ColorBlock cbNormal = Headers[index].colors;
        cbNormal.normalColor = NormalColor;
        Headers[index].colors = cbNormal;
        Headers[index].GetComponentInChildren<TextMeshProUGUI>().color = NormalTextColor;
        Headers[index].GetComponent<RectTransform>().sizeDelta = new Vector2(Headers[index].GetComponent<RectTransform>().sizeDelta.x, height + 8);
        OnSelectTab?.Invoke(index);

        if (Title != null)
            Title.text = Headers[index].gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
    }
}