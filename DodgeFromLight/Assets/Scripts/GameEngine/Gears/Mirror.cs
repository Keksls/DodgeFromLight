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
        Cell cell = GameContext.CurrentMap.Grid.GetNeighbor(GameContext.GameManager.PlayerController.CurrentCell.GetCellPos(),
            GameContext.GameManager.PlayerController.Orientation);
        if (cell == null || !GameContext.CurrentMap.Grid.CanGoOnCell(cell))
            return false;

        mirror = GameObject.Instantiate(GameContext.GameManager.GearMirrorPrefab);
        cellPos = cell.GetCellPos();
        cell.UseGearOnCell(this);
        foreach (var en in GameContext.Ennemies)
            en.RefreshPO();
        GameContext.GridController.DrawCellsPO();
        mirror.transform.position = cellPos.ToVector3(0f);
        GameContext.GameManager._playerController.ThrowGear(Side);
        NbTurnLeft = 5;
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnEnd += Events_TurnEnd;
        return true;
    }

    public void Destroy()
    {
        Events.TurnEnd -= Events_TurnEnd;
        GameObject.Destroy(mirror);
        GameContext.CurrentMap.Grid.GetCell(cellPos).RemoveUsedGearOnCell();
        foreach (var en in GameContext.Ennemies)
            en.RefreshPO();
        GameContext.GridController.DrawCellsPO();
    }

    private void Events_TurnEnd()
    {
        if (GameContext.TimeStoped > 0)
            return;
        NbTurnLeft--;
        if(NbTurnLeft <= 0)
            Destroy();
    }
}