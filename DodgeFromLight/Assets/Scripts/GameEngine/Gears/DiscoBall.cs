using System.Collections;
using UnityEngine;

public class DiscoBall : Gear
{
    public int NbTurnLeft;
    public int NbTurnStun;
    public GameObject gameObject;
    private string cell;
    private Cell placedCell;

    public void InitializeDiscoBall(GameObject go, int NbTurn, int nbTurnStun)
    {
        NbTurnLeft = NbTurn;
        gameObject = go;
        NbTurnStun = nbTurnStun;
    }

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

    public void Destroy()
    {
        GameContext.GameManager.StopCoroutine(Destroy_Routine());
        GameContext.GameManager.StartCoroutine(Destroy_Routine());
    }

    IEnumerator Destroy_Routine()
    {
        yield return null;
        GameObject.Destroy(gameObject);
        placedCell.RemoveUsedGearOnCell();
        GameContext.GridController.DrawCellsPO();
    }

    public override bool UseGear()
    {
        Cell c = GameContext.CurrentMap.Grid.GetNeighbor(GameContext.GameManager.PlayerController.CurrentCell.GetCellPos(),
            GameContext.GameManager.PlayerController.Orientation);
        if (c == null || !GameContext.CurrentMap.Grid.CanGoOnCell(c))
            return false;

        // instantiate discoball
        placedCell = c;
        cell = c.ToString();
        GameObject db = GameObject.Instantiate(GameContext.GameManager.GearDiscoballPrefab);
        db.transform.position = c.GetCellPos().ToVector3(0f);
        db.GetComponent<DiscoballController>().Initialize(this);
        c.UseGearOnCell(this);
        GameContext.GameManager._playerController.ThrowGear(Side);
        foreach (var en in GameContext.Ennemies)
            en.RefreshPO();
        GameContext.GridController.DrawCellsPO();
        return true;
    }
}