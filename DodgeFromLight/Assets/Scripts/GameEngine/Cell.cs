using DFLCommonNetwork.GameEngine;
using Newtonsoft.Json;
using System;

public class Cell
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Walkable { get; set; }
    public Entity EntityOnCell { get; set; }
    public CellType Type { get; set; }
    public string FloorID { get; set; }
    public string ElementID { get; set; }
    public float ElementOrientation { get; set; }
    public int Arg1 { get; set; }
    public Orientation FloorOrientation { get; set; }
    public GearType GearType { get; set; }
    [JsonIgnore]
    public Gear GearInUse { get; private set; }
    public float ElementScale { get; set; }
    public bool HasElement { get; set; }
    private event Action OnEnterCell;
    private event Action OnLeaveCell;
    [JsonIgnore]
    public int fCost { get; set; }
    [JsonIgnore]
    public int gCost { get; set; }
    [JsonIgnore]
    public int hCost { get; set; }
    [JsonIgnore]
    public Cell parent { get; set; }
    public bool IsWalkable { get { return Walkable && !HasElement && (GearInUse == null || GearInUse.Type == GearType.DiscoBall); } }

    #region Events
    public void SetWalkable(bool walkable)
    {
        Walkable = walkable;
    }

    public void RegisterOnEnterCallback(Action OnEnterCallback)
    {
        OnEnterCell += OnEnterCallback;
    }

    public void UnRegisterOnEnterCallback(Action OnEnterCallback)
    {
        OnEnterCell -= OnEnterCallback;
    }

    public void RegisterOnLeaveCallback(Action OnLeaveCallback)
    {
        OnLeaveCell += OnLeaveCallback;
    }

    public void UnRegisterOnLeaveCallback(Action OnLeaveCallback)
    {
        OnLeaveCell -= OnLeaveCallback;
    }

    public void Fire_OnEnter()
    {
        OnEnterCell?.Invoke();
    }

    public void Fire_OnLeave()
    {
        OnLeaveCell?.Invoke();
    }
    #endregion

    public Cell(int x, int y, bool walkable = true)
    {
        FloorID = "EmptyTile4";
        ElementID = string.Empty;
        FloorOrientation = Orientation.Up;
        X = x;
        Y = y;
        Walkable = walkable;
        Type = walkable ? CellType.Walkable : CellType.NotWalkable;
        GearType = GearType.None;
        GearInUse = null;
    }

    public void SetGear(GearType Type)
    {
        GearType = Type;
    }

    public void UseGearOnCell(Gear gear)
    {
        GearInUse = gear;
    }

    public void RemoveUsedGearOnCell()
    {
        GearInUse = null;
    }

    public void SetType(CellType type)
    {
        Type = type;
    }

    public void SetElement(string elementID)
    {
        ElementID = elementID;
        ElementScale = 1f;
        ElementOrientation = 0f;
    }

    public void RemoveElement()
    {
        ElementID = string.Empty;
        ElementScale = 0f;
        ElementOrientation = 0f;
    }

    public bool Equals(Cell cell)
    {
        return X == cell.X && Y == cell.Y;
    }

    public CellPos GetCellPos()
    {
        return new CellPos(X, Y);
    }

    public override string ToString()
    {
        return X + "_" + Y;
    }

    public bool IsInLine(CellPos cell)
    {
        return X == cell.X || Y == cell.Y;
    }
}

public enum CellType
{
    Walkable,
    NotWalkable,
    TP,
    Collapsable,
    FlipACoin,
    CheckPoint,
    Spring,
    Bombe,
    Needle,
    SwordSocle
}