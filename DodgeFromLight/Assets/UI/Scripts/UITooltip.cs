using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour
{
    public static UITooltip Instance { get; private set; }
    public RectTransform Back;
    public RectTransform Canvas;
    public TextMeshProUGUI Text;
    public Image Image;
    private bool showTooltip = false;
    private bool isImage = false;
    private Func<string> textFunc;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(string text)
    {
        Show(() => text);
    }

    public void Show(Func<string> text)
    {
        isImage = false;
        if (Image != null)
            Image.gameObject.SetActive(false);
        Back.gameObject.SetActive(true);
        Text.gameObject.SetActive(true);
        textFunc = text;
        showTooltip = true;
    }

    public void Show(Sprite image)
    {
        isImage = true;
        if (Image != null)
            Image.gameObject.SetActive(true);
        Back.gameObject.SetActive(true);
        Text.gameObject.SetActive(false);
        Image.sprite = image;
        showTooltip = true;
    }

    public void Hide()
    {
        Back.gameObject.SetActive(false);
        showTooltip = false;
    }

    private void Update()
    {
        if (!showTooltip) return;

        Vector2 size = Vector2.one;
        if (isImage)
        {
            size = Image.gameObject.GetComponent<RectTransform>().sizeDelta;
        }
        else
        {
            string text = textFunc();
            Text.text = text;
            size = Text.GetRenderedValues();
        }
        Vector2 padding = new Vector2(32f, 16f);
        Back.sizeDelta = size + padding;

        Vector2 pos = (Input.mousePosition / Canvas.localScale.x) + ((Vector3)Back.sizeDelta / 2f) + new Vector3(16f, 16f, 16f);

        if (pos.x + Back.rect.width > (Canvas.rect.width + (Back.sizeDelta / 2f).x))
            pos.x = Canvas.rect.width - Back.rect.width + (Back.sizeDelta / 2f).x;
        if (pos.x < (Back.sizeDelta / 2f).x)
            pos.x = (Back.sizeDelta / 2f).x;
        if (pos.y + Back.rect.height > Canvas.rect.height + (Back.sizeDelta / 2f).y)
            pos.y = Canvas.rect.height - Back.rect.height + (Back.sizeDelta / 2f).y;
        if (pos.y < (Back.sizeDelta / 2f).y)
            pos.y = (Back.sizeDelta / 2f).y;

        Back.anchoredPosition = pos;
    }
}