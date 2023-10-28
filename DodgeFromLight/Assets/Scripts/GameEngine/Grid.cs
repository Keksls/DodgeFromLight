using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
    public bool LobbyMap { get; set; }
    public Cell[,] Cells { get; set; }
    public int Width { get { return Cells.GetLength(0); } }
    public int Height { get { return Cells.GetLength(1); } }
    public Cell StartCell { get; set; }
    public Cell EndCell { get; set; }
    public EnvironmentSettings Environment { get; set; }
    public List<SerializableEnnemy> Ennemies { get; set; }
    public List<SerializableMovingPlatform> MovingPlatforms { get; set; }

    public EnvironmentSettings GetEnvironment()
    {
        if (Environment == null)
        {
            Environment = /*DodgeFromLight.EnvironmentController.GetBuildinEnvironment(BuildInEnvironments.Default);//*/ new EnvironmentSettings();
            Environment.SetDefaultValues();
        }
        return Environment;
    }

    /// <summary>
    /// Get all the neighbors of a given tile in the grid.
    /// </summary>
    /// <PFram name="node">Node to get neighbots for.</PFram>
    /// <returns>List of node neighbors.</returns>
    public List<Cell> GetNeighbours(Cell node, Grid CurrentMap, bool Horizontal = false)
    {
        List<Cell> Neighbours_Tmp = GetLinearNeibours(node, CurrentMap);

        if (Horizontal)
            Neighbours_Tmp.AddRange(GetHorizontalNeibours(node, CurrentMap));

        List<Cell> Neighbours = new List<Cell>();
        foreach (var n in Neighbours_Tmp)
            if (CurrentMap.Cells[n.X, n.Y].IsWalkable)
                Neighbours.Add(n);

        return Neighbours;
    }

    public List<Cell> GetLinearNeibours(Cell node, Grid grid)
    {
        List<Cell> Neighbours = new List<Cell>();

        Cell c = GetNeighborCell(node, grid, -1, 0);
        if (c != null)
            Neighbours.Add(c);
        c = GetNeighborCell(node, grid, 1, 0);
        if (c != null)
            Neighbours.Add(c);
        c = GetNeighborCell(node, grid, 0, -1);
        if (c != null)
            Neighbours.Add(c);
        c = GetNeighborCell(node, grid, 0, 1);
        if (c != null)
            Neighbours.Add(c);

        return Neighbours;
    }

    public List<Cell> GetHorizontalNeibours(Cell node, Grid grid)
    {
        List<Cell> Neighbours = new List<Cell>();

        Cell c = GetNeighborCell(node, grid, -1, -1);
        if (c != null)
            Neighbours.Add(c);
        c = GetNeighborCell(node, grid, 1, 1);
        if (c != null)
            Neighbours.Add(c);
        c = GetNeighborCell(node, grid, 1, -1);
        if (c != null)
            Neighbours.Add(c);
        c = GetNeighborCell(node, grid, -1, 1);
        if (c != null)
            Neighbours.Add(c);

        return Neighbours;
    }

    public Cell GetNeighborCell(Cell cell, Grid grid, int _x, int _y)
    {
        int x = cell.X + _x;
        int y = cell.Y + _y;

        if (grid.CellExist(x, y))
            return grid.GetCell(x, y);
        else
            return null;
    }

    /// <summary>
    /// Create an empty fully walkable grid based on size (width height)
    /// </summary>
    /// <param name="width">width (x size) of the grid</param>
    /// <param name="height">height (y size) of the grid</param>
    public Grid(int width, int height)
    {
        Cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Cells[x, y] = new Cell(x, y);
            }
        Ennemies = new List<SerializableEnnemy>();
        MovingPlatforms = new List<SerializableMovingPlatform>();
        SetStart(GetCell(0, 0));
        SetEnd(GetCell(width - 1, Height - 1));
    }

    /// <summary>
    /// Clear all cells before saving grid
    /// </summary>
    public void ClearCellsForSave()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                Cells[x, y].EntityOnCell = null;
                Cells[x, y].RemoveUsedGearOnCell();
            }
    }

    /// <summary>
    /// Set Start Cell
    /// </summary>
    /// <param name="cell">start cell</param>
    public void SetStart(Cell cell)
    {
        StartCell = cell;

    }

    /// <summary>
    /// Set end Cell
    /// </summary>
    /// <param name="cell">end cell</param>
    public void SetEnd(Cell cell)
    {
        EndCell = cell;
    }

    /// <summary>
    /// is it the Start cell
    /// </summary>
    /// <param name="cell">cell to check</param>
    /// <returns></returns>
    public bool IsStart(Cell cell)
    {
        return StartCell != null && StartCell.X == cell.X && StartCell.Y == cell.Y;
    }

    /// <summary>
    /// is it the End cell
    /// </summary>
    /// <param name="cell">cell to check</param>
    /// <returns></returns>
    public bool IsEnd(Cell cell)
    {
        return EndCell != null && EndCell.X == cell.X && EndCell.Y == cell.Y;
    }

    /// <summary>
    /// get the next target cell based on cell and orientation
    /// </summary>
    /// <param name="cell">the current cell</param>
    /// <param name="orientation">orientation to move on</param>
    /// <returns></returns>
    public Cell GetNextCell(Cell cell, Orientation orientation, int PM)
    {
        return GetNextCell(cell.GetCellPos(), orientation, PM);
    }

    /// <summary>
    /// get the next target cell based on cell and orientation
    /// </summary>
    /// <param name="cell">the current cell</param>
    /// <param name="orientation">orientation to move on</param>
    /// <returns></returns>
    public Cell GetNextCell(DFLCommonNetwork.GameEngine.CellPos cell, Orientation orientation, int PM, bool ignoreEnnemy = false, bool checkPlatforms = false)
    {
        int x = cell.X;
        int y = cell.Y;

        Cell targetCell = null;

        for (int i = 0; i < PM; i++)
        {
            switch (orientation)
            {
                case Orientation.Up:
                    y++;
                    break;
                case Orientation.Down:
                    y--;
                    break;
                case Orientation.Left:
                    x--;
                    break;
                case Orientation.Right:
                    x++;
                    break;
            }

            // check if cell don't exist
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                break;

            // check if cell is walkable
            if (!Cells[x, y].IsWalkable)
            {
                if (checkPlatforms)
                {
                    bool canGo = false;
                    foreach (var platform in DodgeFromLight.MovingPlatforms)
                    {
                        if (platform.Patrol.SimulateLastCellWithoutOrientation(platform.CurrentTA, platform.Orientation).Equals(x, y))
                            canGo = true;
                    }
                    if (!canGo)
                        break;
                }
                else
                    break;
            }

            bool willMove = false;
            // check if cell is empty
            if (Cells[x, y].EntityOnCell != null && (!ignoreEnnemy || Cells[x, y].EntityOnCell.CantWalkOnMe))
            {
                if (Cells[x, y].EntityOnCell is Ennemy) // if there is an ennemy on the cell, check if ennemy will move
                    if (!((Ennemy)Cells[x, y].EntityOnCell).WillChangeCellNextMove())
                        break;
                    else
                        willMove = true;
            }

            if (Cells[x, y].EntityOnCell != null && ignoreEnnemy && Cells[x, y].EntityOnCell.CantWalkOnMe && !willMove)
                break;

            // set target cell
            targetCell = Cells[x, y];
        }

        return targetCell;
    }

    public bool CanGoOnCell(Cell cell)
    {
        int x = cell.X;
        int y = cell.Y;
        // check if cell don't exist
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return false;

        // check if cell is walkable
        if (!Cells[x, y].IsWalkable)
            return false;

        // check if cell is empty
        if (Cells[x, y].EntityOnCell != null)
            return false;

        return true;
    }

    /// <summary>
    /// get the orientation to have for going from cell a to b
    /// </summary>
    /// <param name="a">cell a (start)</param>
    /// <param name="b">cell b (target)</param>
    /// <returns></returns>
    public Orientation GetOrientation(DFLCommonNetwork.GameEngine.CellPos a, DFLCommonNetwork.GameEngine.CellPos b)
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

    /// <summary>
    /// get the orientation to have for going from cell a to b
    /// </summary>
    /// <param name="a">cell a (start)</param>
    /// <param name="b">cell b (target)</param>
    /// <returns></returns>
    public Orientation GetOrientation(Cell a, Cell b)
    {
        return GetOrientation(a.GetCellPos(), b.GetCellPos());
    }

    /// <summary>
    /// Is the given pois inside the grid
    /// </summary>
    /// <param name="x">x pos</param>
    /// <param name="y">y pos</param>
    /// <returns></returns>
    public bool IsInGrid(int x, int y)
    {
        return x >= 0 || y >= 0 || x < Width || y < Height;
    }

    /// <summary>
    /// determinate if there is an ennemy on this cell. If yes, determinate if ennemy will stil be here at the end of the turn
    /// </summary>
    /// <param name="cell">cell to check</param>
    /// <returns></returns>
    public bool IsThereEnnemyOnCell(DFLCommonNetwork.GameEngine.CellPos cell)
    {
        int x = cell.X;
        int y = cell.Y;

        // check if cell is empty
        Ennemy en;
        if (Cells[x, y].EntityOnCell != null && Cells[x, y].EntityOnCell is Ennemy)
        {
            en = ((Ennemy)Cells[x, y].EntityOnCell);
            if (en.CurrentTA == 0 || en.Patrol.NbCells == 0)
                return true;
            List<DFLCommonNetwork.GameEngine.CellPos> ennemyCells = en.Patrol.SimulateNextCells(en.CurrentTA, en.Orientation);
            if (ennemyCells.Count > 0)
            {
                foreach (DFLCommonNetwork.GameEngine.CellPos c in ennemyCells)
                {
                    if (c.X == x && c.Y == y)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// determinate if there is an ennemy on cells behind the given cell (defined by the giver orientation) on NbCell cells behind
    /// </summary>
    /// <param name="cell">the start cell</param>
    /// <param name="orientation">orientation to go throw</param>
    /// <param name="NbCell">nb cells to check</param>
    /// <returns></returns>
    public bool IsThereEnnemyOnOrientation(Cell cell, Orientation orientation, int NbCell)
    {
        int x = cell.X;
        int y = cell.Y;

        for (int i = 0; i < NbCell; i++)
        {
            switch (orientation)
            {
                case Orientation.Up:
                    y++;
                    break;
                case Orientation.Down:
                    y--;
                    break;
                case Orientation.Left:
                    x--;
                    break;
                case Orientation.Right:
                    x++;
                    break;
            }

            // check if cell don't exist
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                break;

            // check if cell is empty
            if (IsThereEnnemyOnCell(new DFLCommonNetwork.GameEngine.CellPos(x, y)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// get the orientation to have for going from cell a to b
    /// </summary>
    /// <param name="a">cell a (start)</param>
    /// <param name="b">cell b (target)</param>
    /// <returns></returns>
    public Orientation GetCellOrientation(Cell a, Cell b)
    {
        if (a.X < b.X && a.Y == b.Y)
            return Orientation.Right;
        else if (a.X < b.X && a.Y == b.Y)
            return Orientation.Left;
        else if (a.Y < b.Y && a.X == b.X)
            return Orientation.Up;
        else if (a.Y > b.Y && a.X == b.X)
            return Orientation.Down;

        return Orientation.None;
    }

    /// <summary>
    /// Get Vector3 according to the given orientation
    /// </summary>
    /// <param name="orientation">orientation</param>
    /// <returns></returns>
    public Vector3 GetOrientationVector(Orientation orientation)
    {
        switch (orientation)
        {
            default:
            case Orientation.None:
                return Vector3.zero;
            case Orientation.Up:
                return Vector3.up * 0f;
            case Orientation.Down:
                return Vector3.up * 180f;
            case Orientation.Left:
                return Vector3.up * -90f;
            case Orientation.Right:
                return Vector3.up * 90f;
        }
    }

    public Orientation AddOrientations(Orientation a, Orientation b)
    {
        Orientation dir = a;
        if (a != Orientation.None)
        {
            switch (b)
            {
                case Orientation.Left:
                    switch (a)
                    {
                        case Orientation.Up:
                            dir = Orientation.Right;
                            break;
                        case Orientation.Right:
                            dir = Orientation.Down;
                            break;
                        case Orientation.Down:
                            dir = Orientation.Left;
                            break;
                        case Orientation.Left:
                            dir = Orientation.Up;
                            break;
                    }
                    break;

                case Orientation.Down:
                    switch (a)
                    {
                        case Orientation.Up:
                            dir = Orientation.Down;
                            break;
                        case Orientation.Right:
                            dir = Orientation.Left;
                            break;
                        case Orientation.Down:
                            dir = Orientation.Up;
                            break;
                        case Orientation.Left:
                            dir = Orientation.Right;
                            break;
                    }
                    break;

                case Orientation.Right:
                    switch (a)
                    {
                        case Orientation.Up:
                            dir = Orientation.Left;
                            break;
                        case Orientation.Right:
                            dir = Orientation.Up;
                            break;
                        case Orientation.Down:
                            dir = Orientation.Right;
                            break;
                        case Orientation.Left:
                            dir = Orientation.Down;
                            break;
                    }
                    break;
            }
        }
        return dir;
    }

    /// <summary>
    /// Get Orientation according to the given direction
    /// </summary>
    /// <param name="dir">dir</param>
    /// <returns></returns>
    public Orientation GetOrientationFromVector(Vector3 dir)
    {
        if (dir.x > 0)
            return Orientation.Right;
        else if (dir.x < 0)
            return Orientation.Left;
        else if (dir.z > 0)
            return Orientation.Up;
        else if (dir.z < 0)
            return Orientation.Down;

        return Orientation.None;
    }

    /// <summary>
    /// get the inverse of the given orientation
    /// </summary>
    /// <param name="orientation">base orientation</param>
    /// <returns></returns>
    public Orientation GetInverseOrientation(Orientation orientation)
    {
        Orientation inverse = Orientation.None;
        switch (orientation)
        {
            case Orientation.Up:
                return Orientation.Down;
            case Orientation.Down:
                return Orientation.Up;
            case Orientation.Left:
                return Orientation.Right;
            case Orientation.Right:
                return Orientation.Left;
        }
        return inverse;
    }

    /// <summary>
    /// Check if a cell exist in this grid
    /// </summary>
    /// <param name="pos">cell pos</param>
    /// <returns></returns>
    public bool CellExist(DFLCommonNetwork.GameEngine.CellPos pos)
    {
        return CellExist(pos.X, pos.Y);
    }

    /// <summary>
    /// checkif a cell exist in this grid
    /// </summary>
    /// <param name="x">x pos</param>
    /// <param name="y">y pos</param>
    /// <returns></returns>
    public bool CellExist(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    /// <summary>
    /// get a cell
    /// </summary>
    /// <param name="x">x pos</param>
    /// <param name="y">y pos</param>
    /// <returns></returns>
    public Cell GetCell(int x, int y)
    {
        if (CellExist(x, y))
            return Cells[x, y];
        else return null;
    }

    /// <summary>
    /// get a cell
    /// </summary>
    /// <param name="pos">cell pos</param>
    /// <returns></returns>
    public Cell GetCell(DFLCommonNetwork.GameEngine.CellPos pos)
    {
        return GetCell(pos.X, pos.Y);
    }

    /// <summary>
    /// Add an ennemy to the grid
    /// </summary>
    /// <param name="type">type of the ennemy</param>
    /// <param name="pos">start pos of the ennemy</param>
    /// <returns></returns>
    public SerializableEnnemy AddEnnemy(EnnemyType type, DFLCommonNetwork.GameEngine.CellPos pos)
    {
        SerializableEnnemy ennemy = new SerializableEnnemy()
        {
            StartPos = pos,
            Type = type,
            WayPoints = new List<DFLCommonNetwork.GameEngine.CellPos>(),
            StartOrientation = Orientation.Up
        };
        switch (type)
        {
            case EnnemyType.Cyclops:
                ennemy.PO = 3;
                ennemy.TA = 1;
                break;
            case EnnemyType.Gloutton:
                ennemy.PO = 1;
                ennemy.TA = 1;
                break;
            case EnnemyType.Keeper:
                ennemy.PO = 2;
                ennemy.TA = 0;
                break;
            case EnnemyType.Knight:
                ennemy.PO = 1;
                ennemy.TA = 1;
                break;
            case EnnemyType.Scoute:
                ennemy.PO = 3;
                ennemy.TA = 1;
                break;
            case EnnemyType.Trap:
                ennemy.PO = 4;
                ennemy.TA = 0;
                break;
            case EnnemyType.Lizardman:
                ennemy.PO = 2;
                ennemy.TA = 1;
                break;
            case EnnemyType.Thunderbird:
                ennemy.PO = 1;
                ennemy.TA = 1;
                break;
            case EnnemyType.Axe:
                ennemy.PO = 0;
                ennemy.TA = 0;
                break;
            case EnnemyType.Hammer:
                ennemy.PO = 0;
                ennemy.TA = 0;
                break;
            case EnnemyType.Catapult:
                ennemy.PO = 0;
                ennemy.TA = 0;
                break;
            case EnnemyType.Pillar:
                ennemy.PO = 0;
                ennemy.TA = 0;
                break;
            default:
                break;
        }
        Ennemies.Add(ennemy);
        return ennemy;
    }

    public SerializableMovingPlatform AddMovingPlatform(DFLCommonNetwork.GameEngine.CellPos cell)
    {
        SerializableMovingPlatform platform = new SerializableMovingPlatform()
        {
            StartOrientation = Orientation.Up,
            StartPos = cell,
            TA = 1,
            WayPoints = new List<DFLCommonNetwork.GameEngine.CellPos>()
        };
        MovingPlatforms.Add(platform);
        return platform;
    }

    public bool RemoveMovingPlatform(DFLCommonNetwork.GameEngine.CellPos pos)
    {
        SerializableMovingPlatform latform = null;
        foreach (var se in MovingPlatforms)
            if (se.StartPos.ToString().Equals(pos.ToString()) ||
                (se.WayPoints != null && se.WayPoints.Count > 0 &&
                se.WayPoints[0].X == pos.X && se.WayPoints[0].Y == pos.Y))
                latform = se;
        if (latform != null)
            MovingPlatforms.Remove(latform);
        return latform != null;
    }

    /// <summary>
    /// Remove an ennemy from the grid
    /// </summary>
    /// <param name="pos">start Pos of  the ennemy to remove</param>
    /// <returns>true if success</returns>
    public bool RemoveEnnemy(DFLCommonNetwork.GameEngine.CellPos pos)
    {
        SerializableEnnemy ennemy = null;
        foreach (var se in Ennemies)
            if (se.StartPos.ToString().Equals(pos.ToString()) ||
                (se.WayPoints != null && se.WayPoints.Count > 0 &&
                se.WayPoints[0].X == pos.X && se.WayPoints[0].Y == pos.Y))
                ennemy = se;
        if (ennemy != null)
            Ennemies.Remove(ennemy);
        return ennemy != null;
    }

    /// <summary>
    /// get the nearset cell from a list
    /// </summary>
    /// <param name="targetCell">the cell to reach</param>
    /// <param name="AllowedCells">cells to check</param>
    /// <returns>nearset cell pos</returns>
    public CellPos GetNearestCell(CellPos targetCell, List<CellPos> AllowedCells)
    {
        CellPos cell = AllowedCells[0];
        float minDist = float.MaxValue;
        foreach (CellPos c in AllowedCells)
        {
            float dist = Vector2Int.Distance(new Vector2Int(c.X, c.Y), new Vector2Int(targetCell.X, targetCell.Y));
            if (dist < minDist && dist != 0)
            {
                minDist = dist;
                cell = c;
            }
        }
        return cell;
    }

    /// <summary>
    /// Get ennemy on cell if there is one
    /// </summary>
    /// <param name="cell">cell to get ennemy on</param>
    /// <returns></returns>
    public SerializableEnnemy GetEnnemyOnCell(Cell cell)
    {
        return GetEnnemyOnCell(cell.GetCellPos());
    }

    /// <summary>
    /// Get ennemy on cell if there is one
    /// </summary>
    /// <param name="cell">cell to get ennemy on</param>
    /// <returns></returns>
    public SerializableEnnemy GetEnnemyOnCell(DFLCommonNetwork.GameEngine.CellPos cell)
    {
        foreach (var ennemy in Ennemies)
        {
            if (ennemy.StartPos.ToString().Equals(cell.ToString()))
                return ennemy;
        }
        return null;
    }

    /// <summary>
    /// Set ennemy PO on cell if there is one
    /// </summary>
    /// <param name="cell">cell to get ennemy on</param>
    /// <returns></returns>
    public bool SetEnnemyPO(DFLCommonNetwork.GameEngine.CellPos cell, int PO)
    {
        foreach (var ennemy in Ennemies)
        {
            if (ennemy.StartPos.ToString().Equals(cell.ToString()))
            {
                ennemy.PO = PO;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Set ennemy PO on cell if there is one
    /// </summary>
    /// <param name="cell">cell to get ennemy on</param>
    /// <returns></returns>
    public bool SetEnnemyTA(DFLCommonNetwork.GameEngine.CellPos cell, int TA)
    {
        foreach (var ennemy in Ennemies)
        {
            if (ennemy.StartPos.ToString().Equals(cell.ToString()))
            {
                ennemy.TA = TA;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Validate the grid and return error messages
    /// </summary>
    /// <param name="ErrorMessages">ref error list</param>
    /// <returns></returns>
    public bool ValidateGrid(ref List<string> ErrorMessages)
    {
        if (EndCell == null)
            ErrorMessages.Add("No End cell");

        if (StartCell == null)
            ErrorMessages.Add("No Start cell");

        foreach (var se in Ennemies)
        {
            if (se.Type == EnnemyType.Cyclops ||
                se.Type == EnnemyType.Gloutton ||
                se.Type == EnnemyType.Knight ||
                se.Type == EnnemyType.Scoute)
            {
                if (se.WayPoints.Count > 0)
                {
                    for (int i = 0; i < se.WayPoints.Count; i++)
                    {
                        CellPos startCell = se.WayPoints[i % se.WayPoints.Count];
                        CellPos endCell = se.WayPoints[(i + 1) % se.WayPoints.Count];
                        if (!startCell.IsInLine(endCell))
                        {
                            ErrorMessages.Add(se.Type.ToString() + " has invalide AI Path (contains diagonal)");
                            break;
                        }
                    }
                }
            }
        }

        for (int x = 0; x < DodgeFromLight.CurrentMap.Grid.Width; x++)
            for (int y = 0; y < DodgeFromLight.CurrentMap.Grid.Width; y++)
            {
                Cell c = DodgeFromLight.CurrentMap.Grid.GetCell(x, y);
                if (c != null && !string.IsNullOrEmpty(c.ElementID))
                {
                    var mdd = DodgeFromLight.Databases.ResourcesData.GetMapDeco(c.ElementID);
                    foreach (var unwalkable in mdd.UnWalkableCells)
                    {
                        // get cell pos acording to element orientation
                        Vector3 pos = new Vector3(unwalkable.X, 0, unwalkable.Y);
                        pos = Quaternion.Euler(0, c.ElementOrientation, 0) * pos;
                        DFLCommonNetwork.GameEngine.CellPos p = new DFLCommonNetwork.GameEngine.CellPos(c.X + Mathf.RoundToInt(pos.x), c.Y + Mathf.RoundToInt(pos.z));

                        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(p);
                        if (cell == null)
                        {
                            ErrorMessages.Add("Some elements have bounds out of the map grid");
                            break;
                        }
                    }
                }
            }

        return ErrorMessages.Count == 0;
    }

    public bool ValidateEnnemyPath(Ennemy se)
    {
        if (se.Type == EnnemyType.Cyclops ||
               se.Type == EnnemyType.Gloutton ||
               se.Type == EnnemyType.Knight ||
               se.Type == EnnemyType.Scoute)
        {
            if (se.WayPoints.Count > 0)
            {
                for (int i = 0; i < se.WayPoints.Count; i++)
                {
                    CellPos startCell = se.WayPoints[i % se.WayPoints.Count];
                    CellPos endCell = se.WayPoints[(i + 1) % se.WayPoints.Count];
                    if (!startCell.IsInLine(endCell))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// get the Neighbor of a cell if exists
    /// </summary>
    /// <param name="cell">cell to get neighbor</param>
    /// <param name="orientation">neighbor direction</param>
    /// <returns>null if don't exist</returns>
    public Cell GetNeighbor(DFLCommonNetwork.GameEngine.CellPos cell, Orientation orientation, int nbCells = 1)
    {
        int x = cell.X;
        int y = cell.Y;
        switch (orientation)
        {
            case Orientation.Up:
                y += nbCells;
                break;

            case Orientation.Down:
                y -= nbCells;

                break;

            case Orientation.Left:
                x -= nbCells;
                break;

            case Orientation.Right:
                x += nbCells;
                break;
        }

        if (CellExist(x, y))
            return GetCell(x, y);
        else
            return null;
    }

    /// <summary>
    /// Remove element on cell
    /// </summary>
    /// <param name="cell">the cell to remove element on</param>
    public void DestroyElementOnCell(Cell cell)
    {
        if (cell != null && cell.ElementID != string.Empty)
        {
            var mdd = DodgeFromLight.Databases.ResourcesData.GetMapDeco(cell.ElementID);
            foreach (var unwalkable in mdd.UnWalkableCells)
            {
                // get cell pos acording to element orientation
                Vector3 pos = new Vector3(unwalkable.X, 0, unwalkable.Y);
                pos = Quaternion.Euler(0, cell.ElementOrientation, 0) * pos;
                DFLCommonNetwork.GameEngine.CellPos p = new DFLCommonNetwork.GameEngine.CellPos(cell.X + Mathf.RoundToInt(pos.x), cell.Y + Mathf.RoundToInt(pos.z));

                Cell c = DodgeFromLight.CurrentMap.Grid.GetCell(p);
                if (c != null)
                    c.HasElement = false;
            }
            cell.ElementID = string.Empty;
            DodgeFromLight.GridController.RemoveElement(cell);
        }
    }

    /// <summary>
    /// Get the farest cell in line
    /// </summary>
    /// <param name="startCell">start cell</param>
    /// <param name="orientation">line orientation</param>
    /// <param name="maxDist">max cell distance</param>
    /// <param name="ignoreUnWalkable">ignore unwalkable cells</param>
    /// <param name="ignoreEnnemies">ignore ennemies on cells</param>
    /// <param name="ignoreElements">ignore deco elements on cells</param>
    /// <returns>pos of the farest cell</returns>
    public DFLCommonNetwork.GameEngine.CellPos GetFarestCell(DFLCommonNetwork.GameEngine.CellPos startCell, Orientation orientation, int maxDist, InLineCellType Type)
    {
        DFLCommonNetwork.GameEngine.CellPos lastCell = startCell;

        switch (Type)
        {
            default:
            case InLineCellType.Push:
                for (int i = 0; i < maxDist; i++)
                {
                    Cell cell = GetNeighbor(lastCell, orientation);
                    if (cell != null && IsCellWalkable(cell, true) && !IsThereEnnemyOnCell(cell.GetCellPos()) && !WillBeEnnemyNextTurn(cell) && !IsThereElementOnCell(cell))
                        lastCell = cell.GetCellPos();
                    else
                        return lastCell;
                }
                return lastCell;

            case InLineCellType.Jump:
                DFLCommonNetwork.GameEngine.CellPos lastGoodCell = lastCell;
                for (int i = 0; i < maxDist; i++)
                {
                    Cell cell = GetNeighbor(lastCell, orientation);
                    if (cell == null)
                        continue;
                    if (IsCellWalkable(cell, false) && !IsThereEnnemyOnCell(cell.GetCellPos()) && !IsThereElementOnCell(cell))
                        lastGoodCell = cell.GetCellPos();
                    lastCell = cell.GetCellPos();
                }
                return lastGoodCell;

            case InLineCellType.Grapple:
                List<DFLCommonNetwork.GameEngine.CellPos> Cells = new List<DFLCommonNetwork.GameEngine.CellPos>();
                for (int i = 0; i < maxDist; i++)
                {
                    Cell cell = GetNeighbor(lastCell, orientation);
                    if (cell == null)
                        continue;
                    if (WillBeEnnemyNextTurn(cell) || IsThereElementOnCell(cell) || IsThereEnnemyOnCell(cell.GetCellPos()))
                        break;
                    Cells.Add(cell.GetCellPos());
                    lastCell = cell.GetCellPos();
                }
                for (int i = Cells.Count - 1; i >= 0; i--)
                {
                    Cell cell = GetCell(Cells[i]);
                    if (cell.Walkable || IsCellWalkable(cell, true))
                        return Cells[i];
                }
                return startCell;
        }
    }

    public bool IsThereElementOnCell(Cell cell)
    {
        return cell.HasElement;
    }

    public bool IsCellWalkable(Cell cell, bool checkNextTurn)
    {
        if (cell.Walkable)
            return true;
        bool walkable = false;
        foreach (var platform in DodgeFromLight.MovingPlatforms)
        {
            if (checkNextTurn)
            {
                if (platform.Patrol.SimulateLastCellWithoutOrientation(platform.CurrentTA, platform.Orientation).Equals(cell))
                    walkable = true;
            }
            else
            {
                if (platform.CurrentCell.Equals(cell))
                    walkable = true;
            }
        }
        return walkable;
    }

    public bool WillBeEnnemyNextTurn(Cell cell)
    {
        foreach (Ennemy en in DodgeFromLight.Ennemies)
        {
            if (en.Patrol.NbCells > 0 && en.Patrol.SimulateLastCell(en.CurrentTA, en.Orientation).Equals(cell))
                return true;
        }
        return false;
    }
}

public enum InLineCellType
{
    Push,
    Jump,
    Grapple
}