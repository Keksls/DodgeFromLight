using System.Collections.Generic;

public static class GameContext
{
    public static UI_Notifications UI_Notifications;
    public static UI_WorkerNotifier UI_WorkerNotifier;
    public static UI_Modal UI_Modal;
    public static CursorManager CursorManager;
    public static Databases Databases;
    public static SceneTransitions SceneTransitions;
    public static AudioManager AudioManager;
    public static GameSettingsManager GameSettingsManager;
    public static TutorialController TutorialController;
    public static EnvironmentController EnvironmentController;
    public static GameManager GameManager;
    public static GridController GridController;
    public static GameRules CurrentRules;
    public static PlayerCharacter Character;
    public static List<Ennemy> Ennemies;
    public static List<MovingPlatform> MovingPlatforms;
    public static FullMap CurrentMap;
    public static int WaitForEndOfAction = 0;
    public static int EntitiesMoving { get; set; }
    public static bool HasEntitiesMoving { get { return EntitiesMoving > 0 || WaitForEndOfAction > 0; } }
    public static int TimeStoped = 0;
    private static bool initialized = false;

    /// <summary>
    /// Initializes the shared Unity presentation services and retained view collections.
    /// </summary>
    public static void Initialize()
    {
        // Initialization is idempotent because several scene services register themselves.
        TimeStoped = 0;
        if (initialized)
            return;

        initialized = true;
        EntitiesMoving = 0;
        Ennemies = new List<Ennemy>();
        MovingPlatforms = new List<MovingPlatform>();
        CurrentMap = null;

        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
    }

    /// <summary>
    /// Applies the active map environment to the Unity presentation.
    /// </summary>
    public static void SetEnvironment(FullMap map)
    {
        // Environment data remains visual and never feeds the deterministic simulation.
        EnvironmentController.SetFromMap(map);
        EnvironmentController.SetParticleEmissionSize(map.Grid);
    }

    /// <summary>
    /// Finds an instantiated enemy view at its authored starting position.
    /// </summary>
    public static Ennemy GetEnnemy(CellPos pos)
    {
        // This lookup is retained for map-editor and presentation compatibility only.
        foreach (Ennemy ennemy in Ennemies)
        {
            if (ennemy.StartCell.Equals(pos))
                return ennemy;
        }
        return null;
    }

    /// <summary>
    /// Add an action to wait for end
    /// </summary>
    public static void StartWaitingAction()
    {
        // Presentation animations can temporarily block additional view input.
        WaitForEndOfAction++;
    }

    /// <summary>
    /// Stop waiting for end of action
    /// </summary>
    public static void StopWaitingAction()
    {
        // Balance the presentation-only wait counter after an animation completes.
        WaitForEndOfAction--;
    }

    /// <summary>
    /// Refreshes visual environment state when a map presentation starts.
    /// </summary>
    private static void Events_StartMap()
    {
        // Runtime gear placement belongs exclusively to the deterministic Core.
        TimeStoped = 0;
        SetEnvironment(CurrentMap);
    }

    /// <summary>
    /// Returns to the local main menu.
    /// </summary>
    public static void RestartGame()
    {
        // Scene navigation remains a Unity presentation responsibility.
        SceneTransitions.LoadScene("MainMenu");
    }
}
