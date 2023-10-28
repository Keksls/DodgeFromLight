using DFLCommonNetwork.GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Ennemy : Entity
{
    public Patrol Patrol;
    public int currentWayPointIndex;
    public EnnemyType Type;
    public bool invertWayPoints;
    public List<CellPos> CurrentPO;
    public List<CellPos> CurrentUnreachablePO;
    public CellPos StartCell;
    public Orientation StartOrientation;
    public List<CellPos> WayPoints;
    public HashSet<AwakeningType> ExcludedAwakening;
    public Dictionary<AwakeningType, Awakening> Awakenings;
    public int NbPMUsed = 0;
    public string Arg;
    public bool DisplaySelfPO = false;
    public bool AnimateOnKill = true;
    public bool NoLineView = false;

    public override void Awake()
    {
        base.Awake();
        ExcludedAwakening = new HashSet<AwakeningType>();
        Awakenings = new Dictionary<AwakeningType, Awakening>();
        invertWayPoints = false;
        currentWayPointIndex = 0;
        CurrentPO = new List<CellPos>();
        CurrentUnreachablePO = new List<CellPos>();
        NbPMUsed = 0;
    }

    /// <summary>
    /// Add an awakening to this ennemy
    /// </summary>
    /// <param name="type">awakening to add</param>
    public bool AddAwakening(AwakeningType type)
    {
        if (Awakenings.ContainsKey(type))
            return false;
        Awakenings.Add(type, Awakening.GetAwakening(type));
        GetAwakening(type).DoEffect(this);
        GetAwakening(type).RefreshEffect();
        return true;
    }

    /// <summary>
    /// Get Awakening on this ennemy
    /// </summary>
    /// <param name="type">type of the aawakening to  get</param>
    /// <returns>awakening if ennemy has it</returns>
    public Awakening GetAwakening(AwakeningType type)
    {
        if (Awakenings.ContainsKey(type))
            return Awakenings[type];
        else
            return null;
    }

    /// <summary>
    /// Remove an awakening for this ennemy
    /// </summary>
    /// <param name="type">awakening to remove</param>
    /// <returns>true if removed</returns>
    public bool RemoveAwakening(AwakeningType type)
    {
        return Awakenings.Remove(type);
    }

    /// <summary>
    /// Use all PM for the ennemy
    /// </summary>
    public void UsePM(float speed)
    {
        StopCoroutine("UsePM_Routine");
        StartCoroutine(UsePM_Routine(speed));
    }

    /// <summary>
    /// place the ennemy on the first cell of his patrol routine
    /// </summary>
    public void PlaceOnFirstCell()
    {
        if (Patrol.NbCells == 0)
        {
            Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(StartCell);
            PlaceOnCell(cell, true);
            SetOrientation(StartOrientation);
        }
        else
        {
            Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(Patrol.FirstCell);
            PlaceOnCell(cell, true);
            SetOrientation(Patrol.FirstOrientation);
        }
    }

    internal override void Events_TurnStart()
    {
        // refresh all awakening effects
        foreach (Awakening awakening in Awakenings.Values)
        {
            awakening.RefreshEffect();
            awakening.DoEffect(this);
        }

        base.Events_TurnStart();
    }

    internal override void Events_TurnEnd()
    {
        base.Events_TurnEnd();
        if (CurrentCell != null)
            CurrentCell.EntityOnCell = this;
    }

    /// <summary>
    /// will this ennemy change cell on next move
    /// </summary>
    /// <returns></returns>
    public bool WillChangeCellNextMove()
    {
        if (CurrentTA <= 0)
            return false;
        return !Patrol.SimulateLastCell(CurrentTA, Orientation).Equals(CurrentCell);
    }

    IEnumerator UsePM_Routine(float speed)
    {
        bool refreshAllPO = CurrentTA > 1;

        while (CurrentTA > 0 && Patrol.NbCells > 0)
        {
            CellPos nextCell = Patrol.NextCell();

            if (nextCell.IsInOrientation(CurrentCell.GetCellPos(), Orientation)) // next cell is in ennemy orientation, just walk toward
            { // if ennemy at less than 2 cell, switch my orientation
                bool EnnemyOnMyOrientation = DodgeFromLight.CurrentMap.Grid.IsThereEnnemyOnCell(nextCell);
                if (EnnemyOnMyOrientation || !DodgeFromLight.CurrentMap.Grid.GetCell(nextCell).IsWalkable)
                {
                    Events.Fire_EntityStartMove(this);
                    Patrol.Inverse();
                    Patrol.NextCell();
                    SetOrientation(GetInversedOrientation(Orientation));
                    yield return new WaitForSeconds(speed);
                    CurrentTA--;
                    RefreshPO();
                    DoEffect();
                    Events.Fire_EntityEndMove(this);
                }
                else
                {
                    Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(nextCell);
                    if (cell == null)
                        yield break;

                    // fire walking event
                    Events.Fire_EntityStartMove(this);
                    Moving = true;
                    // cancel current movement
                    StopCoroutine("MoveToCell_Routine");
                    PlaceOnCell(cell, false);
                    // move to target cell
                    yield return StartCoroutine(MoveToCell_Routine(cell, speed, null, null));
                    PlaceOnCell(cell, true);

                    RefreshPO();
                    if (refreshAllPO && CurrentTA > 0)
                        DodgeFromLight.GridController.DrawCellsPO();
                    DoEffect();
                }
            }
            else // next cell is not orientation, need to turn
            {
                Patrol.CancelNextCell();
                Events.Fire_EntityStartMove(this);
                float enlapsed = 0f;
                Vector3 startRot = transform.localEulerAngles;
                Vector3 endRot = DodgeFromLight.CurrentMap.Grid.GetOrientationVector(
                    DodgeFromLight.CurrentMap.Grid.GetOrientation(CurrentCell.GetCellPos(), nextCell));
                float rotWeight = Mathf.Abs(endRot.y - startRot.y);
                float inversedRotWeight = Mathf.Abs((360 + endRot.y) - startRot.y);
                if (inversedRotWeight < rotWeight)
                    endRot.y += 360;
                while (enlapsed < speed)
                {
                    transform.localEulerAngles = Vector3.Lerp(startRot, endRot, enlapsed / speed);
                    enlapsed += Time.deltaTime;
                    yield return null;
                }
                SetOrientation(DodgeFromLight.CurrentMap.Grid.GetOrientation(CurrentCell.GetCellPos(), nextCell));
                CurrentTA--;
                RefreshPO();
                DoEffect();
                Events.Fire_EntityEndMove(this);
            }
            NbPMUsed++;
        }
    }

    /// <summary>
    /// Draw ennemy Path (Must be call on a script attached to a Camera component that implement OnPostRender)
    /// </summary>
    public void DrawPath(Transform tfrm)
    {
        GL.PushMatrix();
        DodgeFromLight.GridController.lineMaterial.SetPass(0);
        GL.MultMatrix(tfrm.localToWorldMatrix);

        // Draw lines
        if (WayPoints.Count > 1)
        {
            GL.Begin(GL.LINES);
            for (int i = 0; i < WayPoints.Count; i++)
            {
                GL.Color(DodgeFromLight.Databases.CellPOData.GetEnnemyColor(Type).UpColor);
                CellPos startCell = WayPoints[i % WayPoints.Count];
                CellPos endCell = WayPoints[(i + 1) % WayPoints.Count];
                GL.Vertex(new Vector3(startCell.X, .04f, startCell.Y));
                GL.Vertex(new Vector3(endCell.X, .04f, endCell.Y));
            }
            GL.End();
        }

        // Draw Quad
        GL.Begin(GL.QUADS);
        float minSize = DodgeFromLight.GridController.PathSquareSize / DodgeFromLight.GridController.PathSquareOffsetSize / 6f;
        float maxSize = DodgeFromLight.GridController.PathSquareSize * DodgeFromLight.GridController.PathSquareOffsetSize / 6f;
        for (float i = 0; i < WayPoints.Count; i++)
        {
            float size = Mathf.Lerp(minSize, maxSize, i / (float)WayPoints.Count);

            GL.Color(DodgeFromLight.Databases.CellPOData.GetEnnemyColor(Type).UpColor);
            CellPos startCell = WayPoints[(int)i];

            GL.Vertex(new Vector3(startCell.X - size, .04f, startCell.Y + size));
            GL.Vertex(new Vector3(startCell.X + size, .04f, startCell.Y + size));
            GL.Vertex(new Vector3(startCell.X + size, .04f, startCell.Y - size));
            GL.Vertex(new Vector3(startCell.X - size, .04f, startCell.Y - size));
        }
        GL.End();

        GL.PopMatrix();
    }

    /// <summary>
    /// Refresh the PO cells and check if player is in range. Return true if Player is in view PO
    /// </summary>
    public virtual bool RefreshPO()
    {
        CurrentPO = CleanPO(GetBrutPO());
        return CheckLightView();
    }

    /// <summary>
    /// Check if player is in the PO
    /// </summary>
    public bool CheckLightView()
    {
        Grid grid = DodgeFromLight.CurrentMap.Grid;
        if (DodgeFromLight.GameManager == null || DodgeFromLight.GameManager.PlayerController == null || DodgeFromLight.GameManager.PlayerController.CurrentCell == null)
            return false;
        CurrentPO.Add(CurrentCell.GetCellPos());
        Cell cell;

        foreach (CellPos p in CurrentPO)
        {
            cell = grid.GetCell(p);
            if (cell.Equals(DodgeFromLight.GameManager._playerController.CurrentCell))
            {
                // move to player and attack
                Attack(grid, p, false);
                CurrentPO.RemoveAt(CurrentPO.Count - 1);
                return true;
            }

            if (cell.GearInUse != null)
            {
                Gear gear = cell.GearInUse;
                switch (gear.Type)
                {
                    case GearType.DiscoBall:
                        ((DiscoBall)gear).Destroy();
                        Stun(((DiscoBall)gear).NbTurnStun + 1);
                        RefreshPO();
                        break;
                }
            }
        }

        if (DodgeFromLight.GameManager._playerController.IsCastingPet) // check for killing pet
        {
            foreach (CellPos p in CurrentPO)
            {
                cell = grid.GetCell(p);
                if (cell.Equals(DodgeFromLight.GameManager._petController.CurrentCell))
                {
                    // move to player and attack
                    Attack(grid, p, true);
                    CurrentPO.RemoveAt(CurrentPO.Count - 1);
                    return true;
                }
            }
        }

        if (CurrentPO.Count > 0)
            CurrentPO.RemoveAt(CurrentPO.Count - 1);
        return false;
    }

    public void Stun(int nbTurn)
    {
        GameObject stunVFX = Instantiate(DodgeFromLight.GameManager.GearDiscoballStunVFXPrefab);
        stunVFX.transform.position = transform.position;
        AddAlteration(new Alteration(AlterationType.PO, -9999, nbTurn));
        AddAlteration(new Alteration(AlterationType.PM, -9999, nbTurn, () => { Destroy(stunVFX); }));
    }

    public virtual void Attack(Grid grid, CellPos p, bool recoverPlace)
    {
        // move to player and attack
        Play("Attack", true);
        PoolManager.Instance.PushBackAllPool(PoolName.POCell);
        if (AnimateOnKill)
        {
            CellPos startCell = CurrentCell.GetCellPos();
            SetOrientation(grid.GetCellOrientation(CurrentCell, grid.GetCell(p)));
            CellPos closestCell = DodgeFromLight.CurrentMap.Grid.GetNearestCell(p, CurrentPO.Concat(new List<CellPos>() { CurrentCell.GetCellPos() }).ToList());
            Events.Fire_EnnemySeePlayer(this, grid.GetCell(p));
            GoToCell(grid.GetCell(closestCell), DodgeFromLight.GameManager.EntitiesMovementDuration, () =>
            {
                DodgeFromLight.GridController.DrawCellsPO();
                LookAtCell(p);
                if (recoverPlace)
                    PlaceOnCell(DodgeFromLight.CurrentMap.Grid.GetCell(startCell), true);
            }, () =>
            {
                LookAtCell(p);
            });
        }
        else
        {
            Events.Fire_EnnemySeePlayer(this, grid.GetCell(p));
        }
    }

    /// <summary>
    /// Get base PO cells for this ennemy
    /// </summary>
    /// <returns></returns>
    public abstract List<CellPos> GetBrutPO();

    /// <summary>
    /// Do spetial effect of this entity, override this to implement effect
    /// </summary>
    public virtual void DoEffect()
    {
    }

    /// <summary>
    /// Clean the base PO to remove out of grid and non walkable cells
    /// Process view line
    /// </summary>
    /// <param name="po">brut PO</param>
    /// <returns>clean PO</returns>
    public List<CellPos> CleanPO(List<CellPos> po)
    {
        List<CellPos> clean = new List<CellPos>();
        Grid grid = DodgeFromLight.CurrentMap.Grid;
        string playerName = DodgeFromLight.GameManager?.PlayerController?.Name;
        Cell c;
        foreach (CellPos p in po)
        {
            c = grid.GetCell(p);
            if (p.X >= 0 && p.Y >= 0 && p.X < grid.Width && p.Y < grid.Height &&
                c.IsWalkable &&
                (c.GearInUse == null || c.GearInUse.Type == GearType.DiscoBall || c.GearInUse.Type != GearType.Mirror) &&
                (c.EntityOnCell == null || (c.EntityOnCell.Name == playerName))) // <= we can see entities
            {
                clean.Add(p);
            }
        }
        if (!NoLineView)
            ProcessLigneDeVue(DodgeFromLight.CurrentMap.Grid, clean);
        if (DisplaySelfPO)
            clean.Add(CurrentCell.GetCellPos());
        return clean;
    }

    /// <summary>
    /// Remove cells that are used by entities or not walkable
    /// </summary>
    /// <param name="grid">the current grid</param>
    /// <param name="po">semi-cleaned PO</param>
    public void ProcessLigneDeVue(Grid grid, List<CellPos> po)
    {
        float step = .5f;
        Vector2 start = new Vector2(CurrentCell.X, CurrentCell.Y);
        HashSet<CellPos> ToRemove = new HashSet<CellPos>();
        int x, y = 0;
        foreach (var cell in po)
        {
            if (cell.X != CurrentCell.X || cell.Y != CurrentCell.Y)
            {
                Vector2 stop = new Vector2(cell.X, cell.Y);
                Vector2 offset = stop - start;
                offset = offset / offset.magnitude * step;
                Vector2 current = new Vector2(start.x, start.y);

                // tant qu'on n'a pas atteint la cellule ciblée
                while ((int)Math.Round(current.x) != cell.X || (int)Math.Round(current.y) != cell.Y)
                {
                    current += offset;
                    x = (int)Math.Round(current.x);
                    y = (int)Math.Round(current.y);

                    if ((x != CurrentCell.X || y != CurrentCell.Y) && (!grid.Cells[x, y].IsWalkable || grid.Cells[x, y].EntityOnCell != null))
                    {
                        if (x == cell.X && y == cell.Y && grid.Cells[x, y].EntityOnCell != null)
                        { }
                        else
                            ToRemove.Add(cell);
                        break;
                    }
                }
            }
        }
        CurrentUnreachablePO = new List<CellPos>();
        foreach (var cell in ToRemove)
        {
            po.Remove(cell);
            CurrentUnreachablePO.Add(cell);
        }
    }

    /// <summary>
    /// return the inverse of the giver orientation
    /// </summary>
    /// <param name="dir">orientation to inverse</param>
    /// <returns>inversed orientation</returns>
    public Orientation GetInversedOrientation(Orientation dir)
    {
        switch (dir)
        {
            default:
            case Orientation.None:
                return Orientation.None;

            case Orientation.Up:
                return Orientation.Down;
            case Orientation.Right:
                return Orientation.Left;
            case Orientation.Down:
                return Orientation.Up;
            case Orientation.Left:
                return Orientation.Right;
        }
    }
}

