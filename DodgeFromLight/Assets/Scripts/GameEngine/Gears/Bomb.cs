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
        GameObject vfx = GameObject.Instantiate(GameContext.GameManager.GearBombeVFXPrefab);
        vfx.transform.position = GameContext.GameManager.PlayerController.transform.position;
        // destroy deco elements at player CAC
        Grid grid = GameContext.CurrentMap.Grid;
        CellPos cell = GameContext.GameManager.PlayerController.CurrentCell.GetCellPos();
        if (grid != null)
        {
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Up));
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Right));
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Down));
            grid.DestroyElementOnCell(grid.GetNeighbor(cell, Orientation.Left));
        }
        // throw current gear
        GameContext.GameManager._playerController.ThrowGear(Side);
        return true;
    }
}