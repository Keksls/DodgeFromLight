using System;

/// <summary>
/// Exposes local gameplay and presentation events.
/// </summary>
public static class Events
{
    public static event Action<Entity> EntityStartMove;
    public static event Action<Entity> EntityEndMove;
    public static event Action<Cell, Cell> PlayerStartChangeCell;
    public static event Action<Cell, Cell> PlayerEndChangeCell;
    public static event Action TurnStart;
    public static event Action TurnEnd;
    public static event Action StartMap;
    public static event Action EnnemiesSpawned;
    public static event Action<GearType> SetGear;
    public static event Action SaveGetted;
    public static event Action<Ennemy, Cell> EnnemySeePlayer;
    public static event Action<int> WinXP;
    public static event Action SaveUpdated;
    /// <summary>
    /// Notifies listeners that an entity started moving.
    /// </summary>
    public static void Fire_EntityStartMove(Entity entity)
    {
        // Publish movement without coupling entities to presentation systems.
        EntityStartMove?.Invoke(entity);
    }

    /// <summary>
    /// Notifies listeners that an entity finished moving.
    /// </summary>
    public static void Fire_EntityEndMove(Entity entity)
    {
        // Publish movement completion to local gameplay observers.
        EntityEndMove?.Invoke(entity);
    }

    /// <summary>
    /// Notifies listeners that a turn started.
    /// </summary>
    public static void Fire_TurnStart()
    {
        // Keep turn observers independent from the game manager implementation.
        TurnStart?.Invoke();
    }

    /// <summary>
    /// Notifies listeners that a turn ended.
    /// </summary>
    public static void Fire_TurnEnd()
    {
        // Publish the completed turn after all entity actions resolve.
        TurnEnd?.Invoke();
    }

    /// <summary>
    /// Notifies listeners that a map started.
    /// </summary>
    public static void Fire_StartMap()
    {
        // Allow UI and tutorial systems to bind to map startup.
        StartMap?.Invoke();
    }

    /// <summary>
    /// Notifies listeners that all enemies spawned.
    /// </summary>
    public static void Fire_EnnemiesSpawned()
    {
        // Signal that the authored enemy set is ready for dependent systems.
        EnnemiesSpawned?.Invoke();
    }

    /// <summary>
    /// Notifies listeners that a gear slot changed.
    /// </summary>
    public static void Fire_SetGear(GearType type)
    {
        // Publish the selected gear type to local UI and tutorials.
        SetGear?.Invoke(type);
    }

    /// <summary>
    /// Notifies listeners that an enemy detected the player.
    /// </summary>
    public static void Fire_EnnemySeePlayer(Ennemy ennemy, Cell cell)
    {
        // Forward the detecting enemy and observed cell.
        EnnemySeePlayer?.Invoke(ennemy, cell);
    }

    /// <summary>
    /// Notifies listeners that the player started changing cells.
    /// </summary>
    public static void Fire_PlayerStartChangeCell(Cell lastCell, Cell newCell)
    {
        // Publish the transition before the movement animation begins.
        PlayerStartChangeCell?.Invoke(lastCell, newCell);
    }

    /// <summary>
    /// Notifies listeners that the player finished changing cells.
    /// </summary>
    public static void Fire_PlayerEndChangeCell(Cell lastCell, Cell newCell)
    {
        // Publish the resolved transition to dependent gameplay systems.
        PlayerEndChangeCell?.Invoke(lastCell, newCell);
    }

    /// <summary>
    /// Notifies listeners that the local save is available.
    /// </summary>
    public static void Fire_SaveGetted()
    {
        // Preserve the established event name while sourcing data locally.
        SaveGetted?.Invoke();
    }

    /// <summary>
    /// Notifies listeners that local progression changed.
    /// </summary>
    public static void Fire_SaveUpdated()
    {
        // Refresh inventory and player presentation after a durable change.
        SaveUpdated?.Invoke();
    }

    /// <summary>
    /// Notifies listeners that local experience was awarded.
    /// </summary>
    public static void Fire_WinXP(int experience)
    {
        // Forward the awarded amount to progression presentation.
        WinXP?.Invoke(experience);
    }
}