public enum EnnemyType
{
    Cyclops = 0,
    Gloutton = 1,
    Keeper = 2,
    Knight = 3,
    Scoute = 4,
    Trap = 5,
    Lizardman = 6,
    Thunderbird = 7,
    Axe = 8,
    Hammer = 9,
    Catapult = 10,
    Pillar = 11
}

public class SerializableEnnemy
{
    public EnnemyType Type { get; set; }
    public Orientation StartOrientation { get; set; }
    public CellPos StartPos { get; set; }
    public List<CellPos> WayPoints { get; set; }
    public int PO { get; set; }
    public int TA { get; set; }
    public string Args { get; set; }

    public SerializableEnnemy() { }

    public Ennemy ToEnnemy(GameObject go, Grid grid)
    {
        Ennemy ennemy = null;
        switch (Type)
        {
            case EnnemyType.Cyclops:
                ennemy = go.AddComponent<Cyclops>();
                break;
            case EnnemyType.Gloutton:
                ennemy = go.AddComponent<Gloutton>();
                break;
            case EnnemyType.Keeper:
                ennemy = go.AddComponent<Keeper>();
                break;
            case EnnemyType.Knight:
                ennemy = go.AddComponent<Knight>();
                break;
            case EnnemyType.Scoute:
                ennemy = go.AddComponent<Scoute>();
                break;
            case EnnemyType.Trap:
                ennemy = go.AddComponent<Trap>();
                ((Trap)(ennemy)).Initialize(Args);
                break;
            case EnnemyType.Lizardman:
                ennemy = go.AddComponent<Lizardman>();
                break;
            case EnnemyType.Thunderbird:
                ennemy = go.AddComponent<Thunderbird>();
                break;
            case EnnemyType.Axe:
                ennemy = go.AddComponent<Axe>();
                break;
            case EnnemyType.Hammer:
                ennemy = go.AddComponent<Hamer>();
                break;
            case EnnemyType.Catapult:
                ennemy = go.AddComponent<Catapult>();
                break;
            case EnnemyType.Pillar:
                ennemy = go.AddComponent<Pillar>();
                break;
        }

        ennemy.Type = Type;
        ennemy.StartCell = StartPos;
        ennemy.StartOrientation = StartOrientation;
        ennemy.Patrol = new Patrol();
        ennemy.Patrol.SetWayPoints(WayPoints, grid);
        ennemy.WayPoints = WayPoints;
        ennemy.PO = PO;
        ennemy.TABase = TA;
        return ennemy;
    }
}

