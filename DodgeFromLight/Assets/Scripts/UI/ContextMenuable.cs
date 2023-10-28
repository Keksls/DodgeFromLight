using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ContextMenuable : MonoBehaviour, IPointerClickHandler
{
    public ContextMenuItems Items = new ContextMenuItems();

    public void ClearItems()
    {
        Items.Items.Clear();
    }

    public void AddItem(string text, Action callback)
    {
        Items.Add(new ContextMenuItem(text, callback));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Right)
        {
            ContextMenu.Instance.ClearItems();
            ContextMenu.Instance.Bind(this);
            ContextMenu.Instance.Show();
        }
    }
}
