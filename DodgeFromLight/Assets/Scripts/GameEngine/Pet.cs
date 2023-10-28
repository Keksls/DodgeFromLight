using UnityEngine;

public class Pet : MonoBehaviour
{
    [HideInInspector]
    public Transform Target;
    public AnimationClip Walk;
    public AnimationClip Idle;
    public AnimationClip Spawn;
    public AnimationClip Die;
    public AnimationClip Win;
    public AnimationClip Right;
    public AnimationClip Left;
    public RuntimeAnimatorController Controller;
    public Transform Pelvis;
    private AnimatorOverrideController currentController;
    public float SmoothTime = 0.15f;
    public float SmoothTimeRot = 0.3f;
    public float SmoothTimeCasted = 0.05f;
    public float SmoothTimeRotCasted = 0.05f;
    public bool IsCasted = false;
    private Vector3 velocity = Vector3.zero;
    private Vector3 velocityRot = Vector3.zero;
    private Animator Animator;
    private Quaternion pelvisDefaultRotation;

    public void Initialize(Transform playerTransform)
    {
        Target = playerTransform;
        Animator = GetComponent<Animator>();
        currentController = new AnimatorOverrideController(Controller);
        if (Pelvis)
            pelvisDefaultRotation = Pelvis.localRotation;

        currentController[PetAnimation.Walk.ToString()] = Walk;
        currentController[PetAnimation.Die.ToString()] = Die;
        currentController[PetAnimation.Idle.ToString()] = Idle;
        currentController[PetAnimation.Spawn.ToString()] = Spawn;
        currentController["Tail Attack"] = Win;
        currentController["Turn Right"] = Right;
        currentController["Turn Left"] = Left;
        Animator.runtimeAnimatorController = currentController;
        Events.EnnemySeePlayer -= Events_EnnemySeePlayer;
        Events.EnnemySeePlayer += Events_EnnemySeePlayer;
        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
        Play(PetAnimation.Spawn, true);
    }

    private void Events_StartMap()
    {
        Play(PetAnimation.Spawn, true);
    }

    private void Events_EnnemySeePlayer(Ennemy obj, Cell cell)
    {
        Play(PetAnimation.Die, true);
    }

    private void OnDisable()
    {
        Events.EnnemySeePlayer -= Events_EnnemySeePlayer;
        Events.StartMap -= Events_StartMap;
    }

    private void LateUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, Target.position, ref velocity, IsCasted ? SmoothTimeCasted : SmoothTime);
        float dist = Vector3.Distance(transform.position, Target.position);
        if (dist < 0.5f)
        {
            transform.forward = Vector3.SmoothDamp(transform.forward, Target.forward, ref velocityRot, IsCasted ? SmoothTimeRotCasted : SmoothTimeRot);
            float rDist = Vector3.SignedAngle(Target.forward, transform.forward, Vector3.up);
            //Debug.Log(rDist.ToString());
            if (dist < 0.1f && Mathf.Abs(rDist) < 5f)
                Play(PetAnimation.Idle);
            else
            {
                if (rDist > 10)
                    Play(PetAnimation.Left);
                else if (rDist < -10)
                    Play(PetAnimation.Right);
                else
                    Play(PetAnimation.Idle);
            }
        }
        else
        {
            Play(PetAnimation.Walk);
            transform.LookAt(Target);
        }

        if (Pelvis)
            Pelvis.localRotation = pelvisDefaultRotation;
    }

    /// <summary>
    /// Play animation clip
    /// </summary>
    /// <param name="clip">clip to play</param>
    /// <param name="lockAnimator">wait for end on clip</param>
    public void Play(PetAnimation clip, bool lockAnimator = false, float duration = -1f)
    {
        if (Animator == null)
            return;

        switch (clip)
        {
            case PetAnimation.Idle:
                Animator.SetBool("Walk", false);
                break;
            case PetAnimation.Spawn:
                Animator.Play("Spawn");
                break;
            case PetAnimation.Walk:
                Animator.SetInteger("Direction", 0);
                Animator.SetBool("Walk", true);
                break;
            case PetAnimation.Die:
                Animator.Play("Die");
                break;
            case PetAnimation.Win:
                Animator.Play("Win");
                break;
            case PetAnimation.Right:
                Animator.SetInteger("Direction", 1);
                Animator.SetBool("Walk", true);
                break;
            case PetAnimation.Left:
                Animator.SetInteger("Direction", -1);
                Animator.SetBool("Walk", true);
                break;
            default:
                break;
        }
    }
}

public enum PetAnimation
{
    Idle,
    Spawn,
    Walk,
    Die,
    Win,
    Right,
    Left
}