public class Patrol
{
    public List<CellPos> Cells;
    public int CurrentIndex { get; private set; }
    public bool Inversed { get; private set; }
    public CellPos FirstCell { get; private set; }
    public Orientation FirstOrientation { get; private set; }
    public int NbCells { get { return Cells.Count; } }

    public Patrol()
    {
        Cells = new List<CellPos>();
        Inversed = false;
        CurrentIndex = 0;
    }

    public void Reset()
    {
        Inversed = false;
        CurrentIndex = 0;
    }

    public void Inverse()
    {
        Inversed = !Inversed;
    }

    public Orientation GetInversedOrientation(Orientation dir)
    {
        switch (dir)
        {
            default:
            case Orientation.None:
                return Orientation.None;

            case Orientation.Up:
                return Orientation.Down;
            case Orientation.Right:
                return Orientation.Left;
            case Orientation.Down:
                return Orientation.Up;
            case Orientation.Left:
                return Orientation.Right;
        }
    }

    public CellPos NextCell()
    {
        if (!Inversed) // normal patrol
        {
            CurrentIndex++;
            if (CurrentIndex == Cells.Count)
                CurrentIndex = 0;
        }
        else // inversed patrol
        {
            CurrentIndex--;
            if (CurrentIndex < 0)
                CurrentIndex = Cells.Count - 1;
        }
        return Cells[CurrentIndex];
    }

