using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMouseHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool UsePointerExit = false;
    private RectTransform elementTransform;
    private bool isMouseOver;
    private Action onMouseEnter;
    private Action onMouseExit;

    /// <summary>
    /// Caches the rectangle transform used to evaluate pointer containment.
    /// </summary>
    private void Awake()
    {
        // Resolve the component once because the check can run every frame.
        elementTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Registers the callback invoked when the pointer enters this UI element.
    /// </summary>
    public UIMouseHoverDetector RegisterMouseEnter(Action callback)
    {
        // Keep a single callback so rebinding replaces the previous owner cleanly.
        onMouseEnter = callback;
        return this;
    }

    /// <summary>
    /// Registers the callback invoked when the pointer exits this UI element.
    /// </summary>
    public UIMouseHoverDetector RegisterMouseExit(Action callback)
    {
        // Keep a single callback so rebinding replaces the previous owner cleanly.
        onMouseExit = callback;
        return this;
    }

    /// <summary>
    /// Records pointer entry and invokes the registered callback.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Track entry for the manual containment fallback.
        isMouseOver = true;
        onMouseEnter?.Invoke();
    }

    /// <summary>
    /// Clears the hover state and invokes the registered exit callback.
    /// </summary>
    public void ForceMouseExit()
    {
        // Centralize exit behavior for both event-driven and manual detection.
        isMouseOver = false;
        onMouseExit?.Invoke();
    }

    /// <summary>
    /// Detects pointer exit manually when event-driven exit handling is disabled.
    /// </summary>
    private void Update()
    {
        // Some nested canvases miss pointer-exit events, so screen containment is the fallback.
        if (UsePointerExit)
            return;

        if (isMouseOver)
        {
            Vector2 mousePos = Input.mousePosition;
            Rect elementRect = RectTransformToScreenSpace(elementTransform);
            if (!elementRect.Contains(mousePos))
            {
                isMouseOver = false;
                onMouseExit?.Invoke();
            }
        }
    }

    /// <summary>
    /// Converts a rectangle transform into its screen-space bounds.
    /// </summary>
    public static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        // Apply lossy scale so nested canvas scaling is represented in screen coordinates.
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }

    /// <summary>
    /// Handles Unity pointer-exit events when explicit event-driven tracking is enabled.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Ignore the event when the manual containment fallback owns exit detection.
        if (UsePointerExit)
            ForceMouseExit();
    }
}
