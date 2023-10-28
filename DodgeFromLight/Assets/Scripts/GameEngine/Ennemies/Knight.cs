using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Knight : Ennemy
{
    public override void Awake()
    {
        base.Awake();
        Name = "Fiery Knighht";
        Type = EnnemyType.Knight;
        if (TABase == 0)
            TABase = 1;
        if (PO == 0)
            PO = 1;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> po = new List<CellPos>();

        switch (Orientation)
        {
            case Orientation.Up:
                // all around the entity
                po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y + 1));
                po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y + 1));
                break;
            case Orientation.Right:
                // all around the entity
                po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y - 1));
                po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y + 1));
                break;
            case Orientation.Down:
                // all around the entity
                po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y - 1));
                po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y - 1));
                break;
            case Orientation.Left:
                // all around the entity
                po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y - 1));
                po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y + 1));
                break;
        }

        for(int i = 1; i <= PO; i++)
        {
            if (Orientation != Orientation.Up)
                po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - i));
            if (Orientation != Orientation.Down)
                po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + i));
            if (Orientation != Orientation.Right)
                po.Add(new CellPos(CurrentCell.X - i, CurrentCell.Y));
            if (Orientation != Orientation.Left)
                po.Add(new CellPos(CurrentCell.X + i, CurrentCell.Y));
        }

        return po;
    }
}