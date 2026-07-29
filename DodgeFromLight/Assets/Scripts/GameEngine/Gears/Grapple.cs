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
        CellPos target = GameContext.CurrentMap.Grid.GetFarestCell(GameContext.GameManager.PlayerController.CurrentCell.GetCellPos(),
            GameContext.GameManager.PlayerController.Orientation, NbCells, InLineCellType.Grapple);
        bool sameCell = target.Equals(GameContext.GameManager.PlayerController.CurrentCell.GetCellPos());
        if (!sameCell)
        {
            GameContext.GameManager.PlayerMove(Orientation.None);
            // spawn grapple
            GameObject grapple = GameObject.Instantiate(GameContext.GameManager.GearGrapplePrefab);
            Vector3 pos = GameContext.GameManager.PlayerController.transform.position;
            pos.y += 0.35f;
            grapple.transform.position = pos;
            grapple.transform.eulerAngles = GameContext.CurrentMap.Grid.GetOrientationVector(GameContext.GameManager.PlayerController.Orientation);
            // throw the grapple
            GameContext.GameManager.StartCoroutine(ThrowGrapple(target, grapple));

            // throw current gear
            GameContext.GameManager._playerController.ThrowGear(Side);
        }
        return true;
    }

    IEnumerator ThrowGrapple(CellPos cell, GameObject grapple)
    {
        Events.Fire_PlayerStartChangeCell(GameContext.GameManager.PlayerController.CurrentCell, GameContext.CurrentMap.Grid.GetCell(cell));
        float dist = cell.Distance(GameContext.GameManager.PlayerController.CurrentCell.GetCellPos());
        Transform player = GameContext.GameManager.PlayerController.transform;
        GameContext.StartWaitingAction();
        float duration = GameContext.GameManager.EntitiesMovementDuration * (dist / NbCells);
        float enlapsed = 0f;
        Vector3 startPos = player.position;
        startPos.y = 0.35f;
        Vector3 endPos = cell.ToVector3(0f);
        endPos.y = 0.35f;
        Cell lastCell = GameContext.GameManager.PlayerController.CurrentCell;
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
        duration = GameContext.GameManager.EntitiesMovementDuration * (dist / NbCells) * 2f;
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
        GameContext.GameManager.PlayerController.PlaceOnCell(GameContext.CurrentMap.Grid.GetCell(cell), true);
        Events.Fire_PlayerEndChangeCell(lastCell, GameContext.CurrentMap.Grid.GetCell(cell));
        GameContext.GameManager.CheckIfReachEndCell();
        GameContext.StopWaitingAction();

        foreach (Ennemy ennemy in GameContext.Ennemies)
        {
            ennemy.RefreshPO();
            ennemy.DoEffect();
        }
        GameContext.GridController.DrawCellsPO();
    }
}