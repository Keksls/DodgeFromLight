using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Gloutton : Ennemy
{
    public int AttractionPO = 4;
    public int NbCellsToAttract = 1;
    public List<CellPos> AttractionPOCells;

    public override void Awake()
    {
        base.Awake();
        Name = "Thirsty Gloutton";
        Type = EnnemyType.Gloutton;
        AttractionPO = 4;
        NbCellsToAttract = 1;
        if (TABase == 0)
            TABase = 1;
        if (PO == 0)
            PO = 1;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> po = new List<CellPos>();

        switch (Orientation)
        {
            case Orientation.Up:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + i));
                break;

            case Orientation.Down:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - i));
                break;

            case Orientation.Left:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X - i, CurrentCell.Y));
                break;

            case Orientation.Right:
                for (int i = 1; i <= PO; i++)
                    po.Add(new CellPos(CurrentCell.X + i, CurrentCell.Y));
                break;
        }

        return po;
    }

    private List<CellPos> GetAttractionBrutPO()
    {
        List<CellPos> po = new List<CellPos>();

        switch (Orientation)
        {
            case Orientation.Up:
                for (int i = PO + 1; i <= AttractionPO; i++)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + i));
                break;

            case Orientation.Down:
                for (int i = PO + 1; i <= AttractionPO; i++)
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - i));
                break;

            case Orientation.Left:
                for (int i = PO + 1; i <= AttractionPO; i++)
                    po.Add(new CellPos(CurrentCell.X - i, CurrentCell.Y));
                break;

            case Orientation.Right:
                for (int i = PO + 1; i <= AttractionPO; i++)
                    po.Add(new CellPos(CurrentCell.X + i, CurrentCell.Y));
                break;
        }

        return po;
    }

    public override bool RefreshPO()
    {
        // process attraction PO
        AttractionPOCells = CleanPO(GetAttractionBrutPO());

        return base.RefreshPO();
    }

    public override void DoEffect()
    {
        Grid grid = DodgeFromLight.CurrentMap.Grid;
        string playerName = DodgeFromLight.GameManager?.PlayerController?.Name;
        Cell tmpCell = null;
        foreach (CellPos po in AttractionPOCells)
        {
            tmpCell = grid.GetCell(po);
            if (tmpCell.EntityOnCell != null && tmpCell.EntityOnCell.Name == playerName)
            {
                Entity player = tmpCell.EntityOnCell;
                // move to player and attack
                Play("Attract", true, DodgeFromLight.GameManager.EntitiesMovementDuration);
                DodgeFromLight.StopWaitingAction();
                DodgeFromLight.GridController.DrawCellsPO();
                player.StopMovementsForThisTurn(); // stop the player
                StopMovementsForThisTurn(); // stop ennemy movement for this turn

                // attract player
                Orientation displacementOrientation = DodgeFromLight.CurrentMap.Grid.GetOrientation(tmpCell, CurrentCell);
                Cell targetPlayerCell = grid.GetNextCell(tmpCell, displacementOrientation, NbCellsToAttract);
                DodgeFromLight.StartWaitingAction();
                player.GoToCell(targetPlayerCell, DodgeFromLight.GameManager.EntitiesMovementDuration, () =>
                {
                    RefreshPO();
                    DodgeFromLight.GridController.DrawCellsPO();
                    DodgeFromLight.StopWaitingAction();
                });
            }
        }
    }
}