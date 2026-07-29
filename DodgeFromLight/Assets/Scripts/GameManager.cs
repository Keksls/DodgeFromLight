using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using GameSession = global::DodgeFromLight.Application.Game.GameSession;
using GameSessionReadModel =
    global::DodgeFromLight.Application.Game.GameSessionReadModel;
using CellTypeIds = global::DodgeFromLight.Core.Model.CellTypeIds;
using CoreDirection = global::DodgeFromLight.Core.Model.Direction;
using CoreGridPosition = global::DodgeFromLight.Core.Model.GridPosition;
using CorePlayerState = global::DodgeFromLight.Core.Model.PlayerState;
using CoreSimulationState = global::DodgeFromLight.Core.Model.SimulationState;
using GearTypeIds = global::DodgeFromLight.Core.Model.GearTypeIds;
using LegacyMapImporter = global::DodgeFromLight.Infrastructure.Maps.LegacyMapImporter;
using PlacedObjectState = global::DodgeFromLight.Core.Model.PlacedObjectState;
using PlayerCommand = global::DodgeFromLight.Core.Commands.PlayerCommand;
using SimulationEvent = global::DodgeFromLight.Core.Events.SimulationEvent;
using SimulationEventType = global::DodgeFromLight.Core.Events.SimulationEventType;
using SimulationStatus = global::DodgeFromLight.Core.Model.SimulationStatus;

public class GameManager : MonoBehaviour
{
    public InGameUI InGameUI;
    public CameraFollow Follower;

    public GamePlayer_Controller _playerController;
    public CastingPetController _petController;
    public Entity PlayerController { get { return _petController != null ? _petController : _playerController; } }
    public bool AutoStartGame = false;
    public bool Paused = false;

    public int NbTurn { get; private set; }
    [Range(0.0001f, 1f)]
    public float EntitiesMovementDuration = 0.1f;
    public System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();

    public GameObject GearPickupVFX;
    public GameObject CheckboxSaveVFX;

    // gears 
    public GameObject GearBombeVFXPrefab;
    public GameObject GearGrapplePrefab;
    public GameObject GearDiscoballPrefab;
    public GameObject GearDiscoballStunVFXPrefab;
    public GameObject GearMirrorPrefab;
    public Cell LastCheckPointReached = null;
    public AnimationCurve GearJumpCurve;
    public float GearJumpYOffset = 10f;
    public GameObject GearJumpSpringPrefab;
    // cell spring
    public AnimationCurve SpringAnimationCurve;
    [Range(0.5f, 20f)]
    public float YSpringJump;
    public float JumpDuration = 0.5f;

    public GameObject ItemPartPrefab;

    private readonly Dictionary<string, GameObject> corePlacedObjectViews =
        new Dictionary<string, GameObject>();
    private GameSession coreSession;
    private CoreGridPosition? pendingCoreCheckpoint;
    private bool coreCommandRunning;
    private bool coreTerminalPresentationRunning;
    private bool coreVictoryReady;
    private bool wasTimerRunningBeforePause;
    private GameObject coreGrappleView;
    private CoreWeaponAnchorPresenter coreWeaponAnchorPresenter;
    private string coreSourceGridJson;
    private string coreSourceMapJson;

    private CoreSimulationState coreState => coreSession?.CurrentState;
    public bool UsesCoreSimulation => coreSession != null;
    public bool CanUseSoloHudActions =>
        UsesCoreSimulation &&
        !coreCommandRunning &&
        !Paused &&
        coreState.Status == SimulationStatus.Running;
    public bool CanControlLoadedRun =>
        UsesCoreSimulation &&
        !coreCommandRunning;
    public bool CanRestartCurrentRun =>
        CanControlLoadedRun &&
        !Paused;
    public bool CanContinueAfterVictory =>
        CanRestartCurrentRun &&
        coreVictoryReady &&
        coreState.Status == SimulationStatus.Won;
    public bool HasNextMapInSequence =>
        GameContext.CurrentRules != null &&
        (GameContext.CurrentRules.Endless ||
         GameContext.CurrentRules.CurrentGrid + 1 <
         GameContext.CurrentRules.NbGrids);

    /// <summary>
    /// Registers the gameplay scene manager and its presentation debug callback.
    /// </summary>
    void Awake()
    {
        // Register the scene services used by the deterministic runtime and its views.
        GameContext.GameManager = this;
        GameContext.Initialize();

        Follower.PostRenderCallback = () =>
        {
            if (Input.GetKey(KeyCode.P))
            {
                foreach (Ennemy en in GameContext.Ennemies)
                    en.DrawPath(transform);
                foreach (MovingPlatform platform in GameContext.MovingPlatforms)
                    platform.DrawPath(transform);
            }
        };
    }

    /// <summary>
    /// Starts map loading once the retained presentation pools are available.
    /// </summary>
    private void Start()
    {
        // Direct scene entry must own a valid map sequence before pool readiness starts gameplay.
        EnsureGameplayRules();
        if (PoolManager.PoolReady)
            PoolManager_OnPoolingReady();
        else
        {
            PoolManager.OnPoolingReady += PoolManager_OnPoolingReady;
        }
    }

    /// <summary>
    /// Releases the presentation-pool readiness subscription.
    /// </summary>
    private void OnDestroy()
    {
        // Release transient views and the only external lifecycle subscription owned here.
        DestroyCoreGrappleView();
        ClearCoreWeaponAnchorPresentation();
        PoolManager.OnPoolingReady -= PoolManager_OnPoolingReady;
    }

    /// <summary>
    /// Starts the gameplay session after the presentation pools finish loading.
    /// </summary>
    private void PoolManager_OnPoolingReady()
    {
        // Ignore readiness callbacks received in non-gameplay scenes.
        if (SceneManager.GetActiveScene().name != "Main")
            return;
        StartGame();
        NbTurn = 0;
    }

    /// <summary>
    /// Handles global restart and checkpoint input for the active simulation runtime.
    /// </summary>
    private void Update()
    {
        // Core sessions resolve global inputs without firing historical turn events.
        if (UsesCoreSimulation)
            HandleCoreGlobalInput();
    }

