using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Cyclops : Ennemy
{
    public override void Awake()
    {
        base.Awake();
        Name = "Siamese Cyclops";
        Type = EnnemyType.Cyclops;
        if (TABase == 0)
            TABase = 1;
        if (PO == 0)
            PO = 3;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> po = new List<CellPos>();

        if (NbPMUsed % 2 == 0)
        {
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
        }
        else
        {
            switch (Orientation)
            {
                case Orientation.Down:
                    for (int i = 1; i <= PO; i++)
                        po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + i));
                    break;

                case Orientation.Up:
                    for (int i = 1; i <= PO; i++)
                        po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - i));
                    break;

                case Orientation.Right:
                    for (int i = 1; i <= PO; i++)
                        po.Add(new CellPos(CurrentCell.X - i, CurrentCell.Y));
                    break;

                case Orientation.Left:
                    for (int i = 1; i <= PO; i++)
                        po.Add(new CellPos(CurrentCell.X + i, CurrentCell.Y));
                    break;
            }
        }

        return po;
    }
}