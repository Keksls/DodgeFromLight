using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Owns map-authoring state, board interaction, persistence, and playtest transitions.
/// </summary>
public sealed class MapCreatorController : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public Camera Camera;
    public GridController GridController;

    private readonly List<string> _errors =
        new List<string>();
    private readonly List<CellPos> _pathBackup =
        new List<CellPos>();
    private Cell _selectedCell;
    private MapDecoData _selectedMapDeco;
    private Ennemy _selectedEnnemy;
    private MovingPlatform _selectedMovingPlatform;
    private bool _pointerOverUi;
    private bool _keyboardCaptured;
    private bool _isReady;
    private bool _isBusy;
    private bool _isDirty;
    private MapCreatorSceneView _sceneView;
    private string _statusMessage = string.Empty;
    private string _environmentId = "Default";
    private string _environmentLabel = "Par défaut";

    public ToolType CurrentTool { get; private set; }
    public string CurrentDecorationId { get; private set; } = string.Empty;
    public bool IsReady => _isReady;
    public bool IsBusy => _isBusy;
    public bool IsDirty => _isDirty;
    public bool ShowEnvironment => _sceneView != null && _sceneView.ShowEnvironment;
    public bool IsPointerOverUi => _pointerOverUi;
    public string StatusMessage => _statusMessage;
    public string EnvironmentId => _environmentId;
    public string EnvironmentLabel => _environmentLabel;
    public IReadOnlyList<string> ValidationErrors => _errors;
    public Cell SelectedCell => _selectedCell;
    public Ennemy SelectedEnnemy => _selectedEnnemy;
    public MovingPlatform SelectedMovingPlatform => _selectedMovingPlatform;

    private readonly JsonSerializerSettings _snapshotSettings = new JsonSerializerSettings { ContractResolver = new MapCreatorSnapshotResolver() };
    private string _revisionSnapshot;
    private readonly HashSet<string> _paintedCells = new HashSet<string>();
    private CellPos? _hoveredPosition;
    private CellPos? _strokeStart;
    private CellPos? _lastPaintPosition;
    private bool _strokeActive;
    private bool _strokeChanged;
    private GearType _armedGear;
    public string BrushMode { get; private set; } = "Paint";
    public bool ShowGrid { get; private set; } = true;
    public Orientation PlacementOrientation { get; private set; } = Orientation.Up;
    public CellPos? HoveredPosition => _hoveredPosition;
    public bool HoverAllowed { get; private set; }
    public string HoverHint { get; private set; } = "Choisis un bloc, puis clique sur la grille.";
    public string ArmedGearId => _armedGear.ToString();
    public int PresentationRevision { get; private set; }

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

        UndoableActionsManager.Clear();
        _sceneView = new MapCreatorSceneView(Camera);
    }

    /// <summary>
    /// Restores the authored render state and releases editor-camera callbacks when leaving the creator.
    /// </summary>
    private void OnDestroy()
    {
        _sceneView?.Dispose();
        _sceneView = null;
    }

    /// <summary>
    /// Opens the current local workspace or creates a new authored map.
    /// </summary>
    private IEnumerator Start()
    {
        // Continuing the local workspace makes the creator immediately useful after every scene entry.
        _isBusy = true;
        _statusMessage = "Chargement de la carte…";
        // Enemy overlays use pooled objects, so direct scene entry must wait for all pools.
        if (PoolManager.Instance == null)
        {
            _isBusy = false;
            _statusMessage = "Initialisation impossible : PoolManager absent de la scène";
            Debug.LogError(_statusMessage, this);
            yield break;
        }
        while (!PoolManager.PoolReady)
            yield return null;

        bool needsInitialSave = false;
        try
        {
            needsInitialSave = OpenWorkspace();
        }
        catch (Exception exception)
        {
            // An invalid local workspace must report its cause instead of keeping the loading scrim forever.
            _isBusy = false;
            _statusMessage = "Échec du chargement : " + exception.Message;
            Debug.LogException(exception, this);
        }

        if (needsInitialSave)
            yield return SaveMap(LoadMap);
    }

    /// <summary>
    /// Opens the saved workspace or creates a new map and reports whether it needs its first save.
    /// </summary>
    private bool OpenWorkspace()
    {
        // Preserve existing authored content; only an absent workspace creates the default board.
        if (GridManager.GetGrid() != null && GridManager.GetMap() != null)
        {
            LoadMap();
            return false;
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
        return true;
    }

    /// <summary>
    /// Resolves explicit pointer gestures and shortcuts while respecting UI capture.
    /// </summary>
    private void Update()
    {
        // Picking uses the board plane, so tall props and holes cannot steal a tile click.
        if (!_isReady || _isBusy)
            return;
        CellPos? previousHover = _hoveredPosition;
        _hoveredPosition = _pointerOverUi || (CameraOrbit.Instance != null && CameraOrbit.Instance.IsTransitioning) ? null : GetClickedCell();
        if (!Nullable.Equals(previousHover, _hoveredPosition))
            PresentationRevision++;
        Cell hovered = _hoveredPosition.HasValue ? GameContext.CurrentMap.Grid.GetCell(_hoveredPosition.Value) : null;
        HoverHint = GetPlacementReason(hovered);
        HoverAllowed = hovered != null && string.IsNullOrEmpty(HoverHint);
        if (HoverAllowed)
            HoverHint = BrushMode == "Select" ? "Clique pour modifier ce bloc." :
                BrushMode == "Move" ? "Clique sur une case libre pour déplacer la sélection." :
                BrushMode == "Pick" ? "Clique pour reprendre ce bloc et son orientation." :
                BrushMode == "Rectangle" ? "Glisse pour dessiner une zone, puis relâche." :
                "Clique ou maintiens pour placer. R : pivoter. Échap : sélectionner.";

        if (!_keyboardCaptured)
        {
            HandleSelectionShortcuts();
            HandleGlobalShortcuts();
            HandlePathShortcuts();
            if (Input.GetKeyDown(KeyCode.R))
                RotatePlacement();
            if (Input.GetKeyDown(KeyCode.V))
                SetViewMode(CameraOrbit.Instance == null || !CameraOrbit.Instance.IsPlanView);
        }
        if (Input.GetMouseButtonDown(0) && hovered != null && !_keyboardCaptured)
        {
            _strokeActive = true;
            PresentationRevision++;
            _strokeChanged = false;
            _strokeStart = _hoveredPosition;
            _lastPaintPosition = _hoveredPosition;
            _paintedCells.Clear();
            if (BrushMode != "Rectangle")
                PaintCell(hovered);
        }
        if (_strokeActive && Input.GetMouseButton(0) && hovered != null && !_keyboardCaptured &&
            BrushMode != "Rectangle" && SupportsContinuousPaint())
        {
            // Interpolate fast pointer motion so a stroke leaves no accidental gaps.
            CellPos previous = _lastPaintPosition ?? hovered.GetCellPos();
            int steps = Mathf.Max(Mathf.Abs(hovered.X - previous.X), Mathf.Abs(hovered.Y - previous.Y));
            for (int step = 0; step <= steps; step++)
            {
                float t = steps == 0 ? 1f : (float)step / steps;
                PaintCell(GameContext.CurrentMap.Grid.GetCell(
                    Mathf.RoundToInt(Mathf.Lerp(previous.X, hovered.X, t)),
                    Mathf.RoundToInt(Mathf.Lerp(previous.Y, hovered.Y, t))));
            }
            _lastPaintPosition = hovered.GetCellPos();
        }

        if (_strokeActive && (Input.GetMouseButtonUp(0) || _keyboardCaptured))
        {
            // Releasing over a panel cancels rectangle application, but commits already painted cells safely.
            if (BrushMode == "Rectangle" && hovered != null && _strokeStart.HasValue && !_keyboardCaptured)
            {
                for (int x = Mathf.Min(_strokeStart.Value.X, hovered.X); x <= Mathf.Max(_strokeStart.Value.X, hovered.X); x++)
                    for (int y = Mathf.Min(_strokeStart.Value.Y, hovered.Y); y <= Mathf.Max(_strokeStart.Value.Y, hovered.Y); y++)
                        PaintCell(GameContext.CurrentMap.Grid.GetCell(x, y));
            }
            _strokeActive = false;
            PresentationRevision++;
            _strokeStart = null;
            if (_strokeChanged)
            {
                UpdateMap();
                ValidateGrid();
                MarkDirty("Modification appliquée. Annuler : Ctrl+Z.");
            }
        }
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
            SetBrushMode("Select");
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
                SetBrushMode("Select");
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
        PresentationRevision++;
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
        BrushMode = "Paint";
        _statusMessage =
            "Décor armé : " + _selectedMapDeco.ID;
    }

    /// <summary>
    /// Arms a gear for direct placement on the board.
    /// </summary>
    public void SelectGear(string gearId)
    {
        // Gear authoring uses the same palette-to-grid workflow as enemies and decoration.
        if (!Enum.TryParse(gearId, true, out _armedGear))
            return;
        SetTool(ToolType.Gear);
        BrushMode = "Paint";
        _statusMessage = "Clique sur le sol pour placer cet équipement.";
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

    #region Workbench gestures and history

    /// <summary>
    /// Creates a fresh local document after the UI explicitly resolves unsaved changes.
    /// </summary>
    public void CreateMap(int width, int height, bool saveCurrent)
    {
        // Existing library entries are never deleted when beginning a different project.
        if (_isBusy || width < 4 || height < 4 || width > 64 || height > 64)
            return;
        CancelPathEditing();
        if (saveCurrent)
        {
            StartCoroutine(SaveMap(() => CreateMap(width, height, false)));
            return;
        }
        _selectedCell = null;
        _selectedEnnemy = null;
        _selectedMovingPlatform = null;
        var metadata = new Map("Nouvelle carte", width, height);
        metadata.SetAuthor(SaveManager.CurrentSave.DisplayName);
        GameContext.CurrentMap = new FullMap(new Grid(width, height), metadata);
        GameContext.CurrentMap.Grid.Environment = GameContext.EnvironmentController.GetBuildinEnvironment(BuildInEnvironments.Default);
        SetTool(ToolType.Walkable);
        UndoableActionsManager.Clear();
        UpdateMap();
        ValidateGrid();
        _revisionSnapshot = CaptureRevision();
        _isReady = true;
        _isDirty = true;
        _statusMessage = "Nouvelle carte créée. Clique sur son nom pour la renommer.";
        FocusSelection();
    }

    /// <summary>
    /// Chooses one explicit gesture rather than overloading a selected palette item.
    /// </summary>
    public void SetBrushMode(string mode)
    {
        PresentationRevision++;
        // Rectangle fill is restricted to terrain and floor appearance.
        CancelPathEditing();
        if ((mode == "Paint" || mode == "Rectangle") && CurrentTool == ToolType.Select)
            SetTool(ToolType.Walkable);
        BrushMode = mode == "Rectangle" && !SupportsRectangle() ? "Paint" : mode;
        _statusMessage = mode == "Rectangle" && !SupportsRectangle()
            ? "Le rectangle sert au sol et au vide. Ce bloc se pose individuellement."
            : "Mode actif : " + BrushMode;
    }

    /// <summary>
    /// Switches the same physical board between top-down orthographic and perspective views.
    /// </summary>
    public void SetViewMode(bool plan)
    {
        // Camera state remains independent of map data and document history.
        if (CameraOrbit.Instance != null)
            CameraOrbit.Instance.SetPlanView(plan);
        PresentationRevision++;
    }

    /// <summary>
    /// Selects a named gizmo viewpoint while keeping camera presentation separate from document history.
    /// </summary>
    public void SetCameraView(string view)
    {
        if (CameraOrbit.Instance != null) CameraOrbit.Instance.SetView(view);
        PresentationRevision++;
    }

    /// <summary>
    /// Selects neutral work lighting or the map's authored environment without changing saved content.
    /// </summary>
    public void SetEnvironmentPreview(bool show)
    {
        _sceneView?.SetEnvironmentPreview(show);
        PresentationRevision++;
        _statusMessage = show ? "Ambiance du jeu : aperçu du rendu final." : "Éclairage de travail : couleurs neutres, effets masqués.";
    }

    /// <summary>
    /// Toggles the unobtrusive projected grid in the three-dimensional view.
    /// </summary>
    public void ToggleGrid()
    {
        PresentationRevision++;
        // Both camera projections share this optional overlay; selection feedback stays visible independently.
        ShowGrid = !ShowGrid;
    }

    /// <summary>
    /// Rotates the armed placement direction in ninety-degree steps.
    /// </summary>
    public void RotatePlacement()
    {
        PresentationRevision++;
        // The same direction is used by the ghost indicator and committed authored data.
        PlacementOrientation = (Orientation)((int)PlacementOrientation % 4 + 1);
    }

    /// <summary>
    /// Reports whether a tile belongs to the currently previewed brush footprint.
    /// </summary>
    public bool IsPreviewCell(int x, int y)
    {
        // Hover feedback allocates no temporary scene objects.
        if (!_hoveredPosition.HasValue)
            return false;
        if (BrushMode != "Rectangle" || !_strokeStart.HasValue)
        {
            if (_hoveredPosition.Value.X == x && _hoveredPosition.Value.Y == y)
                return true;
            if (CurrentTool == ToolType.MapDeco && _selectedMapDeco != null && BrushMode == "Paint")
                foreach (var offset in _selectedMapDeco.UnWalkableCells)
                {
                    Vector3 rotated = Quaternion.Euler(0f, GetOrientationAngle(PlacementOrientation), 0f) * new Vector3(offset.X, 0f, offset.Y);
                    if (_hoveredPosition.Value.X + Mathf.RoundToInt(rotated.x) == x &&
                        _hoveredPosition.Value.Y + Mathf.RoundToInt(rotated.z) == y)
                        return true;
                }
            return false;
        }
        return x >= Mathf.Min(_strokeStart.Value.X, _hoveredPosition.Value.X) &&
               x <= Mathf.Max(_strokeStart.Value.X, _hoveredPosition.Value.X) &&
               y >= Mathf.Min(_strokeStart.Value.Y, _hoveredPosition.Value.Y) &&
               y <= Mathf.Max(_strokeStart.Value.Y, _hoveredPosition.Value.Y);
    }

    /// <summary>
    /// Indicates which tools support continuous painting.
    /// </summary>
    private bool SupportsContinuousPaint()
    {
        // Selection, paths, objectives, and moves remain deliberate single-click operations.
        return (BrushMode == "Erase" || CurrentTool != ToolType.Select) && CurrentTool != ToolType.Path && BrushMode != "Select" && BrushMode != "Move" &&
               BrushMode != "Pick" && CurrentTool != ToolType.Start && CurrentTool != ToolType.End;
    }

    /// <summary>
    /// Indicates which palette choices can fill rectangular regions safely.
    /// </summary>
    private bool SupportsRectangle()
    {
        // Enemy and prop repetition stays explicit instead of accidentally filling the map.
        return CurrentTool == ToolType.Walkable || CurrentTool == ToolType.UnWalkable ||
               (CurrentTool == ToolType.MapDeco && _selectedMapDeco != null && _selectedMapDeco.Type == "Floor");
    }

    /// <summary>
    /// Explains a rejected placement before the player commits it.
    /// </summary>
    private string GetPlacementReason(Cell cell)
    {
        // Placement rules and hover feedback share one source of truth.
        if (cell == null)
            return "Déplace la souris sur la grille.";
        Grid grid = GameContext.CurrentMap.Grid;
        if (BrushMode == "Select" || BrushMode == "Pick")
            return string.Empty;
        if (CurrentTool == ToolType.Path)
        {
            if (cell.HasElement || !string.IsNullOrEmpty(cell.ElementID))
                return "Le trajet est bloqué par un décor.";
            if (_selectedEnnemy != null && !cell.Walkable)
                return "Un ennemi ne traverse pas le vide.";
            if (_selectedMovingPlatform != null && cell.Type != CellType.NotWalkable)
                return "Le trajet d'une plateforme reste sur le vide.";
            var points = GetSelectedWaypoints();
            return points != null && points.Count > 0 && !cell.IsInLine(points.Last())
                ? "Chaque segment doit être horizontal ou vertical." : string.Empty;
        }
        if (BrushMode == "Move")
        {
            if (_selectedCell == null)
                return "Sélectionne d'abord un objet à déplacer.";
            if (cell.Equals(_selectedCell))
                return "Choisis la case de destination.";
            if (cell.EntityOnCell != null || cell.HasElement || !string.IsNullOrEmpty(cell.ElementID) || cell.GearType != GearType.None)
                return "Cette case est occupée.";
            if (!cell.Walkable)
                return "Le déplacement nécessite du sol.";
            if (_selectedEnnemy == null && string.IsNullOrEmpty(_selectedCell.ElementID) && _selectedCell.GearType == GearType.None)
                return "Choisis un décor, un ennemi ou un équipement à déplacer.";
            if (!string.IsNullOrEmpty(_selectedCell.ElementID))
                return GetDecorationFootprintReason(_selectedCell.ElementID, cell, _selectedCell.ElementOrientation, _selectedCell);
            return string.Empty;
        }
        if (BrushMode == "Erase")
            return grid.IsStart(cell) || grid.IsEnd(cell) ? "Déplace le départ ou la sortie avant d'effacer ce sol." : string.Empty;
        if (CurrentTool == ToolType.Select)
            return string.Empty;
        if (CurrentTool == ToolType.MapDeco && _selectedMapDeco != null && _selectedMapDeco.Type == "Floor")
            return cell.Walkable ? string.Empty : "Peins du sol avant de choisir son apparence.";
        if (cell.EntityOnCell != null || cell.HasElement || !string.IsNullOrEmpty(cell.ElementID))
            return "Case occupée : sélectionne, déplace ou efface d'abord son contenu.";
        if (CurrentTool == ToolType.MovingPlatform)
            return cell.Type == CellType.NotWalkable && !GridController.Platforms.ContainsKey(cell.ToString())
                ? string.Empty : "Une plateforme se place sur le vide.";
        if (GridController.Platforms.ContainsKey(cell.ToString()))
            return "Efface la plateforme avant de modifier son support.";
        if (CurrentTool == ToolType.MapDeco && _selectedMapDeco != null)
        {
            string footprint = GetDecorationFootprintReason(_selectedMapDeco.ID, cell, GetOrientationAngle(PlacementOrientation), null);
            if (!string.IsNullOrEmpty(footprint))
                return footprint;
        }
        if (CurrentTool == ToolType.UnWalkable && (grid.IsStart(cell) || grid.IsEnd(cell)))
            return "Déplace le départ ou la sortie avant de retirer ce sol.";
        if ((int)CurrentTool < 99 || CurrentTool == ToolType.Gear || CurrentTool == ToolType.MapDeco)
            return cell.Type == CellType.Walkable && !grid.IsStart(cell) && !grid.IsEnd(cell)
                ? string.Empty : "Choisis une case de sol libre, hors départ et sortie.";
        return string.Empty;
    }

    /// <summary>
    /// Checks every occupied tile of a rotated decoration before placement or movement.
    /// </summary>
    private string GetDecorationFootprintReason(string id, Cell anchor, float angle, Cell movingSource)
    {
        // No invalid footprint is committed merely because its anchor tile is free.
        var data = GameContext.Databases.ResourcesData.GetMapDeco(id);
        if (anchor == null || data == null)
            return "Ce décor n'est pas disponible sur cette case.";
        Grid grid = GameContext.CurrentMap.Grid;
        foreach (var offset in data.UnWalkableCells)
        {
            Vector3 rotated = Quaternion.Euler(0f, angle, 0f) * new Vector3(offset.X, 0f, offset.Y);
            Cell covered = grid.GetCell(anchor.X + Mathf.RoundToInt(rotated.x), anchor.Y + Mathf.RoundToInt(rotated.z));
            if (covered == null)
                return "Le décor dépasse la carte. Pivote-le ou rapproche-le du centre.";
            bool sourceFootprint = false;
            if (movingSource != null)
                foreach (var sourceOffset in data.UnWalkableCells)
                {
                    Vector3 previous = Quaternion.Euler(0f, movingSource.ElementOrientation, 0f) * new Vector3(sourceOffset.X, 0f, sourceOffset.Y);
                    if (movingSource.X + Mathf.RoundToInt(previous.x) == covered.X && movingSource.Y + Mathf.RoundToInt(previous.z) == covered.Y)
                        sourceFootprint = true;
                }
            if (!covered.Equals(movingSource) &&
                ((covered.HasElement && !sourceFootprint) || covered.EntityOnCell != null ||
                 covered.GearType != GearType.None || grid.IsStart(covered) || grid.IsEnd(covered) ||
                 (!string.IsNullOrEmpty(covered.ElementID) && !covered.Equals(anchor))))
                return "Le décor recouvre un autre objet ou un objectif.";
        }
        return string.Empty;
    }

    /// <summary>
    /// Applies one tile within a gesture and records it only once per stroke.
    /// </summary>
    private void PaintCell(Cell cell)
    {
        // Painting over the same tile cannot rotate or recreate content repeatedly.
        if (cell != null && (BrushMode == "Select" || BrushMode == "Pick" || BrushMode == "Erase"))
            cell = ResolveDecorationAnchor(cell);
        if (cell == null || !_paintedCells.Add(cell.ToString()))
            return;
        string rejection = GetPlacementReason(cell);
        if (!string.IsNullOrEmpty(rejection))
        {
            _statusMessage = rejection;
            return;
        }
        if (BrushMode == "Select")
        {
            SelectCell(cell);
            return;
        }
        if (BrushMode == "Pick")
        {
            PickCell(cell);
            return;
        }
        if (BrushMode == "Move")
        {
            MoveSelectedContent(cell);
            return;
        }
        if (BrushMode == "Erase")
        {
            // Consume the complete footprint so one eraser stroke cannot also remove its supporting floor.
            if (!string.IsNullOrEmpty(cell.ElementID))
            {
                var data = GameContext.Databases.ResourcesData.GetMapDeco(cell.ElementID);
                foreach (var offset in data.UnWalkableCells)
                {
                    Vector3 rotated = Quaternion.Euler(0f, cell.ElementOrientation, 0f) * new Vector3(offset.X, 0f, offset.Y);
                    Cell covered = GameContext.CurrentMap.Grid.GetCell(cell.X + Mathf.RoundToInt(rotated.x), cell.Y + Mathf.RoundToInt(rotated.z));
                    if (covered != null)
                        _paintedCells.Add(covered.ToString());
                }
            }
            EraseCell(cell);
            DrawFeedbacks();
            return;
        }
        bool changed = ApplyCurrentTool(cell);
        if (!changed)
            return;
        if (CurrentTool == ToolType.MapDeco && _selectedMapDeco != null && _selectedMapDeco.Type != "Floor")
            cell.ElementOrientation = GetOrientationAngle(PlacementOrientation);
        else if (cell.EntityOnCell is Ennemy enemy)
        {
            var definition = GameContext.CurrentMap.Grid.GetEnnemyOnCell(cell);
            definition.StartOrientation = PlacementOrientation;
            enemy.SetOrientation(PlacementOrientation);
        }
        else
            cell.FloorOrientation = PlacementOrientation;
        if (CurrentTool == ToolType.MapDeco)
            DrawFeedbacks();
        MarkDirty("Bloc placé.");
    }

    /// <summary>
    /// Resolves any covered tile to the anchor of the decoration occupying it.
    /// </summary>
    private Cell ResolveDecorationAnchor(Cell target)
    {
        // Large walls and props can be selected or erased anywhere on their footprint.
        if (!target.HasElement || !string.IsNullOrEmpty(target.ElementID))
            return target;
        Grid grid = GameContext.CurrentMap.Grid;
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                Cell anchor = grid.GetCell(x, y);
                if (string.IsNullOrEmpty(anchor.ElementID))
                    continue;
                var data = GameContext.Databases.ResourcesData.GetMapDeco(anchor.ElementID);
                foreach (var offset in data.UnWalkableCells)
                {
                    Vector3 rotated = Quaternion.Euler(0f, anchor.ElementOrientation, 0f) * new Vector3(offset.X, 0f, offset.Y);
                    if (anchor.X + Mathf.RoundToInt(rotated.x) == target.X && anchor.Y + Mathf.RoundToInt(rotated.z) == target.Y)
                        return anchor;
                }
            }
        return target;
    }

    /// <summary>
    /// Removes only the topmost content layer, leaving supporting terrain intact.
    /// </summary>
    private void EraseCell(Cell cell)
    {
        // Repeated explicit gestures peel prop, enemy, gear, mechanic, then floor.
        Grid grid = GameContext.CurrentMap.Grid;
        if (!string.IsNullOrEmpty(cell.ElementID))
            cell.RemoveElement();
        else if (grid.GetEnnemyOnCell(cell) != null)
            grid.RemoveEnnemy(cell.GetCellPos());
        else if (GridController.Platforms.ContainsKey(cell.ToString()))
            grid.RemoveMovingPlatform(cell.GetCellPos());
        else if (cell.GearType != GearType.None)
            cell.SetGear(GearType.None);
        else if (cell.Type != CellType.Walkable && cell.Type != CellType.NotWalkable)
            cell.SetType(CellType.Walkable);
        else
        {
            cell.SetType(CellType.NotWalkable);
            cell.SetWalkable(false);
        }
        MarkDirty("Contenu effacé.");
    }

    /// <summary>
    /// Arms the content under the pointer for convenient reuse.
    /// </summary>
    private void PickCell(Cell cell)
    {
        // The most visible content layer takes priority over its supporting floor.
        var enemy = GameContext.CurrentMap.Grid.GetEnnemyOnCell(cell);
        if (!string.IsNullOrEmpty(cell.ElementID))
            SelectDecoration(cell.ElementID);
        else if (enemy != null)
            SetTool((ToolType)enemy.Type);
        else if (cell.GearType != GearType.None)
            SelectGear(cell.GearType.ToString());
        else if (!string.IsNullOrEmpty(cell.FloorID))
            SelectDecoration(cell.FloorID);
        PlacementOrientation = enemy != null ? enemy.StartOrientation :
            !string.IsNullOrEmpty(cell.ElementID) ? (Orientation)(Mathf.RoundToInt(cell.ElementOrientation / 90f) % 4 + 1) : cell.FloorOrientation;
        BrushMode = "Paint";
    }

    /// <summary>
    /// Moves selected decoration, gear, or enemy data to a free tile.
    /// </summary>
    private void MoveSelectedContent(Cell destination)
    {
        // Validate every translated route point before modifying either source or destination.
        Cell source = _selectedCell;
        if (source == null)
            return;
        Grid grid = GameContext.CurrentMap.Grid;
        var enemy = grid.GetEnnemyOnCell(source);
        if (enemy != null)
        {
            int dx = destination.X - source.X;
            int dy = destination.Y - source.Y;
            var route = enemy.WayPoints.Select(point => new CellPos(point.X + dx, point.Y + dy)).ToList();
            if (route.Any(point => grid.GetCell(point) == null || !grid.GetCell(point).Walkable))
            {
                _statusMessage = "Le trajet sortirait du sol. Modifie-le avant de déplacer cet ennemi.";
                return;
            }
            enemy.StartPos = destination.GetCellPos();
            enemy.WayPoints = route;
        }
        else if (!string.IsNullOrEmpty(source.ElementID))
        {
            destination.SetElement(source.ElementID);
            destination.ElementOrientation = source.ElementOrientation;
            destination.ElementScale = source.ElementScale;
            source.RemoveElement();
        }
        else if (source.GearType != GearType.None)
        {
            destination.SetGear(source.GearType);
            source.SetGear(GearType.None);
        }
        else
        {
            _statusMessage = "Sélectionne un décor, un ennemi ou un équipement à déplacer.";
            return;
        }
        _selectedCell = destination;
        MarkDirty("Objet déplacé.");
    }

    /// <summary>
    /// Captures canonical authored data without transient scene entity references.
    /// </summary>
    private string CaptureRevision()
    {
        // Whole-document snapshots preserve content combinations that field-specific undo could lose.
        return JsonConvert.SerializeObject(GameContext.CurrentMap, _snapshotSettings);
    }

    /// <summary>
    /// Records a changed document after a complete gesture.
    /// </summary>
    private void RecordRevision(bool coalesce = false)
    {
        // The bounded history shares immutable serialized strings rather than cloning Unity objects.
        string current = CaptureRevision();
        UndoableActionsManager.Record(_revisionSnapshot, current, coalesce);
        _revisionSnapshot = current;
    }

    /// <summary>
    /// Rebuilds the editor from one canonical in-memory document revision.
    /// </summary>
    private void RestoreRevision(string revision)
    {
        // No disk file is written by undo or redo.
        _selectedCell = null;
        _selectedEnnemy = null;
        _selectedMovingPlatform = null;
        GameContext.CurrentMap = JsonConvert.DeserializeObject<FullMap>(revision, _snapshotSettings);
        _revisionSnapshot = revision;
        _isDirty = true;
        UpdateMap();
        ValidateGrid();
    }

    #endregion

    #region Tools and board mutation

    /// <summary>
    /// Arms one board tool and safely resolves active path editing.
    /// </summary>
    public void SetTool(ToolType tool)
    {
        PresentationRevision++;
        // Switching away from an unfinished path restores its previous authored value.
        if (CurrentTool == ToolType.Path &&
            tool != ToolType.Path)
        {
            CancelPathEditing();
        }

        CurrentTool = tool;
        BrushMode = tool == ToolType.Select ? "Select" : "Paint";
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
            case ToolType.Gear:
                if (cell.GearType == _armedGear)
                    return false;
                cell.SetGear(_armedGear);
                return true;
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
    private bool SetCellType(Cell cell, CellType type, bool walkable)
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
        if (_selectedMapDeco == null ||
            cell.Type != CellType.Walkable)
        {
            return false;
        }

        if (_selectedMapDeco.Type == "Floor")
        {

            cell.FloorID = _selectedMapDeco.ID;
        }
        else
        {

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
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Adds one enemy to a compatible empty walkable cell.
    /// </summary>
    private bool AddEnemy(EnnemyType type, Cell cell)
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
        SetTool(ToolType.Select);
        SelectCell(cell);
        return true;
    }

    /// <summary>
    /// Erases the topmost selected layer without deleting unrelated supporting content.
    /// </summary>
    public void DeleteSelection()
    {
        // The keyboard and inspector share the same layer-aware eraser.
        if (_selectedCell == null || _isBusy)
            return;
        CancelPathEditing();
        Grid grid = GameContext.CurrentMap.Grid;
        if (grid.IsStart(_selectedCell) || grid.IsEnd(_selectedCell))
        {
            _statusMessage = "Déplace le départ ou la sortie avant de les effacer.";
            return;
        }
        EraseCell(_selectedCell);
        UpdateMap();
        ValidateGrid();
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
    private static float GetOrientationAngle(Orientation orientation)
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

        if (CurrentTool == ToolType.Path)
            return;
        List<CellPos> waypoints =
            GetSelectedWaypoints();
        _pathBackup.Clear();
        if (waypoints != null)
            _pathBackup.AddRange(waypoints);
        SetSelectedWaypoints(new List<CellPos> { _selectedCell.GetCellPos() });
        CurrentTool = ToolType.Path;
        BrushMode = "Paint";
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

        if (waypoints.Count > 0 && waypoints.Last().Equals(cell.GetCellPos()))
            return false;
        waypoints.Add(cell.GetCellPos());
        PresentationRevision++;
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
        BrushMode = "Select";
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
        BrushMode = "Select";
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
    /// Updates both the preview entity and its persisted route definition.
    /// </summary>
    private void SetSelectedWaypoints(List<CellPos> waypoints)
    {
        PresentationRevision++;
        // Keeping the source list shared makes path edits survive saving and undo.
        if (_selectedEnnemy != null)
        {
            _selectedEnnemy.WayPoints = waypoints;
            var definition = GameContext.CurrentMap.Grid.GetEnnemyOnCell(_selectedEnnemy.StartCell);
            if (definition != null)
                definition.WayPoints = waypoints;
        }
        else if (_selectedMovingPlatform != null)
        {
            _selectedMovingPlatform.WayPoints = waypoints;
            var definition = GameContext.CurrentMap.Grid.MovingPlatforms.FirstOrDefault(
                item => item.StartPos.Equals(_selectedCell.GetCellPos()));
            if (definition != null)
                definition.WayPoints = waypoints;
        }
    }

    #endregion

    #region Selection and refresh

    /// <summary>
    /// Projects the pointer onto the authoring grid independently of scene colliders.
    /// </summary>
    private CellPos? GetClickedCell()
    {
        // All cells remain addressable in empty space and beneath decorative meshes.
        if (Camera == null || GameContext.CurrentMap == null)
            return null;
        var plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out float distanceToGrid))
            return null;
        Vector3 position = ray.GetPoint(distanceToGrid);
        int x = Mathf.FloorToInt(position.x + 0.5f);
        int y = Mathf.FloorToInt(position.z + 0.5f);
        return GameContext.CurrentMap.Grid.GetCell(x, y) != null ? new CellPos(x, y) : (CellPos?)null;
    }

    /// <summary>
    /// Selects one cell and resolves any enemy or moving platform occupying it.
    /// </summary>
    private void SelectCell(Cell cell)
    {
        PresentationRevision++;
        // Selection changes refresh the previous highlight without rebuilding the whole map.
        if (cell == null)
            return;
        cell = ResolveDecorationAnchor(cell);
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

        _statusMessage =
            "Case sélectionnée : " + cell.X + ", " + cell.Y;
    }

    /// <summary>
    /// Clears the active selection while preserving an in-progress trajectory edit.
    /// </summary>
    private void UnselectCell()
    {
        PresentationRevision++;
        // Path editing keeps its entity context until the user explicitly validates or cancels.
        if (CurrentTool == ToolType.Path)
            return;

        _selectedCell = null;
        _selectedEnnemy = null;
        _selectedMovingPlatform = null;
        _statusMessage =
            "Sélection effacée";
    }





    /// <summary>
    /// Fits the full board in the workbench viewport.
    /// </summary>
    public void FocusSelection()
    {
        // A visible reset button always recovers from an accidental pan or zoom.
        if (CameraOrbit.Instance != null && GameContext.CurrentMap != null)
            CameraOrbit.Instance.FrameBoard(GameContext.CurrentMap.Grid.Width, GameContext.CurrentMap.Grid.Height);
    }

    /// <summary>
    /// Refreshes one cell, its cross, colliders, element occupancy, and selection feedback.
    /// </summary>
    private void RefreshCell(Cell cell)
    {
        // Defer rebuilds to the end of a paint gesture.
        if (_strokeActive)
            return;
        if (cell == null)
            return;

        GridController.RefreshCrossCell(
            cell,
            GameContext.CurrentMap.Grid,
            true);
        DrawFeedbacks();
        _sceneView?.Refresh();
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
        GameContext.SetEnvironment(GameContext.CurrentMap);
        DrawFeedbacks();
        _sceneView?.Refresh();
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
            GameContext.CurrentMap.Grid.ValidateGrid(_errors);
        }
    }

    /// <summary>
    /// Marks the current revision dirty and exposes concise status feedback.
    /// </summary>
    private void MarkDirty(string message, bool coalesce = false)
    {
        PresentationRevision++;
        // Dirty state is cleared only after persistence completes successfully.
        _isDirty = true;
        _statusMessage = message ?? "Carte modifiée";
        if (_strokeActive)
        {
            _strokeChanged = true;
            return;
        }
        RecordRevision(coalesce);
    }

    #endregion

    #region Element occupancy feedback

    /// <summary>
    /// Refreshes derived decoration occupancy without creating per-tile marker objects.
    /// </summary>
    public void DrawFeedbacks()
    {
        // The Fugui viewport draws occupancy, selection, and hover using pooled DrawList geometry.
        ResetElementOccupancy();
        ProcessElementOccupancy();
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



    #endregion

    #region Environment, history, persistence, and playtest

    /// <summary>
    /// Applies one built-in environment preset and marks the revision dirty.
    /// </summary>
    public void SetBuildinEnvironment(BuildInEnvironments environment)
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
    public void SetEnvironmentValue(string valueId, float value)
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
    public void SetEnvironmentColor(string colorId, Color color)
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
    private void ApplyCustomEnvironment(EnvironmentSettings environment)
    {
        // Live preview remains immediate for numeric and color edits.
        GameContext.CurrentMap.Grid.Environment = environment;
        _environmentId = string.Empty;
        _environmentLabel = "Personnalisée";
        GameContext.SetEnvironment(GameContext.CurrentMap);
        MarkDirty("Réglages d’ambiance modifiés", true);
    }

    /// <summary>
    /// Restores the previous complete authoring gesture.
    /// </summary>
    public void Undo()
    {
        // An unfinished path is cancelled before touching document history.
        if (_strokeActive || _isBusy || !UndoableActionsManager.CanUndo)
            return;
        CancelPathEditing();
        RestoreRevision(UndoableActionsManager.Undo(CaptureRevision()));
        _statusMessage = "Modification annulée.";
    }

    /// <summary>
    /// Reapplies the next complete authoring gesture.
    /// </summary>
    public void Redo()
    {
        // Revisions restore metadata, mechanics, enemies, decoration, and ambiance together.
        if (_strokeActive || _isBusy || !UndoableActionsManager.CanRedo)
            return;
        CancelPathEditing();
        RestoreRevision(UndoableActionsManager.Redo(CaptureRevision()));
        _statusMessage = "Modification rétablie.";
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
        BrushMode = "Select";
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
        _revisionSnapshot = CaptureRevision();
    }

    /// <summary>
    /// Starts a non-overlapping save operation for the current map.
    /// </summary>
    public void RequestSave()
    {
        // Repeated clicks during preview capture are ignored.
        if (!_isReady || _isBusy || _strokeActive || CurrentTool == ToolType.Path)
            return;
        StartCoroutine(SaveMap());
    }

    /// <summary>
    /// Saves the authored map and preview while always releasing the busy state.
    /// </summary>
    public IEnumerator SaveMap(Action callback = null)
    {
        // Yield once to show progress without waiting for an Editor Game-view render.
        if (_isBusy && _isReady)
            yield break;

        _isBusy = true;
        _statusMessage = "Enregistrement de la carte…";
        yield return null;

        bool saved = false;
        try
        {
            byte[] preview = CapturePreview();
            GameContext.CurrentMap.Map.AlreadyFinished = 0;
            GameContext.CurrentMap.Map.SetState(MapState.Draft);
            LocalMapRepository.Save(GameContext.CurrentMap, preview);
            UpdateMap();
            _isDirty = false;
            _statusMessage = "Carte enregistrée";
            _revisionSnapshot = CaptureRevision();
            saved = true;
        }
        catch (Exception exception)
        {
            // Keep the revision recoverable and expose the real failure instead of a permanent overlay.
            _isDirty = true;
            _statusMessage = "Échec de l’enregistrement : " + exception.Message;
            Debug.LogException(exception, this);
        }
        finally
        {
            _isBusy = false;
        }

        if (saved)
            callback?.Invoke();
    }

    /// <summary>
    /// Captures a square map preview through the Universal Render Pipeline.
    /// </summary>
    private byte[] CapturePreview()
    {
        // An explicit SRP render request also works when the Editor Scene view owns focus.
        Camera camera = ScreenshotCamera;
        if (camera == null)
            return Array.Empty<byte>();

        bool wasActive = camera.gameObject.activeSelf;
        bool wasEnabled = camera.enabled;
        Vector3 previousPosition = camera.transform.position;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = null;
        Texture2D screenshot = null;

        try
        {
            camera.enabled = false;
            camera.gameObject.SetActive(true);
            int maximumSize = Mathf.Max(
                GameContext.CurrentMap.Grid.Width,
                GameContext.CurrentMap.Grid.Height);
            camera.transform.position = new Vector3(
                GameContext.CurrentMap.Grid.Width * 0.5f - 0.5f,
                maximumSize,
                GameContext.CurrentMap.Grid.Height * 0.5f - 0.5f);

            const int resolution = 128;
            renderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            var request = new UniversalRenderPipeline.SingleCameraRequest
            {
                destination = renderTexture
            };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new InvalidOperationException("Le pipeline actif ne permet pas la capture URP.");

            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0f, 0f, resolution, resolution), 0, 0);
            screenshot.Apply();
            return screenshot.EncodeToPNG();
        }
        finally
        {
            // Restore the authored camera and render target even when rendering or encoding fails.
            RenderTexture.active = previousActive;
            camera.targetTexture = previousTarget;
            camera.transform.position = previousPosition;
            camera.gameObject.SetActive(wasActive);
            camera.enabled = wasEnabled;
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (screenshot != null)
                Destroy(screenshot);
        }
    }

    /// <summary>
    /// Saves the current revision and returns to the main menu.
    /// </summary>
    public void SaveAndClose()
    {
        // Scene departure happens only after persistence reports completion.
        if (_isBusy)
            return;
        CancelPathEditing();
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
            CurrentTool == ToolType.Path ||
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
    public void RegisterToolButton(ToolType tool, GameObject button)
    {
        // Runtime tool state is now data-driven and deliberately ignores scene button objects.
    }

    /// <summary>
    /// Gets a French display label for one built-in environment.
    /// </summary>
    private static string GetEnvironmentLabel(BuildInEnvironments environment)
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
