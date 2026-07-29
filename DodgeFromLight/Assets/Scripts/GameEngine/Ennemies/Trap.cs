using System.Collections.Generic;
using UnityEngine;

public class Trap : Ennemy
{
    int NbCellMinAttract = 1;
    int NbCellMaxAttract = 4;
    int NbCellMinPush = 1;
    int NbCellMaxPush = 4;

    public override void Awake()
    {
        Name = "Oculte Trap";
        Type = EnnemyType.Trap;
        base.Awake();
        CantWalkOnMe = true;
        if (TABase == 0)
            TABase = -9999;
        if (PO == 0)
            PO = 4;
    }

    public void Initialize(string args)
    {
        if (string.IsNullOrEmpty(args))
            return;
        string[] spl = args.Split(';');
        if (spl.Length == 4)
        {
            NbCellMinAttract = int.Parse(spl[0]);
            NbCellMaxAttract = int.Parse(spl[1]);
            NbCellMinPush = int.Parse(spl[2]);
            NbCellMaxPush = int.Parse(spl[3]);
        }
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

    public override void DoEffect()
    {
        Grid grid = GameContext.CurrentMap.Grid;
        string playerName = GameContext.GameManager?.PlayerController?.Name;
        Cell tmpCell = null;
        foreach (CellPos po in CurrentPO)
        {
            tmpCell = grid.GetCell(po);
            if (tmpCell.EntityOnCell != null && tmpCell.EntityOnCell.Name == playerName)
            {
                Entity player = tmpCell.EntityOnCell;
                player.StopMovementsForThisTurn(); // stop the player

                GameContext.GridController.DrawCellsPO();
                Play("Attract", true, .5f);
                GameContext.StopWaitingAction();
                // attract player
                int NbCellsToAttract = Random.Range(NbCellMinAttract, NbCellMaxAttract);
                Orientation displacementOrientation = GameContext.CurrentMap.Grid.GetOrientation(tmpCell, CurrentCell);
                Cell targetPlayerCell = grid.GetNextCell(tmpCell, displacementOrientation, NbCellsToAttract);

                Events.Fire_EntityStartMove(player);
                player.GoToCell(targetPlayerCell, GameContext.GameManager.EntitiesMovementDuration / 2f, () =>
                  {
                    // push player
                    int NbCellsToPush = Random.Range(NbCellMinPush, NbCellMaxPush);
                      Orientation displacementOrientation2 = GameContext.CurrentMap.Grid.GetOrientation(CurrentCell, tmpCell);
                      Cell targetPlayerCell2 = grid.GetNextCell(targetPlayerCell ?? tmpCell, displacementOrientation2, NbCellsToPush);

                      player.GoToCell(targetPlayerCell2, GameContext.GameManager.EntitiesMovementDuration / 2f, () =>
                      {
                          Events.Fire_EntityEndMove(player);
                          CurrentPO = CleanPO(GetBrutPO());
                          GameContext.GridController.DrawCellsPO();
                      });
                  });
            }
        }
    }

    public override void Attack(Grid grid, CellPos p, bool recoverPlace)
    {
    }
}
