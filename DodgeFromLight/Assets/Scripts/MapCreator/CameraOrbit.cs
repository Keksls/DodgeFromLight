using System;
using UnityEngine;

/// <summary>
/// Provides orbit, pan, focus, and zoom controls for the map-creator camera.
/// </summary>
public sealed class CameraOrbit : MonoBehaviour
{
    public Camera Camera;
    public Vector3 targetPos;
    public float distance = 10f;
    public float mouseSensitivity = 1f;
    public float xSpeed = 250f;
    public float ySpeed = 120f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    public float scrollSpeed = 5f;
    public bool forceUpdate;

    private Vector3 _lastPosition;
    private MapCreatorController _mapCreator;
    private float _x;
    private float _y;
    private float _previousDistance;
    private bool _hasMoved;

    public static CameraOrbit Instance;
    public static Action PostRenderCallback;

    /// <summary>
    /// Registers the active map-creator camera instance.
    /// </summary>
    private void Awake()
    {
        // The creator scene contains a single authoring camera.
        Instance = this;
    }

    /// <summary>
    /// Captures the authored camera orientation and resolves the Fugui input guard.
    /// </summary>
    private void Start()
    {
        // Initial Euler values preserve the scene-authored opening angle.
        Vector3 angles = transform.eulerAngles;
        _x = angles.y;
        _y = angles.x;
        _mapCreator =
            FindFirstObjectByType<MapCreatorController>();
    }

    /// <summary>
    /// Updates map-creator camera gestures after board and UI input state is known.
    /// </summary>
    private void LateUpdate()
    {
        // Fugui panels and modals reserve pointer gestures while forced framing remains available.
        if (IsPointerBlocked() && !forceUpdate)
            return;

        HandlePan();
        HandleZoom();
        HandleOrbit();
        HandlePanRelease();
    }

    /// <summary>
    /// Pans the target and camera while the middle mouse button is dragged.
    /// </summary>
    private void HandlePan()
    {
        // Panning speed scales with distance so large maps remain navigable.
        if (Input.GetMouseButtonDown(2))
        {
            _lastPosition = Input.mousePosition;
            _hasMoved = false;
        }
        if (!Input.GetMouseButton(2) ||
            GameContext.CurrentMap == null)
        {
            return;
        }

        if (Vector3.Distance(
                _lastPosition,
                Input.mousePosition) <= 0.25f)
        {
            return;
        }

        _hasMoved = true;
        Vector3 delta =
            Input.mousePosition - _lastPosition;
        Vector3 movement =
            delta *
            (mouseSensitivity * distance * 0.1f);
        transform.Translate(
            movement.x,
            movement.y,
            0f);
        targetPos += movement;
        _lastPosition = Input.mousePosition;
    }

    /// <summary>
    /// Changes camera distance from the wheel and keypad zoom shortcuts.
    /// </summary>
    private void HandleZoom()
    {
        // Exponential distance scaling gives precision nearby and speed on large maps.
        distance = Mathf.Max(2f, distance);
        float scroll =
            Input.GetAxis("Mouse ScrollWheel");
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
            scroll += 0.1f;
        else if (Input.GetKeyDown(KeyCode.KeypadMinus))
            scroll -= 0.1f;

        distance -=
            scroll *
            scrollSpeed *
            Mathf.Pow(
                Vector3.Distance(
                    transform.position,
                    targetPos),
                0.9f);
        distance = Mathf.Max(2f, distance);
        if (Math.Abs(_previousDistance - distance) <= 0.001f)
            return;

        _previousDistance = distance;
        ApplyCameraTransform();
    }

    /// <summary>
    /// Orbits around the target while the right mouse button is held.
    /// </summary>
    private void HandleOrbit()
    {
        // Forced updates reuse the same transform path without needing synthetic input.
        if (!Input.GetMouseButton(1) && !forceUpdate)
            return;

        _x +=
            Input.GetAxis("Mouse X") *
            xSpeed *
            0.02f;
        _y -=
            Input.GetAxis("Mouse Y") *
            ySpeed *
            0.02f;
        _y =
            ClampAngle(
                _y,
                yMinLimit,
                yMaxLimit);
        ApplyCameraTransform();
        forceUpdate = false;
    }

    /// <summary>
    /// Refocuses the camera target when a middle-button pan gesture ends.
    /// </summary>
    private void HandlePanRelease()
    {
        // A middle click targets the pointed surface; a completed drag targets the view center.
        if (!Input.GetMouseButtonUp(2))
            return;

        RaycastHit hit;
        if (!_hasMoved)
        {
            if (Camera == null ||
                !Physics.Raycast(
                    Camera.ScreenPointToRay(Input.mousePosition),
                    out hit))
            {
                return;
            }
            targetPos = hit.point;
        }
        else if (Physics.Raycast(
                     transform.position,
                     transform.forward,
                     out hit))
        {
            targetPos = hit.point;
            distance =
                Vector3.Distance(
                    transform.position,
                    targetPos);
        }
        else
        {
            return;
        }

        ApplyCameraTransform();
    }

    /// <summary>
    /// Applies the current orbit angles and distance around the target.
    /// </summary>
    private void ApplyCameraTransform()
    {
        // One transform function prevents orbit, zoom, and refocus from drifting apart.
        Quaternion rotation =
            Quaternion.Euler(_y, _x, 0f);
        Vector3 position =
            rotation *
            new Vector3(0f, 0f, -distance) +
            targetPos;
        transform.rotation = rotation;
        transform.position = position;
    }

    /// <summary>
    /// Reports whether a visible Fugui editor surface currently owns the pointer.
    /// </summary>
    private bool IsPointerBlocked()
    {
        // The controller receives precise responsive rectangles from the foreground presenter.
        return _mapCreator != null &&
               _mapCreator.IsPointerOverUi;
    }

    /// <summary>
    /// Normalizes and clamps one camera orbit angle.
    /// </summary>
    private static float ClampAngle(
        float angle,
        float minimum,
        float maximum)
    {
        // Normalization avoids precision drift across repeated full rotations.
        if (angle < -360f)
            angle += 360f;
        if (angle > 360f)
            angle -= 360f;
        return Mathf.Clamp(angle, minimum, maximum);
    }

    /// <summary>
    /// Invokes map path rendering after the camera completes its frame.
    /// </summary>
    private void OnPostRender()
    {
        // Runtime entities draw their existing path visualization in camera space.
        PostRenderCallback?.Invoke();
    }
}
