using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Lizardman : Ennemy
{
    public override void Awake()
    {
        base.Awake();
        Name = "Zioned Lizardman";
        Type = EnnemyType.Lizardman;
        if (TABase == 0)
            TABase = 1;
        if (PO == 0)
            PO = 1;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> result = new List<CellPos>();

        // up right
        int x = CurrentCell.X;
        int y = CurrentCell.Y;
        for (int i = 0; i < PO; i++)
            result.Add(new CellPos(++x, ++y));

        // up left
        x = CurrentCell.X;
        y = CurrentCell.Y;
        for (int i = 0; i < PO; i++)
            result.Add(new CellPos(--x, ++y));

        // down left
        x = CurrentCell.X;
        y = CurrentCell.Y;
        for (int i = 0; i < PO; i++)
            result.Add(new CellPos(--x, --y));

        // down right
        x = CurrentCell.X;
        y = CurrentCell.Y;
        for (int i = 0; i < PO; i++)
            result.Add(new CellPos(++x, --y));

        return result;
    }
}