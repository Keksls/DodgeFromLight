using DFLCommonNetwork.GameEngine;
using System.Collections;
using UnityEngine;

public class Grapple : Gear
{
    public int NbCells = 5;

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
        // get target cell pos
        CellPos target = DodgeFromLight.CurrentMap.Grid.GetFarestCell(DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos(),
            DodgeFromLight.GameManager.PlayerController.Orientation, NbCells, InLineCellType.Grapple);
        bool sameCell = target.Equals(DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos());
        if (!sameCell)
        {
            DodgeFromLight.GameManager.PlayerMove(Orientation.None);
            // spawn grapple
            GameObject grapple = GameObject.Instantiate(DodgeFromLight.GameManager.GearGrapplePrefab);
            Vector3 pos = DodgeFromLight.GameManager.PlayerController.transform.position;
            pos.y += 0.35f;
            grapple.transform.position = pos;
            grapple.transform.eulerAngles = DodgeFromLight.CurrentMap.Grid.GetOrientationVector(DodgeFromLight.GameManager.PlayerController.Orientation);
            // throw the grapple
            DodgeFromLight.GameManager.StartCoroutine(ThrowGrapple(target, grapple));

            // throw current gear
            DodgeFromLight.GameManager._playerController.ThrowGear(Side);
        }
        return true;
    }

    IEnumerator ThrowGrapple(CellPos cell, GameObject grapple)
    {
        Events.Fire_PlayerStartChangeCell(DodgeFromLight.GameManager.PlayerController.CurrentCell, DodgeFromLight.CurrentMap.Grid.GetCell(cell));
        float dist = cell.Distance(DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos());
        Transform player = DodgeFromLight.GameManager.PlayerController.transform;
        DodgeFromLight.StartWaitingAction();
        float duration = DodgeFromLight.GameManager.EntitiesMovementDuration * (dist / NbCells);
        float enlapsed = 0f;
        Vector3 startPos = player.position;
        startPos.y = 0.35f;
        Vector3 endPos = cell.ToVector3(0f);
        endPos.y = 0.35f;
        Cell lastCell = DodgeFromLight.GameManager.PlayerController.CurrentCell;
        LineRenderer lr = grapple.GetComponent<LineRenderer>();

        // throw grapple
        while (enlapsed < duration)
        {
            Vector3 pos = Vector3.Lerp(startPos, endPos, enlapsed / duration);
            grapple.transform.position = pos;
            lr.SetPosition(0, player.position);
            lr.SetPosition(1, grapple.transform.position);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        yield return null;
        // move player
        startPos.y = 0f;
        endPos.y = 0f;
        enlapsed = 0f;
        duration = DodgeFromLight.GameManager.EntitiesMovementDuration * (dist / NbCells) * 2f;
        while (enlapsed < duration)
        {
            Vector3 pos = Vector3.Lerp(startPos, endPos, enlapsed / duration);
            player.position = pos;
            yield return null;
            lr.SetPosition(0, player.position);
            lr.SetPosition(1, grapple.transform.position);
            enlapsed += Time.deltaTime;
        }

        // destroy grapple
        GameObject.Destroy(grapple);

        // cleannely end turn and place player
        DodgeFromLight.GameManager.PlayerController.PlaceOnCell(DodgeFromLight.CurrentMap.Grid.GetCell(cell), true);
        Events.Fire_PlayerEndChangeCell(lastCell, DodgeFromLight.CurrentMap.Grid.GetCell(cell));
        DodgeFromLight.GameManager.CheckIfReachEndCell();
        DodgeFromLight.StopWaitingAction();

        foreach (Ennemy ennemy in DodgeFromLight.Ennemies)
        {
            ennemy.RefreshPO();
            ennemy.DoEffect();
        }
        DodgeFromLight.GridController.DrawCellsPO();
    }
}