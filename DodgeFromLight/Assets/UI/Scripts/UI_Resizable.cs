using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Resizable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool Horizontal = false;
    public bool Vertical = false;
    public int MouseButtonIndex = 0;
    public RectTransform Resizable;
    public List<Graphic> ForceUpdate;
    public float MinH = 64f;
    public float MaxH = 960f;
    public float MinV = 64f;
    public float MaxV = 960f;
    public float SmashThreshold = 0.5f;
    public bool ReleaseOnSmash = true;
    public bool EnableSmash = true;
    public event Action OnResize;
    public event Action OnSmash;
    private bool clicked = false;
    private bool isHovered = false;
    private Vector3 clickMousePosition;
    private Vector3 clickRectSize;
    private Vector3 clickRectPosition;

    private void Update()
    {
        if (Input.GetMouseButtonDown(MouseButtonIndex) && isHovered)
        {
            isHovered = false;
            clicked = true;
            clickMousePosition = Input.mousePosition;
            clickRectPosition = Resizable.anchoredPosition;
            clickRectSize = Resizable.sizeDelta;
            foreach (Graphic graphic in ForceUpdate)
                graphic.enabled = false;
        }
        else if (Input.GetMouseButtonUp(MouseButtonIndex) && clicked)
        {
            clicked = false;
            foreach (Graphic graphic in ForceUpdate)
                graphic.enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (!clicked)
            return;
        if(Horizontal && Vertical) // x & y resize
        {
            float xDelta = Input.mousePosition.x - clickMousePosition.x;
            float yDelta = Input.mousePosition.y - clickMousePosition.y;
            Vector2 size = new Vector2(clickRectSize.x + xDelta, clickRectSize.y + yDelta);

            // Smash H
            if(size.x <= MinH * SmashThreshold)
            {
                OnSmash?.Invoke();
                if(ReleaseOnSmash)
                {
                    clicked = false;
                    isHovered = false;
                    return;
                }
            }
            size.x = Mathf.Clamp(size.x, MinH, MaxH);

            // Smash V
            if (size.y <= MinV * SmashThreshold)
            {
                OnSmash?.Invoke();
                if (ReleaseOnSmash)
                {
                    clicked = false;
                    isHovered = false;
                    return;
                }
            }
            size.y = Mathf.Clamp(size.y, MinV, MaxV);

            //  set size and position
            Resizable.sizeDelta = size;
            Resizable.anchoredPosition = new Vector2(size.x / 2f, size.y / 2f);
            OnResize?.Invoke();
        }
        else if (Horizontal) // x resize
        {
            float xDelta = Input.mousePosition.x - clickMousePosition.x;
            Vector2 size = new Vector2(clickRectSize.x + xDelta, clickRectSize.y);

            // Smash H
            if (size.x <= MinH * SmashThreshold)
            {
                OnSmash?.Invoke();
                if (ReleaseOnSmash)
                {
                    clicked = false;
                    isHovered = false;
                    return;
                }
            }
            size.x = Mathf.Clamp(size.x, MinH, MaxH);

            //  set size and position
            Resizable.sizeDelta = size;
            Resizable.anchoredPosition = new Vector2(size.x / 2f, clickRectPosition.y);
            OnResize?.Invoke();
        }
        else if (Vertical)
        {
            float yDelta = Input.mousePosition.y - clickMousePosition.y;
            Vector2 size = new Vector2(clickRectSize.x, clickRectSize.y + yDelta);

            // Smash V
            if (size.y <= MinV * SmashThreshold)
            {
                OnSmash?.Invoke();
                if (ReleaseOnSmash)
                {
                    clicked = false;
                    isHovered = false;
                    return;
                }
            }
            size.y = Mathf.Clamp(size.y, MinV, MaxV);

            //  set size and position
            Resizable.sizeDelta = size;
            Resizable.anchoredPosition = new Vector2(clickRectPosition.x, size.y / 2f);
            OnResize?.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}