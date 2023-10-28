using DFLCommonNetwork.GameEngine;
using UnityEngine;

public class Bomb : Gear
{
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
    }

    public override bool UseGear()
    {
        // play VFX
        GameObject vfx = GameObject.Instantiate(DodgeFromLight.GameManager.GearBombeVFXPrefab);
        vfx.transform.position = DodgeFromLight.GameManager.PlayerController.transform.position;
        // destroy deco elements at player CAC
        Grid grid = DodgeFromLight.CurrentMap.Grid;
        CellPos cell = DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos();
        if (grid != null)
        {
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Up));
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Right));
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Down));
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Left));
        }
        // throw current gear
        DodgeFromLight.GameManager._playerController.ThrowGear(Side);
        return true;
    }
}