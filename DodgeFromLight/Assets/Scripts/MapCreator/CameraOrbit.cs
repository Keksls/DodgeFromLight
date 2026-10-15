using UnityEngine;

/// <summary>
/// Owns smooth orbit poses and a focus-normalized perspective-to-orthographic projection transition.
/// </summary>
public sealed class CameraOrbit : MonoBehaviour
{
    public Camera Camera;
    public Vector3 targetPos;
    public float distance = 15f;
    public float mouseSensitivity = 1f;
    public float xSpeed = 180f;
    public float ySpeed = 100f;
    public float yMinLimit = 20f;
    public float yMaxLimit = 80f;
    public float scrollSpeed = 5f;
    public bool forceUpdate;
    public float transitionDuration = 0.48f;

    private MapCreatorController _creator;
    private Vector3 _previousPointer;
    private float _yaw = 35f;
    private float _pitch = 50f;
    private bool _panning;
    private bool _orbiting;
    private Rect _editingViewport;
    private Quaternion _rotation = Quaternion.Euler(90f, 0f, 0f);
    private Quaternion _fromRotation;
    private Quaternion _toRotation;
    private float _projectionBlend = 1f;
    private float _fromBlend;
    private float _toBlend;
    private float _transitionStart = -1f;
    private float _perspectiveFieldOfView = 60f;
    public bool IsPlanView { get; private set; } = true;
    public bool IsTransitioning => _transitionStart >= 0f;
    public static CameraOrbit Instance;

