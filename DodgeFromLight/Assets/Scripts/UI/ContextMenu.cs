using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenu : MonoBehaviour
{
    public static ContextMenu Instance;
    public GameObject ItemPrefab;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
            Hide();
        if (Input.GetMouseButtonDown(1))
            Hide();
        if (Input.GetMouseButtonDown(2))
            Hide();
        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    public void ClearItems()
    {
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Vector2 pos = Input.mousePosition;
        pos.x += GetComponent<RectTransform>().sizeDelta.x / 2f;
        pos.y -= GetComponent<RectTransform>().sizeDelta.y / 2f;
        float maxX = pos.x + GetComponent<RectTransform>().sizeDelta.x;
        if (maxX > Screen.width)
            pos.x -= GetComponent<RectTransform>().sizeDelta.x;
        transform.position = pos;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Bind(ContextMenuable menu)
    {
        foreach (var item in menu.Items.Items)
        {
            AddItem(item.Text, item.Callback);
        }
    }

    public void AddItem(string text, Action callback)
    {
        GameObject item = Instantiate(ItemPrefab);
        item.transform.SetParent(transform, false);
        item.GetComponentInChildren<Text>().text = text;
        item.GetComponentInChildren<Button>().onClick.AddListener(() => { callback?.Invoke(); Hide(); });
    }
}

public struct ContextMenuItem
{
    public string Text;
    public Action Callback;

    public ContextMenuItem(string text, Action callback)
    {
        Text = text;
        Callback = callback;
    }
}

public class ContextMenuItems
{
    public List<ContextMenuItem> Items;

    public ContextMenuItems()
    {
        Items = new List<ContextMenuItem>();
    }

    public void Add(ContextMenuItem item)
    {
        Items.Add(item);
    }
}