using UnityEngine;

[RequireComponent(typeof(UIMouseHoverDetector))]
[RequireComponent(typeof(Animator))]
public class UI_PointerHoverAnimations : MonoBehaviour
{
    public AnimationClip EnterClip;
    public AnimationClip ExitClip;
    private Animator animator;
    private UIMouseHoverDetector detector;

    /// <summary>
    /// Resolves the required components and connects hover callbacks.
    /// </summary>
    private void Awake()
    {
        // Bind once so pointer events can trigger their corresponding animations directly.
        detector = GetComponent<UIMouseHoverDetector>();
        animator = GetComponent<Animator>();
        detector.RegisterMouseEnter(PlayEnterClip);
        detector.RegisterMouseExit(PlayExitClip);
    }

    /// <summary>
    /// Plays the configured pointer-enter animation.
    /// </summary>
    public void PlayEnterClip()
    {
        // Use the configured clip name so prefab-specific animators remain supported.
        animator.Play(EnterClip.name);
    }

    /// <summary>
    /// Plays the configured pointer-exit animation.
    /// </summary>
    public void PlayExitClip()
    {
        // Use the configured clip name so prefab-specific animators remain supported.
        animator.Play(ExitClip.name);
    }
}
