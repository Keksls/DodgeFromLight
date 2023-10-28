using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;
using UnityEngine;

public class Keeper : Ennemy
{
    public override void Awake()
    {
        base.Awake();
        Name = "Hellish Keeper";
        Type = EnnemyType.Keeper;
        //Events.PlayerEndChangeCell += Events_PlayerEndChangeCell;
        Events_PlayerEndChangeCell(null, null);
        ExcludedAwakening.Add(AwakeningType.Concentrate);
        ExcludedAwakening.Add(AwakeningType.Hurry);
        if (TABase == 0)
            TABase = -9999;
        if (PO == 0)
            PO = 2;
    }

    private void Events_PlayerEndChangeCell(Cell arg1, Cell arg2)
    {
        if (DodgeFromLight.GameManager?.PlayerController == null)
            return;
        transform.LookAt(DodgeFromLight.GameManager.PlayerController.transform);
        Vector3 rot = transform.localEulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.localEulerAngles = rot;
    }

    public override void OnDestroy()
    {
       // Events.PlayerEndChangeCell -= Events_PlayerEndChangeCell;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> po = new List<CellPos>();
        CellPos start = CurrentCell.GetCellPos();
        Grid grid = DodgeFromLight.CurrentMap.Grid;

        // MiddleLine
        for (int x = start.X - PO; x <= start.X + PO; x++)
            if (x < grid.Width && x >= 0)
                po.Add(new CellPos(x, start.Y));

        // Up Part
        int i = 0;
        for (int y = start.Y + 1; y <= start.Y + PO; y++)
        {
            i++;
            if (y < grid.Height)
            {
                for (int x = start.X - PO + i; x <= start.X + PO - i; x++)
                    if (x < grid.Width && x >= 0)
                        po.Add(new CellPos(x, y));
            }
            else
                break;
        }

        // Down Part
        i = 0;
        for (int y = start.Y - 1; y >= start.Y - PO; y--)
        {
            i++;
            if (y >= 0)
            {
                for (int x = start.X - PO + i; x <= start.X + PO - i; x++)
                    if (x < grid.Width && x >= 0)
                        po.Add(new CellPos(x, y));
            }
            else
                break;
        }

        return po;
    }
}