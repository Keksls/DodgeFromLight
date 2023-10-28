using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Scoute : Ennemy
{
    public override void Awake()
    {
        Name = "Brazen Scoute";
        Type = EnnemyType.Scoute;
        base.Awake();
        if (TABase == 0)
            TABase = 1;
        if (PO == 0)
            PO = 3;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> po = new List<CellPos>();

        switch (Orientation)
        {
            case Orientation.Up:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + i));
                break;

            case Orientation.Down:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - i));
                break;

            case Orientation.Left:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X - i, CurrentCell.Y));
                break;

            case Orientation.Right:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X + i, CurrentCell.Y));
                break;
        }

        return po;
    }
}