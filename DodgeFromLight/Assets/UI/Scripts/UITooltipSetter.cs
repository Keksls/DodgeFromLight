using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipSetter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string Message;
    public Func<string> MessageFunc;
    public bool UsePointerEvent = false;
    public Sprite Image;
    private Rect elementRect;
    private RectTransform elementTransform;
    private bool isMouseOver = false;

    private void Awake()
    {
        elementTransform = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        elementRect = elementTransform.rect;
        if(Image != null)
        {
            UITooltip.Instance.Show(Image);
            return;
        }
        if (MessageFunc != null)
            UITooltip.Instance.Show(MessageFunc);
        else
            UITooltip.Instance.Show(Message);
    }

    void LateUpdate()
    {
        if(isMouseOver)
        {
            if (!UsePointerEvent)
            {
                Vector2 mousePos = Input.mousePosition;
                elementRect = GetScreenCoordinates(elementTransform);
                if (!elementRect.Contains(mousePos))
                {
                    isMouseOver = false;
                    UITooltip.Instance.Hide();
                }
            }
        }
    }
    public Rect GetScreenCoordinates(RectTransform uiElement)
    {
        var worldCorners = new Vector3[4];
        uiElement.GetWorldCorners(worldCorners);
        var result = new Rect(
                      worldCorners[0].x,
                      worldCorners[0].y,
                      worldCorners[2].x - worldCorners[0].x,
                      worldCorners[2].y - worldCorners[0].y);
        return result;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!UsePointerEvent)
            return;
        isMouseOver = false;
        UITooltip.Instance.Hide();
    }
}