using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Pillar : Ennemy
{
    int nbTurn;
    PillarController controller;

    public override void Awake()
    {
        base.Awake();
        CantWalkOnMe = true;
        Name = "Eternity Pillar";
        Type = EnnemyType.Pillar;
        nbTurn = 0;
        Events.TurnStart -= Events_TurnStart1;
        Events.TurnStart += Events_TurnStart1;
        AnimateOnKill = false;
        NoLineView = true;
        controller = GetComponent<PillarController>();
        TABase = -9999;
    }

    private void Events_TurnStart1()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        nbTurn++;
        bool reload = nbTurn % 2 == 0;
    }

    public override void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart1;
    }

    public override List<CellPos> GetBrutPO()
    {
        int state = nbTurn % 4;
        if (Orientation == Orientation.Right || Orientation == Orientation.Left)
            state = (nbTurn + 1) % 4;

        List<CellPos> po = new List<CellPos>();

        switch (state)
        {
            case 0:
                po.Add(new CellPos(CurrentCell.X + 4, CurrentCell.Y));
                po.Add(new CellPos(CurrentCell.X - 4, CurrentCell.Y));
                break;

            case 1:
                po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 4));
                po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 4));
                break;

            case 2:
                po.Add(new CellPos(CurrentCell.X + 2, CurrentCell.Y));
                po.Add(new CellPos(CurrentCell.X - 2, CurrentCell.Y));
                break;

            case 3:
                po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 2));
                po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 2));
                break;
        }

        List<CellPos> targets = new List<CellPos>();

        Grid grid = DodgeFromLight.CurrentMap.Grid;
        for (int i = 0; i < 2; i++)
        {
            int nb = po.Count;
            CellPos cp = po[i];
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

            if (grid.CellExist(cp) || nb != po.Count)
                targets.Add(cp);
        }
        controller.ClearTargets();
        controller.SetTargetCells(targets);
        return po;
    }
}
