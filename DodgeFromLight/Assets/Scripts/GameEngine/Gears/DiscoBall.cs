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
        DodgeFromLight.GameManager.StopCoroutine(Destroy_Routine());
        DodgeFromLight.GameManager.StartCoroutine(Destroy_Routine());
    }

    IEnumerator Destroy_Routine()
    {
        yield return null;
        GameObject.Destroy(gameObject);
        placedCell.RemoveUsedGearOnCell();
        DodgeFromLight.GridController.DrawCellsPO();
    }

    public override bool UseGear()
    {
        Cell c = DodgeFromLight.CurrentMap.Grid.GetNeighbor(DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos(),
            DodgeFromLight.GameManager.PlayerController.Orientation);
        if (c == null || !DodgeFromLight.CurrentMap.Grid.CanGoOnCell(c))
            return false;

        // instantiate discoball
        placedCell = c;
        cell = c.ToString();
        GameObject db = GameObject.Instantiate(DodgeFromLight.GameManager.GearDiscoballPrefab);
        db.transform.position = c.GetCellPos().ToVector3(0f);
        db.GetComponent<DiscoballController>().Initialize(this);
        c.UseGearOnCell(this);
        DodgeFromLight.GameManager._playerController.ThrowGear(Side);
        foreach (var en in DodgeFromLight.Ennemies)
            en.RefreshPO();
        DodgeFromLight.GridController.DrawCellsPO();
        return true;
    }
}