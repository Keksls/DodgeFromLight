using DFLCommonNetwork.GameEngine;
using UnityEngine;

public class Mirror : Gear
{
    public int NbTurnLeft = 5;
    GameObject mirror;
    CellPos cellPos;

    public override bool CanUseGear()
    {
        return true;
    }

    public override bool IsActivated()
    {
        return true;
    }

    public override void ThrowGear()
    {
        Events.TurnEnd -= Events_TurnEnd;
    }

    public override bool UseGear()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetNeighbor(DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos(),
            DodgeFromLight.GameManager.PlayerController.Orientation);
        if (cell == null || !DodgeFromLight.CurrentMap.Grid.CanGoOnCell(cell))
            return false;

        mirror = GameObject.Instantiate(DodgeFromLight.GameManager.GearMirrorPrefab);
        cellPos = cell.GetCellPos();
        cell.UseGearOnCell(this);
        foreach (var en in DodgeFromLight.Ennemies)
            en.RefreshPO();
        DodgeFromLight.GridController.DrawCellsPO();
        mirror.transform.position = cellPos.ToVector3(0f);
        DodgeFromLight.GameManager._playerController.ThrowGear(Side);
        NbTurnLeft = 5;
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnEnd += Events_TurnEnd;
        return true;
    }

    public void Destroy()
    {
        Events.TurnEnd -= Events_TurnEnd;
        GameObject.Destroy(mirror);
        DodgeFromLight.CurrentMap.Grid.GetCell(cellPos).RemoveUsedGearOnCell();
        foreach (var en in DodgeFromLight.Ennemies)
            en.RefreshPO();
        DodgeFromLight.GridController.DrawCellsPO();
    }

    private void Events_TurnEnd()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        NbTurnLeft--;
        if(NbTurnLeft <= 0)
            Destroy();
    }
}