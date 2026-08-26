using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Owns map-authoring state, board interaction, persistence, and playtest transitions.
/// </summary>
public sealed class MapCreatorController : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public Camera Camera;
    public GridController GridController;
    public Material SelectedMaterial;
    public GameObject UnwalkableFeedbackPrefab;

    private readonly List<GameObject> _unwalkableFeedback =
        new List<GameObject>();
    private readonly List<string> _errors =
        new List<string>();
    private readonly List<CellPos> _pathBackup =
        new List<CellPos>();
    private MaterialChanger _materialChanger;
    private Cell _selectedCell;
    private MapDecoData _selectedMapDeco;
    private Ennemy _selectedEnnemy;
    private MovingPlatform _selectedMovingPlatform;
    private Action _postRenderCallback;
    private bool _pointerOverUi;
    private bool _keyboardCaptured;
    private bool _isReady;
    private bool _isBusy;
    private bool _isDirty;
    private string _statusMessage = string.Empty;
    private string _environmentId = "Default";
    private string _environmentLabel = "Par défaut";

    public ToolType CurrentTool { get; private set; }
    public string CurrentDecorationId { get; private set; } = string.Empty;
    public bool IsReady => _isReady;
    public bool IsBusy => _isBusy;
    public bool IsDirty => _isDirty;
    public bool IsPointerOverUi => _pointerOverUi;
    public string StatusMessage => _statusMessage;
    public string EnvironmentId => _environmentId;
    public string EnvironmentLabel => _environmentLabel;
    public IReadOnlyList<string> ValidationErrors => _errors;
    public Cell SelectedCell => _selectedCell;
    public Ennemy SelectedEnnemy => _selectedEnnemy;
    public MovingPlatform SelectedMovingPlatform => _selectedMovingPlatform;

    #region Lifecycle and input

    /// <summary>
    /// Initializes authoring services and installs path visualization.
    /// </summary>
    private void Awake()
    {
        // The old Canvas is intentionally disabled before child Start methods can recreate legacy controls.
        Transform obsoleteCanvas = transform.Find("Canvas");
        if (obsoleteCanvas != null)
            obsoleteCanvas.gameObject.SetActive(false);

        _materialChanger = new MaterialChanger();
        UndoableActionsManager.Clear();
        _postRenderCallback = DrawActivePaths;
        CameraOrbit.PostRenderCallback = _postRenderCallback;
    }

    /// <summary>
    /// Opens the current local workspace or creates a new authored map.
    /// </summary>
    private void Start()
    {
        // Continuing the local workspace makes the creator immediately useful after every scene entry.
        _isBusy = true;
        _statusMessage = "Chargement de la carte…";
        if (GridManager.GetGrid() != null && GridManager.GetMap() != null)
        {
            LoadMap();
            return;
        }

        const int defaultSize = 8;
        var metadata =
            new Map("Nouvelle carte", defaultSize, defaultSize);
        metadata.SetAuthor(SaveManager.CurrentSave.DisplayName);
        GameContext.CurrentMap =
            new FullMap(
                new Grid(defaultSize, defaultSize),
                metadata);
        GameContext.CurrentMap.Grid.Environment =
            GameContext.EnvironmentController.GetBuildinEnvironment(
                BuildInEnvironments.Default);
        _environmentId = BuildInEnvironments.Default.ToString();
        _environmentLabel =
            GetEnvironmentLabel(BuildInEnvironments.Default);
        UpdateMap();
        StartCoroutine(SaveMap(LoadMap));
    }

    /// <summary>
    /// Handles board clicks and keyboard authoring shortcuts outside Fugui surfaces.
    /// </summary>
    private void Update()
    {
        // UI capture prevents a button click or typed character from also mutating the board.
        if (!_isReady || _isBusy)
            return;

        if (!_pointerOverUi &&
            Input.GetMouseButtonDown(0))
        {
            CellPos? cellPosition = GetClickedCell();
            if (cellPosition.HasValue)
            {
                Cell cell =
                    GameContext.CurrentMap.Grid.GetCell(
                        cellPosition.Value);
                if (ApplyCurrentTool(cell))
                    MarkDirty("Carte modifiée");
                ValidateGrid();
            }
            else
            {
                UnselectCell();
            }
        }

        if (_keyboardCaptured)
            return;

        HandleSelectionShortcuts();
        HandleGlobalShortcuts();
        HandlePathShortcuts();
    }

    /// <summary>
    /// Releases path rendering and scene-selection feedback.
    /// </summary>
    private void OnDestroy()
    {
        // Only clear the shared callback when this controller still owns it.
        if (CameraOrbit.PostRenderCallback == _postRenderCallback)
            CameraOrbit.PostRenderCallback = null;
        ClearFeedbacks();
        _materialChanger?.ResetAllRenderers();
        _materialChanger?.Clear();
    }

    /// <summary>
    /// Handles Escape, delete, orientation, path, and focus shortcuts for the selected cell.
    /// </summary>
    private void HandleSelectionShortcuts()
    {
        // Selection commands share the same public actions as the Fugui inspector.
        if (Input.GetKeyDown(KeyCode.Escape) &&
            CurrentTool != ToolType.Path)
        {
            UnselectCell();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
            DeleteSelection();

        if (_selectedCell != null)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
                SetSelectionOrientation((int)Orientation.Up);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                SetSelectionOrientation((int)Orientation.Right);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                SetSelectionOrientation((int)Orientation.Down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                SetSelectionOrientation((int)Orientation.Left);

            if (Input.GetKeyDown(KeyCode.P))
                BeginPathEditing();
        }

        if (Input.GetKeyDown(KeyCode.Space))
            FocusSelection();
    }

    /// <summary>
    /// Handles save, tool, and history shortcuts that apply to the whole editor.
    /// </summary>
    private void HandleGlobalShortcuts()
    {
        // Control modifiers are accepted on both sides for keyboard-layout independence.
        bool control =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (control)
                RequestSave();
            else
                SetTool(ToolType.Select);
        }
        if (Input.GetKeyDown(KeyCode.W))
            SetTool(ToolType.Walkable);
        if (Input.GetKeyDown(KeyCode.N))
            SetTool(ToolType.UnWalkable);
        if (control && Input.GetKeyDown(KeyCode.Z))
            Undo();
        if (control && Input.GetKeyDown(KeyCode.Y))
            Redo();
    }

    /// <summary>
    /// Handles explicit confirmation and cancellation while a trajectory is being drawn.
    /// </summary>
    private void HandlePathShortcuts()
    {
        // Enter and Escape mirror the visible inspector actions.
        if (CurrentTool != ToolType.Path)
            return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmPathEditing();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPathEditing();
        }
    }

    /// <summary>
    /// Draws all paths on demand or the selected entity path during camera rendering.
    /// </summary>
    private void DrawActivePaths()
    {
        // Holding P keeps the useful overview behavior from the original creator.
        if (!enabled)
            return;

        if (Input.GetKey(KeyCode.P))
        {
            foreach (Ennemy enemy in GameContext.Ennemies)
                enemy.DrawPath(transform);
            foreach (MovingPlatform platform in GameContext.MovingPlatforms)
                platform.DrawPath(transform);
            return;
        }

        if (_selectedEnnemy != null)
            _selectedEnnemy.DrawPath(transform);
        else if (_selectedMovingPlatform != null)
            _selectedMovingPlatform.DrawPath(transform);
    }

    #endregion

    #region Presentation bridge

    /// <summary>
    /// Records whether the pointer currently overlaps a Fugui editor surface.
    /// </summary>
    public void SetPointerOverUi(bool pointerOverUi)
    {
        // Board raycasts and camera gestures consume the same presentation hit-test.
        _pointerOverUi = pointerOverUi;
    }

    /// <summary>
    /// Records whether a Fugui text field or modal currently owns keyboard input.
    /// </summary>
    public void SetKeyboardCaptured(bool keyboardCaptured)
    {
        // Captured typing must never trigger authoring shortcuts.
        _keyboardCaptured = keyboardCaptured;
    }

    /// <summary>
    /// Renames the active authored map and marks it dirty.
    /// </summary>
    public void RenameMap(string name)
    {
        // Empty names are rejected by both presentation and controller guards.
        if (GameContext.CurrentMap == null ||
            GameContext.CurrentMap.Map == null ||
            string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string normalized = name.Trim();
        if (GameContext.CurrentMap.Map.Name == normalized)
            return;

        GameContext.CurrentMap.Map.Name = normalized;
        MarkDirty("Nom de la carte modifié");
    }

    /// <summary>
    /// Selects a tool from its stable enum name.
    /// </summary>
    public void SelectTool(string toolId)
    {
        // String parsing keeps gameplay enums out of the Fugui presentation assembly.
        ToolType tool;
        if (Enum.TryParse(toolId, true, out tool))
            SetTool(tool);
    }

    /// <summary>
    /// Arms one authored decoration by its stable resource identifier.
    /// </summary>
    public void SelectDecoration(string decorationId)
    {
        // Resource validation avoids retaining an invalid palette selection.
        if (string.IsNullOrWhiteSpace(decorationId) ||
            !GameContext.Databases.ResourcesData.HasMapDeco(decorationId))
        {
            return;
        }

        _selectedMapDeco =
            GameContext.Databases.ResourcesData.GetMapDeco(decorationId);
        CurrentDecorationId = decorationId;
        CurrentTool = ToolType.MapDeco;
        _statusMessage =
            "Décor armé : " + _selectedMapDeco.ID;
    }

    /// <summary>
    /// Assigns a gear to the selected compatible cell from its stable enum name.
    /// </summary>
    public void SelectGear(string gearId)
    {
        // Invalid and incompatible assignments are ignored without disturbing selection.
        GearType gear;
        if (!Enum.TryParse(gearId, true, out gear))
            return;
        SetGearOnCell(gear);
    }

    /// <summary>
    /// Applies a built-in environment from its stable enum name.
    /// </summary>
    public void SelectEnvironment(string environmentId)
    {
        // Environment files remain authoritative for all visual tuning values.
        BuildInEnvironments environment;
        if (!Enum.TryParse(environmentId, true, out environment))
            return;
        SetBuildinEnvironment(environment);
    }

    /// <summary>
    /// Gets a localized display label for the active tool.
    /// </summary>
    public string GetCurrentToolLabel()
    {
        // Player-facing language stays stable even where legacy enum names contain typos.
        if (CurrentTool == ToolType.MapDeco &&
            !string.IsNullOrWhiteSpace(CurrentDecorationId))
        {
            return CurrentDecorationId;
        }

        switch (CurrentTool)
        {
            case ToolType.Select:
                return "Sélection";
            case ToolType.Walkable:
                return "Sol praticable";
            case ToolType.UnWalkable:
                return "Vide";
            case ToolType.Start:
                return "Départ";
            case ToolType.End:
                return "Sortie";
            case ToolType.CheckPoint:
                return "Point de contrôle";
            case ToolType.TP:
                return "Téléporteur";
            case ToolType.Collapsable:
                return "Sol fragile";
            case ToolType.FlipACoin:
                return "Pile ou face";
            case ToolType.Spring:
                return "Ressort";
            case ToolType.Bombe:
                return "Bombe temporisée";
            case ToolType.Needle:
                return "Pics";
            case ToolType.MovingPlatform:
                return "Plateforme mobile";
            case ToolType.SwordSocle:
                return "Socle d’épée";
            case ToolType.Path:
                return "Trajectoire";
            default:
                return CurrentTool.ToString();
        }
    }

    /// <summary>
    /// Gets the selected entity or floor orientation for inspector display.
    /// </summary>
    public string GetSelectionOrientationLabel()
    {
        // Elements can free-rotate, whereas floors and enemies use cardinal directions.
        if (_selectedCell == null)
            return "Aucune";
        if (!string.IsNullOrEmpty(_selectedCell.ElementID))
            return Mathf.RoundToInt(_selectedCell.ElementOrientation) + "°";
        if (_selectedEnnemy != null)
            return _selectedEnnemy.StartOrientation.ToString();
        return _selectedCell.FloorOrientation.ToString();
    }

    /// <summary>
    /// Reports whether the selected decoration supports free or stepped rotation.
    /// </summary>
    public bool CanRotateSelectedElement()
    {
        // Every placed decoration can at least rotate by authored ninety-degree steps.
        return _selectedCell != null &&
               !string.IsNullOrEmpty(_selectedCell.ElementID);
    }

    /// <summary>
    /// Reports whether the selected decoration supports scaling.
    /// </summary>
    public bool CanScaleSelectedElement()
    {
        // The resource database remains authoritative for per-asset scaling constraints.
        if (_selectedCell == null ||
            string.IsNullOrEmpty(_selectedCell.ElementID))
        {
            return false;
        }

        return GameContext.Databases.ResourcesData
            .GetMapDeco(_selectedCell.ElementID)
            .Scalable;
    }

    /// <summary>
    /// Reports whether the selected cell can contain a gear.
    /// </summary>
    public bool CanAssignGearToSelection()
    {
        // Gears occupy empty ordinary floor and never overlap entities or mechanics.
        return _selectedCell != null &&
               _selectedCell.Type == CellType.Walkable &&
               _selectedCell.EntityOnCell == null &&
               string.IsNullOrEmpty(_selectedCell.ElementID) &&
               !_selectedCell.HasElement;
    }

    /// <summary>
    /// Gets the number of authored trajectory points on the selected entity.
    /// </summary>
    public int GetSelectedPathPointCount()
    {
        // Enemy and moving-platform paths share the same inspector metric.
        if (_selectedEnnemy != null &&
            _selectedEnnemy.WayPoints != null)
        {
            return _selectedEnnemy.WayPoints.Count;
        }
        if (_selectedMovingPlatform != null &&
            _selectedMovingPlatform.WayPoints != null)
        {
            return _selectedMovingPlatform.WayPoints.Count;
        }
        return 0;
    }

    #endregion

    #region Tools and board mutation

    /// <summary>
    /// Arms one board tool and safely resolves active path editing.
    /// </summary>
    public void SetTool(ToolType tool)
    {
        // Switching away from an unfinished path restores its previous authored value.
        if (CurrentTool == ToolType.Path &&
            tool != ToolType.Path)
        {
            CancelPathEditing();
        }

        CurrentTool = tool;
        if (tool != ToolType.MapDeco)
        {
            _selectedMapDeco = null;
            CurrentDecorationId = string.Empty;
        }
        _statusMessage =
            "Outil actif : " + GetCurrentToolLabel();
    }

    /// <summary>
    /// Applies the active authoring tool to one clicked board cell.
    /// </summary>
    private bool ApplyCurrentTool(Cell cell)
    {
        // Returning a mutation flag centralizes dirty-state handling for all board clicks.
        if (cell == null)
            return false;

        switch (CurrentTool)
        {
            case ToolType.Select:
                SelectCell(cell);
                return false;
            case ToolType.Walkable:
                return SetCellType(cell, CellType.Walkable, true);
            case ToolType.UnWalkable:
                if (cell.EntityOnCell != null || cell.HasElement)
                    return false;
                return SetCellType(cell, CellType.NotWalkable, false);
            case ToolType.MapDeco:
                return PlaceDecoration(cell);
            case ToolType.TP:
                return SetCellType(cell, CellType.TP, true);
            case ToolType.FlipACoin:
                return SetCellType(cell, CellType.FlipACoin, true);
            case ToolType.Collapsable:
                return SetCellType(cell, CellType.Collapsable, true);
            case ToolType.Needle:
                if (!SetCellType(cell, CellType.Needle, true))
                    return false;
                cell.Arg1 = 0;
                return true;
            case ToolType.Spring:
                if (!SetCellType(cell, CellType.Spring, true))
                    return false;
                cell.Arg1 = 5;
                return true;
            case ToolType.Bombe:
                if (!SetCellType(cell, CellType.Bombe, true))
                    return false;
                cell.Arg1 = 10;
                return true;
            case ToolType.SwordSocle:
                return SetCellType(cell, CellType.SwordSocle, true);
            case ToolType.Start:
                return SetStartCell(cell);
            case ToolType.CheckPoint:
                return SetCellType(cell, CellType.CheckPoint, true);
            case ToolType.End:
                return SetEndCell(cell);
            case ToolType.MovingPlatform:
                return AddMovingPlatform(cell);
            case ToolType.Path:
                AddPathPoint(cell);
                return false;
            case ToolType.Cyclops:
            case ToolType.Gloutton:
            case ToolType.Keeper:
            case ToolType.Knight:
            case ToolType.Scoute:
            case ToolType.Trap:
            case ToolType.Lizardman:
            case ToolType.Thunderbird:
            case ToolType.Axe:
            case ToolType.Hammer:
            case ToolType.Catapult:
            case ToolType.Pillar:
                return AddEnemy((EnnemyType)CurrentTool, cell);
            default:
                return false;
        }
    }

    /// <summary>
    /// Changes one cell type, walkability, visuals, and selection feedback.
    /// </summary>
    private bool SetCellType(
        Cell cell,
        CellType type,
        bool walkable)
    {
        // Occupied authored decorations cannot be silently overwritten by mechanics.
        if (cell.HasElement ||
            !string.IsNullOrEmpty(cell.ElementID))
        {
            SelectCell(cell);
            return false;
        }
        if (type != CellType.Walkable &&
            (GameContext.CurrentMap.Grid.IsStart(cell) ||
             GameContext.CurrentMap.Grid.IsEnd(cell)))
        {
            _statusMessage =
                "Déplace d’abord le départ ou la sortie";
            SelectCell(cell);
            return false;
        }

        if (cell.Type == type &&
            cell.Walkable == walkable)
        {
            SelectCell(cell);
            return false;
        }

        UndoableActionsManager.AddUndoableAction(
            new Undoable_SetCellType(
                cell.GetCellPos(),
                cell.Type,
                type));
        cell.SetType(type);
        cell.SetWalkable(walkable);
        RefreshCell(cell);
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Places the armed floor or decoration asset on a compatible cell.
    /// </summary>
    private bool PlaceDecoration(Cell cell)
    {
        // Clicking occupied content selects it instead of replacing it unexpectedly.
        if (cell.EntityOnCell != null ||
            !string.IsNullOrEmpty(cell.ElementID) ||
            cell.HasElement)
        {
            SelectCell(cell);
            return false;
        }
        if (_selectedMapDeco == null ||
            cell.Type != CellType.Walkable)
        {
            return false;
        }

        if (_selectedMapDeco.Type == "Floor")
        {
            UndoableActionsManager.AddUndoableAction(
                new Undoable_SetFloorType(
                    cell.GetCellPos(),
                    cell.FloorID,
                    _selectedMapDeco.ID));
            cell.FloorID = _selectedMapDeco.ID;
        }
        else
        {
            UndoableActionsManager.AddUndoableAction(
                new Undoable_SetElement(
                    cell.GetCellPos(),
                    cell.ElementID,
                    _selectedMapDeco.ID));
            cell.SetElement(_selectedMapDeco.ID);
        }

        RefreshCell(cell);
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Sets a new start cell while preserving an ordinary walkable floor.
    /// </summary>
    private bool SetStartCell(Cell cell)
    {
        // Start cannot overlap authored content or an enemy.
        if (cell.HasElement ||
            cell.EntityOnCell != null ||
            !string.IsNullOrEmpty(cell.ElementID) ||
            GameContext.CurrentMap.Grid.IsEnd(cell))
        {
            return false;
        }
        if (GameContext.CurrentMap.Grid.IsStart(cell))
            return false;

        cell.SetWalkable(true);
        cell.SetType(CellType.Walkable);
        GameContext.CurrentMap.Grid.SetStart(cell);
        UpdateMap();
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Sets a new exit cell while preserving an ordinary walkable floor.
    /// </summary>
    private bool SetEndCell(Cell cell)
    {
        // Exit cannot overlap authored content or an enemy.
        if (cell.HasElement ||
            cell.EntityOnCell != null ||
            !string.IsNullOrEmpty(cell.ElementID) ||
            GameContext.CurrentMap.Grid.IsStart(cell))
        {
            return false;
        }
        if (GameContext.CurrentMap.Grid.IsEnd(cell))
            return false;

        cell.SetWalkable(true);
        cell.SetType(CellType.Walkable);
        GameContext.CurrentMap.Grid.SetEnd(cell);
        UpdateMap();
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Adds a moving platform to one empty non-walkable cell.
    /// </summary>
    private bool AddMovingPlatform(Cell cell)
    {
        // Platforms originate over void and cannot overlap any authored object.
        if (cell.EntityOnCell != null ||
            !string.IsNullOrEmpty(cell.ElementID) ||
            cell.HasElement ||
            cell.Type != CellType.NotWalkable ||
            GridController.Platforms.ContainsKey(cell.ToString()))
        {
            return false;
        }

        SerializableMovingPlatform platform =
            GameContext.CurrentMap.Grid.AddMovingPlatform(
                cell.GetCellPos());
        GridController.SpawnPlatform(
            GameContext.CurrentMap.Grid,
            platform);
        _selectedMovingPlatform =
            GridController.Platforms[cell.ToString()]
                .GetComponent<MovingPlatform>();
        SetTool(ToolType.Select);
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Adds one enemy to a compatible empty walkable cell.
    /// </summary>
    private bool AddEnemy(
        EnnemyType type,
        Cell cell)
    {
        // All retained enemy families follow the same placement guard.
        if (cell.EntityOnCell != null ||
            cell.HasElement ||
            !string.IsNullOrEmpty(cell.ElementID) ||
            !cell.Walkable ||
            GameContext.CurrentMap.Grid.GetEnnemyOnCell(cell) != null)
        {
            return false;
        }

        SerializableEnnemy serialized =
            GameContext.CurrentMap.Grid.AddEnnemy(
                type,
                cell.GetCellPos());
        _selectedEnnemy =
            GridController.SpawnEnnemy(
                GameContext.CurrentMap.Grid,
                serialized);
        GridController.AddEnnemyCollider(_selectedEnnemy);
        SetTool(ToolType.Select);
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Deletes all authored content associated with the selected cell.
    /// </summary>
    public void DeleteSelection()
    {
        // Deletion is explicit, context-safe, and refreshes validation immediately.
        if (_selectedCell == null ||
            GameContext.CurrentMap == null)
        {
            return;
        }

        Cell cell = _selectedCell;
        bool changed = false;
        if (!string.IsNullOrEmpty(cell.ElementID))
        {
            cell.RemoveElement();
            changed = true;
        }
        if (cell.GearType != GearType.None)
        {
            cell.SetGear(GearType.None);
            changed = true;
        }
        if (_selectedEnnemy != null)
        {
            GameContext.CurrentMap.Grid.RemoveEnnemy(
                cell.GetCellPos());
            changed = true;
        }
        if (_selectedMovingPlatform != null)
        {
            GameContext.CurrentMap.Grid.RemoveMovingPlatform(
                cell.GetCellPos());
            changed = true;
        }
        if (cell.Type != CellType.Walkable)
        {
            cell.SetType(CellType.Walkable);
            cell.SetWalkable(true);
            changed = true;
        }

        if (!changed)
            return;

        UnselectCell();
        UpdateMap();
        ValidateGrid();
        MarkDirty("Contenu supprimé");
    }

    /// <summary>
    /// Assigns one retained gear type to the selected compatible cell.
    /// </summary>
    public void SetGearOnCell(GearType type)
    {
        // The compatibility guard protects entity and mechanic cells from invalid gear overlap.
        if (!CanAssignGearToSelection() ||
            _selectedCell.GearType == type)
        {
            return;
        }

        _selectedCell.SetGear(type);
        RefreshCell(_selectedCell);
        SelectCell(_selectedCell);
        MarkDirty("Gear assigné");
        ValidateGrid();
    }

    /// <summary>
    /// Applies a cardinal orientation to the selected element, enemy, or floor.
    /// </summary>
    public void SetSelectionOrientation(int orientationId)
    {
        // Invalid enum values are ignored before mutating authored state.
        if (_selectedCell == null ||
            !Enum.IsDefined(typeof(Orientation), orientationId) ||
            orientationId == (int)Orientation.None)
        {
            return;
        }

        Orientation orientation =
            (Orientation)orientationId;
        if (!string.IsNullOrEmpty(_selectedCell.ElementID))
        {
            float angle = GetOrientationAngle(orientation);
            if (Mathf.Approximately(
                    _selectedCell.ElementOrientation,
                    angle))
            {
                return;
            }
            _selectedCell.ElementOrientation = angle;
        }
        else if (_selectedEnnemy != null)
        {
            SerializableEnnemy enemy =
                GameContext.CurrentMap.Grid.GetEnnemyOnCell(
                    _selectedEnnemy.StartCell);
            if (enemy != null)
                enemy.StartOrientation = orientation;
            _selectedEnnemy.SetOrientation(orientation);
            _selectedEnnemy.RefreshPO();
        }
        else
        {
            if (_selectedCell.FloorOrientation == orientation)
                return;
            _selectedCell.FloorOrientation = orientation;
        }

        RefreshCell(_selectedCell);
        SelectCell(_selectedCell);
        MarkDirty("Orientation modifiée");
    }

    /// <summary>
    /// Rotates the selected decoration by a relative angle.
    /// </summary>
    public void RotateSelectedElement(float deltaDegrees)
    {
        // Authored constraints snap non-free decorations to ninety-degree steps.
        if (!CanRotateSelectedElement())
            return;

        MapDecoData data =
            GameContext.Databases.ResourcesData.GetMapDeco(
                _selectedCell.ElementID);
        float angle =
            Mathf.Repeat(
                _selectedCell.ElementOrientation + deltaDegrees,
                360f);
        if (!data.CanFreeRotate())
            angle = Mathf.Round(angle / 90f) * 90f;
        _selectedCell.ElementOrientation = angle;
        RefreshCell(_selectedCell);
        SelectCell(_selectedCell);
        MarkDirty("Décor pivoté");
    }

    /// <summary>
    /// Scales the selected decoration by a bounded relative amount.
    /// </summary>
    public void ScaleSelectedElement(float delta)
    {
        // The original editor's safe scale range remains enforced.
        if (!CanScaleSelectedElement())
            return;

        float scale =
            Mathf.Clamp(
                _selectedCell.ElementScale + delta,
                0.5f,
                3f);
        if (Mathf.Approximately(
                scale,
                _selectedCell.ElementScale))
        {
            return;
        }

        _selectedCell.ElementScale = scale;
        RefreshCell(_selectedCell);
        SelectCell(_selectedCell);
        MarkDirty("Échelle du décor modifiée");
    }

    /// <summary>
    /// Adjusts the selected spring range or timed-bomb delay.
    /// </summary>
    public void AdjustSelectedArgument(int delta)
    {
        // Mechanic-specific bounds prevent zero-turn bombs and unusable springs.
        if (_selectedCell == null ||
            (_selectedCell.Type != CellType.Spring &&
             _selectedCell.Type != CellType.Bombe))
        {
            return;
        }

        int maximum =
            _selectedCell.Type == CellType.Spring ? 20 : 99;
        int value =
            Mathf.Clamp(_selectedCell.Arg1 + delta, 1, maximum);
        if (value == _selectedCell.Arg1)
            return;

        _selectedCell.Arg1 = value;
        RefreshCell(_selectedCell);
        SelectCell(_selectedCell);
        MarkDirty("Paramètre de mécanique modifié");
    }

    /// <summary>
    /// Toggles immediate activation on the selected needle trap.
    /// </summary>
    public void ToggleSelectedNeedle()
    {
        // The serialized integer representation remains compatible with existing maps.
        if (_selectedCell == null ||
            _selectedCell.Type != CellType.Needle)
        {
            return;
        }

        _selectedCell.Arg1 =
            _selectedCell.Arg1 == 1 ? 0 : 1;
        RefreshCell(_selectedCell);
        SelectCell(_selectedCell);
        MarkDirty("Déclenchement des pics modifié");
    }

    /// <summary>
    /// Converts one cardinal orientation to its authored Y-axis angle.
    /// </summary>
    private static float GetOrientationAngle(
        Orientation orientation)
    {
        // Angles match Unity's clockwise Y-axis convention used by map decorations.
        switch (orientation)
        {
            case Orientation.Right:
                return 90f;
            case Orientation.Down:
                return 180f;
            case Orientation.Left:
                return 270f;
            default:
                return 0f;
        }
    }

    #endregion

    #region Path authoring

    /// <summary>
    /// Starts a reversible trajectory edit for the selected enemy or moving platform.
    /// </summary>
    public void BeginPathEditing()
    {
        // A backup makes Escape genuinely non-destructive.
        if (_selectedEnnemy == null &&
            _selectedMovingPlatform == null)
        {
            return;
        }

        _pathBackup.Clear();
        List<CellPos> waypoints =
            GetSelectedWaypoints();
        if (waypoints != null)
            _pathBackup.AddRange(waypoints);
        SetSelectedWaypoints(new List<CellPos>());
        CurrentTool = ToolType.Path;
        CurrentDecorationId = string.Empty;
        _statusMessage =
            "Trajectoire : clique des points alignés, puis valide avec Entrée";
    }

    /// <summary>
    /// Adds one aligned waypoint to the active trajectory.
    /// </summary>
    private bool AddPathPoint(Cell cell)
    {
        // Enemy paths stay on walkable cells while platforms stay over void.
        if (cell == null ||
            !string.IsNullOrEmpty(cell.ElementID) ||
            cell.HasElement)
        {
            return false;
        }

        List<CellPos> waypoints =
            GetSelectedWaypoints();
        if (waypoints == null)
            return false;
        if (_selectedEnnemy != null && !cell.Walkable)
            return false;
        if (_selectedMovingPlatform != null &&
            cell.Type != CellType.NotWalkable)
        {
            return false;
        }
        if (waypoints.Count > 0 &&
            !cell.IsInLine(waypoints.Last()))
        {
            _statusMessage =
                "Point refusé : chaque segment doit rester horizontal ou vertical";
            return false;
        }

        waypoints.Add(cell.GetCellPos());
        _statusMessage =
            "Trajectoire : " + waypoints.Count + " point(s)";
        return true;
    }

    /// <summary>
    /// Validates and commits the active trajectory.
    /// </summary>
    public void ConfirmPathEditing()
    {
        // Enemy-specific path rules remain authoritative before committing the draft.
        if (CurrentTool != ToolType.Path)
            return;
        if (_selectedEnnemy != null &&
            !GameContext.CurrentMap.Grid.ValidateEnnemyPath(
                _selectedEnnemy))
        {
            _statusMessage =
                "Trajectoire invalide : corrige-la ou annule avec Échap";
            return;
        }

        _pathBackup.Clear();
        CurrentTool = ToolType.Select;
        MarkDirty("Trajectoire validée");
        ValidateGrid();
    }

    /// <summary>
    /// Restores the trajectory that existed before the current path edit.
    /// </summary>
    public void CancelPathEditing()
    {
        // Cancellation restores an independent list so future edits cannot mutate the backup.
        if (CurrentTool != ToolType.Path)
            return;

        SetSelectedWaypoints(
            new List<CellPos>(_pathBackup));
        _pathBackup.Clear();
        CurrentTool = ToolType.Select;
        _statusMessage = "Modification de trajectoire annulée";
    }

    /// <summary>
    /// Gets the mutable waypoint list of the selected path-capable entity.
    /// </summary>
    private List<CellPos> GetSelectedWaypoints()
    {
        // Both runtime entity types expose the same serialized waypoint shape.
        if (_selectedEnnemy != null)
            return _selectedEnnemy.WayPoints;
        return _selectedMovingPlatform != null
            ? _selectedMovingPlatform.WayPoints
            : null;
    }

    /// <summary>
    /// Replaces the waypoint list of the selected path-capable entity.
    /// </summary>
    private void SetSelectedWaypoints(
        List<CellPos> waypoints)
    {
        // List replacement matches the serialization model used by enemies and platforms.
        if (_selectedEnnemy != null)
            _selectedEnnemy.WayPoints = waypoints;
        else if (_selectedMovingPlatform != null)
            _selectedMovingPlatform.WayPoints = waypoints;
    }

    #endregion

    #region Selection and refresh

    /// <summary>
    /// Raycasts the board and returns the clicked cell position.
    /// </summary>
    private CellPos? GetClickedCell()
    {
        // Cell GameObjects retain their established x_y names for fast coordinate lookup.
        if (Camera == null)
            return null;

        Ray ray =
            Camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit))
            return null;

        string cellName = hit.transform.gameObject.name;
        if (!cellName.Contains("_"))
            return null;

        string[] parts = cellName.Split('_');
        int x;
        int y;
        if (parts.Length < 2 ||
            !int.TryParse(parts[0], out x) ||
            !int.TryParse(parts[1], out y))
        {
            return null;
        }

        Cell cell =
            GameContext.CurrentMap.Grid.GetCell(x, y);
        if (cell == null ||
            GridController.GetCellGameObject(cell) == null)
        {
            return null;
        }
        return cell.GetCellPos();
    }

    /// <summary>
    /// Selects one cell and resolves any enemy or moving platform occupying it.
    /// </summary>
    private void SelectCell(Cell cell)
    {
        // Selection changes refresh the previous highlight without rebuilding the whole map.
        if (cell == null)
            return;

        if (_selectedCell != null &&
            !_selectedCell.Equals(cell))
        {
            ClearSelectionHighlight(_selectedCell);
        }

        _selectedCell = cell;
        _selectedEnnemy =
            cell.EntityOnCell as Ennemy;
        _selectedMovingPlatform = null;
        GameObject platformObject;
        if (_selectedEnnemy == null &&
            GridController.Platforms.TryGetValue(
                cell.ToString(),
                out platformObject))
        {
            _selectedMovingPlatform =
                platformObject.GetComponent<MovingPlatform>();
        }

        DrawSelectionHighlight();
        _statusMessage =
            "Case sélectionnée : " + cell.X + ", " + cell.Y;
    }

    /// <summary>
    /// Clears the active selection while preserving an in-progress trajectory edit.
    /// </summary>
    private void UnselectCell()
    {
        // Path editing keeps its entity context until the user explicitly validates or cancels.
        if (CurrentTool == ToolType.Path)
            return;

        if (_selectedCell != null)
            ClearSelectionHighlight(_selectedCell);
        _selectedCell = null;
        _selectedEnnemy = null;
        _selectedMovingPlatform = null;
        _statusMessage =
            "Sélection effacée";
    }

    /// <summary>
    /// Draws material feedback around the current selected cell or platform.
    /// </summary>
    private bool DrawSelectionHighlight()
    {
        // Renderer material replacement remains the established readable 3D selection cue.
        if (_selectedCell == null)
            return false;

        _materialChanger.ResetAllRenderers();
        _materialChanger.Clear();
        GameObject cellObject =
            GridController.GetCellGameObject(_selectedCell);
        if (_selectedMovingPlatform != null)
        {
            GridController.Platforms.TryGetValue(
                _selectedCell.ToString(),
                out cellObject);
        }
        if (cellObject == null)
            return false;

        foreach (Renderer renderer in
                 cellObject.GetComponentsInChildren<Renderer>())
        {
            _materialChanger.AddRenderer(
                renderer,
                renderer.materials);
        }
        _materialChanger.SetMaterial(SelectedMaterial);
        return true;
    }

    /// <summary>
    /// Restores normal visuals and colliders for one previously selected cell.
    /// </summary>
    private void ClearSelectionHighlight(Cell cell)
    {
        // Refreshing the cross removes instantiated material copies and keeps adjacency correct.
        _materialChanger.ResetAllRenderers();
        _materialChanger.Clear();
        if (cell == null ||
            GameContext.CurrentMap == null)
        {
            return;
        }

        GridController.RefreshCrossCell(
            cell,
            GameContext.CurrentMap.Grid,
            true);
        GridController.AddCellColider(cell, true);
    }

    /// <summary>
    /// Reframes the creator camera around the selected cell or the map center.
    /// </summary>
    public void FocusSelection()
    {
        // The same action works as a reliable camera reset when nothing is selected.
        if (CameraOrbit.Instance == null ||
            GameContext.CurrentMap == null)
        {
            return;
        }

        if (_selectedCell != null)
        {
            CameraOrbit.Instance.targetPos =
                _selectedCell.GetCellPos().ToVector3(0.2f);
        }
        else
        {
            CameraOrbit.Instance.targetPos =
                new Vector3(
                    GameContext.CurrentMap.Grid.Width * 0.5f,
                    1f,
                    GameContext.CurrentMap.Grid.Height * 0.5f);
        }
        CameraOrbit.Instance.distance = 15f;
        CameraOrbit.Instance.forceUpdate = true;
    }

    /// <summary>
    /// Refreshes one cell, its cross, colliders, element occupancy, and selection feedback.
    /// </summary>
    private void RefreshCell(Cell cell)
    {
        // Local refresh keeps interactive authoring responsive on larger maps.
        if (cell == null)
            return;

        _materialChanger.ResetAllRenderers();
        _materialChanger.Clear();
        GridController.RefreshCrossCell(
            cell,
            GameContext.CurrentMap.Grid,
            true);
        GridController.AddCellColider(cell, true);
        DrawFeedbacks();
    }

    /// <summary>
    /// Rebuilds all board visuals, entities, colliders, and environment state.
    /// </summary>
    public void UpdateMap()
    {
        // Full rebuild is reserved for structural operations, history, load, and deletion.
        GridController.ClearGrid();
        GridController.DrawGrid(GameContext.CurrentMap.Grid);
        GridController.SpawnEnnemies(GameContext.CurrentMap.Grid);
        GridController.SpawnPlatforms(GameContext.CurrentMap.Grid);
        GridController.AddEnnemiesColliders();
        GridController.AddCellsColiders(GameContext.CurrentMap.Grid);
        GameContext.SetEnvironment(GameContext.CurrentMap);
        DrawFeedbacks();
        if (_selectedCell != null)
        {
            _selectedCell =
                GameContext.CurrentMap.Grid.GetCell(
                    _selectedCell.GetCellPos());
            SelectCell(_selectedCell);
        }
    }

    /// <summary>
    /// Validates the current grid and refreshes all blocking authoring errors.
    /// </summary>
    public void ValidateGrid()
    {
        // Validation is cheap enough to keep accurate after every authored mutation.
        _errors.Clear();
        if (GameContext.CurrentMap != null &&
            GameContext.CurrentMap.Grid != null)
        {
            GameContext.CurrentMap.Grid.ValidateGrid(
                ref _errors);
        }
    }

    /// <summary>
    /// Marks the current revision dirty and exposes concise status feedback.
    /// </summary>
    private void MarkDirty(string message)
    {
        // Dirty state is cleared only after persistence completes successfully.
        _isDirty = true;
        _statusMessage = message ?? "Carte modifiée";
    }

    #endregion

    #region Element occupancy feedback

    /// <summary>
    /// Recomputes all decoration occupancy and redraws unwalkable feedback markers.
    /// </summary>
    public void DrawFeedbacks()
    {
        // Multi-cell decoration bounds stay visible while authoring nearby content.
        ResetElementOccupancy();
        ProcessElementOccupancy();
        ClearFeedbacks();
        for (int x = 0;
             x < GameContext.CurrentMap.Grid.Width;
             x++)
        {
            for (int y = 0;
                 y < GameContext.CurrentMap.Grid.Height;
                 y++)
            {
                Cell cell =
                    GameContext.CurrentMap.Grid.GetCell(x, y);
                if (!cell.HasElement)
                    continue;

                GameObject feedback =
                    Instantiate(UnwalkableFeedbackPrefab);
                feedback.transform.position =
                    cell.GetCellPos().ToVector3(0f);
                _unwalkableFeedback.Add(feedback);
            }
        }
    }

    /// <summary>
    /// Clears transient multi-cell decoration occupancy flags.
    /// </summary>
    private static void ResetElementOccupancy()
    {
        // Occupancy is derived data and is recalculated from every placed decoration.
        for (int x = 0;
             x < GameContext.CurrentMap.Grid.Width;
             x++)
        {
            for (int y = 0;
                 y < GameContext.CurrentMap.Grid.Height;
                 y++)
            {
                GameContext.CurrentMap.Grid
                    .GetCell(x, y)
                    .HasElement = false;
            }
        }
    }

    /// <summary>
    /// Marks cells covered by every rotated multi-cell decoration.
    /// </summary>
    private static void ProcessElementOccupancy()
    {
        // Authored unwalkable offsets are rotated around their decoration's anchor cell.
        for (int x = 0;
             x < GameContext.CurrentMap.Grid.Width;
             x++)
        {
            for (int y = 0;
                 y < GameContext.CurrentMap.Grid.Height;
                 y++)
            {
                Cell anchor =
                    GameContext.CurrentMap.Grid.GetCell(x, y);
                if (string.IsNullOrEmpty(anchor.ElementID))
                    continue;

                MapDecoData data =
                    GameContext.Databases.ResourcesData.GetMapDeco(
                        anchor.ElementID);
                foreach (MapDecoCellPos offset in data.UnWalkableCells)
                {
                    Vector3 rotated =
                        Quaternion.Euler(
                            0f,
                            anchor.ElementOrientation,
                            0f) *
                        new Vector3(offset.X, 0f, offset.Y);
                    Cell occupied =
                        GameContext.CurrentMap.Grid.GetCell(
                            new CellPos(
                                anchor.X + Mathf.RoundToInt(rotated.x),
                                anchor.Y + Mathf.RoundToInt(rotated.z)));
                    if (occupied != null)
                        occupied.HasElement = true;
                }
            }
        }
    }

    /// <summary>
    /// Destroys all transient decoration occupancy feedback objects.
    /// </summary>
    private void ClearFeedbacks()
    {
        // Feedback objects are runtime-only and never persist with map content.
        foreach (GameObject feedback in _unwalkableFeedback)
        {
            if (feedback != null)
                Destroy(feedback);
        }
        _unwalkableFeedback.Clear();
    }

    #endregion

    #region Environment, history, persistence, and playtest

    /// <summary>
    /// Applies one built-in environment preset and marks the revision dirty.
    /// </summary>
    public void SetBuildinEnvironment(
        BuildInEnvironments environment)
    {
        // The full environment payload is copied into the active map before live preview.
        if (GameContext.CurrentMap == null)
            return;

        GameContext.CurrentMap.Grid.Environment =
            GameContext.EnvironmentController.GetBuildinEnvironment(
                environment);
        _environmentId = environment.ToString();
        _environmentLabel = GetEnvironmentLabel(environment);
        GameContext.SetEnvironment(GameContext.CurrentMap);
        MarkDirty("Ambiance appliquée : " + _environmentLabel);
    }

    /// <summary>
    /// Updates one bounded numeric environment value from its stable editor identifier.
    /// </summary>
    public void SetEnvironmentValue(
        string valueId,
        float value)
    {
        // The adapter supplies bounds while this switch preserves the authoritative serialized property map.
        if (GameContext.CurrentMap == null ||
            string.IsNullOrWhiteSpace(valueId))
        {
            return;
        }

        EnvironmentSettings environment =
            GameContext.CurrentMap.Grid.GetEnvironment();
        switch (valueId)
        {
            case "ParticleSize":
                environment.ParticleSize = value;
                break;
            case "ParticleDensity":
                environment.ParticleDensity = value;
                break;
            case "FoamScale":
                environment.FoamScale = value;
                break;
            case "SunIntensity":
                environment.SunIntensity = value;
                break;
            case "Seed":
                environment.Seed = value;
                break;
            case "Scale":
                environment.Scale = value;
                break;
            case "Contrast":
                environment.Contrast = value;
                break;
            case "AnimationSpeed":
                environment.AnimationSpeed = value;
                break;
            case "DustAmount":
                environment.DustAmount = value;
                break;
            case "Nebular1Strength":
                environment.Nebular1Strength = value;
                break;
            case "Nebular2Strength":
                environment.Nebular2Strength = value;
                break;
            case "SunSize":
                environment.SunSize = value;
                break;
            case "SunSkyIntensity":
                environment.SunSkyIntensity = value;
                break;
            case "SunHorizontal":
                environment.SunHorizontal = value;
                break;
            case "SunUp":
                environment.SunUp = value;
                break;
            default:
                return;
        }

        ApplyCustomEnvironment(environment);
    }

    /// <summary>
    /// Updates one environment color from its stable editor identifier.
    /// </summary>
    public void SetEnvironmentColor(
        string colorId,
        Color color)
    {
        // All color values use the existing serializable vector representation used by map JSON.
        if (GameContext.CurrentMap == null ||
            string.IsNullOrWhiteSpace(colorId))
        {
            return;
        }

        EnvironmentSettings environment =
            GameContext.CurrentMap.Grid.GetEnvironment();
        var serializedColor =
            new Serializable_Vector4(color);
        switch (colorId)
        {
            case "FoamColor":
                environment.FoamColor = serializedColor;
                break;
            case "DepthDeep":
                environment.DepthDeep = serializedColor;
                break;
            case "DepthShallow":
                environment.DepthShallow = serializedColor;
                break;
            case "SunColor":
                environment.SunColor = serializedColor;
                break;
            case "SkyColor":
                environment.SkyColor = serializedColor;
                break;
            case "EquatorColor":
                environment.EquatorColor = serializedColor;
                break;
            case "GroundColor":
                environment.GroundColor = serializedColor;
                break;
            case "SunSkyColor":
                environment.SunSkyColor = serializedColor;
                break;
            case "Nebular1ColorMain":
                environment.Nebular1ColorMain = serializedColor;
                break;
            case "Nebular1ColorMid":
                environment.Nebular1ColorMid = serializedColor;
                break;
            case "Nebular2Color1":
                environment.Nebular2Color1 = serializedColor;
                break;
            case "Nebular2Color2":
                environment.Nebular2Color2 = serializedColor;
                break;
            default:
                return;
        }

        ApplyCustomEnvironment(environment);
    }

    /// <summary>
    /// Applies a modified environment payload and records a custom dirty revision.
    /// </summary>
    private void ApplyCustomEnvironment(
        EnvironmentSettings environment)
    {
        // Live preview remains immediate for numeric and color edits.
        GameContext.CurrentMap.Grid.Environment = environment;
        _environmentId = string.Empty;
        _environmentLabel = "Personnalisée";
        GameContext.SetEnvironment(GameContext.CurrentMap);
        MarkDirty("Réglages d’ambiance modifiés");
    }

    /// <summary>
    /// Restores the previous supported authoring action.
    /// </summary>
    public void Undo()
    {
        // History operations rebuild the board because their actions mutate serialized cell data.
        if (!UndoableActionsManager.CanUndo)
            return;
        UndoableActionsManager.Undo();
        UpdateMap();
        ValidateGrid();
        MarkDirty("Action annulée");
    }

    /// <summary>
    /// Reapplies the next supported authoring action.
    /// </summary>
    public void Redo()
    {
        // Redo shares the same complete refresh guarantees as undo.
        if (!UndoableActionsManager.CanRedo)
            return;
        UndoableActionsManager.Redo();
        UpdateMap();
        ValidateGrid();
        MarkDirty("Action rétablie");
    }

    /// <summary>
    /// Loads the active local workspace into the creator.
    /// </summary>
    public void LoadMap()
    {
        // Loading resets transient history and exposes a clean saved revision.
        UndoableActionsManager.Clear();
        GameContext.CurrentMap = GridManager.GetFullMap();
        _selectedCell = null;
        _selectedEnnemy = null;
        _selectedMovingPlatform = null;
        CurrentTool = ToolType.Select;
        CurrentDecorationId = string.Empty;
        _environmentId = string.Empty;
        _environmentLabel = "Personnalisée";
        UpdateMap();
        ValidateGrid();
        FocusSelection();
        _isReady = true;
        _isBusy = false;
        _isDirty = false;
        _statusMessage = "Carte prête";
    }

    /// <summary>
    /// Starts a non-overlapping save operation for the current map.
    /// </summary>
    public void RequestSave()
    {
        // Repeated clicks during preview capture are ignored.
        if (!_isReady || _isBusy)
            return;
        StartCoroutine(SaveMap());
    }

    /// <summary>
    /// Saves the current authored map and preview to the local map library.
    /// </summary>
    public IEnumerator SaveMap(
        Action callback = null)
    {
        // Preview capture and serialization complete as one visible busy operation.
        if (_isBusy && _isReady)
            yield break;

        _isBusy = true;
        _statusMessage = "Enregistrement de la carte…";
        yield return StartCoroutine(
            CapturePreview(
                bytes =>
                {
                    GameContext.CurrentMap.Map.AlreadyFinished = 0;
                    GameContext.CurrentMap.Map.SetState(MapState.Draft);
                    LocalMapRepository.Save(
                        GameContext.CurrentMap,
                        bytes);
                }));
        UpdateMap();
        _isDirty = false;
        _isBusy = false;
        _statusMessage = "Carte enregistrée";
        callback?.Invoke();
    }

    /// <summary>
    /// Captures a square preview of all authored board content.
    /// </summary>
    private IEnumerator CapturePreview(
        Action<byte[]> callback)
    {
        // A dedicated orthographic camera produces deterministic library thumbnails.
        Camera camera = ScreenshotCamera;
        if (camera == null)
        {
            callback?.Invoke(new byte[0]);
            yield break;
        }

        camera.gameObject.SetActive(true);
        int maximumSize =
            Mathf.Max(
                GameContext.CurrentMap.Grid.Width,
                GameContext.CurrentMap.Grid.Height);
        camera.transform.position =
            new Vector3(
                GameContext.CurrentMap.Grid.Width * 0.5f - 0.5f,
                maximumSize,
                GameContext.CurrentMap.Grid.Height * 0.5f - 0.5f);

        const int resolution = 128;
        var renderTexture =
            new RenderTexture(
                resolution,
                resolution,
                24,
                UnityEngine.Experimental.Rendering.DefaultFormat.LDR)
            {
                antiAliasing = 4
            };
        camera.targetTexture = renderTexture;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        var screenshot =
            new Texture2D(
                resolution,
                resolution,
                TextureFormat.RGB24,
                false);
        camera.Render();
        RenderTexture previous =
            RenderTexture.active;
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(
            new Rect(0f, 0f, resolution, resolution),
            0,
            0);
        screenshot.Apply();
        byte[] bytes = screenshot.EncodeToPNG();

        camera.targetTexture = null;
        RenderTexture.active = previous;
        renderTexture.Release();
        Destroy(renderTexture);
        Destroy(screenshot);
        camera.gameObject.SetActive(false);
        callback?.Invoke(bytes);
    }

    /// <summary>
    /// Saves the current revision and returns to the main menu.
    /// </summary>
    public void SaveAndClose()
    {
        // Scene departure happens only after persistence reports completion.
        if (_isBusy)
            return;
        StartCoroutine(
            SaveMap(
                () => StartCoroutine(
                    LoadScene("MainMenu"))));
    }

    /// <summary>
    /// Leaves the creator without saving the current in-memory revision.
    /// </summary>
    public void DiscardAndClose()
    {
        // The saved local workspace remains intact for the next creator entry.
        if (_isBusy)
            return;
        StartCoroutine(LoadScene("MainMenu"));
    }

    /// <summary>
    /// Saves and launches the current map as a local playtest.
    /// </summary>
    public void PlayMap()
    {
        // Validation remains a hard gate so playtests never enter a known broken grid.
        ValidateGrid();
        if (_isBusy ||
            !_isReady ||
            _errors.Count > 0)
        {
            _statusMessage =
                _errors.Count > 0
                    ? "Corrige les erreurs avant de tester"
                    : _statusMessage;
            return;
        }

        StartCoroutine(
            SaveMap(
                () =>
                {
                    PlayerPrefs.SetInt("ForceGrid", 1);
                    GameContext.CurrentRules =
                        new GameRules(
                            false,
                            false,
                            GameContext.CurrentMap.Map.ID);
                    StartCoroutine(LoadScene("Main"));
                }));
    }

    /// <summary>
    /// Clears creator runtime state and loads another local scene.
    /// </summary>
    private IEnumerator LoadScene(string sceneName)
    {
        // Cleanup occurs before the scene transition to avoid pooled path and grid objects leaking.
        _isBusy = true;
        _statusMessage = "Fermeture de la forge…";
        ClearFeedbacks();
        if (PoolManager.Instance != null)
            PoolManager.Instance.PushBackAllPool(PoolName.POCell);
        yield return null;

        if (GameContext.GameManager != null)
        {
            Destroy(GameContext.GameManager.gameObject);
            GameContext.GameManager = null;
        }
        GameContext.SceneTransitions.LoadScene(sceneName);
    }

    /// <summary>
    /// Provides the old serialized button callback with the new safe departure behavior.
    /// </summary>
    public void Close()
    {
        // The inactive legacy Canvas cannot bypass the new Fugui confirmation workflow.
        DiscardAndClose();
    }

    /// <summary>
    /// Accepts obsolete tool registration calls from the disabled legacy Canvas.
    /// </summary>
    public void RegisterToolButton(
        ToolType tool,
        GameObject button)
    {
        // Runtime tool state is now data-driven and deliberately ignores scene button objects.
    }

    /// <summary>
    /// Gets a French display label for one built-in environment.
    /// </summary>
    private static string GetEnvironmentLabel(
        BuildInEnvironments environment)
    {
        // StreamingAssets filenames remain stable behind player-facing localized labels.
        switch (environment)
        {
            case BuildInEnvironments.Fairy:
                return "Féérique";
            case BuildInEnvironments.Hellish:
                return "Infernal";
            case BuildInEnvironments.Holly:
                return "Sacré";
            case BuildInEnvironments.Night:
                return "Nuit";
            case BuildInEnvironments.Poison:
                return "Poison";
            case BuildInEnvironments.Sunshine:
                return "Solaire";
            case BuildInEnvironments.Clair:
                return "Clair";
            default:
                return "Par défaut";
        }
    }

    #endregion
}
