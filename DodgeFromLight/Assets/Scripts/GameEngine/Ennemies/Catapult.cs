using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Catapult : Ennemy
{
    int nbTurn;
    int dist;
    CatapultController controller;

    public override void Awake()
    {
        base.Awake();
        Name = "Shit Catapult";
        CantWalkOnMe = true;
        Type = EnnemyType.Catapult;
        nbTurn = 0;
        Events.TurnStart -= Events_TurnStart1;
        Events.TurnStart += Events_TurnStart1;
        bool reload = nbTurn % 2 == 0;
        AnimateOnKill = false;
        NoLineView = true;
        controller = GetComponent<CatapultController>();
        if (reload)
            controller.Reload();
        else if (CurrentCell != null)
            controller.Throw(GetThrowCell());
        TABase = -9999;
        dist = 5;
    }

    private void Events_TurnStart1()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        nbTurn++;
        bool reload = nbTurn % 2 == 0;
        if (reload)
            controller.Reload();
        else if (CurrentCell != null)
            controller.Throw(GetThrowCell());
    }

    public override void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart1;
    }

    private CellPos GetThrowCell()
    {
        switch (Orientation)
        {
            default:
            case Orientation.Up:
                return new CellPos(CurrentCell.X, CurrentCell.Y + dist);

            case Orientation.Right:
                return new CellPos(CurrentCell.X + dist, CurrentCell.Y);

            case Orientation.Down:
                return new CellPos(CurrentCell.X, CurrentCell.Y - dist);

            case Orientation.Left:
                return new CellPos(CurrentCell.X - dist, CurrentCell.Y);
        }
    }

    public override List<CellPos> GetBrutPO()
    {
        bool reloading = nbTurn % 2 == 0;
        List<CellPos> po = new List<CellPos>();
        if (reloading)
        {
            switch (Orientation)
            {
                case Orientation.Up:
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 1));
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 2));
                    break;
                case Orientation.Right:
                    po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y));
                    po.Add(new CellPos(CurrentCell.X - 2, CurrentCell.Y));
                    break;
                case Orientation.Down:
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 1));
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 2));
                    break;
                case Orientation.Left:
                    po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y));
                    po.Add(new CellPos(CurrentCell.X + 2, CurrentCell.Y));
                    break;
            }
            return po;
        }

        CellPos cp = GetThrowCell();
        Grid grid = DodgeFromLight.CurrentMap.Grid;
        Cell c = grid.GetNeighbor(cp, Orientation.Up);
        if (c != null)
            po.Add(c.GetCellPos());
        c = grid.GetNeighbor(cp, Orientation.Down);
        if (c != null)
            po.Add(c.GetCellPos());
        c = grid.GetNeighbor(cp, Orientation.Right);
        if (c != null)
            po.Add(c.GetCellPos());
        c = grid.GetNeighbor(cp, Orientation.Left);
        if (c != null)
            po.Add(c.GetCellPos());
        if (grid.CellExist(cp))
            po.Add(cp);
        return po;
    }
}
