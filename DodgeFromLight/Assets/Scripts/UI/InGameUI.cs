using System.Collections;
using UnityEngine;

/// <summary>
/// Owns pause, restart, and departure behavior for the local gameplay scene.
/// </summary>
public class InGameUI : MonoBehaviour
{
    public float HideCursorDuration = 5f;
    public float MouseMovementThreshold = 0.1f;

    private bool _isExiting;

#if !UNITY_EDITOR
    private float _lastMouseMovementTime;
    private Vector3 _lastMousePosition;
#endif

    /// <summary>
    /// Handles pause input and runtime cursor visibility.
    /// </summary>
    private void Update()
    {
        // Escape toggles the DrawList pause state without activating a retained Canvas.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameContext.GameManager != null && GameContext.GameManager.Paused)
                TryResume();
            else
                TryPause();
        }

#if !UNITY_EDITOR
        UpdateRuntimeCursor();
#endif
    }

#if !UNITY_EDITOR
    /// <summary>
    /// Applies the configured idle cursor behavior outside the Unity editor.
    /// </summary>
    private void UpdateRuntimeCursor()
    {
        // Modal pause and result screens always keep the pointer available.
        if (GameContext.GameManager != null && GameContext.GameManager.Paused)
        {
            Cursor.visible = true;
            _lastMouseMovementTime = Time.unscaledTime;
            _lastMousePosition = Input.mousePosition;
            return;
        }

        var settingsManager = GameContext.GameSettingsManager;
        if (settingsManager == null ||
            !settingsManager.CurrentSettings.Global.AutoHideCursor)
        {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        if (Vector3.Distance(mousePosition, _lastMousePosition) >
            MouseMovementThreshold)
        {
            Cursor.visible = true;
            _lastMouseMovementTime = Time.unscaledTime;
        }
        else if (_lastMouseMovementTime + HideCursorDuration <=
                 Time.unscaledTime)
        {
            Cursor.visible = false;
        }

        _lastMousePosition = mousePosition;
    }
#endif

    /// <summary>
    /// Restores the system cursor whenever the scene controller is disabled.
    /// </summary>
    private void OnDisable()
    {
        // Scene transitions must never leave the desktop cursor hidden.
        Cursor.visible = true;
    }

    /// <summary>
    /// Tries to pause the loaded local run.
    /// </summary>
    public bool TryPause()
    {
        // Presentation and timer suspension remain synchronized by GameManager.
        var gameManager = GameContext.GameManager;
        if (gameManager == null ||
            gameManager.Paused ||
            !gameManager.CanControlLoadedRun)
        {
            return false;
        }

        gameManager.EnterPause();
        Cursor.visible = true;
        return true;
    }

    /// <summary>
    /// Tries to resume the paused local run.
    /// </summary>
    public bool TryResume()
    {
        // Resuming an already active run is intentionally a no-op.
        var gameManager = GameContext.GameManager;
        if (gameManager == null || !gameManager.Paused)
            return false;

        gameManager.ExitPause();
        return true;
    }

    /// <summary>
    /// Tries to restart the current deterministic attempt.
    /// </summary>
    public bool TryRestart()
    {
        // A paused attempt is resumed before the guarded restart request is evaluated.
        var gameManager = GameContext.GameManager;
        if (gameManager == null ||
            !gameManager.CanControlLoadedRun ||
            GameContext.WaitForEndOfAction > 0)
        {
            return false;
        }

        if (gameManager.Paused)
            gameManager.ExitPause();
        return gameManager.TryRestartCurrentRun();
    }

    /// <summary>
    /// Tries to leave the current local play session.
    /// </summary>
    public bool TryExit()
    {
        // Repeated DrawList clicks cannot start competing cleanup coroutines.
        if (_isExiting || GameContext.GameManager == null)
            return false;

        _isExiting = true;
        if (PoolManager.Instance != null)
            PoolManager.Instance.PushBackAllPool(PoolName.POCell);
        if (GameContext.GameManager.Paused)
            GameContext.GameManager.ExitPause();

        string destination = PlayerPrefs.HasKey("ForceGrid")
            ? "MapCreator"
            : "MainMenu";
        if (destination == "MainMenu")
            PlayerPrefs.DeleteKey("ForceGrid");
        StartCoroutine(ExitRoutine(destination));
        return true;
    }

    /// <summary>
    /// Preserves the scene-event entry point for leaving the current session.
    /// </summary>
    public void Exit()
    {
        // Serialized callers and DrawList callers share the same guarded operation.
        TryExit();
    }

    /// <summary>
    /// Preserves the scene-event entry point for restarting the current attempt.
    /// </summary>
    public void RestartMap()
    {
        // Serialized callers and DrawList callers share the same guarded operation.
        TryRestart();
    }

    /// <summary>
    /// Preserves the scene-event entry point for resuming a paused attempt.
    /// </summary>
    public void ClosePauseMenu()
    {
        // This scene-event entry point controls state only and never owns a Canvas.
        TryResume();
    }

    /// <summary>
    /// Releases the active gameplay manager before loading the requested local scene.
    /// </summary>
    private IEnumerator ExitRoutine(string sceneName)
    {
        // Defer destruction by one frame so the current DrawList callback can finish safely.
        yield return null;
        var gameManager = GameContext.GameManager;
        if (gameManager != null)
        {
            Destroy(gameManager.gameObject);
            GameContext.GameManager = null;
        }

        GameContext.SceneTransitions.LoadScene(sceneName);
    }
}
