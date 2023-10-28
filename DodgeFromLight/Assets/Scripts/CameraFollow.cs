using System;
using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Orientation Orientation;
    public Action PostRenderCallback;
    public AnimationCurve StartAnimationCurve;
    public AnimationCurve StartRotationAnimationCurve;
    public float YUp = 10f;
    // camera will follow this object
    public Transform Target;
    //camera transform
    public Transform camTransform;
    // offset between camera and target
    public Vector3 Offset;
    public Vector3 BaseOffset;
    // change this value to get desired smoothness
    public float SmoothTime = 0.3f;
    private bool animating = false;
    // This value will change at the runtime depending on target movement. Initialize with zero vector.
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        SetOrientation(Orientation.Up);
        BaseOffset = Offset;
    }

    private void OnEnable()
    {
        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
    }

    private void Events_StartMap()
    {
        SetOrientation(Orientation.Up);
    }

    private void OnDisable()
    {
        Events.StartMap -= Events_StartMap;
    }

    private void Update()
    {
        if (DodgeFromLight.GameManager != null && DodgeFromLight.HasEntitiesMoving)
            return;

        // turn right
        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.TurnCameraRight).IsDown())
        {
            switch (Orientation)
            {
                case Orientation.Up:
                    Orientation = Orientation.Right;
                    break;
                case Orientation.Right:
                    Orientation = Orientation.Down;
                    break;
                case Orientation.Down:
                    Orientation = Orientation.Left;
                    break;
                case Orientation.Left:
                    Orientation = Orientation.Up;
                    break;
            }
            SetOrientation(Orientation);
            DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        }

        // turn left
        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.TurnCameraLeft).IsDown())
        {
            switch (Orientation)
            {
                case Orientation.Up:
                    Orientation = Orientation.Left;
                    break;
                case Orientation.Right:
                    Orientation = Orientation.Up;
                    break;
                case Orientation.Down:
                    Orientation = Orientation.Right;
                    break;
                case Orientation.Left:
                    Orientation = Orientation.Down;
                    break;
            }
            SetOrientation(Orientation);
            DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        }
    }

    private void SetOrientation(Orientation dir)
    {
        Orientation = dir;
        switch (Orientation)
        {
            case Orientation.Up:
                Offset = new Vector3(0f, 5f, -5f);
                break;
            case Orientation.Right:
                Offset = new Vector3(5f, 5f, 0f);
                break;
            case Orientation.Down:
                Offset = new Vector3(0f, 5f, 5f);
                break;
            case Orientation.Left:
                Offset = new Vector3(-5f, 5f, 0);
                break;
        }
    }

    private void LateUpdate()
    {
        if (animating)
            return;
        // update position
        Vector3 targetPosition = Target.position + Offset;
        camTransform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, SmoothTime);

        // update rotation
        transform.LookAt(Target);
    }

    IEnumerator AnimateCamera_Routine(Vector3 start, Vector3 end)
    {
        DodgeFromLight.GameManager.InGameUI.HideUI();

        animating = true;
        DodgeFromLight.StartWaitingAction();

        end = Target.position + Offset;
        transform.LookAt(end);
        Quaternion erot = transform.rotation;

        start = start + (Offset / 4f);
        transform.LookAt(start);
        Quaternion srot = transform.rotation;


        float enlapsed = 0f;
        float duration = 2f;
        while (enlapsed < duration)
        {
            Vector3 pos = Vector3.Lerp(start, end, enlapsed / duration);
            pos.y += StartAnimationCurve.Evaluate(enlapsed / duration) * YUp;
            camTransform.position = pos;
            transform.LookAt(pos - Offset);// = Quaternion.Lerp(srot, erot, (enlapsed / duration ) * StartRotationAnimationCurve.Evaluate(enlapsed / duration));
            yield return null;
            enlapsed += Time.deltaTime;
        }
        DodgeFromLight.StopWaitingAction();
        animating = false;

        DodgeFromLight.GameManager.InGameUI.ShowUI();
    }

    public void AnimateCamera(Vector3 start, Vector3 end)
    {
        StartCoroutine(AnimateCamera_Routine(start, end));
    }

    private void OnPostRender()
    {
        PostRenderCallback?.Invoke();
    }
}