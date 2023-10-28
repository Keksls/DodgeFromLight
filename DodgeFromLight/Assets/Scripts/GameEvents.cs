using DFLCommonNetwork.GameEngine;
using System;

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
    public static event Action UserLogin;
    public static event Action SaveGetted;
    public static event Action<Ennemy, Cell> EnnemySeePlayer;
    public static event Action<int> WinXP;
    public static event Action SaveUpdated;
    public static event Action EnterHub;
    public static event Action<LitePlayerSave, string, int, int> EnterLobby;
    public static event Action LeaveHub;
    public static event Action OnMapDownloaded;
    public static event Action LeaveLobby;
    public static event Action<string> ClickOnClickableObject;

    public static void Fire_OnMapDownloaded()
    {
        OnMapDownloaded?.Invoke();
    }

    public static void Fire_ClickOnClickableObject(string arg)
    {
        ClickOnClickableObject?.Invoke(arg);
    }

    public static void Fire_EnterHub()
    {
        EnterHub?.Invoke();
    }
    public static void Fire_EnterLobby(LitePlayerSave player, string name, int maxClients, int nbCLients)
    {
        EnterLobby?.Invoke(player, name, maxClients, nbCLients);
    }
    public static void Fire_LeaveHub()
    {
        LeaveHub?.Invoke();
    }
    public static void Fire_LeaveLobbyt()
    {
        LeaveLobby?.Invoke();
    }

    public static void Fire_EntityStartMove(Entity entity)
    {
        EntityStartMove?.Invoke(entity);
    }

    public static void Fire_WinXP(int XP)
    {
        WinXP?.Invoke(XP);
    }

    public static void Fire_SaveUpdated()
    {
        SaveUpdated?.Invoke();
    }

    public static void Fire_EntityEndMove(Entity entity)
    {
        EntityEndMove?.Invoke(entity);
    }

    public static void Fire_TurnStart()
    {
        TurnStart?.Invoke();
    }

    public static void Fire_SaveGetted()
    {
        SaveGetted?.Invoke();
    }

    public static void Fire_UserLogin()
    {
        UserLogin?.Invoke();
    }

    public static void Fire_TurnEnd()
    {
        TurnEnd?.Invoke();
    }

    public static void Fire_StartMap()
    {
        StartMap?.Invoke();
    }

    public static void Fire_EnnemiesSpawned()
    {
        EnnemiesSpawned?.Invoke();
    }

    public static void Fire_SetGear(GearType type)
    {
        SetGear?.Invoke(type);
    }

    public static void Fire_EnnemySeePlayer(Ennemy ennemy, Cell cell)
    {
        EnnemySeePlayer?.Invoke(ennemy, cell);
    }

    public static void Fire_PlayerStartChangeCell(Cell lastCell, Cell newCell)
    {
        PlayerStartChangeCell?.Invoke(lastCell, newCell);
    }

    public static void Fire_PlayerEndChangeCell(Cell lastCell, Cell newCell)
    {
        PlayerEndChangeCell?.Invoke(lastCell, newCell);
    }

}