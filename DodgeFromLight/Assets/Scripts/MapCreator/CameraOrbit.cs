using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraOrbit : MonoBehaviour
{
    public Camera Camera;
    public Vector3 targetPos;
    public float distance = 10.0f;
    public float mouseSensitivity = 1.0f;
    private Vector3 lastPosition;
    public static CameraOrbit Instance;

    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20;
    public float yMaxLimit = 80;

    public float scrollSpeed = 5f;

    float x = 0.0f;
    float y = 0.0f;
    public bool forceUpdate = false;
    bool hasMoved = false;

    public static Action PostRenderCallback;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    float prevDistance;

    void LateUpdate()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && !forceUpdate)
            return;

        if (Input.GetMouseButtonDown(2))
        {
            lastPosition = Input.mousePosition;
            hasMoved = false;
        }

        if (Input.GetMouseButton(2) && DodgeFromLight.CurrentMap != null)
        {
            if(Vector3.Distance(lastPosition, Input.mousePosition) > 0.25f)
            {
                hasMoved = true;
                Vector3 delta = Input.mousePosition - lastPosition;
                transform.Translate(delta.x * mouseSensitivity * distance * 0.1f, delta.y * mouseSensitivity * distance * 0.1f, 0);
                lastPosition = Input.mousePosition;
                targetPos += (delta * mouseSensitivity * distance * 0.1f);
            }
        }

        if (distance < 2) distance = 2;
        float scrool = Input.GetAxis("Mouse ScrollWheel");
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
            scrool += .1f;
        else if (Input.GetKeyDown(KeyCode.KeypadMinus))
            scrool -= .1f;
        distance -= scrool * scrollSpeed * Mathf.Pow(Vector3.Distance(transform.position, targetPos), 0.9f);
        if (Input.GetMouseButton(1) || forceUpdate)
        {
            var pos = Input.mousePosition;
            var dpiScale = 1f;
            if (Screen.dpi < 1) dpiScale = 1;
            if (Screen.dpi < 200) dpiScale = 1;
            else dpiScale = Screen.dpi / 200f;

            if (pos.x < 380 * dpiScale && Screen.height - pos.y < 250 * dpiScale) return;

            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);
            var rotation = Quaternion.Euler(y, x, 0);
            var position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPos;
            transform.rotation = rotation;
            transform.position = position;
            forceUpdate = false;
        }

        if (Math.Abs(prevDistance - distance) > 0.001f)
        {
            prevDistance = distance;
            var rot = Quaternion.Euler(y, x, 0);
            var po = rot * new Vector3(0.0f, 0.0f, -distance) + targetPos;
            transform.rotation = rot;
            transform.position = po;
        }

        if(Input.GetMouseButtonUp(2))
        {
            if (!hasMoved)
            {
                RaycastHit hit;
                Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    targetPos = hit.point;
                    var pos = Input.mousePosition;
                    var dpiScale = 1f;
                    if (Screen.dpi < 1) dpiScale = 1;
                    if (Screen.dpi < 200) dpiScale = 1;
                    else dpiScale = Screen.dpi / 200f;

                    if (pos.x < 380 * dpiScale && Screen.height - pos.y < 250 * dpiScale) return;

                    x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                    y = ClampAngle(y, yMinLimit, yMaxLimit);
                    var rotation = Quaternion.Euler(y, x, 0);
                    var position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPos;
                    transform.rotation = rotation;
                    transform.position = position;
                }
            }
            else
            {
                RaycastHit hit;

                if (Physics.Raycast(transform.position, transform.forward, out hit))
                {
                    targetPos = hit.point;
                    var pos = Input.mousePosition;
                    var dpiScale = 1f;
                    if (Screen.dpi < 1) dpiScale = 1;
                    if (Screen.dpi < 200) dpiScale = 1;
                    else dpiScale = Screen.dpi / 200f;

                    if (pos.x < 380 * dpiScale && Screen.height - pos.y < 250 * dpiScale) return;

                    x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                    y = ClampAngle(y, yMinLimit, yMaxLimit);
                    var rotation = Quaternion.Euler(y, x, 0);
                    var position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPos;
                    transform.rotation = rotation;
                    transform.position = position;

                    distance = Vector3.Distance(transform.position, targetPos);
                }
            }
        }
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    private void OnPostRender()
    {
        PostRenderCallback?.Invoke();
    }
}