    public void CancelNextCell()
    {
        if (Inversed) // inversed patrol
        {
            CurrentIndex++;
            if (CurrentIndex == Cells.Count)
                CurrentIndex = 0;
        }
        else // normal patrol
        {
            CurrentIndex--;
            if (CurrentIndex < 0)
                CurrentIndex = Cells.Count - 1;
        }
    }

    public CellPos GetCurrentCell()
    {
        return Cells[CurrentIndex];
    }

    public List<CellPos> SimulateNextCells(int nbTA, Orientation startOrientation)
    {
        List<CellPos> cells = new List<CellPos>();
        int index = CurrentIndex;
        Orientation dir = startOrientation;
        CellPos lastCell = Cells[CurrentIndex];
        CellPos nextCell = lastCell;

        for (int i = 0; i < nbTA; i++)
        {
            nextCell = NextCell();
            if (!nextCell.IsInOrientation(lastCell, dir))
            {
                dir = GetOrientation(lastCell, nextCell);
                i++;
            }
            if (i < nbTA)
            {
                cells.Add(nextCell);
                lastCell = nextCell;
            }
        }
        CurrentIndex = index;
        return cells;
    }

    public List<CellPos> SimulateNextCellsWithoutOrientation(int nbTA)
    {
        List<CellPos> cells = new List<CellPos>();
        int index = CurrentIndex;
        CellPos lastCell = Cells[CurrentIndex];
        for (int i = 0; i < nbTA; i++)
        {
            lastCell = NextCell();
            cells.Add(lastCell);
        }
        CurrentIndex = index;
        return cells;
    }

