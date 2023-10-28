using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class FlexibleDraggableObject : MonoBehaviour
{
    public RectTransform Canvas;
    public GameObject Target;
    private RectTransform Back;
    private EventTrigger _eventTrigger;

    void Start ()
    {
        _eventTrigger = GetComponent<EventTrigger>();
        _eventTrigger.AddEventTrigger(OnDrag, EventTriggerType.Drag);
        Back = Target.GetComponent<RectTransform>();
    }

    void OnDrag(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData) data;
        Target.transform.Translate(ped.delta);

        Vector2 pos = Back.anchoredPosition;
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