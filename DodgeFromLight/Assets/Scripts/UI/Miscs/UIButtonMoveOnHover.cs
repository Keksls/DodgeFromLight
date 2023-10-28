using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonMoveOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float XPos;
    public float XPosHover;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector3 pos = GetComponent<RectTransform>().anchoredPosition;
        pos.x = XPosHover;
        GetComponent<RectTransform>().anchoredPosition = pos;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Vector3 pos = GetComponent<RectTransform>().anchoredPosition;
        pos.x = XPos;
        GetComponent<RectTransform>().anchoredPosition = pos;
    }
}