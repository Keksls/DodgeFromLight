using UnityEngine;

[RequireComponent(typeof(UIMouseHoverDetector))]
[RequireComponent(typeof(Animator))]
public class UI_PointerHoverAnimations : MonoBehaviour
{
    public AnimationClip EnterClip;
    public AnimationClip ExitClip;
    private Animator Animator;
    private UIMouseHoverDetector detector;

    private void Awake()
    {
        detector = GetComponent<UIMouseHoverDetector>();
        Animator = GetComponent<Animator>();
        detector.OnMouseEnter(PlayEnterClip);
        detector.OnMouseExit(PlayExitClip);
    }

    public void PlayEnterClip()
    {
        Animator.Play(EnterClip.name);
    }

    public void PlayExitClip()
    {
        Animator.Play(ExitClip.name);
    }
}