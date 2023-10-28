using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_MenuBar : MonoBehaviour
{
    public List<UI_MenuBarItem> Items;
    private int selectedIndex = -1;

    private void Awake()
    {
        int id = 0;
        foreach (UI_MenuBarItem item in Items)
        {
            item.ItemButton.GetComponent<Button>().onClick.RemoveAllListeners();
            int i = id;
            item.ID = i;
            item.ItemButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                    Select(i);
            });
            id++;
        }
    }

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() || UI_Console.IsTyping)
            return;

        foreach (UI_MenuBarItem item in Items)
            if (Input.GetKeyDown(item.Shortcut))
            {
                if (selectedIndex != item.ID)
                    Select(item.ID);
            }
    }

    public void Select(int index)
    {
        selectedIndex = index;
        Items[index].OnClickEnable.Invoke();
    }
}

[Serializable]
public class UI_MenuBarItem
{
    public GameObject ItemButton;
    public Button.ButtonClickedEvent OnClickEnable;
    public KeyCode Shortcut;
    [HideInInspector]
    public int ID;
}