using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public Transform GridContainer;
    public GameObject WalkableCellPrefab;
    public GameObject StartCellPrefab;
    public GameObject EndCellPrefab;
    public GameObject CheckPointPrefab;
    public GameObject CheckPointTakenPrefab;
    public GameObject SpringPrefab;
    public GameObject TPCellPrefab;
    public GameObject CollapsableCellPrefab;
    public GameObject FlipACoinCellPrefab;
    public GameObject NeedleCellPrefab;
    public GameObject MovingPlatformPrefab;
    public GameObject SwordSoclePrefab;

    public GameObject Status;
    public GameObject FogPrefab;
    public Transform Fog;

    public GameObject DownWallPrefab;
    public GameObject CornerExtPrefab;
    public GameObject CornerIntPrefab;

    public Transform EnnemiesContainer;
    public GameObject CyclopsPrefab;
    public GameObject GlouttonPrefab;
    public GameObject KeeperPrefab;
    public GameObject KnightPrefab;
    public GameObject ScoutePrfab;
    public GameObject TrapPrefab;
    public GameObject LizardmanPrefab;
    public GameObject ThunderbirdPrefab;
    public GameObject AxePrefab;
    public GameObject HammerPrefab;
    public GameObject CatapultPrefab;
    public GameObject PillarPrefab;

    public float PathSquareSize = 2f;
    public float PathSquareOffsetSize = 2f;
    [HideInInspector]
    public Material lineMaterial;

    public GameObject BombePrefab;
    public GameObject BombeVFXPrefab;
    public GameObject GearBox;

    private Dictionary<string, GameObject> cellsGO;
    private Dictionary<string, GameObject> cellsElements;
    public Dictionary<string, GameObject> Ennemies;
    public Dictionary<string, GameObject> Platforms;
    public Dictionary<string, GameObject> gearsGO;

    private bool reloadGridNextFrame = false;

    private void Awake()
    {
        DodgeFromLight.GridController = this;

        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    private void LateUpdate()
    {
        if (reloadGridNextFrame && cellToReloadNextFrame != null)
        {
            RefreshCrossCell(cellToReloadNextFrame, DodgeFromLight.CurrentMap.Grid, false);
            cellToReloadNextFrame = null;
            reloadGridNextFrame = false;
        }
    }

    public GameObject GetCellGameObject(Cell cell)
    {
        if (cellsGO.ContainsKey(cell.ToString()))
            return cellsGO[cell.ToString()];
        return null;
    }

    public GameObject GetCellElementGameObject(Cell cell)
    {
        if (cellsElements.ContainsKey(cell.ToString()))
            return cellsElements[cell.ToString()];
        return null;
    }

    public void DrawGrid(Grid grid, bool IsLobby = false)
    {
        cellsElements = new Dictionary<string, GameObject>();
        cellsGO = new Dictionary<string, GameObject>();
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                DrawCell(x, y, grid, IsLobby);
            }

        // draw out walls
        DrawWall(-1, grid.Height, CornerExtPrefab, 0f); // left up
        DrawWall(grid.Width, grid.Height, CornerExtPrefab, 90f); // right up
        DrawWall(-1, -1, CornerExtPrefab, -90); // left down
        DrawWall(grid.Width, -1, CornerExtPrefab, 180f); // right down

        GameObject wall;
        for (int x = 0; x < grid.Width; x++) // up and down
        {
            wall = DrawWall(x, grid.Height, DownWallPrefab, 90f, 0f, 0f); // up
            wall.transform.localScale = Vector3.one;
            wall = DrawWall(x, -1, DownWallPrefab, -90f, 0f, 0f); // down
            wall.transform.localScale = Vector3.one;
        }

        for (int y = 0; y < grid.Height; y++) // left and right
        {
            wall = DrawWall(grid.Width, y, DownWallPrefab, 180f, 0f); // right
            wall.transform.localScale = Vector3.one;
            wall = DrawWall(-1, y, DownWallPrefab, 0f, 0); // left
            wall.transform.localScale = Vector3.one;
        }

        Fog = Instantiate(FogPrefab).transform;
        Fog.localScale = new Vector3(0.6f * ((float)grid.Width + .33f) / 6f, 1f, 0.6f * ((float)grid.Height + .33f) / 6f);
        Fog.position = new Vector3(0.5f * ((float)grid.Width - .1667f) - 0.5f, -2.5f / 6f, 0.5f * ((float)grid.Height - .1667f) - 0.5f);
    }

    private GameObject DrawWall(int x, int y, List<GameObject> prefab)
    {
        return DrawWall(x, y, prefab[Random.Range(0, prefab.Count)]);
    }

    private GameObject DrawWall(int x, int y, GameObject prefab, float YRot = 0f, float Xoffset = 0f, float Zoffset = 0f)
    {
        GameObject cellGo = Instantiate(prefab);
        cellGo.transform.position = new Vector3(x + Xoffset, 0, y + Zoffset);
        cellGo.transform.localEulerAngles = Vector3.up * YRot;
        cellGo.transform.SetParent(GridContainer);
        return cellGo;
    }

    public GameObject DrawCell(int x, int y, Grid grid, bool IsLobby)
    {
        GameObject cellGo = null;
        if (grid.IsStart(grid.Cells[x, y]) && !grid.LobbyMap)
        {
            cellGo = Instantiate(StartCellPrefab);
            cellGo.isStatic = true;
        }
        else if (grid.IsEnd(grid.Cells[x, y]) && !grid.LobbyMap)
        {
            cellGo = Instantiate(EndCellPrefab);
            cellGo.isStatic = true;
        }
        else
        {
            Cell cell = grid.GetCell(x, y);
            switch (grid.Cells[x, y].Type)
            {
                default:
                case CellType.Walkable:
                    cellGo = Instantiate(DodgeFromLight.Databases.ResourcesData.GetMapDeco(cell.FloorID).Prefab);
                    cellGo.transform.localPosition = Vector3.zero;
                    cellGo.transform.localEulerAngles = grid.GetOrientationVector(cell.FloorOrientation);
                    break;

                case CellType.Needle:
                    cellGo = Instantiate(NeedleCellPrefab);
                    cellGo.transform.localPosition = Vector3.zero;
                    cellGo.transform.localEulerAngles = grid.GetOrientationVector(cell.FloorOrientation);
                    cellGo.GetComponentInChildren<Needle>().Initialize(cell.Arg1);
                    break;

                case CellType.SwordSocle:
                    cellGo = Instantiate(SwordSoclePrefab);
                    cellGo.transform.localPosition = Vector3.zero;
                    cellGo.transform.localEulerAngles = grid.GetOrientationVector(cell.FloorOrientation);
                    break;

                case CellType.Bombe:
                    cellGo = Instantiate(DodgeFromLight.Databases.ResourcesData.GetMapDeco(cell.FloorID).Prefab);
                    cellGo.transform.localPosition = Vector3.zero;
                    cellGo.transform.localEulerAngles = grid.GetOrientationVector(cell.FloorOrientation);

                    GameObject bombe = Instantiate(BombePrefab);
                    bombe.transform.SetParent(cellGo.transform);
                    bombe.transform.localPosition = Vector3.zero;
                    BmbeController bc = bombe.AddComponent<BmbeController>();
                    bc.Initialize(cell.Arg1, cell.GetCellPos());
                    break;

                case CellType.CheckPoint:
                    if (DodgeFromLight.GameManager != null && DodgeFromLight.GameManager.LastCheckPointReached != null && DodgeFromLight.GameManager.LastCheckPointReached.Equals(cell))
                        cellGo = Instantiate(CheckPointTakenPrefab);
                    else
                        cellGo = Instantiate(CheckPointPrefab);
                    break;

                case CellType.Spring:
                    cellGo = Instantiate(SpringPrefab);
                    cellGo.transform.localEulerAngles = grid.GetOrientationVector(cell.FloorOrientation);
                    break;

                case CellType.NotWalkable:
                    cellGo = new GameObject(x + "_" + y);

                    if (IsNeighborWalkable(grid, cell, Orientation.Up))
                    {
                        GameObject cellGoUp = Instantiate(DownWallPrefab);
                        cellGoUp.transform.localEulerAngles = Vector3.up * -90f;
                        cellGoUp.transform.localPosition = new Vector3(0, -0.0001f, 0.5f);
                        cellGoUp.transform.SetParent(cellGo.transform);
                    }

                    if (IsNeighborWalkable(grid, cell, Orientation.Right))
                    {
                        GameObject cellGoRight = Instantiate(DownWallPrefab);
                        cellGoRight.transform.localEulerAngles = Vector3.zero;
                        cellGoRight.transform.localPosition = new Vector3(0.5f, -0.0001f, 0);
                        cellGoRight.transform.SetParent(cellGo.transform);
                    }

                    if (IsNeighborWalkable(grid, cell, Orientation.Down))
                    {
                        GameObject cellGoDown = Instantiate(DownWallPrefab);
                        cellGoDown.transform.localEulerAngles = Vector3.up * 90f;
                        cellGoDown.transform.localPosition = new Vector3(0, -0.0001f, -0.5f);
                        cellGoDown.transform.SetParent(cellGo.transform);
                    }

                    if (IsNeighborWalkable(grid, cell, Orientation.Left))
                    {
                        GameObject cellGoLeft = Instantiate(DownWallPrefab);
                        cellGoLeft.transform.localEulerAngles = Vector3.up * 180f;
                        cellGoLeft.transform.localPosition = new Vector3(-0.5f, -0.0001f, 0);
                        cellGoLeft.transform.SetParent(cellGo.transform);
                    }
                    break;

                case CellType.TP:
                    cellGo = Instantiate(WalkableCellPrefab);
                    cellGo.transform.localEulerAngles = Vector3.up * (90 * Random.Range(0, 3));
                    GameObject TPCell = Instantiate(TPCellPrefab);
                    TPCell.transform.localPosition = Vector3.zero;
                    TPCell.transform.SetParent(cellGo.transform);
                    break;

                case CellType.Collapsable:
                    cellGo = Instantiate(WalkableCellPrefab);
                    cellGo.transform.localEulerAngles = Vector3.up * (90 * Random.Range(0, 3));
                    if (grid.Cells[x, y].Walkable)
                    {
                        GameObject Pillar = Instantiate(CollapsableCellPrefab);
                        Pillar.transform.localPosition = Vector3.zero;
                        Pillar.transform.SetParent(cellGo.transform);
                    }
                    else
                    {
                        GameObject Pillar = Instantiate(Status);
                        Pillar.transform.localPosition = Vector3.zero;
                        Pillar.transform.SetParent(cellGo.transform);
                    }
                    break;

                case CellType.FlipACoin:
                    cellGo = Instantiate(WalkableCellPrefab);
                    cellGo.transform.localEulerAngles = Vector3.up * (90 * Random.Range(0, 3));
                    GameObject FACRune = Instantiate(FlipACoinCellPrefab);
                    FACRune.transform.SetParent(cellGo.transform);
                    FACRune.transform.localPosition = Vector3.zero;
                    FACRune.transform.rotation = Quaternion.identity;
                    break;
            }
        }

        cellGo.name = x + "_" + y;
        cellGo.transform.position = new Vector3(x, 0f, y);
        cellGo.transform.SetParent(GridContainer);
        cellsGO.Add(x + "_" + y, cellGo);

        // add element
        Cell c = grid.GetCell(x, y);
        if (!string.IsNullOrEmpty(c.ElementID))
        {
            MapDecoData mdd = DodgeFromLight.Databases.ResourcesData.GetMapDeco(c.ElementID);
            if (mdd != null)
            {
                GameObject elementGo = Instantiate(mdd.Prefab);
                elementGo.name = c.ToString() + "_" + c.ElementID;
                elementGo.transform.position = new Vector3(x, 0f, y);
                elementGo.transform.localScale = Vector3.one * c.ElementScale * mdd.BaseScale;
                elementGo.transform.localEulerAngles = Vector3.up * c.ElementOrientation;
                elementGo.transform.SetParent(cellGo.transform);
                cellsElements.Add(cellGo.name, elementGo);

                if (IsLobby)
                {
                    ClickableMapObject clickable = elementGo.GetComponentInChildren<ClickableMapObject>();
                    if (clickable != null)
                        clickable.Cell = c.GetCellPos();
                }
            }
        }

        // add gear
        if (c.GearType != GearType.None)
        {
            GameObject gear = Instantiate(GearBox);
            gear.GetComponent<GearRingController>().SetGear(c.GearType);
            gear.transform.SetParent(cellGo.transform);
            gear.transform.position = c.GetCellPos().ToVector3(0f);
            gearsGO.Add(c.ToString(), gear);
        }

        return cellGo;
    }

    public void RemoveElement(Cell cell)
    {
        if (cellsElements.ContainsKey(cell.ToString()))
        {
            Destroy(cellsElements[cell.ToString()]);
            cellsElements.Remove(cell.ToString());
        }
    }

    public void RefreshCrossCell(Cell cell, Grid grid, bool addColliders)
    {
        List<Cell> Cells = new List<Cell>();
        Cells.Add(cell);
        Cell c = grid.GetNeighbor(cell.GetCellPos(), Orientation.Up);
        if (c != null)
            Cells.Add(c);
        c = grid.GetNeighbor(cell.GetCellPos(), Orientation.Down);
        if (c != null)
            Cells.Add(c);
        c = grid.GetNeighbor(cell.GetCellPos(), Orientation.Right);
        if (c != null)
            Cells.Add(c);
        c = grid.GetNeighbor(cell.GetCellPos(), Orientation.Left);
        if (c != null)
            Cells.Add(c);

        foreach (Cell ce in Cells)
            RefreshCell(ce, grid);

        if (addColliders)
            foreach (Cell ce in Cells)
                AddCellColider(ce);
    }

    public void RefreshCell(Cell cell, Grid grid)
    {
        if (cell == null)
            return;
        if (cellsGO.ContainsKey(cell.ToString()))
        {
            Destroy(cellsGO[cell.ToString()]);
            cellsGO.Remove(cell.ToString());
        }
        if (cellsElements.ContainsKey(cell.ToString()))
        {
            Destroy(cellsElements[cell.ToString()]);
            cellsElements.Remove(cell.ToString());
        }
        if (gearsGO.ContainsKey(cell.ToString()))
        {
            Destroy(gearsGO[cell.ToString()]);
            gearsGO.Remove(cell.ToString());
        }
        DrawCell(cell.X, cell.Y, grid, false);
    }

    public bool IsNeighborWalkable(Grid grid, Cell cell, Orientation orientation)
    {
        int x = cell.X;
        int y = cell.Y;
        switch (orientation)
        {
            case Orientation.None:
            default:
                return false;

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

        if (grid.GetCell(x, y) != null)
            return grid.GetCell(x, y).Type != CellType.NotWalkable;
        else
            return true;
    }

    public void ClearGrid(bool dontClearEnnemies = false)
    {
        if (Fog != null)
            Destroy(Fog.gameObject);
        int childs = GridContainer.childCount;
        if (cellsElements != null)
            cellsElements.Clear();
        cellsElements = new Dictionary<string, GameObject>();
        for (int i = childs - 1; i >= 0; i--)
            Destroy(GridContainer.GetChild(i).gameObject);
        if (cellsGO != null)
            cellsGO.Clear();
        cellsGO = new Dictionary<string, GameObject>();
        UnSpawnGears();
        ClearPlatforms();
        if (dontClearEnnemies)
            return;
        if (Ennemies != null)
        {
            foreach (GameObject go in Ennemies.Values)
                Destroy(go);
            Ennemies.Clear();
        }
        Ennemies = new Dictionary<string, GameObject>();
        Platforms = new Dictionary<string, GameObject>();
        DodgeFromLight.Ennemies = new List<Ennemy>();
        DodgeFromLight.MovingPlatforms = new List<MovingPlatform>();
    }

    public void UnSpawnGear(Cell cell)
    {
        if (gearsGO.ContainsKey(cell.ToString()))
        {
            Destroy(gearsGO[cell.ToString()]);
            gearsGO.Remove(cell.ToString());
        }
    }

    public void UnSpawnGears()
    {
        if (gearsGO != null)
        {
            foreach (var gear in gearsGO)
                Destroy(gearsGO[gear.Key]);
            gearsGO.Clear();
        }
        gearsGO = new Dictionary<string, GameObject>();
    }

    #region Colliders
    public void AddCellsColiders(Grid grid, bool keepUnWalkable = true, bool addElementCollider = false)
    {
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                Cell cell = grid.GetCell(x, y);
                if ((cell.Walkable && !cell.HasElement) || keepUnWalkable)
                    AddCellColider(cell, addElementCollider);
            }
    }

    public void AddCellColider(Cell cell, bool addElementCollider = false)
    {
        BoxCollider col = cellsGO[cell.ToString()].AddComponent<BoxCollider>();
        CursorSetter cs = cellsGO[cell.ToString()].AddComponent<CursorSetter>();
        cs.PointerEnterCursor = CursorType.Hand;
        Vector3 localScale = cellsGO[cell.ToString()].transform.localScale;
        col.size = new Vector3((1f / localScale.x), 0.5f * (1f / localScale.y), (1f / localScale.z));
        col.center = new Vector3(0, -0.25f * (1f / localScale.y), 0);

        if (!addElementCollider) return;

        if (cellsElements.ContainsKey(cell.ToString()))
        {
            Collider c = cellsElements[cell.ToString()].GetComponent<Collider>();
            if (c == null)
            {
                MeshCollider mc = cellsElements[cell.ToString()].AddComponent<MeshCollider>();
                mc.sharedMesh = cellsElements[cell.ToString()].GetComponentInChildren<MeshFilter>().mesh;
                mc.convex = true;
            }
            CursorSetter cse = cellsElements[cell.ToString()].AddComponent<CursorSetter>();
            cse.PointerEnterCursor = CursorType.Hand;
        }
    }

    public void AddEnnemiesColliders()
    {
        foreach (Ennemy ennemy in DodgeFromLight.Ennemies)
            AddEnnemyCollider(ennemy);
    }

    public void AddEnnemyCollider(Ennemy ennemy)
    {
        GameObject ennemyGo = Ennemies[ennemy.CurrentCell.ToString()];
        CursorSetter cs = ennemyGo.AddComponent<CursorSetter>();
        cs.PointerEnterCursor = CursorType.Hand;
        ennemyGo.name = ennemy.CurrentCell.ToString() + "_" + ennemy.Name;
        CapsuleCollider col = ennemyGo.AddComponent<CapsuleCollider>();
        col.height = 1.25f;
        col.center = new Vector3(0, 0.66f, 0);
    }
    #endregion

    #region Ennemies
    public void ClearPlatforms()
    {
        if (Platforms != null)
        {
            foreach (GameObject go in Platforms.Values)
                Destroy(go);
            Platforms.Clear();
        }
        Platforms = new Dictionary<string, GameObject>();
    }

    public MovingPlatform SpawnPlatform(Grid grid, SerializableMovingPlatform platform)
    {
        GameObject platGo = null;
        platGo = Instantiate(MovingPlatformPrefab);
        platGo.transform.SetParent(GridContainer);
        MovingPlatform plat = platform.ToMovingPlatform(platGo, grid);
        plat.PlaceOnFirstCell();
        DodgeFromLight.MovingPlatforms.Add(plat);
        Platforms.Add(plat.CurrentCell.ToString(), platGo);
        return plat;
    }

    public void SpawnPlatforms(Grid grid)
    {
        DodgeFromLight.MovingPlatforms = new List<MovingPlatform>();
        if (grid.MovingPlatforms != null && grid.MovingPlatforms.Count > 0)
            foreach (SerializableMovingPlatform platform in grid.MovingPlatforms)
                SpawnPlatform(grid, platform);
    }

    public void ClearEnnemies()
    {
        if (Ennemies != null)
        {
            foreach (GameObject go in Ennemies.Values)
                Destroy(go);
            Ennemies.Clear();
        }
        Ennemies = new Dictionary<string, GameObject>();
    }

    public void SpawnEnnemies(Grid grid)
    {
        DodgeFromLight.Ennemies = new List<Ennemy>();
        if (grid.Ennemies != null && grid.Ennemies.Count > 0)
            foreach (SerializableEnnemy serializableEnnemy in grid.Ennemies)
            {
                GameObject ennemyGo = null;
                switch (serializableEnnemy.Type)
                {
                    case EnnemyType.Cyclops:
                        ennemyGo = Instantiate(CyclopsPrefab);
                        break;
                    case EnnemyType.Gloutton:
                        ennemyGo = Instantiate(GlouttonPrefab);
                        break;
                    case EnnemyType.Keeper:
                        ennemyGo = Instantiate(KeeperPrefab);
                        break;
                    case EnnemyType.Knight:
                        ennemyGo = Instantiate(KnightPrefab);
                        break;
                    case EnnemyType.Scoute:
                        ennemyGo = Instantiate(ScoutePrfab);
                        break;
                    case EnnemyType.Trap:
                        ennemyGo = Instantiate(TrapPrefab);
                        break;
                    case EnnemyType.Lizardman:
                        ennemyGo = Instantiate(LizardmanPrefab);
                        break;
                    case EnnemyType.Thunderbird:
                        ennemyGo = Instantiate(ThunderbirdPrefab);
                        break;
                    case EnnemyType.Axe:
                        ennemyGo = Instantiate(AxePrefab);
                        break;
                    case EnnemyType.Hammer:
                        ennemyGo = Instantiate(HammerPrefab);
                        break;
                    case EnnemyType.Catapult:
                        ennemyGo = Instantiate(CatapultPrefab);
                        break;
                    case EnnemyType.Pillar:
                        ennemyGo = Instantiate(PillarPrefab);
                        break;
                }

                if (ennemyGo == null)
                {
                    Debug.LogError("trying to instanciate unknown ennemy.");
                    continue;
                }
                ennemyGo.transform.SetParent(EnnemiesContainer);
                Ennemy ennemy = serializableEnnemy.ToEnnemy(ennemyGo, grid);
                ennemy.PlaceOnFirstCell();
                ennemy.RefreshPO();
                Animator animator = ennemyGo.GetComponentInChildren<Animator>();
                if (animator != null)
                    ennemy.Animator = animator;
                DodgeFromLight.Ennemies.Add(ennemy);
                Ennemies.Add(ennemy.CurrentCell.ToString(), ennemyGo);
            }
        Events.Fire_EnnemiesSpawned();
        DrawCellsPO();
    }

    public Ennemy SpawnEnnemy(Grid grid, SerializableEnnemy serializableEnnemy)
    {
        GameObject ennemyGo = null;
        switch (serializableEnnemy.Type)
        {
            case EnnemyType.Cyclops:
                ennemyGo = Instantiate(CyclopsPrefab);
                break;
            case EnnemyType.Gloutton:
                ennemyGo = Instantiate(GlouttonPrefab);
                break;
            case EnnemyType.Keeper:
                ennemyGo = Instantiate(KeeperPrefab);
                break;
            case EnnemyType.Knight:
                ennemyGo = Instantiate(KnightPrefab);
                break;
            case EnnemyType.Scoute:
                ennemyGo = Instantiate(ScoutePrfab);
                break;
            case EnnemyType.Trap:
                ennemyGo = Instantiate(TrapPrefab);
                break;
            case EnnemyType.Lizardman:
                ennemyGo = Instantiate(LizardmanPrefab);
                break;
            case EnnemyType.Thunderbird:
                ennemyGo = Instantiate(ThunderbirdPrefab);
                break;
            case EnnemyType.Axe:
                ennemyGo = Instantiate(AxePrefab);
                break;
            case EnnemyType.Hammer:
                ennemyGo = Instantiate(HammerPrefab);
                break;
            case EnnemyType.Catapult:
                ennemyGo = Instantiate(CatapultPrefab);
                break;
            case EnnemyType.Pillar:
                ennemyGo = Instantiate(PillarPrefab);
                break;
        }

        if (ennemyGo == null)
        {
            Debug.LogError("trying to instanciate unknown ennemy.");
            return null;
        }
        ennemyGo.transform.SetParent(EnnemiesContainer);
        Ennemy ennemy = serializableEnnemy.ToEnnemy(ennemyGo, grid);
        ennemy.PlaceOnFirstCell();
        ennemy.RefreshPO();
        Animator animator = ennemyGo.GetComponentInChildren<Animator>();
        if (animator != null)
            ennemy.Animator = animator;
        DodgeFromLight.Ennemies.Add(ennemy);
        Ennemies.Add(ennemy.CurrentCell.ToString(), ennemyGo);

        DrawCellsPO();

        return ennemy;
    }

    //private void Update()
    //{
    //    CellPOData.UpdateColors();
    //}

    public void DrawCellsPO()
    {
        PoolManager.Instance.PushBackAllPool(PoolName.POCell);

        // preprocess cell po to stack ennemies by cell
        Dictionary<string, CellPODataRuntime> CellPODataRuntime = new Dictionary<string, CellPODataRuntime>();
        foreach (Ennemy ennemy in DodgeFromLight.Ennemies)
        {
            foreach (CellPos cell in ennemy.CurrentPO)
            {
                if (!CellPODataRuntime.ContainsKey(cell.ToString()))
                    CellPODataRuntime.Add(cell.ToString(), new CellPODataRuntime(cell));
                CellPODataRuntime[cell.ToString()].UpMaterials.Add(DodgeFromLight.Databases.CellPOData.GetUpMaterial(ennemy.Type));
                CellPODataRuntime[cell.ToString()].DownMaterials.Add(DodgeFromLight.Databases.CellPOData.GetDownMaterial(ennemy.Type));
            }

            // add attraction PO
            if (ennemy.Type == EnnemyType.Gloutton)
            {
                var cells = ((Gloutton)ennemy).AttractionPOCells;
                foreach (CellPos cell in cells)
                {
                    if (!CellPODataRuntime.ContainsKey(cell.ToString()))
                        CellPODataRuntime.Add(cell.ToString(), new CellPODataRuntime(cell));
                    CellPODataRuntime[cell.ToString()].UpMaterials.Add(DodgeFromLight.Databases.CellPOData.AttractionCellUpMaterial);
                    CellPODataRuntime[cell.ToString()].DownMaterials.Add(DodgeFromLight.Databases.CellPOData.AttractionCellDownMaterial);
                }
            }
        }

        foreach (CellPODataRuntime cell in CellPODataRuntime.Values)
        {
            PoolableObject cellPO = PoolManager.Instance.GetPoolable(PoolName.POCell);
            cellPO.GameObject.GetComponent<POCellController>().SetMaterials(cell.UpMaterials, cell.DownMaterials);
            cellPO.Transform.position = new Vector3(cell.cellPos.X, 0f, cell.cellPos.Y);
        }
    }
    #endregion

    public void AnimateSpring(CellPos cellPos)
    {
        if (!cellsGO.ContainsKey(cellPos.ToString()))
            return;

        Animator an = cellsGO[cellPos.ToString()].GetComponent<Animator>();
        if (an == null)
            return;

        an.Play("JumpTileAnimation");
    }

    public void AnimateTP(CellPos cellPos)
    {
        if (!cellsGO.ContainsKey(cellPos.ToString()))
            return;

        Animator an = cellsGO[cellPos.ToString()].GetComponentInChildren<Animator>();
        if (an == null)
            return;

        an.Play("TPAnimation");

        var pss = cellsGO[cellPos.ToString()].GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in pss)
            ps.Play();

    }

    Cell cellToReloadNextFrame = null;
    public void ReloadCellNextFrame(Cell cellPos)
    {
        cellToReloadNextFrame = cellPos;
        reloadGridNextFrame = true;
    }

    internal class CellPODataRuntime
    {
        public CellPos cellPos;
        public List<Material> UpMaterials;
        public List<Material> DownMaterials;

        public CellPODataRuntime(CellPos pos)
        {
            cellPos = pos;
            UpMaterials = new List<Material>();
            DownMaterials = new List<Material>();
        }
    }
}