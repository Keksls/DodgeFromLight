using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterCameraController : MonoBehaviour
{
    public Vector3 Offset = new Vector3(0f, 4f, 0f);
    public EventSystem EventSystem;
    public Transform target;
    public float distance = 15.0f;
    public float minDistance = 10.0f;
    public float maxDistance = 60.0f;
    public float mouseSensitivity = 1.0f;

    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20;
    public float yMaxLimit = 80;

    public float scrollSpeed = 5f;

    float x = 0.0f;
    float y = 40.0f;
    public bool forceUpdate = false;


    void Start()
    {
        var angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void LateUpdate()
    {
        if ((EventSystem.IsPointerOverGameObject() && !Input.GetMouseButton(1)) || target == null)
            return;

        if (distance < minDistance) distance = minDistance;
        float scrool = Input.GetAxis("Mouse ScrollWheel");
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
            scrool += .1f;
        else if (Input.GetKeyDown(KeyCode.KeypadMinus))
            scrool -= .1f;
        distance -= scrool * scrollSpeed * Mathf.Pow(Vector3.Distance(transform.position, target.position + Offset), 0.9f);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        if (Input.GetMouseButton(1))
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
        }

        var rotation = Quaternion.Euler(y, x, 0);
        var position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position + Offset;
        transform.rotation = rotation;
        transform.position = position;
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