    /// <summary>
    /// Handles restart, map traversal, and checkpoint activation for a core-driven run.
    /// </summary>
    private void HandleCoreGlobalInput()
    {
        // Global actions remain available independently of the player-view input component.
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Undo).IsDown())
        {
            TryUndoLastCommand();
            return;
        }

        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.RestartMap).IsDown())
        {
            var resumeFromCheckpoint =
                !Input.GetKey(KeyCode.LeftControl) &&
                !Input.GetKey(KeyCode.RightControl);
            TryRestartCurrentRun(resumeFromCheckpoint);
            return;
        }

        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.NextMap).IsDown())
        {
            TryContinueAfterVictory();
            return;
        }

        if (GameContext.GameSettingsManager
            .GetInput(InputSettingsType.TakeCheckPoint)
            .IsDown())
        {
            ExecuteCoreCommand(PlayerCommand.ActivateCheckpoint());
        }
    }

    /// <summary>
    /// Restores the state preceding the latest accepted Core command.
    /// </summary>
    public bool TryUndoLastCommand()
    {
        // Terminal, paused, and animated states cannot be safely replaced mid-presentation.
        if (!CanUseSoloHudActions ||
            !coreSession.CanUndo)
        {
            return false;
        }

        if (!coreSession.TryUndo(out _))
            return false;

        coreCommandRunning = true;
        NbTurn = coreState.TurnIndex;
        DestroyCoreGrappleView();
        ClearCoreWeaponAnchorPresentation();
        RebuildCorePresentationFromSource();
        coreCommandRunning = false;
        return true;
    }

    /// <summary>
    /// Restarts the current deterministic attempt when presentation is idle.
    /// </summary>
    public bool TryRestartCurrentRun()
    {
        // UI callers share the same animation and pause guard as undo.
        return TryRestartCurrentRun(true);
    }

    /// <summary>
    /// Restarts from either the active checkpoint or the authored start cell.
    /// </summary>
    private bool TryRestartCurrentRun(bool resumeFromCheckpoint)
    {
        // Ctrl+restart intentionally discards the checkpoint for a complete new attempt.
        if (!CanRestartCurrentRun)
            return false;

        RestartGrid(resumeFromCheckpoint);
        return true;
    }

    /// <summary>
    /// Advances a completed run only after its victory presentation is finished.
    /// </summary>
    public bool TryContinueAfterVictory()
    {
        // Result-screen and keyboard actions share one guarded progression path.
        if (!CanContinueAfterVictory)
            return false;

        coreCommandRunning = true;
        AdvanceToNextMap();
        return true;
    }

    /// <summary>
    /// Gets the application-owned read model consumed by the solo HUD.
    /// </summary>
    public GameSessionReadModel GetSoloSessionReadModel()
    {
        // The Fugui bridge receives application data without reading Core fields itself.
        return UsesCoreSimulation
            ? coreSession.GetReadModel()
            : null;
    }

    /// <summary>
    /// Rebuilds every Unity view from the pristine authored map and current Core snapshot.
    /// </summary>
    private void RebuildCorePresentationFromSource()
    {
        // Undo never reverses individual VFX; it renders one complete authoritative snapshot.
        if (string.IsNullOrWhiteSpace(coreSourceGridJson) ||
            string.IsNullOrWhiteSpace(coreSourceMapJson))
        {
            Debug.LogError(
                "Cannot rebuild the Core presentation because the source map snapshot is missing.");
            return;
        }

        var sourceGrid = JsonConvert.DeserializeObject<Grid>(coreSourceGridJson);
        var sourceMap = JsonConvert.DeserializeObject<Map>(coreSourceMapJson);
        if (sourceGrid == null || sourceMap == null)
        {
            Debug.LogError(
                "Cannot rebuild the Core presentation because the source map snapshot is invalid.");
            return;
        }

        GameContext.CurrentMap = new FullMap(sourceGrid, sourceMap);
        ApplyCoreBoardSnapshotToLegacyGrid(sourceGrid);
        var checkpoint = coreState.Player.CheckpointPosition;
        LastCheckPointReached = checkpoint.HasValue
            ? sourceGrid.GetCell(checkpoint.Value.X, checkpoint.Value.Y)
            : null;
        GameContext.GridController.ClearGrid();
        GameContext.GridController.DrawGrid(sourceGrid);
        GameContext.GridController.SpawnEnnemies(sourceGrid, false);
        GameContext.GridController.SpawnPlatforms(sourceGrid);
        foreach (var enemy in GameContext.Ennemies)
            enemy.enabled = false;
        foreach (var platform in GameContext.MovingPlatforms)
            platform.enabled = false;

        Follower.Target = _playerController.transform;
        ApplyCoreState(System.Array.Empty<SimulationEvent>());
    }

    /// <summary>
    /// Projects persistent Core board mutations onto a fresh legacy grid used for rendering.
    /// </summary>
    private void ApplyCoreBoardSnapshotToLegacyGrid(Grid grid)
    {
        // Mutations are applied before drawing so removed objects never flash for one frame.
        foreach (var position in coreState.Board.CollapsedCells)
            MarkLegacyCellDestroyed(grid, position);
        foreach (var bomb in coreState.Board.TimedBombs)
        {
            if (bomb.HasExploded)
                MarkLegacyCellDestroyed(grid, bomb.Position);
        }

        foreach (var position in coreState.Board.RemovedGearCells)
        {
            var cell = grid.GetCell(position.X, position.Y);
            if (cell != null)
                cell.SetGear(GearType.None);
        }

        foreach (var position in coreState.Board.DestroyedElementCells)
        {
            RemoveLegacyElementFromGrid(
                grid,
                grid.GetCell(position.X, position.Y));
        }
    }

    /// <summary>
    /// Marks one fresh legacy cell as visually destroyed.
    /// </summary>
    private static void MarkLegacyCellDestroyed(
        Grid grid,
        CoreGridPosition position)
    {
        // The Core retains the original definition while the view uses a non-walkable tile.
        var cell = grid.GetCell(position.X, position.Y);
        if (cell == null)
            return;

        cell.SetType(CellType.NotWalkable);
        cell.SetWalkable(false);
    }

    /// <summary>
    /// Removes one authored decoration and its rotated footprint from a fresh legacy grid.
    /// </summary>
    private static void RemoveLegacyElementFromGrid(Grid grid, Cell anchorCell)
    {
        // Footprint reconstruction mirrors the retained authoring convention without firing VFX.
        if (anchorCell == null || string.IsNullOrEmpty(anchorCell.ElementID))
            return;

        var resources = GameContext.Databases.ResourcesData;
        if (resources.HasMapDeco(anchorCell.ElementID))
        {
            var decoration = resources.GetMapDeco(anchorCell.ElementID);
            foreach (var unwalkable in decoration.UnWalkableCells)
            {
                var offset = Quaternion.Euler(
                                 0f,
                                 anchorCell.ElementOrientation,
                                 0f) *
                             new Vector3(unwalkable.X, 0f, unwalkable.Y);
                var footprintCell = grid.GetCell(
                    anchorCell.X + Mathf.RoundToInt(offset.x),
                    anchorCell.Y + Mathf.RoundToInt(offset.z));
                if (footprintCell != null)
                    footprintCell.HasElement = false;
            }
        }

        anchorCell.HasElement = false;
        anchorCell.RemoveElement();
    }

    /// <summary>
    /// Creates the default local session when Main was opened without explicit rules.
    /// </summary>
    private static void EnsureGameplayRules()
    {
        // Editor playtests and menu launches keep their explicitly selected rules.
        if (GameContext.CurrentRules != null &&
            GameContext.CurrentRules.NbGrids > 0)
        {
            return;
        }

        GameContext.CurrentRules = LocalGameSessionFactory.CreateDefault(
            GameContext.TutorialController);
    }

    /// <summary>
    /// Reads one player input and sends an explicit command to the deterministic simulation.
    /// </summary>
    public void HandleCorePlayerInput()
    {
        // Ignore view input while paused, resolving a command, or after a terminal result.
        if (!UsesCoreSimulation ||
            coreCommandRunning ||
            Paused ||
            coreState.Status != SimulationStatus.Running ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            return;
        }

        Orientation direction = Orientation.None;
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Right).IsDown())
            direction = Orientation.Right;
        else if (GameContext.GameSettingsManager.GetInput(InputSettingsType.left).IsDown())
            direction = Orientation.Left;
        else if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Forward).IsDown())
            direction = Orientation.Up;
        else if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Backward).IsDown())
            direction = Orientation.Down;

        if (direction != Orientation.None)
        {
            ExecuteCoreCommand(PlayerCommand.Move(
                ToCoreDirection(RotateInputForCamera(direction))));
            return;
        }

        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear1).IsDown())
        {
            ExecuteCoreCommand(GetCoreGearCommand(0));
            return;
        }
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear2).IsDown())
        {
            ExecuteCoreCommand(GetCoreGearCommand(1));
            return;
        }
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.PlantSword).IsDown())
        {
            ExecuteCoreCommand(PlayerCommand.PlantOrSwapAnchor());
            return;
        }
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.DeployShield).IsDown())
        {
            ExecuteCoreCommand(PlayerCommand.PushAnchor());
            return;
        }
        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Jump).IsDown())
            ExecuteCoreCommand(PlayerCommand.Jump());
    }

    /// <summary>
    /// Chooses pickup or use for one inventory slot from the current cell state.
    /// </summary>
    private PlayerCommand GetCoreGearCommand(int slotIndex)
    {
        // Standing on an available authored gear gives pickup priority over slot use.
        if (coreState.Map.TryGetCell(coreState.Player.Position, out var cell) &&
            !string.IsNullOrEmpty(cell.GearId) &&
            coreState.Board.IsGearAvailable(coreState.Player.Position))
        {
            return PlayerCommand.PickupGear(slotIndex);
        }

        return PlayerCommand.UseGear(slotIndex);
    }

    /// <summary>
    /// Applies camera-relative movement mapping used by the historical isometric controls.
    /// </summary>
    private Orientation RotateInputForCamera(Orientation direction)
    {
        // Rotate the requested cardinal direction by the camera's current quarter-turn.
        switch (Follower.Orientation)
        {
            case Orientation.Left:
                switch (direction)
                {
                    case Orientation.Up:
                        return Orientation.Right;
                    case Orientation.Right:
                        return Orientation.Down;
                    case Orientation.Down:
                        return Orientation.Left;
                    case Orientation.Left:
                        return Orientation.Up;
                }

                break;
            case Orientation.Down:
                switch (direction)
                {
                    case Orientation.Up:
                        return Orientation.Down;
                    case Orientation.Right:
                        return Orientation.Left;
                    case Orientation.Down:
                        return Orientation.Up;
                    case Orientation.Left:
                        return Orientation.Right;
                }

                break;
            case Orientation.Right:
                switch (direction)
                {
                    case Orientation.Up:
                        return Orientation.Left;
                    case Orientation.Right:
                        return Orientation.Up;
                    case Orientation.Down:
                        return Orientation.Right;
                    case Orientation.Left:
                        return Orientation.Down;
                }

                break;
        }

        return direction;
    }

    /// <summary>
    /// Converts a presentation orientation to the core cardinal direction.
    /// </summary>
    private static CoreDirection ToCoreDirection(Orientation orientation)
    {
        // Both enums use the same semantic directions but remain dependency-separated.
        switch (orientation)
        {
            case Orientation.Up:
                return CoreDirection.Up;
            case Orientation.Right:
                return CoreDirection.Right;
            case Orientation.Down:
                return CoreDirection.Down;
            case Orientation.Left:
                return CoreDirection.Left;
            default:
                return CoreDirection.None;
        }
    }

    /// <summary>
    /// Executes one core command and applies its resolved state to presentation views.
    /// </summary>
    private void ExecuteCoreCommand(PlayerCommand command)
    {
        // Resolve immediately, then let presentation consume the ordered event facts.
        if (!UsesCoreSimulation || coreCommandRunning || command == null)
            return;

        coreCommandRunning = true;
        var result = coreSession.Execute(command);
        if (!result.IsAccepted)
        {
            Debug.Log("Command rejected: " + result.RejectionReason);
            coreCommandRunning = false;
            return;
        }

        StartTimer();
        NbTurn = coreState.TurnIndex;
        StartCoroutine(PresentCoreCommand(result.Events));
    }

    /// <summary>
    /// Presents an accepted core command before synchronizing every resolved view.
    /// </summary>
    private IEnumerator PresentCoreCommand(
        IReadOnlyList<SimulationEvent> events)
    {
        // Player events provide the exact authored route without returning authority to views.
        foreach (var simulationEvent in events)
            yield return PresentCorePlayerEvent(simulationEvent);

        ApplyCoreState(events);
        if (coreState.Status == SimulationStatus.Lost)
            StartCoroutine(PresentCoreDefeat());
        else if (coreState.Status == SimulationStatus.Won)
            PlayerReachEnd();
        else
            coreCommandRunning = false;
    }

    /// <summary>
    /// Presents one player-related simulation event with its matching motion language.
    /// </summary>
    private IEnumerator PresentCorePlayerEvent(
        SimulationEvent simulationEvent)
    {
        // Non-player facts remain available to their dedicated view synchronizers.
        switch (simulationEvent.Type)
        {
            case SimulationEventType.PlayerMoved:
                yield return AnimateCorePlayerLinearMovement(
                    simulationEvent.From,
                    simulationEvent.To,
                    simulationEvent.Direction);
                break;
            case SimulationEventType.PlayerForcedMoved:
                if (simulationEvent.ContentId == GearTypeIds.Grapple)
                {
                    yield return AnimateCoreGrappleMovement(
                        simulationEvent.From,
                        simulationEvent.To,
                        simulationEvent.Direction);
                }
                else
                {
                    yield return AnimateCorePlayerLinearMovement(
                        simulationEvent.From,
                        simulationEvent.To,
                        simulationEvent.Direction);
                }

                break;
            case SimulationEventType.PlayerSpringLaunched:
                GameContext.GridController.AnimateSpring(
                    new CellPos(simulationEvent.From.X, simulationEvent.From.Y));
                yield return AnimateCorePlayerArcMovement(
                    simulationEvent.From,
                    simulationEvent.To,
                    simulationEvent.Direction,
                    SpringAnimationCurve,
                    YSpringJump,
                    JumpDuration,
                    "Jump");
                break;
            case SimulationEventType.PlayerTeleported:
                GameContext.GridController.AnimateTP(
                    new CellPos(simulationEvent.From.X, simulationEvent.From.Y));
                yield return null;
                SetCorePlayerPresentationPosition(simulationEvent.To);
                break;
            case SimulationEventType.AnchorPlanted:
                yield return GetCoreWeaponAnchorPresenter().PresentPlant(
                    new Vector3(
                        simulationEvent.To.X,
                        0f,
                        simulationEvent.To.Y),
                    EntitiesMovementDuration);
                break;
            case SimulationEventType.AnchorSwapped:
                yield return GetCoreWeaponAnchorPresenter().PresentSwap(
                    new Vector3(
                        simulationEvent.From.X,
                        0f,
                        simulationEvent.From.Y),
                    new Vector3(
                        simulationEvent.To.X,
                        0f,
                        simulationEvent.To.Y),
                    EntitiesMovementDuration * 2f,
                    EntitiesMovementDuration);
                break;
            case SimulationEventType.AnchorPushed:
                yield return GetCoreWeaponAnchorPresenter().PresentPush(
                    new Vector3(
                        simulationEvent.From.X,
                        0f,
                        simulationEvent.From.Y),
                    new Vector3(
                        simulationEvent.To.X,
                        0f,
                        simulationEvent.To.Y),
                    EntitiesMovementDuration * 0.5f,
                    _playerController.ShieldStrikeVFXPrefab);
                break;
            case SimulationEventType.PlayerJumped:
                yield return AnimateCorePlayerArcMovement(
                    simulationEvent.From,
                    simulationEvent.To,
                    simulationEvent.Direction,
                    GearJumpCurve,
                    GearJumpYOffset,
                    EntitiesMovementDuration * 2f,
                    "Jump");
                break;
        }
    }

    /// <summary>
    /// Launches the retained grapple view and pulls the player to its resolved destination.
    /// </summary>
    private IEnumerator AnimateCoreGrappleMovement(
        CoreGridPosition from,
        CoreGridPosition to,
        CoreDirection direction)
    {
        // The hook and cable visualize an already accepted Core displacement.
        SetCorePlayerPresentationDirection(direction);
        var startPosition = new Vector3(from.X, 0f, from.Y);
        var endPosition = new Vector3(to.X, 0f, to.Y);
        var hookStartPosition = startPosition + Vector3.up * 0.35f;
        var hookEndPosition = endPosition + Vector3.up * 0.35f;
        var distance = Mathf.Max(
            1,
            Mathf.Abs(to.X - from.X) + Mathf.Abs(to.Y - from.Y));
        var launchDuration = Mathf.Max(
            0.0001f,
            EntitiesMovementDuration * (distance / 5f));
        var pullDuration = launchDuration * 2f;
        _playerController.transform.position = startPosition;

        if (GearGrapplePrefab == null)
        {
            Debug.LogError("The grapple presentation prefab is not assigned.");
            yield return AnimateCorePlayerPull(
                startPosition,
                endPosition,
                pullDuration,
                null,
                null);
            yield break;
        }

        DestroyCoreGrappleView();
        coreGrappleView = Instantiate(GearGrapplePrefab);
        var grapple = coreGrappleView;
        grapple.transform.position = hookStartPosition;
        grapple.transform.localEulerAngles =
            GameContext.CurrentMap.Grid.GetOrientationVector(
                ToLegacyOrientation(direction));
        var lineRenderer = grapple.GetComponent<LineRenderer>();
        UpdateCoreGrappleLine(
            lineRenderer,
            startPosition,
            hookStartPosition);

        var elapsed = 0f;
        while (elapsed < launchDuration)
        {
            grapple.transform.position = Vector3.Lerp(
                hookStartPosition,
                hookEndPosition,
                elapsed / launchDuration);
            UpdateCoreGrappleLine(
                lineRenderer,
                startPosition,
                grapple.transform.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        grapple.transform.position = hookEndPosition;
        UpdateCoreGrappleLine(
            lineRenderer,
            startPosition,
            hookEndPosition);
        yield return AnimateCorePlayerPull(
            startPosition,
            endPosition,
            pullDuration,
            grapple.transform,
            lineRenderer);
        DestroyCoreGrappleView();
    }

    /// <summary>
    /// Pulls the player toward an anchored grapple while maintaining the cable endpoints.
    /// </summary>
    private IEnumerator AnimateCorePlayerPull(
        Vector3 startPosition,
        Vector3 endPosition,
        float duration,
        Transform grappleTransform,
        LineRenderer lineRenderer)
    {
        // Jump gives the pulled character an airborne pose instead of walking over holes.
        _playerController.Play("Jump", smooth: false);
        var elapsed = 0f;
        while (elapsed < duration)
        {
            _playerController.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                elapsed / duration);
            UpdateCoreGrappleLine(
                lineRenderer,
                _playerController.transform.position,
                grappleTransform == null
                    ? endPosition
                    : grappleTransform.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _playerController.transform.position = endPosition;
        UpdateCoreGrappleLine(
            lineRenderer,
            endPosition,
            grappleTransform == null
                ? endPosition
                : grappleTransform.position);
    }

    /// <summary>
    /// Updates both world-space endpoints of the retained grapple cable.
    /// </summary>
    private static void UpdateCoreGrappleLine(
        LineRenderer lineRenderer,
        Vector3 playerPosition,
        Vector3 hookPosition)
    {
        // Missing line renderers degrade to hook-only presentation without breaking the turn.
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, playerPosition + Vector3.up * 0.35f);
        lineRenderer.SetPosition(1, hookPosition);
    }

    /// <summary>
    /// Destroys the transient grapple view without affecting its resolved Core movement.
    /// </summary>
    private void DestroyCoreGrappleView()
    {
        // Restarts and scene exits may interrupt the presentation coroutine mid-flight.
        if (coreGrappleView != null)
            Destroy(coreGrappleView);
        coreGrappleView = null;
    }

    /// <summary>
    /// Gets the retained presenter responsible for the equipped weapon anchor.
    /// </summary>
    private CoreWeaponAnchorPresenter GetCoreWeaponAnchorPresenter()
    {
        // Lazy creation waits until the gameplay player and camera views are available.
        if (coreWeaponAnchorPresenter == null)
        {
            coreWeaponAnchorPresenter = new CoreWeaponAnchorPresenter(
                _playerController,
                Follower);
        }

        return coreWeaponAnchorPresenter;
    }

    /// <summary>
    /// Clears every retained weapon-anchor view and restores the held weapon.
    /// </summary>
    private void ClearCoreWeaponAnchorPresentation()
    {
        // Map reloads and scene exits may interrupt an anchor animation mid-frame.
        if (coreWeaponAnchorPresenter != null)
            coreWeaponAnchorPresenter.Clear();
    }

    /// <summary>
    /// Interpolates a grounded player displacement while playing the walk animation.
    /// </summary>
    private IEnumerator AnimateCorePlayerLinearMovement(
        CoreGridPosition from,
        CoreGridPosition to,
        CoreDirection direction)
    {
        // Scale duration by travelled cells so speed boosts remain readable.
        SetCorePlayerPresentationDirection(direction);
        _playerController.Play("Walk");
        var startPosition = new Vector3(from.X, 0f, from.Y);
        var endPosition = new Vector3(to.X, 0f, to.Y);
        var distance = Mathf.Max(
            1,
            Mathf.Abs(to.X - from.X) + Mathf.Abs(to.Y - from.Y));
        var duration = Mathf.Max(
            0.0001f,
            EntitiesMovementDuration * distance);
        var elapsed = 0f;
        _playerController.transform.position = startPosition;
        while (elapsed < duration)
        {
            _playerController.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _playerController.transform.position = endPosition;
    }

    /// <summary>
    /// Interpolates a player displacement along a configurable vertical arc.
    /// </summary>
    private IEnumerator AnimateCorePlayerArcMovement(
        CoreGridPosition from,
        CoreGridPosition to,
        CoreDirection direction,
        AnimationCurve heightCurve,
        float height,
        float duration,
        string animationName)
    {
        // Arc motion is presentation-only; the destination was already resolved by Core.
        SetCorePlayerPresentationDirection(direction);
        _playerController.Play(animationName);
        var startPosition = new Vector3(from.X, 0f, from.Y);
        var endPosition = new Vector3(to.X, 0f, to.Y);
        var safeDuration = Mathf.Max(0.0001f, duration);
        var elapsed = 0f;
        _playerController.transform.position = startPosition;
        while (elapsed < safeDuration)
        {
            var progress = elapsed / safeDuration;
            var position = Vector3.Lerp(startPosition, endPosition, progress);
            if (heightCurve != null)
                position.y = heightCurve.Evaluate(progress) * height;
            _playerController.transform.position = position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _playerController.transform.position = endPosition;
    }

    /// <summary>
    /// Updates the player's presentation-facing without invoking legacy callbacks.
    /// </summary>
    private void SetCorePlayerPresentationDirection(CoreDirection direction)
    {
        // Direction.None preserves the last valid facing for stationary commands.
        if (direction == CoreDirection.None)
            return;

        _playerController.Orientation = ToLegacyOrientation(direction);
        _playerController.transform.localEulerAngles =
            GameContext.CurrentMap.Grid.GetOrientationVector(
                _playerController.Orientation);
    }

    /// <summary>
    /// Places the player presentation at one resolved core coordinate.
    /// </summary>
    private void SetCorePlayerPresentationPosition(CoreGridPosition position)
    {
        // Teleports snap between endpoints while their cell VFX remains event-driven.
        _playerController.transform.position =
            new Vector3(position.X, 0f, position.Y);
    }

    /// <summary>
    /// Presents defeat before handing control to the result screen.
    /// </summary>
    private IEnumerator PresentCoreDefeat()
    {
        // Keep the existing death animation without automatically erasing the failed attempt.
        coreTerminalPresentationRunning = true;
        StopWatch.Stop();
        _playerController.Play("Die");
        yield return new WaitForSeconds(0.35f);
        coreTerminalPresentationRunning = false;
        coreCommandRunning = false;
    }

    /// <summary>
    /// Starts the local run timer after the first accepted command.
    /// </summary>
    public void StartTimer()
    {
        // Rejected commands and loading screens never start timing.
        if (!StopWatch.IsRunning)
            StopWatch.Start();
    }

    /// <summary>
    /// Starts the resolved victory presentation and local progression flow.
    /// </summary>
    private void PlayerReachEnd()
    {
        // Victory has already been validated by Core before presentation begins.
        if (!UsesCoreSimulation ||
            coreState.Status != SimulationStatus.Won ||
            coreTerminalPresentationRunning ||
            coreVictoryReady)
        {
            return;
        }

        coreTerminalPresentationRunning = true;
        coreCommandRunning = true;
        StopWatch.Stop();
        StartCoroutine(AnimateWin());
    }

    /// <summary>
    /// Plays the retained victory animation before enabling result actions.
    /// </summary>
    private IEnumerator AnimateWin()
    {
        // Commit progression once, then expose explicit result-screen actions.
        yield return null;
        PlayerController.Animator.Play("Win");
        yield return new WaitForSeconds(1f);
        CommitCurrentMapCompletion();
        coreVictoryReady = true;
        coreTerminalPresentationRunning = false;
        coreCommandRunning = false;
    }

    /// <summary>
    /// Requests movement or waiting from the active authoritative runtime.
    /// </summary>
    public void PlayerMove(Orientation dir)
    {
        // Camera actions and remaining presentation callbacks route through Core only.
        if (!UsesCoreSimulation)
            return;
        ExecuteCoreCommand(
            dir == Orientation.None
                ? PlayerCommand.Wait()
                : PlayerCommand.Move(ToCoreDirection(dir)));
    }

    /// <summary>
    /// Preserves the historical presentation API while Core owns exit detection.
    /// </summary>
    public void CheckIfReachEndCell()
    {
        // This adapter cannot mutate state; it only reflects an already resolved win.
        if (UsesCoreSimulation && coreState.Status == SimulationStatus.Won)
            PlayerReachEnd();
    }

    /// <summary>
    /// Loads one local map and starts its deterministic simulation session.
    /// </summary>
    private IEnumerator SetGridRoutine(bool animate = true)
    {
        // Keep loading asynchronous for one frame so the existing worker overlay can render.
        coreCommandRunning = true;
        GameContext.UI_WorkerNotifier.Show("Loading map...");
        yield return null;
        NbTurn = 0;
        StopWatch.Stop();
        StopWatch.Reset();
        Time.timeScale = 1f;
        GameContext.WaitForEndOfAction = 0;
        GameContext.EntitiesMoving = 0;

        GameContext.CurrentRules.GetCurrentFullMap((map) =>
        {
            if (map == null)
            {
                GameContext.SceneTransitions.LoadScene("MainMenu");
            }
            else
            {
                SetupCoreMap(map, animate);
            }
        });
    }

    /// <summary>
    /// Imports a loaded local map and initializes its core simulation and Unity views.
    /// </summary>
    private void SetupCoreMap(FullMap map, bool animate)
    {
        // Import the in-memory source without mutating its authored StreamingAssets files.
        coreTerminalPresentationRunning = false;
        coreVictoryReady = false;
        coreSourceGridJson = JsonConvert.SerializeObject(map.Grid);
        coreSourceMapJson = JsonConvert.SerializeObject(map.Map);
        var importer = new LegacyMapImporter();
        var import = importer.Import(
            coreSourceGridJson,
            coreSourceMapJson,
            map.Map?.Name ?? "Local map");
        if (!import.IsSuccessful)
        {
            foreach (var issue in import.Issues)
                Debug.LogError(issue.Code + ": " + issue.Message);
            GameContext.UI_WorkerNotifier.Hide();
            coreCommandRunning = false;
            GameContext.SceneTransitions.LoadScene("MainMenu");
            return;
        }

        GameContext.CurrentMap = map;
        var initialCoreState =
            CoreSimulationState.CreateInitial(import.Package.Map);
        var resumedCheckpoint = pendingCoreCheckpoint;
        var didResumeCheckpoint =
            resumedCheckpoint.HasValue &&
            initialCoreState.Map.IsInside(resumedCheckpoint.Value);
        if (didResumeCheckpoint)
        {
            var initialPlayer = new CorePlayerState(
                resumedCheckpoint.Value,
                CoreDirection.Up,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                null,
                resumedCheckpoint.Value,
                true);
            var restoredCoreState = new CoreSimulationState(
                initialCoreState.Map,
                initialPlayer,
                initialCoreState.Board,
                initialCoreState.Enemies,
                initialCoreState.MovingPlatforms,
                initialCoreState.Random,
                0,
                SimulationStatus.Running);
            coreSession = new GameSession(restoredCoreState);
        }
        else
        {
            coreSession = new GameSession(import.Package.Map);
        }

        pendingCoreCheckpoint = null;
        LastCheckPointReached = didResumeCheckpoint
            ? map.Grid.GetCell(resumedCheckpoint.Value.X, resumedCheckpoint.Value.Y)
            : null;
        NbTurn = 0;
        GameContext.GridController.ClearGrid();
        ClearCoreWeaponAnchorPresentation();
        GameContext.GridController.DrawGrid(GameContext.CurrentMap.Grid);
        Follower.Offset = Follower.BaseOffset;
        _playerController.GearA = null;
        _playerController.GearE = null;
        _playerController.EndAllAlterations();
        _playerController.TABase = 1;
        _playerController.StopAllCoroutines();
        _playerController.UnlockAnimator();
        _playerController.ResetPlayer();
        if (_petController != null)
            Destroy(_petController.gameObject);

        GameContext.GridController.SpawnEnnemies(
            GameContext.CurrentMap.Grid,
            false);
        GameContext.GridController.SpawnPlatforms(GameContext.CurrentMap.Grid);
        foreach (var enemy in GameContext.Ennemies)
            enemy.enabled = false;
        foreach (var platform in GameContext.MovingPlatforms)
            platform.enabled = false;

        Follower.Target = _playerController.transform;
        Events.Fire_StartMap();
        ApplyCoreState(System.Array.Empty<SimulationEvent>());
        coreCommandRunning = false;
        GameContext.UI_WorkerNotifier.Hide();
        if (animate)
        {
            Follower.AnimateCamera(
                GameContext.CurrentMap.Grid.EndCell.GetCellPos().ToVector3(2f));
        }
    }

    /// <summary>
    /// Applies one authoritative core state and its events to every active Unity view.
    /// </summary>
    private void ApplyCoreState(IReadOnlyList<SimulationEvent> events)
    {
        // Board mutations are applied before actor transforms and danger overlays.
        ApplyCoreBoardEvents(events);
        SyncCorePlayerView();
        SyncCoreEnemyViews(events);
        SyncCorePlatformViews();
        SyncCoreCellViews();
        SyncCorePlacedObjectViews();
        SyncCoreAnchorView();
        GameContext.TimeStoped = coreState.Player.TimeStopTurns;
    }

    /// <summary>
    /// Applies resolved board events to the retained visual grid data and VFX.
    /// </summary>
    private void ApplyCoreBoardEvents(IReadOnlyList<SimulationEvent> events)
    {
        // Legacy grid objects are presentation data only and never feed results back into Core.
        foreach (var simulationEvent in events)
        {
            var targetCell = GameContext.CurrentMap.Grid.GetCell(
                simulationEvent.To.X,
                simulationEvent.To.Y);
            switch (simulationEvent.Type)
            {
                case SimulationEventType.CellCollapsed:
                case SimulationEventType.TimedBombExploded:
                    if (simulationEvent.Type == SimulationEventType.TimedBombExploded)
                    {
                        SpawnCoreExplosionVfx(
                            simulationEvent.To,
                            GameContext.GridController.BombeVFXPrefab,
                            "timed bomb");
                    }
                    if (targetCell != null)
                    {
                        targetCell.SetType(CellType.NotWalkable);
                        targetCell.SetWalkable(false);
                        GameContext.GridController.RefreshCrossCell(
                            targetCell,
                            GameContext.CurrentMap.Grid,
                            false);
                    }

                    break;
                case SimulationEventType.GearUsed:
                    if (simulationEvent.ContentId == GearTypeIds.Bomb)
                    {
                        SpawnCoreExplosionVfx(
                            simulationEvent.From,
                            GearBombeVFXPrefab,
                            "bomb gear");
                    }
                    break;
                case SimulationEventType.ElementDestroyed:
                    if (simulationEvent.ContentId == GearTypeIds.Bomb)
                    {
                        // Each destroyed decoration receives its own resolved impact effect.
                        SpawnCoreExplosionVfx(
                            simulationEvent.To,
                            GearBombeVFXPrefab,
                            "bomb decoration impact");
                    }

                    if (targetCell != null)
                    {
                        targetCell.HasElement = false;
                        targetCell.RemoveElement();
                        GameContext.GridController.RemoveElement(targetCell);
                    }

                    break;
                case SimulationEventType.GearPickedUp:
                    if (targetCell != null)
                    {
                        targetCell.SetGear(GearType.None);
                        GameContext.GridController.UnSpawnGear(targetCell);
                    }

                    break;
                case SimulationEventType.CheckpointActivated:
                    LastCheckPointReached = targetCell;
                    _playerController.SaveCheckpointVFX();
                    if (targetCell != null)
                    {
                        GameContext.GridController.RefreshCrossCell(
                            targetCell,
                            GameContext.CurrentMap.Grid,
                            false);
                    }

                    break;
                case SimulationEventType.EnemyStunned:
                    if (simulationEvent.EntityIndex >= 0 &&
                        simulationEvent.EntityIndex < GameContext.Ennemies.Count)
                    {
                        var stunView = Instantiate(GearDiscoballStunVFXPrefab);
                        stunView.transform.position =
                            GameContext.Ennemies[simulationEvent.EntityIndex].transform.position;
                        Destroy(stunView, Mathf.Max(0.25f, simulationEvent.Value));
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Spawns one retained explosion effect at a Core-resolved position.
    /// </summary>
    private static void SpawnCoreExplosionVfx(
        CoreGridPosition position,
        GameObject explosionPrefab,
        string sourceName)
    {
        // Visual effects consume Core facts and never determine affected gameplay cells.
        if (explosionPrefab == null)
        {
            Debug.LogError(
                "The " + sourceName + " explosion VFX prefab is not assigned.");
            return;
        }

        var explosion = Instantiate(explosionPrefab);
        explosion.transform.position = new Vector3(position.X, 0f, position.Y);
    }

    /// <summary>
    /// Synchronizes the hero transform and logical view cell from the core state.
    /// </summary>
    private void SyncCorePlayerView()
    {
        // Direct view assignment avoids invoking historical movement or cell callbacks.
        var position = coreState.Player.Position;
        _playerController.LastCell = _playerController.CurrentCell;
        _playerController.CurrentCell =
            GameContext.CurrentMap.Grid.GetCell(position.X, position.Y);
        _playerController.Orientation = ToLegacyOrientation(coreState.Player.Direction);
        _playerController.transform.position = new Vector3(position.X, 0f, position.Y);
        _playerController.transform.localEulerAngles =
            GameContext.CurrentMap.Grid.GetOrientationVector(
                _playerController.Orientation);
        if (coreState.Status == SimulationStatus.Running)
            _playerController.Play("Idle");
    }

    /// <summary>
    /// Synchronizes all enemy prefab transforms, phases, and danger overlays.
    /// </summary>
    private void SyncCoreEnemyViews(IReadOnlyList<SimulationEvent> events)
    {
        // Spawn order and core definition order are both the authored enemy order.
        var count = Mathf.Min(coreState.Enemies.Count, GameContext.Ennemies.Count);
        for (var index = 0; index < count; index++)
        {
            var state = coreState.Enemies[index];
            var view = GameContext.Ennemies[index];
            view.LastCell = view.CurrentCell;
            view.CurrentCell = GameContext.CurrentMap.Grid.GetCell(
                state.Position.X,
                state.Position.Y);
            view.Orientation = ToLegacyOrientation(state.Direction);
            view.transform.position = new Vector3(
                state.Position.X,
                0f,
                state.Position.Y);
            view.transform.localEulerAngles =
                GameContext.CurrentMap.Grid.GetOrientationVector(view.Orientation);
            view.CurrentPO = new List<CellPos>();
            foreach (var dangerCell in coreSession.GetEnemyDangerCells(index))
                view.CurrentPO.Add(new CellPos(dangerCell.X, dangerCell.Y));
            if (view is Gloutton gloutton)
            {
                gloutton.AttractionPOCells = new List<CellPos>();
                foreach (var effectCell in
                         coreSession.GetEnemySecondaryEffectCells(index))
                {
                    gloutton.AttractionPOCells.Add(
                        new CellPos(effectCell.X, effectCell.Y));
                }
            }
            if (view is Thunderbird && view.transform.childCount > 0)
            {
                var childPosition = view.transform.GetChild(0).localPosition;
                childPosition.y = state.PhaseIndex % 4 >= 2 ? 1.5f : 0f;
                view.transform.GetChild(0).localPosition = childPosition;
            }

            if (view is Axe)
            {
                if (HasCoreEnemyPhaseChanged(events, index))
                {
                    // Core even phases strike right; the clip name describes the swing direction.
                    view.Play(
                        state.PhaseIndex % 2 == 0
                            ? "GoLeft"
                            : "GoRight");
                }
            }
            else if (view is Hamer)
            {
                if (coreState.TurnIndex == 0 ||
                    HasCoreEnemyPhaseChanged(events, index))
                {
                    // Hammer clips and Core danger sides share the same phase naming.
                    view.Play(
                        state.PhaseIndex % 2 == 0
                            ? "HammerRight"
                            : "HammerLeft");
                }
            }
            else if (view is Catapult)
            {
                if (coreState.TurnIndex == 0 ||
                    HasCoreEnemyPhaseChanged(events, index))
                {
                    // Even phases reload; odd phases launch toward the Core impact center.
                    var catapultController =
                        view.GetComponent<CatapultController>();
                    if (catapultController == null)
                    {
                        Debug.LogError(
                            "The catapult presentation controller is missing.");
                    }
                    else if (state.PhaseIndex % 2 == 0)
                    {
                        catapultController.Reload();
                    }
                    else
                    {
                        catapultController.Throw(
                            GetCoreCatapultImpactCell(
                                state.Position,
                                state.Direction));
                    }
                }
            }
            else
            {
                view.Play("Idle");
            }
        }

        GameContext.GridController.DrawCellsPO();
    }

    /// <summary>
    /// Gets the authored five-cell impact center of a Core catapult state.
    /// </summary>
    private static CellPos GetCoreCatapultImpactCell(
        CoreGridPosition origin,
        CoreDirection direction)
    {
        // Presentation targets the same unfiltered center used to build the Core impact cross.
        switch (direction)
        {
            case CoreDirection.Up:
                return new CellPos(origin.X, origin.Y + 5);
            case CoreDirection.Right:
                return new CellPos(origin.X + 5, origin.Y);
            case CoreDirection.Down:
                return new CellPos(origin.X, origin.Y - 5);
            case CoreDirection.Left:
                return new CellPos(origin.X - 5, origin.Y);
            default:
                return new CellPos(origin.X, origin.Y);
        }
    }

    /// <summary>
    /// Reports whether one enemy changed its Core phase during the presented command.
    /// </summary>
    private static bool HasCoreEnemyPhaseChanged(
        IReadOnlyList<SimulationEvent> events,
        int enemyIndex)
    {
        // Event identity prevents static syncs and time-stop turns from replaying animations.
        foreach (var simulationEvent in events)
        {
            if (simulationEvent.Type == SimulationEventType.EnemyPhaseChanged &&
                simulationEvent.EntityIndex == enemyIndex)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Synchronizes every moving-platform transform from its authoritative route state.
    /// </summary>
    private void SyncCorePlatformViews()
    {
        // Platform spawn order follows map-definition order.
        var count = Mathf.Min(
            coreState.MovingPlatforms.Count,
            GameContext.MovingPlatforms.Count);
        for (var index = 0; index < count; index++)
        {
            var state = coreState.MovingPlatforms[index];
            var view = GameContext.MovingPlatforms[index];
            view.LastCell = view.CurrentCell;
            view.CurrentCell = GameContext.CurrentMap.Grid.GetCell(
                state.Position.X,
                state.Position.Y);
            view.Orientation = ToLegacyOrientation(state.Direction);
            view.transform.position = new Vector3(
                state.Position.X,
                0f,
                state.Position.Y);
            view.transform.localEulerAngles =
                GameContext.CurrentMap.Grid.GetOrientationVector(view.Orientation);
        }
    }

    /// <summary>
    /// Synchronizes needle phases and timed-bomb countdown labels.
    /// </summary>
    private void SyncCoreCellViews()
    {
        // Cell view components receive resolved values and do not advance private timers.
        foreach (var needleState in coreState.Board.Needles)
        {
            var cell = GameContext.CurrentMap.Grid.GetCell(
                needleState.Position.X,
                needleState.Position.Y);
            var cellView = GameContext.GridController.GetCellGameObject(cell);
            var needle = cellView == null
                ? null
                : cellView.GetComponentInChildren<Needle>();
            if (needle != null)
                needle.SetActive(needleState.IsActive);
        }

        foreach (var bombState in coreState.Board.TimedBombs)
        {
            if (bombState.HasExploded)
                continue;
            var cell = GameContext.CurrentMap.Grid.GetCell(
                bombState.Position.X,
                bombState.Position.Y);
            var cellView = GameContext.GridController.GetCellGameObject(cell);
            var bomb = cellView == null
                ? null
                : cellView.GetComponentInChildren<BmbeController>();
            if (bomb != null)
                bomb.SetRemainingTurns(bombState.RemainingTurns);
        }
    }

    /// <summary>
    /// Rebuilds temporary mirror, disco-ball, and darkness presentation objects.
    /// </summary>
    private void SyncCorePlacedObjectViews()
    {
        // Rebuilding a very small collection prevents presentation objects owning hidden timers.
        foreach (var view in corePlacedObjectViews.Values)
        {
            if (view != null)
                Destroy(view);
        }
        corePlacedObjectViews.Clear();

        foreach (var placedObject in coreState.Board.PlacedObjects)
        {
            var view = CreateCorePlacedObjectView(placedObject);
            if (view == null)
                continue;
            view.transform.SetParent(GameContext.GridController.GridContainer);
            view.transform.position = new Vector3(
                placedObject.Position.X,
                0f,
                placedObject.Position.Y);
            corePlacedObjectViews.Add(
                placedObject.ObjectId + "@" + placedObject.Position,
                view);
        }
    }

    /// <summary>
    /// Creates one temporary-object view from the retained asset catalogue.
    /// </summary>
    private GameObject CreateCorePlacedObjectView(PlacedObjectState placedObject)
    {
        // Existing mirror and disco assets are reused; darkness gets a lightweight readable zone.
        if (placedObject.ObjectId == GearTypeIds.PlacedMirror)
            return Instantiate(GearMirrorPrefab);
        if (placedObject.ObjectId == GearTypeIds.PlacedDiscoBall)
        {
            var discoBall = Instantiate(GearDiscoballPrefab);
            var controller = discoBall.GetComponent<DiscoballController>();
            if (controller != null)
                controller.enabled = false;
            return discoBall;
        }
        if (placedObject.ObjectId != GearTypeIds.DarknessCloud)
            return null;

        var darkness = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        darkness.name = "Darkness Cloud";
        darkness.transform.localScale = new Vector3(4.5f, 0.025f, 4.5f);
        var collider = darkness.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        var renderer = darkness.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                     Shader.Find("Standard");
        if (renderer != null && shader != null)
        {
            renderer.material = new Material(shader);
            renderer.material.color = new Color(0.08f, 0.02f, 0.15f, 0.65f);
        }

        return darkness;
    }

    /// <summary>
    /// Synchronizes the equipped weapon view with the resolved Core anchor.
    /// </summary>
    private void SyncCoreAnchorView()
    {
        // The equipped weapon and VFX are cosmetic and never participate in collision.
        if (!coreState.Player.AnchorPosition.HasValue)
        {
            ClearCoreWeaponAnchorPresentation();
            return;
        }

        var position = coreState.Player.AnchorPosition.Value;
        GetCoreWeaponAnchorPresenter().Synchronize(
            new Vector3(position.X, 0f, position.Y));
    }

    /// <summary>
    /// Converts a core cardinal direction to the retained presentation orientation.
    /// </summary>
    private static Orientation ToLegacyOrientation(CoreDirection direction)
    {
        // Conversion is explicit so neither enum becomes a serialized cross-layer contract.
        switch (direction)
        {
            case CoreDirection.Up:
                return Orientation.Up;
            case CoreDirection.Right:
                return Orientation.Right;
            case CoreDirection.Down:
                return Orientation.Down;
            case CoreDirection.Left:
                return Orientation.Left;
            default:
                return Orientation.None;
        }
    }

    /// <summary>
    /// Starts asynchronous loading of the current local map.
    /// </summary>
    public void SetGrid(bool animate = true)
    {
        // Loading always rebuilds a fresh deterministic state.
        StartCoroutine(SetGridRoutine(animate));
    }

    /// <summary>
    /// Accepts obsolete presentation death callbacks without creating a second loss path.
    /// </summary>
    public void GameOver(Cell cell, bool fall = false)
    {
        // Authoritative losses are emitted by SimulationEngine and handled in ExecuteCoreCommand.
    }

    /// <summary>
    /// Restarts the current map from its initial state or activated checkpoint.
    /// </summary>
    private void RestartGrid(bool resumeFromCheckpoint)
    {
        // Reloading the authored map restores destroyed cells, elements, actors, and gears.
        pendingCoreCheckpoint =
            UsesCoreSimulation && resumeFromCheckpoint
                ? coreState.Player.CheckpointPosition
                : null;
        DestroyCoreGrappleView();
        ClearCoreWeaponAnchorPresentation();
        StopAllCoroutines();
        GameContext.WaitForEndOfAction = 0;
        coreTerminalPresentationRunning = false;
        coreVictoryReady = false;
        coreCommandRunning = false;
        SetGrid(false);
    }

    /// <summary>
    /// Advances the active tutorial or local map sequence.
    /// </summary>
    private void AdvanceToNextMap()
    {
        // Tutorial persistence is optional, while traversal follows the loaded sequence.
        if (GameContext.CurrentRules.Tutorial)
        {
            var hasNextMap =
                GameContext.CurrentRules.CurrentGrid + 1 <
                GameContext.CurrentRules.NbGrids;
            if (!hasNextMap)
                FinishTutorial();
            else
            {
                GameContext.CurrentRules.NextGrid();
                SetGrid();
            }

            return;
        }

        if (!GameContext.CurrentRules.Endless &&
            GameContext.CurrentRules.CurrentGrid + 1 >= GameContext.CurrentRules.NbGrids)
        {
            // Finite local sessions return to the main menu after their last map.
            InGameUI.Exit();
            return;
        }

        GameContext.CurrentRules.NextGrid();
        SetGrid();
    }

    /// <summary>
    /// Leaves gameplay after the final tutorial map.
    /// </summary>
    private void FinishTutorial()
    {
        // Reuse the standard local-session exit path.
        InGameUI.Exit();
    }

    /// <summary>
    /// Commits the persistent completion rewards for one local authored map.
    /// </summary>
    private void CommitCurrentMapCompletion()
    {
        // Completion is persisted before navigation so leaving the result screen loses nothing.
        LastCheckPointReached = null;
        StopWatch.Stop();
        if (GameContext.CurrentRules.Tutorial)
        {
            if (GameContext.CurrentRules.TracksTutorialProgress)
                GameContext.TutorialController.CompleteCurrentMap();
            return;
        }

        if (GameContext.CurrentMap?.Map != null)
        {
            bool firstCompletion = !GameContext.CurrentMap.Map.IsFinished();
            GameContext.CurrentMap.Map.AlreadyFinished = 1;
            GameContext.CurrentMap.Map.SetState(MapState.Playable);
            LocalMapRepository.UpdateMetadata(GameContext.CurrentMap.Map);
            if (firstCompletion)
            {
                SaveManager.AddExperience(
                    GameContext.Databases.RewardData.XPForFirstFinishingMap);
            }
        }
    }

    /// <summary>
    /// Initializes the current local gameplay session.
    /// </summary>
    public void StartGame()
    {
        // Reset presentation wait counters before loading the authoritative map.
        GameContext.WaitForEndOfAction = 0;
        GameContext.EntitiesMoving = 0;
        SetGrid();
    }

    /// <summary>
    /// Pauses presentation and local run timing.
    /// </summary>
    public void EnterPause()
    {
        // Simulation commands are rejected while Paused is true.
        Paused = true;
        wasTimerRunningBeforePause = StopWatch.IsRunning;
        Time.timeScale = 0f;
        StopWatch.Stop();
    }

    /// <summary>
    /// Resumes presentation and local run timing.
    /// </summary>
    public void ExitPause()
    {
        // Resume time only after the UI has released the pause state.
        Paused = false;
        Time.timeScale = 1f;
        if (wasTimerRunningBeforePause)
            StopWatch.Start();
        wasTimerRunningBeforePause = false;
    }
}
