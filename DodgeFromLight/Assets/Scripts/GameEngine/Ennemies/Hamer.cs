using DFLCommonNetwork.GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hamer : Ennemy
{
    int nbTurn;

    public override void Awake()
    {
        base.Awake();
        Name = "Gifted Hammer";
        Type = EnnemyType.Hammer;
        nbTurn = 0;
        Events.TurnStart -= Events_TurnStart1;
        Events.TurnStart += Events_TurnStart1;
        bool right = nbTurn % 2 == 0;
        AnimateOnKill = false;
        NoLineView = true;
        CantWalkOnMe = true;
        if (!right)
            GoLeft();
        else
            GoRight();
        TABase = -9999;
    }

    private void Events_TurnStart1()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        nbTurn++;
        bool right = nbTurn % 2 == 0;
        if (!right)
            GoLeft();
        else
            GoRight();
    }

    public override void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart1;
    }

    public override List<CellPos> GetBrutPO()
    {
        bool right = nbTurn % 2 == 0;
        List<CellPos> po = new List<CellPos>();

        switch (Orientation)
        {
            case Orientation.Up:
                if (right)
                    po.Add(new CellPos(CurrentCell.X + 2, CurrentCell.Y));
                else
                    po.Add(new CellPos(CurrentCell.X - 2, CurrentCell.Y));
                break;

            case Orientation.Right:
                if (right)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 2));
                else
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 2));
                break;

            case Orientation.Down:
                if (right)
                    po.Add(new CellPos(CurrentCell.X - 2, CurrentCell.Y));
                else
                    po.Add(new CellPos(CurrentCell.X + 2, CurrentCell.Y));
                break;

            case Orientation.Left:
                if (right)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 2));
                else
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 2));
                break;
        }
        CellPos cp = po[0];

        Cell c = DodgeFromLight.CurrentMap.Grid.GetNeighbor(cp, Orientation.Up);
        if (c != null)
            po.Add(c.GetCellPos());
        c = DodgeFromLight.CurrentMap.Grid.GetNeighbor(cp, Orientation.Down);
        if (c != null)
            po.Add(c.GetCellPos());
        c = DodgeFromLight.CurrentMap.Grid.GetNeighbor(cp, Orientation.Right);
        if (c != null)
            po.Add(c.GetCellPos());
        c = DodgeFromLight.CurrentMap.Grid.GetNeighbor(cp, Orientation.Left);
        if (c != null)
            po.Add(c.GetCellPos());

        return po;
    }

    void GoRight()
    {
        Play("HammerRight");
    }

    void GoLeft()
    {
        Play("HammerLeft");
    }
}
