using UnityEngine;

public class LookatCamera : MonoBehaviour
{
    public bool OnlyY = true;
    public bool InverseY = false;
    public Transform CameraTransform;

    public void Start()
    {
        if (DodgeFromLight.GameManager != null)
            CameraTransform = DodgeFromLight.GameManager.Follower.transform;
        else
            CameraTransform = Camera.main.transform;
    }

    void Update()
    {
        transform.LookAt(CameraTransform);
        if (OnlyY)
        {
            Vector3 euler = transform.eulerAngles;
            euler.x = 0f;
            euler.z = 0f;
            transform.eulerAngles = euler;
        }
        if (InverseY)
        {
            Vector3 euler = transform.eulerAngles;
            euler.y += 180f;
            euler.y %= 360f;
            transform.eulerAngles = euler;
        }
    }
}