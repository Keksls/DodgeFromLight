using UnityEngine;

/// <summary>
/// Stores transient loading feedback consumed by the runtime DrawList interface.
/// </summary>
public class UI_WorkerNotifier : MonoBehaviour
{
    public bool IsMain = true;

    public bool IsVisible { get; private set; }
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Registers the primary scene loading-state source.
    /// </summary>
    private void Awake()
    {
        // Only the scene-designated instance publishes global loading feedback.
        Hide();
        if (IsMain)
            GameContext.UI_WorkerNotifier = this;
    }

    /// <summary>
    /// Publishes a visible transient status message.
    /// </summary>
    public void Show(string message)
    {
        // The DrawList presenter reads this state on the next Fugui render event.
        Message = message ?? string.Empty;
        IsVisible = true;
    }

    /// <summary>
    /// Updates the current transient status message.
    /// </summary>
    public void SetText(string message)
    {
        // Message updates do not implicitly change visibility.
        Message = message ?? string.Empty;
    }

    /// <summary>
    /// Clears the current transient status message.
    /// </summary>
    public void Hide()
    {
        // Empty state prevents an old loading label from leaking into a later scene.
        IsVisible = false;
        Message = string.Empty;
    }
}
