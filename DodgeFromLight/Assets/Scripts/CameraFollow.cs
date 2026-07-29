using System;
using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Orientation Orientation;
    public Action PostRenderCallback;
    public AnimationCurve StartAnimationCurve;
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

    /// <summary>
    /// Initializes the authored camera orientation and reusable follow offset.
    /// </summary>
    private void Start()
    {
        // The active map always begins from the same readable isometric angle.
        SetOrientation(Orientation.Up);
        BaseOffset = Offset;
    }

    /// <summary>
    /// Subscribes the camera reset behavior while this presentation view is active.
    /// </summary>
    private void OnEnable()
    {
        // Defensive removal prevents duplicate callbacks after repeated scene activation.
        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
    }

    /// <summary>
    /// Restores the canonical camera angle when a new map presentation starts.
    /// </summary>
    private void Events_StartMap()
    {
        // Map traversal never inherits a quarter-turn from the previous puzzle.
        SetOrientation(Orientation.Up);
    }

    /// <summary>
    /// Releases the map-start subscription owned by this camera view.
    /// </summary>
    private void OnDisable()
    {
        // Disabled camera views must not react to later map changes.
        Events.StartMap -= Events_StartMap;
    }

    /// <summary>
    /// Applies player-requested quarter-turns while presentation is idle.
    /// </summary>
    private void Update()
    {
        // Camera input cannot interleave with an entity movement presentation.
        if (GameContext.GameManager != null && GameContext.HasEntitiesMoving)
            return;

        // turn right
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.TurnCameraRight).IsDown())
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
            GameContext.GameManager.PlayerMove(Orientation.None);
        }

        // turn left
        else if (GameContext.GameSettingsManager.GetInput(InputSettingsType.TurnCameraLeft).IsDown())
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
            GameContext.GameManager.PlayerMove(Orientation.None);
        }
    }

    /// <summary>
    /// Converts one cardinal camera orientation into its follow offset.
    /// </summary>
    private void SetOrientation(Orientation dir)
    {
        // Four fixed offsets preserve grid readability and deterministic input rotation.
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

    /// <summary>
    /// Smoothly follows the active target after its frame movement is applied.
    /// </summary>
    private void LateUpdate()
    {
        // The authored flyover owns the camera transform while it is running.
        if (animating)
            return;
        // update position
        Vector3 targetPosition = Target.position + Offset;
        camTransform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, SmoothTime);

        // update rotation
        transform.LookAt(Target);
    }

    /// <summary>
    /// Presents the authored map flyover without hiding the Fugui HUD.
    /// </summary>
    private IEnumerator AnimateCameraRoutine(Vector3 overviewStart)
    {
        // Presentation wait state blocks gameplay input without influencing simulation.
        animating = true;
        GameContext.StartWaitingAction();

        overviewStart += Offset / 4f;
        transform.LookAt(overviewStart);
        float enlapsed = 0f;
        float duration = 2f;
        while (enlapsed < duration)
        {
            var targetPosition = Target.position + Offset;
            Vector3 pos = Vector3.Lerp(
                overviewStart,
                targetPosition,
                enlapsed / duration);
            pos.y += StartAnimationCurve.Evaluate(enlapsed / duration) * YUp;
            camTransform.position = pos;
            transform.LookAt(pos - Offset);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        GameContext.StopWaitingAction();
        animating = false;
    }

    /// <summary>
    /// Starts a non-authoritative camera flyover from a board overview.
    /// </summary>
    public void AnimateCamera(Vector3 overviewStart)
    {
        // The coroutine only owns camera presentation timing.
        StartCoroutine(AnimateCameraRoutine(overviewStart));
    }

    /// <summary>
    /// Invokes optional debug drawing after this camera has rendered.
    /// </summary>
    private void OnPostRender()
    {
        // Debug consumers can render paths without entering the gameplay loop.
        PostRenderCallback?.Invoke();
    }
}
