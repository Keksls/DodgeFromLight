using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RotateAndScale : MonoBehaviour
{
    public float AxisDeterminationThreshold = 32f;
    public float RotationSpeed = 0.5f;
    public float ScaleSpeed = 0.008f;
    public float MinScale = 0.5f;
    public float MaxScale = 2f;
    public bool EnableScale = true;
    public bool EnableRotation = true;
    public float currentRotation = 0f;
    public float currentScale = 1f;
    public Action<float> OnRotate;
    public Action<float> OnScale;
    private Vector2 lastMousePosition;
    private bool dragging = false;
    private RotateAndScaleType type = RotateAndScaleType.None;

    public void SetDefaultValues(float rotation, float scale)
    {
        currentRotation = rotation;
        currentScale = scale;
    }

    public void SetConstraintes(bool enableRotation, bool enableScale, float minScale = 0.5f, float maxScale = 3f)
    {
        EnableRotation = enableRotation;
        EnableScale = enableScale;
        MinScale = minScale;
        MaxScale = maxScale;
    }

    public void StartDrag()
    {
        dragging = true;
        type = RotateAndScaleType.None;
        lastMousePosition = Input.mousePosition;
        DodgeFromLight.CursorManager.SetCursor(CursorType.RotationAndScale);
        DodgeFromLight.CursorManager.Lock();

    }

    private void OnMouseEnter()
    {
        DodgeFromLight.CursorManager.SetCursor(CursorType.RotationAndScale);
    }

    private void OnMouseExit()
    {
        if (dragging) return;
        DodgeFromLight.CursorManager.UnLock();
        DodgeFromLight.CursorManager.SetCursor(CursorType.Arrow);
    }

    private void OnMouseDown()
    {
        StartDrag();
    }

    private void OnMouseUp()
    {
        dragging = false;
        type = RotateAndScaleType.None;
        DodgeFromLight.CursorManager.UnLock();
        DodgeFromLight.CursorManager.SetCursor(CursorType.Arrow);
    }

    private void Update()
    {
        if (!dragging)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
            type = RotateAndScaleType.None;
            DodgeFromLight.CursorManager.UnLock();
            DodgeFromLight.CursorManager.SetCursor(CursorType.Arrow);
            return;
        }

        Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
        switch (type)
        {
            default:
            case RotateAndScaleType.None:
                if (Mathf.Abs(delta.x) >= AxisDeterminationThreshold)
                {
                    type = RotateAndScaleType.Rotate;
                    lastMousePosition = Input.mousePosition;
                    DodgeFromLight.CursorManager.UnLock();
                    DodgeFromLight.CursorManager.SetCursor(CursorType.Rotation);
                    DodgeFromLight.CursorManager.Lock();
                }
                else if (Mathf.Abs(delta.y) >= AxisDeterminationThreshold && EnableScale)
                {
                    type = RotateAndScaleType.Scale;
                    lastMousePosition = Input.mousePosition;
                    DodgeFromLight.CursorManager.UnLock();
                    DodgeFromLight.CursorManager.SetCursor(CursorType.Scale);
                    DodgeFromLight.CursorManager.Lock();
                }
                break;

            case RotateAndScaleType.Rotate:
                currentRotation += delta.x * -RotationSpeed;
                currentRotation %= 360f;
                if (!EnableRotation)
                    OnRotate?.Invoke(Mathf.Ceil(currentRotation / 90f) * 90f);
                else
                    OnRotate?.Invoke(currentRotation);
                lastMousePosition = Input.mousePosition;
                break;

            case RotateAndScaleType.Scale:
                float lastScale = currentScale;
                currentScale += delta.y * ScaleSpeed;
                currentScale = Mathf.Clamp(currentScale, MinScale, MaxScale);
                if (currentScale != lastScale)
                    OnScale?.Invoke(currentScale);
                lastMousePosition = Input.mousePosition;
                break;
        }
    }
}

public enum RotateAndScaleType
{
    None = 0,
    Rotate = 1,
    Scale = 2
}