using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public class Axe : Ennemy
{
    int nbTurn;

    public override void Awake()
    {
        base.Awake();
        Name = "Evil Axe";
        Type = EnnemyType.Axe;
        nbTurn = 0;
        AnimateOnKill = false;
        NoLineView = true;
        DisplaySelfPO = true;
        Events.TurnStart -= Events_TurnStart1;
        Events.TurnStart += Events_TurnStart1;
    }

    private void Events_TurnStart1()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        nbTurn++;
        bool right = nbTurn % 2 == 0;
        if (right)
            GoLeft();
        else
            GoRight();
    }

    public override void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart1;
    }

    public override List<CellPos> GetBrutPO()
    {
        bool right = nbTurn % 2 == 0;
        List<CellPos> po = new List<CellPos>();
        po.Add(CurrentCell.GetCellPos());

        switch (Orientation)
        {
            case Orientation.Up:
                if(right)
                {
                    po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y));
                    po.Add(new CellPos(CurrentCell.X + 2, CurrentCell.Y));
                }
                else
                {
                    po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y));
                    po.Add(new CellPos(CurrentCell.X - 2, CurrentCell.Y));
                }
                break;

            case Orientation.Right:
                if (right)
                {
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 1));
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 2));
                }
                else
                {
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 1));
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 2));
                }
                break;

            case Orientation.Down:
                if (right)
                {
                    po.Add(new CellPos(CurrentCell.X - 1, CurrentCell.Y));
                    po.Add(new CellPos(CurrentCell.X - 2, CurrentCell.Y));
                }
                else
                {
                    po.Add(new CellPos(CurrentCell.X + 1, CurrentCell.Y));
                    po.Add(new CellPos(CurrentCell.X + 2, CurrentCell.Y));
                }
                break;

            case Orientation.Left:
                if (right)
                {
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 1));
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y + 2));
                }
                else
                {
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 1));
                    po.Add(new CellPos(CurrentCell.X, CurrentCell.Y - 2));
                }
                break;
        }

        return po;
    }

    void GoRight()
    {
        Play("GoRight");
    }

    void GoLeft()
    {
        Play("GoLeft");
    }
}