    public Orientation GetOrientation(CellPos a, CellPos b)
    {
        if (a.X < b.X)
            return Orientation.Right;
        else if (a.X > b.X)
            return Orientation.Left;
        else if (a.Y < b.Y)
            return Orientation.Up;
        else if (a.Y > b.Y)
            return Orientation.Down;

        return Orientation.None;
    }

    public CellPos SimulateLastCell(int nbTA, Orientation startOrientation)
    {
        int index = CurrentIndex;
        Orientation dir = startOrientation;
        CellPos lastCell = Cells[CurrentIndex];
        CellPos nextCell = lastCell;

        for (int i = 0; i < nbTA; i++)
        {
            nextCell = NextCell();
            if (!nextCell.IsInOrientation(lastCell, dir))
            {
                dir = GetOrientation(lastCell, nextCell);
                i++;
            }
            if (i < nbTA)
                lastCell = nextCell;
        }
        CurrentIndex = index;
        return lastCell;
    }

    public CellPos SimulateLastCellWithoutOrientation(int nbTA, Orientation startOrientation)
    {
        int index = CurrentIndex;
        Orientation dir = startOrientation;
        CellPos lastCell = Cells[CurrentIndex];

        for (int i = 0; i < nbTA; i++)
            lastCell = NextCell();
        CurrentIndex = index;
        return lastCell;
    }

