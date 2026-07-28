using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class DodgeFromLight
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

    public static void Initialize()
    {
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

    public static void SetEnvironment(FullMap map)
    {
        EnvironmentController.SetFromMap(map);
        EnvironmentController.SetParticleEmissionSize(map.Grid);
    }

    public static Ennemy GetEnnemy(CellPos pos)
    {
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
        WaitForEndOfAction++;
    }

    /// <summary>
    /// Stop waiting for end of action
    /// </summary>
    public static void StopWaitingAction()
    {
        WaitForEndOfAction--;
    }

    private static void Events_StartMap()
    {
        //SpawnGear();
        TimeStoped = 0;
        SetEnvironment(CurrentMap);
    }

    static void SpawnGear()
    {
        bool addGear = Random.Range(0, 100) < 0f;
        if (addGear)
        {
            // get random available cell
            List<Cell> availableCells = new List<Cell>();
            for (int x = 0; x < CurrentMap.Grid.Width; x++)
                for (int y = 0; y < CurrentMap.Grid.Height; y++)
                {
                    Cell c = CurrentMap.Grid.GetCell(x, y);
                    if (c.EntityOnCell == null &&
                        !CurrentMap.Grid.IsStart(c) &&
                        !CurrentMap.Grid.IsEnd(c) &&
                        c.IsWalkable)
                        availableCells.Add(CurrentMap.Grid.GetCell(x, y));
                }

            // get random gear
            if (availableCells.Count > 0)
            {
                Cell gearCell = availableCells[Random.Range(0, availableCells.Count - 1)];

                var gears = (GearType[])Enum.GetValues(typeof(GearType));
                // remove this once everithing is implemented
                gears = new GearType[] { GearType.Bomb,
                GearType.DiscoBall,
                GearType.Mirror,
                GearType.RemoteControl,
                GearType.SpeedBoots,
                GearType.TimeController,
                GearType.Grapple};
                GearType type = gears[Random.Range(0, gears.Length)];
                gearCell.SetGear(type);
            }
        }
    }

    public static void RestartGame()
    {
        DodgeFromLight.SceneTransitions.LoadScene("MainMenu");
    }
}