    /// <summary>
    /// Registers the authored camera and remembers its perspective lens.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        if (Camera == null) Camera = GetComponent<Camera>();
        if (Camera != null) _perspectiveFieldOfView = Mathf.Clamp(Camera.fieldOfView, 20f, 90f);
    }

    /// <summary>
    /// Resolves the board input owner and applies the initial orthographic work view.
    /// </summary>
    private void Start()
    {
        _creator = FindFirstObjectByType<MapCreatorController>();
        ApplyPose();
    }

    /// <summary>
    /// Advances camera animation independently from pointer capture and applies deliberate navigation gestures.
    /// </summary>
    private void LateUpdate()
    {
        if (IsTransitioning)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _transitionStart) / Mathf.Max(0.05f, transitionDuration));
            float eased = progress * progress * (3f - 2f * progress);
            _rotation = Quaternion.Slerp(_fromRotation, _toRotation, eased);
            _projectionBlend = Mathf.Lerp(_fromBlend, _toBlend, eased);
            if (progress >= 1f) _transitionStart = -1f;
            ApplyPose();
        }
        if (!Input.GetMouseButton(2) && !Input.GetMouseButton(1))
        {
            _panning = false;
            _orbiting = false;
        }
        bool blocked = _creator == null || _creator.IsPointerOverUi || _creator.IsBusy || IsTransitioning;
        if (!blocked)
        {
            if (Input.GetMouseButtonDown(2) || (IsPlanView && Input.GetMouseButtonDown(1)))
            {
                _panning = true;
                _previousPointer = Input.mousePosition;
            }
            if (!IsPlanView && Input.GetMouseButtonDown(1)) _orbiting = true;
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) > 0.001f)
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(-wheel * 1.8f), 3f, 180f);
                forceUpdate = true;
            }
        }
        if (_panning && !blocked)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            Ray oldRay = Camera.ScreenPointToRay(_previousPointer);
            Ray newRay = Camera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(oldRay, out float oldDistance) && plane.Raycast(newRay, out float newDistance))
                targetPos += oldRay.GetPoint(oldDistance) - newRay.GetPoint(newDistance);
            _previousPointer = Input.mousePosition;
            forceUpdate = true;
        }
        if (_orbiting && !blocked)
        {
            _yaw += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * ySpeed * 0.02f, yMinLimit, yMaxLimit);
            _rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            forceUpdate = true;
        }
        if (forceUpdate)
        {
            ApplyPose();
            forceUpdate = false;
        }
    }

    /// <summary>
    /// Animates the existing keyboard projection shortcut through the same navigation workflow as the gizmo.
    /// </summary>
    public void SetPlanView(bool plan)
    {
        SetView(plan ? "Top" : "Perspective");
    }

    /// <summary>
    /// Selects an understandable top or cardinal observer pose while preserving zoom and the orbit target.
    /// </summary>
    public void SetView(string view)
    {
        if (view != "Top" && view != "Perspective" && view != "North" && view != "East" && view != "South" && view != "West")
            return;
        if (view == "East") _yaw = -90f;
        if (view == "West") _yaw = 90f;
        if (view == "North") _yaw = 180f;
        if (view == "South") _yaw = 0f;
        if (view != "Top" && view != "Perspective") _pitch = 45f;
        IsPlanView = view == "Top";
        _fromRotation = _rotation;
        _toRotation = IsPlanView ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(_pitch, _yaw, 0f);
        _fromBlend = _projectionBlend;
        _toBlend = IsPlanView ? 1f : 0f;
        if (Quaternion.Angle(_fromRotation, _toRotation) < 0.01f && Mathf.Abs(_fromBlend - _toBlend) < 0.0001f)
        {
            _transitionStart = -1f;
            return;
        }
        _transitionStart = Time.unscaledTime;
        _panning = false;
        _orbiting = false;
    }

    /// <summary>
    /// Fits the complete board inside the unobstructed editor area in either projection.
    /// </summary>
    public void FrameBoard(int width, int height)
    {
        targetPos = new Vector3((width - 1) * 0.5f, 0f, (height - 1) * 0.5f);
        float viewportHeight = _editingViewport.height > 0f ? _editingViewport.height : Screen.height * 0.72f;
        float viewportWidth = _editingViewport.width > 0f ? _editingViewport.width : Screen.width * 0.60f;
        distance = Mathf.Max(5f, Mathf.Max(height * Screen.height / viewportHeight, width * Screen.height / viewportWidth) * 1.16f);
        ApplyPose();
    }

    /// <summary>
    /// Keeps the logical target centered beside the panels without cropping the overlay camera stack.
    /// </summary>
    public void SetEditingViewport(Rect viewport)
    {
        if (_editingViewport == viewport) return;
        bool firstLayout = _editingViewport.width <= 0f;
        _editingViewport = viewport;
        if (firstLayout && _creator != null && _creator.IsReady) _creator.FocusSelection();
        else ApplyPose();
    }

    /// <summary>
    /// Applies the animated pose and morphs homogeneous projection matrices normalized at the focus plane.
    /// </summary>
    private void ApplyPose()
    {
        if (Camera == null || Screen.height <= 0) return;
        float halfHeight = distance * 0.5f;
        float focusDistance = Mathf.Max(12f, halfHeight / Mathf.Tan(_perspectiveFieldOfView * Mathf.Deg2Rad * 0.5f));
        float fieldOfView = 2f * Mathf.Atan(halfHeight / focusDistance) * Mathf.Rad2Deg;
        Vector3 offset = Vector3.zero;
        if (_editingViewport.width > 0f)
        {
            Vector2 desiredCenter = new Vector2(_editingViewport.center.x, Screen.height - _editingViewport.center.y);
            Vector2 shift = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) - desiredCenter;
            offset = _rotation * new Vector3(shift.x * distance / Screen.height, shift.y * distance / Screen.height, 0f);
        }
        transform.SetPositionAndRotation(targetPos + offset + _rotation * new Vector3(0f, 0f, -focusDistance), _rotation);
        Camera.orthographic = _projectionBlend >= 0.99999f;
        Camera.orthographicSize = halfHeight;
        Camera.fieldOfView = fieldOfView;
        Matrix4x4 perspective = Matrix4x4.Perspective(fieldOfView, Camera.aspect, Camera.nearClipPlane, Camera.farClipPlane);
        Matrix4x4 orthographic = Matrix4x4.Ortho(-halfHeight * Camera.aspect, halfHeight * Camera.aspect, -halfHeight, halfHeight, Camera.nearClipPlane, Camera.farClipPlane);
        var projection = new Matrix4x4();
        // Both matrices have w=1 at the target distance, avoiding a scale pop halfway through the morph.
        for (int i = 0; i < 16; i++)
            projection[i] = Mathf.Lerp(perspective[i] / focusDistance, orthographic[i], _projectionBlend);
        Camera.projectionMatrix = projection;
    }

    /// <summary>
    /// Releases the custom projection and static scene reference when the rig leaves the editor.
    /// </summary>
    private void OnDestroy()
    {
        if (Camera != null) Camera.ResetProjectionMatrix();
        if (Instance == this) Instance = null;
    }
}
