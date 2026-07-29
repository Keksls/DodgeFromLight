using UnityEngine;

public class Needle : CellTrap
{
    private const float AnimationSpeedMultiplier = 2f;

    public Animation Animation;
    public string UpClip;
    public string DownClip;
    int nbTurns = 0;
    int mod = 2;
    public bool IsOn = false;

    /// <summary>
    /// Subscribes the needle trap to turn changes.
    /// </summary>
    public override void Awake()
    {
        IsOn = false;
        Events.TurnStart -= Events_TurnStart;
        Events.TurnStart += Events_TurnStart;
    }

    /// <summary>
    /// Initializes the visual phase of the needle trap.
    /// </summary>
    public void Initialize(int arg)
    {
        if (GameContext.GameManager != null)
            nbTurns = GameContext.GameManager.NbTurn;
        if (arg == 0)
            mod = 0;
        else
            mod = 1;
        if (nbTurns % 2 == mod)
        {
            On();
        }
        else
        {
            Off();
        }
    }

    /// <summary>
    /// Applies an authoritative needle phase supplied by the deterministic simulation.
    /// </summary>
    public void SetActive(bool isActive)
    {
        // Presentation follows the resolved state without advancing its own counter.
        if (isActive)
            On();
        else
            Off();
    }

    /// <summary>
    /// Updates the needle phase when a new turn starts.
    /// </summary>
    private void Events_TurnStart()
    {
        if (GameContext.TimeStoped > 0)
            return;
        nbTurns++;
        if (nbTurns % 2 == mod)
        {
            On();
        }
        else
        {
            Off();
        }
    }

    /// <summary>
    /// Raises the spikes and marks the trap as active.
    /// </summary>
    private void On()
    {
        IsOn = true;
        PlayNeedleAnimation(UpClip);
    }

    /// <summary>
    /// Lowers the spikes and marks the trap as inactive.
    /// </summary>
    private void Off()
    {
        IsOn = false;
        PlayNeedleAnimation(DownClip);
    }

    /// <summary>
    /// Plays a needle animation clip at twice its authored speed.
    /// </summary>
    private void PlayNeedleAnimation(string clipName)
    {
        // The speed multiplier affects presentation only; trap timing remains turn-based.
        if (Animation == null)
        {
            Debug.LogError($"Needle '{name}' has no Animation component assigned.", this);
            return;
        }

        AnimationState animationState = Animation[clipName];
        if (animationState == null)
        {
            Debug.LogError($"Needle '{name}' cannot find animation clip '{clipName}'.", this);
            return;
        }

        animationState.speed = AnimationSpeedMultiplier;
        Animation.Play(clipName);
    }

    /// <summary>
    /// Unsubscribes the needle trap from turn changes.
    /// </summary>
    private void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart;
    }
}