    public void SetWayPoints(List<CellPos> cells, Grid grid)
    {
        if (cells.Count < 2)
            return;

        FirstOrientation = grid.GetOrientation(cells[0], cells[1]);
        FirstCell = cells[0];

        Cells = new List<CellPos>();
        int x, y, ystart, yend, xstart, xend;
        for (int i = 0; i < cells.Count; i++)
        {
            Orientation orientation = grid.GetOrientation(cells[i], cells[nextIndex()]);
            switch (orientation)
            {
                case Orientation.Up:
                    ystart = cells[i].Y;
                    yend = cells[nextIndex()].Y;
                    x = cells[i].X;
                    for (y = ystart; y < yend; y++)
                        Cells.Add(new CellPos(x, y));
                    break;

                case Orientation.Right:
                    xstart = cells[i].X;
                    xend = cells[nextIndex()].X;
                    y = cells[i].Y;
                    for (x = xstart; x < xend; x++)
                        Cells.Add(new CellPos(x, y));
                    break;

                case Orientation.Down:
                    ystart = cells[i].Y;
                    yend = cells[nextIndex()].Y;
                    x = cells[i].X;
                    for (y = ystart; y > yend; y--)
                        Cells.Add(new CellPos(x, y));
                    break;

                case Orientation.Left:
                    xstart = cells[i].X;
                    xend = cells[nextIndex()].X;
                    y = cells[i].Y;
                    for (x = xstart; x > xend; x--)
                        Cells.Add(new CellPos(x, y));
                    break;
            }

            int nextIndex() { return (i + 1) % cells.Count; }
        }
    }
}