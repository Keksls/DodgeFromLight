using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMouseHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool UsePointerExit = false;
    private Rect elementRect;
    private RectTransform elementTransform;
    private bool isMouseOver = false;
    private Action onMouseEnter;
    private Action onMouseExit;

    private void Awake()
    {
        elementTransform = GetComponent<RectTransform>();
    }

    public UIMouseHoverDetector OnMouseEnter(Action Callback)
    {
        onMouseEnter = Callback;
        return this;
    }

    public UIMouseHoverDetector OnMouseExit(Action Callback)
    {
        onMouseExit = Callback;
        return this;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        elementRect = elementTransform.rect;
        onMouseEnter?.Invoke();
    }

    public void ForceMouseExit()
    {
        isMouseOver = false;
        onMouseExit?.Invoke();
    }

    void Update()
    {
        if (UsePointerExit)
            return;

        if (isMouseOver)
        {
            Vector2 mousePos = Input.mousePosition;
            elementRect = RectTransformToScreenSpace(elementTransform);
            if (!elementRect.Contains(mousePos))
            {
                isMouseOver = false;
                onMouseExit?.Invoke();
            }
        }
    }

    public static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
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
        if (UsePointerExit)
            ForceMouseExit();
    }
}