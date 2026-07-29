using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class UI_MapCreator : MonoBehaviour
{
    public Canvas Canvas;
    public Camera ScreenshotCamera;
    public Camera Camera;
    public UI_EnvironmentSettings UI_EnvironmentSettings;
    public GridController GridController;
    public Transform ToolsContainer;
    public GameObject CollapsablePrefab;
    public GameObject ToolPrefab;
    public ToolType CurrentTool;
    public Material SelectedMaterial;
    public GameObject UnwalkableFeedbackPrefab;
    public TextMeshProUGUI MapName;
    public GameObject EnnemiesPanel;
    public GameObject CellsMechanicsPanel;
    // selected cell vars
    public GameObject EnnemyPanel;
    public GameObject GearsPanel;
    public TextMeshProUGUI EnnemyHeaderName;
    public Image EnnemyPreview;
    // play map button
    public Button PlayButton;

    private List<GameObject> UnwalkableFeedback = new List<GameObject>();
    private Cell selectedCell;
    private MapDecoData selectedMapDeco;
    private Ennemy selectedEnnemy;
    private MovingPlatform selectedMovingPlatform;
    private List<string> errors = new List<string>();
    private MaterialChanger MaterialChanger;
    private Dictionary<ToolType, GameObject> ToolsButtons = new Dictionary<ToolType, GameObject>();
    private Dictionary<string, GameObject> MapDecoButtons = new Dictionary<string, GameObject>();
    private Color CurrentToolButtonBaseColor = Color.white;
    private RotateAndScale currentRotateAndScale = null;

    private void Awake()
    {
        MaterialChanger = new MaterialChanger();
        EnnemyPanel.SetActive(false);
        GearsPanel.SetActive(false);
        UndoableActionsManager.Clear();
        CameraOrbit.PostRenderCallback = () =>
        {
            if (!enabled) return;
            if (Input.GetKey(KeyCode.P))
            {
                foreach (Ennemy en in GameContext.Ennemies)
                    en.DrawPath(transform);
                foreach (MovingPlatform platform in GameContext.MovingPlatforms)
                    platform.DrawPath(transform);
            }
            else if (selectedEnnemy != null)
                selectedEnnemy.DrawPath(transform);
            else if (selectedMovingPlatform != null)
                selectedMovingPlatform.DrawPath(transform);
        };
        // hide all specialPanels
        foreach (GameObject panel in SpecialPanels)
            panel.SetActive(false);
    }

    /// <summary>
    /// Opens the current local workspace or creates a new authored map.
    /// </summary>
    private void Start()
    {
        // Continue the last local edit when possible so quitting never loses discoverability.
        PlayButton.gameObject.SetActive(true);
        GameContext.UI_WorkerNotifier.Show("Loading map...");
        if (GridManager.GetGrid() != null && GridManager.GetMap() != null)
        {
            LoadMap();
            return;
        }

        const int defaultSize = 8;
        var metadata = new Map("Untitled Map", defaultSize, defaultSize);
        metadata.SetAuthor(SaveManager.CurrentSave.DisplayName);
        GameContext.CurrentMap = new FullMap(
            new Grid(defaultSize, defaultSize),
            metadata);
        GameContext.CurrentMap.Grid.Environment =
            GameContext.EnvironmentController.GetBuildinEnvironment(
                BuildInEnvironments.Default);
        UpdateMap();
        StartCoroutine(SaveMap(LoadMap));
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) // select cell
        {
            CellPos? cellPos = GetClickedCell();
            if (cellPos.HasValue)
            {
                ClickOnCell(GameContext.CurrentMap.Grid.GetCell(cellPos.Value));
                ValidateGrid();
            }
            else
                UnSelectCell();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnSelectCell();
        }

        if (Input.GetKeyDown(KeyCode.Delete) && GameContext.CurrentMap.Grid != null && selectedCell != null)
        {
            selectedCell.SetType(CellType.Walkable);
            if (!string.IsNullOrEmpty(selectedCell.ElementID))
            {
                selectedCell.RemoveElement();
                DrawFeedbacks();
            }
            if (selectedEnnemy != null)
            {
                GameContext.CurrentMap.Grid.RemoveEnnemy(selectedCell.GetCellPos());
                UpdateMap();
            }
            else if (selectedMovingPlatform != null)
            {
                GameContext.CurrentMap.Grid.RemoveMovingPlatform(selectedCell.GetCellPos());
                UpdateMap();
            }
            else
                GridController.RefreshCrossCell(selectedCell, GameContext.CurrentMap.Grid, true);
            ValidateGrid();
        }

        if (selectedCell != null)
        {
            if (Input.GetKeyDown(KeyCode.P) && selectedEnnemy != null)
            {
                selectedEnnemy.WayPoints.Clear();
                SetTool(ToolType.Path);
            }

            if (Input.GetKeyDown(KeyCode.P) && selectedMovingPlatform != null)
            {
                selectedMovingPlatform.WayPoints.Clear();
                SetTool(ToolType.Path);
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Orientation dir = Orientation.None;
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    dir = Orientation.Up;
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    dir = Orientation.Down;
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                    dir = Orientation.Right;
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    dir = Orientation.Left;

                var en = GameContext.CurrentMap.Grid.GetEnnemyOnCell(selectedCell);
                // element orientation
                if (!string.IsNullOrEmpty(selectedCell.ElementID))
                {
                    selectedCell.ElementOrientation = GameContext.CurrentMap.Grid.GetOrientationVector(dir).y;
                    GameContext.GridController.RefreshCell(selectedCell, GameContext.CurrentMap.Grid);
                    DrawFeedbacks();
                }
                else if (selectedEnnemy != null)
                {
                    GameContext.CurrentMap.Grid.GetEnnemyOnCell(selectedEnnemy.StartCell).StartOrientation = dir;
                    selectedEnnemy.SetOrientation(dir);
                    selectedEnnemy.RefreshPO();
                    GridController.DrawCellsPO();
                }
                else
                {
                    selectedCell.FloorOrientation = dir;
                    GameContext.GridController.RefreshCell(selectedCell, GameContext.CurrentMap.Grid);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.LeftControl))
                StartCoroutine(SaveMap());
            else
                SetTool(ToolType.Select);
        }

        if (Input.GetKeyDown(KeyCode.W))
            SetTool(ToolType.Walkable);

        if (Input.GetKeyDown(KeyCode.N))
            SetTool(ToolType.UnWalkable);

        if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl))
        {
            UndoableActionsManager.Undo();
            UpdateMap();
        }

        if (Input.GetKeyDown(KeyCode.Y) && Input.GetKey(KeyCode.LeftControl))
        {
            UndoableActionsManager.Redo();
            UpdateMap();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (selectedCell != null)
            {
                CameraOrbit.Instance.targetPos = selectedCell.GetCellPos().ToVector3(0.2f);
                CameraOrbit.Instance.forceUpdate = true;
            }
            CameraOrbit.Instance.distance = 15f;
        }

        if (CurrentTool == ToolType.Path && selectedEnnemy != null)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (GameContext.CurrentMap.Grid.ValidateEnnemyPath(selectedEnnemy))
                    SetTool(ToolType.Select);
                else
                    GameContext.UI_Notifications.Notify("This ennemy has invalide path. 'esc' to cancel.");
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                selectedEnnemy.WayPoints = new List<CellPos>();
                selectedEnnemy.Patrol = new Patrol();
                SetTool(ToolType.Select);
            }
        }
    }

    #region Environments
    public void SetBuildinEnvironment(BuildInEnvironments envType)
    {
        GameContext.CurrentMap.Grid.Environment = GameContext.EnvironmentController.GetBuildinEnvironment(envType);
        GameContext.SetEnvironment(GameContext.CurrentMap);
    }
    #endregion

    #region Tools
    public void SetTool(ToolType tool)
    {
        if (CurrentTool == ToolType.MapDeco)
        {

        }
        else
            ToolsButtons[CurrentTool].GetComponent<Image>().color = CurrentToolButtonBaseColor;
        if (selectedEnnemy != null && CurrentTool == ToolType.Path)
        {
            if (!GameContext.CurrentMap.Grid.ValidateEnnemyPath(selectedEnnemy))
            {
                selectedEnnemy.WayPoints = new List<CellPos>();
                selectedEnnemy.Patrol = new Patrol();
            }
        }
        if (selectedMovingPlatform != null && CurrentTool == ToolType.Path)
        {
            selectedMovingPlatform.WayPoints.Clear();
        }
        CurrentTool = tool;
        if (CurrentTool == ToolType.MapDeco)
        {

        }
        else
        {
            CurrentToolButtonBaseColor = ToolsButtons[CurrentTool].GetComponent<Image>().color;
            ToolsButtons[CurrentTool].GetComponent<Image>().color = Color.green;
        }
    }

    public void RegisterToolButton(ToolType tool, GameObject button)
    {
        if (tool != ToolType.MapDeco && !ToolsButtons.ContainsKey(tool))
            ToolsButtons.Add(tool, button);
    }

    public void BindDecoTools()
    {
        MapDecoButtons = new Dictionary<string, GameObject>();
        var resData = GameContext.Databases.ResourcesData;
        foreach (string type in resData.Types)
        {
            if (type != "Excluded" && !string.IsNullOrEmpty(type))
                DrawDecor(GameContext.Databases.ResourcesData.GetDataForType(type), type);
        }
    }

    void DrawDecor(Dictionary<string, MapDecoData> mapdata, string type)
    {
        GameObject collapsable = Instantiate(CollapsablePrefab);
        collapsable.GetComponentInChildren<TextMeshProUGUI>().text = type;
        collapsable.transform.SetParent(ToolsContainer, false);
        Transform container = collapsable.transform.GetChild(0);
        collapsable.transform.GetChild(0).GetComponent<GridLayoutGroup>().cellSize = new Vector2(104f, 104f);

        foreach (var data in mapdata)
        {
            GameObject tool = Instantiate(ToolPrefab);
            Sprite sprite = Sprite.Create(data.Value.Texture,
                new Rect(0.0f, 0.0f, data.Value.Texture.width, data.Value.Texture.height),
                new Vector2(0.5f, 0.5f), 100.0f);
            tool.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
            tool.transform.SetParent(container, false);
            tool.GetComponent<Button>().onClick.AddListener(() =>
            {
                SetTool(ToolType.MapDeco);
                selectedMapDeco = data.Value;
            });
            UITooltipSetter tip = tool.AddComponent<UITooltipSetter>();
            tip.Message = type + " : " + data.Value.ID;
            MapDecoButtons.Add(data.Value.ID, tool);
        }

        collapsable.GetComponent<UI_Collapsable>().Collapse();
    }
    #endregion

    #region Grid refresh
    public void ValidateGrid()
    {
        errors.Clear();
        if (GameContext.CurrentMap.Grid != null)
            GameContext.CurrentMap.Grid.ValidateGrid(ref errors);
    }

    public void UpdateMap()
    {
        GridController.ClearGrid();
        GridController.DrawGrid(GameContext.CurrentMap.Grid);
        GridController.SpawnEnnemies(GameContext.CurrentMap.Grid);
        GridController.SpawnPlatforms(GameContext.CurrentMap.Grid);
        GridController.AddEnnemiesColliders();
        GridController.AddCellsColiders(GameContext.CurrentMap.Grid);
        GameContext.SetEnvironment(GameContext.CurrentMap);
        if (selectedCell != null)
            SelectCell(selectedCell);
        DrawFeedbacks();
    }

    void RefreshCell(Cell cell, bool clearPanels = true)
    {
        Cell sc = selectedCell;
        UnSelectCell(clearPanels);
        GridController.RefreshCrossCell(cell, GameContext.CurrentMap.Grid, true);
        GridController.AddCellColider(cell, true);
        selectedCell = sc;
        if (selectedCell != null && selectedCell.GetCellPos().Equals(cell.GetCellPos()))
            SelectCell(cell);
        DrawFeedbacks();
    }
    #endregion

    #region Map Elements
    private void ResetMapElementsUnwalkable()
    {
        for (int x = 0; x < GameContext.CurrentMap.Grid.Width; x++)
            for (int y = 0; y < GameContext.CurrentMap.Grid.Height; y++)
            {
                Cell c = GameContext.CurrentMap.Grid.GetCell(x, y);
                c.HasElement = false;
            }
    }

    private void ProcessmapElementsUnwalkable()
    {
        for (int x = 0; x < GameContext.CurrentMap.Grid.Width; x++)
            for (int y = 0; y < GameContext.CurrentMap.Grid.Height; y++)
            {
                Cell c = GameContext.CurrentMap.Grid.GetCell(x, y);
                if (!string.IsNullOrEmpty(c.ElementID))
                {
                    var mdd = GameContext.Databases.ResourcesData.GetMapDeco(c.ElementID);
                    foreach (var unwalkable in mdd.UnWalkableCells)
                    {
                        // get cell pos acording to element orientation
                        Vector3 pos = new Vector3(unwalkable.X, 0, unwalkable.Y);
                        pos = Quaternion.Euler(0, c.ElementOrientation, 0) * pos;
                        CellPos p = new CellPos(c.X + Mathf.RoundToInt(pos.x), c.Y + Mathf.RoundToInt(pos.z));

                        Cell cell = GameContext.CurrentMap.Grid.GetCell(p);
                        if (cell != null)
                        {
                            cell.HasElement = true;
                        }
                    }
                }
            }
    }

    private void ClearFeedbacks()
    {
        foreach (var fb in UnwalkableFeedback)
            Destroy(fb);
        UnwalkableFeedback.Clear();
    }

    public void DrawFeedbacks()
    {
        ResetMapElementsUnwalkable();
        ProcessmapElementsUnwalkable();
        ClearFeedbacks();

        for (int x = 0; x < GameContext.CurrentMap.Grid.Width; x++)
            for (int y = 0; y < GameContext.CurrentMap.Grid.Height; y++)
            {
                Cell c = GameContext.CurrentMap.Grid.GetCell(x, y);
                if (c.HasElement)
                {
                    GameObject fb = Instantiate(UnwalkableFeedbackPrefab);
                    fb.transform.position = c.GetCellPos().ToVector3(0f);
                    UnwalkableFeedback.Add(fb);
                }
            }
    }
    #endregion

    #region Cell Selection
    private void ClickOnCell(Cell cell)
    {
        switch (CurrentTool)
        {
            case ToolType.Select:
                EnnemyPanel.SetActive(false);
                if (cell != null && cell.EntityOnCell != null && cell.EntityOnCell is Ennemy)
                {
                    selectedEnnemy = (Ennemy)cell.EntityOnCell;
                }
                else
                {
                    selectedEnnemy = null;

                    if (GridController.Platforms.ContainsKey(cell.ToString()))
                        selectedMovingPlatform = GridController.Platforms[cell.ToString()].GetComponent<MovingPlatform>();
                    else
                        selectedMovingPlatform = null;
                }
                SelectCell(cell);
                return;

            case ToolType.Walkable:
                cell.SetWalkable(true);
                UndoableActionsManager.AddUndoableAction(
                    new Undoable_SetCellType(
                        cell.GetCellPos(),
                        cell.Type, CellType.Walkable));
                cell.SetType(CellType.Walkable);
                GridController.RefreshCrossCell(cell, GameContext.CurrentMap.Grid, true);
                return;

            case ToolType.UnWalkable:
                if (cell.EntityOnCell != null || cell.HasElement)
                    return;
                cell.SetWalkable(false);
                UndoableActionsManager.AddUndoableAction(
                    new Undoable_SetCellType(
                        cell.GetCellPos(),
                        cell.Type, CellType.NotWalkable));
                cell.SetType(CellType.NotWalkable);
                GridController.RefreshCrossCell(cell, GameContext.CurrentMap.Grid, true);
                return;

            case ToolType.MapDeco:
                if (cell.EntityOnCell != null || !string.IsNullOrEmpty(cell.ElementID) || cell.HasElement)
                {
                    SelectCell(cell);
                    return;
                }
                if (selectedMapDeco != null && cell != null && cell.Type == CellType.Walkable)
                {
                    if (selectedMapDeco.Type == "Floor")
                    {
                        UndoableActionsManager.AddUndoableAction(new Undoable_SetFloorType(cell.GetCellPos(),
                            cell.FloorID, selectedMapDeco.ID));
                        cell.FloorID = selectedMapDeco.ID;
                    }
                    else
                    {
                        UndoableActionsManager.AddUndoableAction(new Undoable_SetElement(cell.GetCellPos(),
                            cell.ElementID, selectedMapDeco.ID));
                        cell.SetElement(selectedMapDeco.ID);
                    }
                    RefreshCell(cell);
                    SelectCell(cell);
                }
                return;

            case ToolType.TP:
                if (cell.HasElement)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.TP);
                break;

            case ToolType.FlipACoin:
                if (cell.HasElement)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.FlipACoin);
                break;

            case ToolType.Collapsable:
                if (cell.HasElement)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.Collapsable);
                break;

            case ToolType.MovingPlatform:
                if (cell.EntityOnCell != null || !string.IsNullOrEmpty(cell.ElementID) || cell.HasElement || cell.Type != CellType.NotWalkable)
                    return;
                if (GridController.Platforms.ContainsKey(cell.ToString()))
                    return;
                var platform = GameContext.CurrentMap.Grid.AddMovingPlatform(cell.GetCellPos());
                GridController.SpawnPlatform(GameContext.CurrentMap.Grid, platform);
                selectedMovingPlatform = GridController.Platforms[cell.ToString()].GetComponent<MovingPlatform>();
                break;

            case ToolType.Needle:
                if (cell.HasElement)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.Needle);
                break;

            case ToolType.Spring:
                if (cell.HasElement)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.Spring);
                cell.Arg1 = 5;
                break;

            case ToolType.Bombe:
                if (cell.HasElement)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.Bombe);
                cell.Arg1 = 10;
                break;

            case ToolType.Start:
                if (cell.HasElement)
                    return;
                if (cell.EntityOnCell != null)
                    return;
                cell.SetWalkable(true);
                GameContext.CurrentMap.Grid.SetStart(cell);
                UpdateMap();
                break;

            case ToolType.CheckPoint:
                if (cell.HasElement)
                    return;
                if (cell.EntityOnCell != null)
                    return;
                cell.SetWalkable(true);
                cell.SetType(CellType.CheckPoint);
                break;

            case ToolType.End:
                if (cell.HasElement)
                    return;
                if (cell.EntityOnCell != null)
                    return;
                cell.SetWalkable(true);
                GameContext.CurrentMap.Grid.SetEnd(cell);
                UpdateMap();
                break;

            case ToolType.Path:
                if (!string.IsNullOrEmpty(cell.ElementID) || (!cell.Walkable && selectedEnnemy != null) || cell.HasElement)
                    return;
                if (selectedEnnemy != null)
                {
                    if (selectedEnnemy.WayPoints != null && selectedEnnemy.WayPoints.Count < 1 ||
                        (selectedEnnemy.WayPoints.Count > 0 && cell.IsInLine(selectedEnnemy.WayPoints.Last())))
                        selectedEnnemy.WayPoints.Add(cell.GetCellPos());
                }
                else if (selectedMovingPlatform != null)
                {
                    if (cell.Type == CellType.NotWalkable)
                    {
                        if (selectedMovingPlatform.WayPoints != null && selectedMovingPlatform.WayPoints.Count < 1 ||
                            (selectedMovingPlatform.WayPoints.Count > 0 && cell.IsInLine(selectedMovingPlatform.WayPoints.Last())))
                            selectedMovingPlatform.WayPoints.Add(cell.GetCellPos());
                    }
                }
                return;

            case ToolType.Cyclops:
            case ToolType.Gloutton:
            case ToolType.Keeper:
            case ToolType.Knight:
            case ToolType.Axe:
            case ToolType.Hammer:
            case ToolType.Catapult:
            case ToolType.Pillar:
            case ToolType.Scoute:
            case ToolType.Trap:
            case ToolType.Lizardman:
            case ToolType.Thunderbird:
                if (cell.EntityOnCell != null || cell.HasElement || !cell.Walkable)
                    return;
                AddEnnemy((EnnemyType)CurrentTool, cell);
                SetTool(ToolType.Select);
                SelectCell(cell);
                if (cell != null && cell.EntityOnCell != null)
                    selectedEnnemy = (Ennemy)cell.EntityOnCell;
                else
                    selectedEnnemy = null;
                if (selectedEnnemy != null)
                {
                    EnnemyPanel.SetActive(true);
                    EnnemyHeaderName.text = selectedEnnemy.Type.ToString();
                    EnnemyPreview.sprite = ToolsButtons[(ToolType)selectedEnnemy.Type].transform.GetChild(0).GetComponent<Image>().sprite;
                }
                return;
        }
        RefreshCell(cell);
        if (CurrentTool == ToolType.MovingPlatform)
            CurrentTool = ToolType.Select;
        SelectCell(cell);
    }

    private CellPos? GetClickedCell()
    {
        RaycastHit hit;
        Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            var pos = hit.transform.position;
            MaterialChanger.ResetAllRenderers();
            MaterialChanger.Clear();

            string cellName = hit.transform.gameObject.name;
            if (cellName.Contains("_"))
            {
                int x = -1, y = -1;
                int.TryParse(cellName.Split('_')[0], out x);
                int.TryParse(cellName.Split('_')[1], out y);
                Cell cell = GameContext.CurrentMap.Grid.GetCell(x, y);
                if (cell != null)
                {
                    GameObject cellGo = GameContext.GridController.GetCellGameObject(cell);
                    if (cellGo == null)
                        return null;
                    var mrs = cellGo.GetComponentsInChildren<Renderer>();
                    foreach (Renderer mr in mrs)
                        MaterialChanger.AddRenderer(mr, mr.materials);
                    MaterialChanger.SetMaterial(SelectedMaterial);
                    return cell.GetCellPos();
                }
            }
        }
        return null;
    }

    private void SelectCell(Cell cell)
    {
        if (selectedCell != null && cell.GetCellPos().Equals(selectedCell.GetCellPos()))
            return;

        if (cell.Type == CellType.Walkable && string.IsNullOrEmpty(cell.ElementID) && !cell.HasElement)
            GearsPanel.SetActive(true);
        else
            GearsPanel.SetActive(false);
        if (cell != null && cell.EntityOnCell != null && cell.EntityOnCell is Ennemy)
            EnnemyPanel.SetActive(true);
        else
            EnnemyPanel.SetActive(false);

        if (selectedEnnemy != null)
        {
            EnnemyPanel.SetActive(true);
            EnnemyHeaderName.text = selectedEnnemy.Type.ToString();
            EnnemyPreview.sprite = ToolsButtons[(ToolType)selectedEnnemy.Type].transform.GetChild(0).GetComponent<Image>().sprite;
        }

        if (selectedCell != null)
        {
            int x = selectedCell.X;
            int y = selectedCell.Y;
            Cell c = GameContext.CurrentMap.Grid.GetCell(x, y);
            MaterialChanger.ResetAllRenderers();
            MaterialChanger.Clear();
            if (c == null) return;
            GridController.RefreshCrossCell(c, GameContext.CurrentMap.Grid, true);
            GridController.AddCellColider(c, true);
        }

        selectedCell = cell;
        if (!FeedbackSelect())
            return;

        // set rotate and scale component on element
        if (!string.IsNullOrEmpty(cell.ElementID))
        {
            GameObject element = GridController.GetCellElementGameObject(cell);
            if (element != null)
            {
                var mdd = GameContext.Databases.ResourcesData.GetMapDeco(cell.ElementID);
                currentRotateAndScale = element.AddComponent<RotateAndScale>();
                currentRotateAndScale.OnRotate = (rotation) =>
                {
                    selectedCell.ElementOrientation = rotation;
                    element.transform.eulerAngles = new Vector3(0f, rotation, 0f);
                    DrawFeedbacks();
                };
                currentRotateAndScale.OnScale = (scale) =>
                {
                    selectedCell.ElementScale = scale;
                    element.transform.localScale = Vector3.one * mdd.BaseScale * scale;
                };
                currentRotateAndScale.SetDefaultValues(selectedCell.ElementOrientation, selectedCell.ElementScale);
                currentRotateAndScale.SetConstraintes(mdd.CanFreeRotate(), mdd.Scalable);
                currentRotateAndScale.StartDrag();
            }
        }

        // ============== Special cell UI
        if (selectedCell != null)
        {
            ignoreOnChange = true;
            // hide all specialPanels
            foreach (GameObject panel in SpecialPanels)
                panel.SetActive(false);

            if (selectedCell.Type == CellType.Spring)
            {
                SpringCellPanel.SetActive(true);
                SpringSlider.value = selectedCell.Arg1;
                SpringCellsText.text = selectedCell.Arg1.ToString();
                LayoutRebuilder.ForceRebuildLayoutImmediate(SpringCellPanel.transform.parent.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
            }
            else if (selectedCell.Type == CellType.Bombe)
            {
                TimeBombePanel.SetActive(true);
                TimeBombField.text = selectedCell.Arg1.ToString();
                LayoutRebuilder.ForceRebuildLayoutImmediate(TimeBombePanel.transform.parent.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
            }
            else if (selectedCell.Type == CellType.Needle)
            {
                NeedleTrapPanel.SetActive(true);
                NeedleTrapToggle.isOn = selectedCell.Arg1 == 1;
                LayoutRebuilder.ForceRebuildLayoutImmediate(NeedleTrapPanel.transform.parent.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
            }
            ignoreOnChange = false;
        }

        if (selectedMovingPlatform != null)
        {
            // hide all specialPanels
            foreach (GameObject panel in SpecialPanels)
                panel.SetActive(false);
            MovingPlatformPanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(MovingPlatformPanel.transform.parent.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }
    }

    private void UnSelectCell(bool clearPanels = true)
    {
        // hide all specialPanels
        if (clearPanels)
            foreach (GameObject panel in SpecialPanels)
                panel.SetActive(false);
        if (CurrentTool == ToolType.Path)
            return;
        if (selectedCell != null)
        {
            int x = selectedCell.X;
            int y = selectedCell.Y;
            selectedCell = null;
            Cell c = GameContext.CurrentMap.Grid.GetCell(x, y);
            MaterialChanger.ResetAllRenderers();
            MaterialChanger.Clear();
            if (c == null) return;
            GridController.RefreshCrossCell(c, GameContext.CurrentMap.Grid, true);
            GridController.AddCellColider(c, true);
            if (currentRotateAndScale != null)
                Destroy(currentRotateAndScale);
        }
        EnnemyPanel.SetActive(false);
        GearsPanel.SetActive(false);
        selectedEnnemy = null;
    }

    public bool FeedbackSelect()
    {
        GameObject cellGo = GameContext.GridController.GetCellGameObject(selectedCell);
        if (selectedMovingPlatform != null)
            cellGo = GridController.Platforms[selectedCell.ToString()];
        if (cellGo == null)
            return false;
        var mrs = cellGo.GetComponentsInChildren<Renderer>();
        foreach (Renderer mr in mrs)
            MaterialChanger.AddRenderer(mr, mr.materials);
        MaterialChanger.SetMaterial(SelectedMaterial);
        return true;
    }

    public void SetGearOnCell(GearType type)
    {
        selectedCell.SetGear(type);
        RefreshCell(selectedCell);
    }
    #endregion

    #region Ennemies
    private void Events_EntityEndMove(Entity entity)
    {
        GameContext.EntitiesMoving--;
    }

    private void Events_EntityStartMove(Entity entity)
    {
        GameContext.EntitiesMoving++;
    }

    private void AddEnnemy(EnnemyType type, Cell cell)
    {
        if (GameContext.CurrentMap.Grid.GetEnnemyOnCell(cell) == null)
        {
            var se = GameContext.CurrentMap.Grid.AddEnnemy(type, cell.GetCellPos());
            selectedEnnemy = GridController.SpawnEnnemy(GameContext.CurrentMap.Grid, se);
            GridController.AddEnnemyCollider(selectedEnnemy);
        }
    }

    public void RemoveEnnemyFromSelectedCell()
    {
        if (selectedEnnemy != null)
        {
            GameContext.CurrentMap.Grid.RemoveEnnemy(selectedCell.GetCellPos());
            UpdateMap();
        }
    }
    #endregion

    #region Cells Mechanic
    public List<GameObject> SpecialPanels;
    public GameObject MovingPlatformPanel;
    private bool ignoreOnChange = false;

    public void ResetWalkableCell()
    {
        selectedCell.SetType(CellType.Walkable);
        RefreshCell(selectedCell, false);
        FeedbackSelect();
    }

    // ================================== Spring Cell
    public GameObject SpringCellPanel;
    public Slider SpringSlider;
    public TextMeshProUGUI SpringCellsText;

    public void SpringSlider_ValueChange(float value)
    {
        if (ignoreOnChange) return;
        SpringCellsText.text = ((int)value).ToString();
        selectedCell.Arg1 = (int)value;
        RefreshCell(selectedCell, false);
        FeedbackSelect();
    }

    // ================================== Time Bombe
    public GameObject TimeBombePanel;
    public TMP_InputField TimeBombField;

    public void TimeBombField_ValueChange(string value)
    {
        if (ignoreOnChange) return;
        int val = -1;
        if (int.TryParse(value, out val))
            selectedCell.Arg1 = val;
        RefreshCell(selectedCell, false);
        FeedbackSelect();
    }

    // ================================== Needle Trap
    public GameObject NeedleTrapPanel;
    public Toggle NeedleTrapToggle;

    public void NeedleTrapToggle_ValueChange(bool value)
    {
        if (ignoreOnChange) return;
        selectedCell.Arg1 = value ? 1 : 0;
        RefreshCell(selectedCell, false);
        FeedbackSelect();
    }

    #endregion

    #region Load, Save & Play
    /// <summary>
    /// Prompts the author to save before leaving the editor.
    /// </summary>
    public void Close()
    {
        // Both exit paths are now entirely local and return to the main menu.
        GameContext.UI_Modal.SetTitle("Save before quit")
                .SetButtonRight("Save and Quit", () =>
                {
                    StartCoroutine(SaveMap(() =>
                    {
                        GameContext.GridController.ClearGrid();
                        StartCoroutine(LoadScene("MainMenu"));
                    }));
                })
                .SetButtonLeft("Quit without saving", () =>
                {
                    GameContext.GridController.ClearGrid();
                    StartCoroutine(LoadScene("MainMenu"));
                })
                .Show("You are going to quit the current map. Do you want to save your unsaved work before ?");
    }

    /// <summary>
    /// Clears editor runtime state and loads another local scene.
    /// </summary>
    private IEnumerator LoadScene(string sceneName)
    {
        // No remote lock needs to be released for a local authored map.
        ClearFeedbacks();
        PoolManager.Instance.PushBackAllPool(PoolName.POCell);
        yield return null;

        if (GameContext.GameManager != null)
        {
            Destroy(GameContext.GameManager.gameObject);
            Destroy(GameContext.GameManager);
            GameContext.GameManager = null;
        }

        GameContext.UI_WorkerNotifier.Hide();
        GameContext.SceneTransitions.LoadScene(sceneName);
    }

    /// <summary>
    /// Loads the active local workspace into the map editor.
    /// </summary>
    public void LoadMap()
    {
        // Rebuild the authored grid and all editor panels from local disk data.
        UndoableActionsManager.Clear();
        GameContext.CurrentMap = GridManager.GetFullMap();
        UpdateMap();
        ValidateGrid();
        CameraOrbit.Instance.targetPos = new Vector3((float)GameContext.CurrentMap.Grid.Width * 0.5f, 1f, (float)GameContext.CurrentMap.Grid.Height * 0.5f);
        CameraOrbit.Instance.forceUpdate = true;
        EnnemiesPanel.SetActive(true);
        CellsMechanicsPanel.SetActive(true);
        MapName.text = GameContext.CurrentMap.Map.Name;
        SetTool(ToolType.Select);
        BindDecoTools();
        UnSelectCell();
        UI_EnvironmentSettings.BindEnvironment(GameContext.CurrentMap.Grid.GetEnvironment());
        GameContext.UI_WorkerNotifier.Hide();
    }

    /// <summary>
    /// Saves the current authored map and preview to the local map library.
    /// </summary>
    public IEnumerator SaveMap(Action callback = null)
    {
        // Capture the preview before serializing the same workspace used by playtests.
        Canvas.gameObject.SetActive(false);
        GameContext.UI_WorkerNotifier.Show("Saving Map");
        yield return StartCoroutine(ScreenShotItems((bytes) =>
        {
            GameContext.CurrentMap.Map.AlreadyFinished = 0;
            GameContext.CurrentMap.Map.SetState(MapState.Draft);
            LocalMapRepository.Save(GameContext.CurrentMap, bytes);
            UpdateMap();
            Canvas.gameObject.SetActive(true);
            GameContext.UI_WorkerNotifier.Hide();
            callback?.Invoke();
        }));
    }

    /// <summary>
    /// Captures a square preview of the authored map.
    /// </summary>
    private IEnumerator ScreenShotItems(Action<byte[]> callback)
    {
        // Render from the dedicated orthographic preview camera.
        Camera cam = ScreenshotCamera;
        cam.gameObject.SetActive(true);
        int maxSize = Mathf.Max(GameContext.CurrentMap.Grid.Width, GameContext.CurrentMap.Grid.Height);
        Vector3 center = new Vector3(GameContext.CurrentMap.Grid.Width * 0.5f - 0.5f, maxSize, GameContext.CurrentMap.Grid.Height * 0.5f - 0.5f);
        cam.transform.position = center;

        // take screenshot
        int resWidth = 128;
        int resHeight = 128;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        cam.Render();
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        cam.gameObject.SetActive(false);
        callback(bytes);
    }

    /// <summary>
    /// Saves and launches the current map as a local playtest.
    /// </summary>
    public void PlayMap()
    {
        // Mark the session so leaving gameplay returns directly to the editor.
        StartCoroutine(SaveMap(() =>
        {
            PlayerPrefs.SetInt("ForceGrid", 1);
            GameContext.CurrentRules = new GameRules(
                false,
                false,
                GameContext.CurrentMap.Map.ID);
            StartCoroutine(LoadScene("Main"));
        }));
    }
    #endregion
}
