using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using DFLCommonNetwork.GameEngine;

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

    public GameObject VFXAwakeningCollector;
    public GameObject VFXAwakeningConcentrate;
    public GameObject VFXAwakeningDetermined;
    public GameObject VFXAwakeningHurry;
    public GameObject VFXAwakeningSplit;

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

    void Awake()
    {
        DodgeFromLight.GameManager = this;
        DodgeFromLight.Initialize();
        Events.EntityStartMove -= Events_EntityStartMove;
        Events.EntityStartMove += Events_EntityStartMove;
        Events.EntityEndMove -= Events_EntityEndMove;
        Events.EntityEndMove += Events_EntityEndMove;
        Events.EnnemySeePlayer -= Events_EnnemySeePlayer;
        Events.EnnemySeePlayer += Events_EnnemySeePlayer;
        Events.PlayerStartChangeCell -= OnPlayerStartChangeCell;
        Events.PlayerStartChangeCell += OnPlayerStartChangeCell;
        Events.PlayerEndChangeCell -= OnPlayerEndChangeCell;
        Events.PlayerEndChangeCell += OnPlayerEndChangeCell;

        Follower.PostRenderCallback = () =>
        {
            if (Input.GetKey(KeyCode.P))
            {
                foreach (Ennemy en in DodgeFromLight.Ennemies)
                    en.DrawPath(transform);
                foreach (MovingPlatform platform in DodgeFromLight.MovingPlatforms)
                    platform.DrawPath(transform);
            }
        };
    }

    private void Start()
    {
        if (PoolManager.PoolReady)
            PoolManager_OnPoolingReady();
        else
        {
            PoolManager.OnPoolingReady += PoolManager_OnPoolingReady;
        }
    }

    private void OnDestroy()
    {
        Events.EntityStartMove -= Events_EntityStartMove;
        Events.EntityEndMove -= Events_EntityEndMove;
        Events.EnnemySeePlayer -= Events_EnnemySeePlayer;
        Events.PlayerStartChangeCell -= OnPlayerStartChangeCell;
        Events.PlayerEndChangeCell -= OnPlayerEndChangeCell;
        PoolManager.OnPoolingReady -= PoolManager_OnPoolingReady;
    }

    private void PoolManager_OnPoolingReady()
    {
        if (SceneManager.GetActiveScene().name != "Main")
            return;
        StartGame();
        NbTurn = 0;
    }

    private void Update()
    {
        // restart map
        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.RestartMap).IsDown())
        {
            if (DodgeFromLight.WaitForEndOfAction > 0 && !playerJustDie)
                return;
            if (Input.GetKey(KeyCode.LeftControl))
                LastCheckPointReached = null;
            RestartGrid();
        }

        // next map
        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.NextMap).IsDown())
        {
            if (DodgeFromLight.WaitForEndOfAction > 0 && !playerJustDie)
                return;
            if (DodgeFromLight.CurrentRules.Tutorial)
                return;
            NextMap();
        }

        // save checkpoint
        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.TakeCheckPoint).IsDown() && _playerController.CurrentCell.Type == CellType.CheckPoint)
        {
            if (LastCheckPointReached != PlayerController.CurrentCell)
            {
                Cell c = null;
                if (LastCheckPointReached != null)
                    c = DodgeFromLight.CurrentMap.Grid.GetCell(LastCheckPointReached.GetCellPos());

                LastCheckPointReached = PlayerController.CurrentCell;
                _playerController.SaveCheckpointVFX();
                InGameUI.PressEnterCheckpointPanel.SetActive(false);
                DodgeFromLight.GridController.RefreshCrossCell(PlayerController.CurrentCell, DodgeFromLight.CurrentMap.Grid, false);
                if (c != null)
                    DodgeFromLight.GridController.RefreshCrossCell(c, DodgeFromLight.CurrentMap.Grid, false);
            }
        }
    }

    /// <summary>
    /// on player change cell
    /// </summary>
    /// <param name="lastCell"></param>
    /// <param name="newCell"></param>
    private void OnPlayerEndChangeCell(Cell lastCell, Cell newCell)
    {
        // last walking cell is collapsable
        if (lastCell.Type == CellType.Collapsable)
        {
            lastCell.Walkable = false;
            lastCell.SetType(CellType.NotWalkable);
            DodgeFromLight.GridController.RefreshCrossCell(lastCell, DodgeFromLight.CurrentMap.Grid, false);
        }

        // just walk on Flip A Coin cell
        if (newCell.Type == CellType.FlipACoin)
        {
            Alteration alteration = new Alteration(AlterationType.PM, 1, 2);
            PlayerController.AddAlteration(alteration);
        }

        // just walk on a TP cell
        if (newCell.Type == CellType.TP)
        {
            DodgeFromLight.StartWaitingAction();
            StartCoroutine(TPCell(newCell));
        }

        // walk on needle
        if (newCell.Type == CellType.Needle)
        {
            GameObject go = DodgeFromLight.GridController.GetCellGameObject(newCell);
            if (go != null)
            {
                Needle needle = go.GetComponentInChildren<Needle>();
                if (needle != null)
                {
                    if (needle.IsOn)
                        GameOver(newCell);
                }
            }
        }

        // enter on checkpoint
        if (newCell.Type == CellType.CheckPoint && (LastCheckPointReached == null || !LastCheckPointReached.Equals(newCell)))
        {
            InGameUI.PressEnterCheckpointPanel.SetActive(true);
        }
        else
            InGameUI.PressEnterCheckpointPanel.SetActive(false);

        // enter on gear cell
        if (newCell.GearType != GearType.None)
            InGameUI.PressAEGearPanel.SetActive(true);
        else
            InGameUI.PressAEGearPanel.SetActive(false);

        // jump on spring
        if (newCell.Type == CellType.Spring && newCell.Arg1 > 0)
        {
            CellPos targetCell = DodgeFromLight.CurrentMap.Grid.GetFarestCell(newCell.GetCellPos(), newCell.FloorOrientation, newCell.Arg1, InLineCellType.Jump);
            bool sameCell = newCell.GetCellPos().Equals(targetCell);
            DodgeFromLight.GridController.AnimateSpring(newCell.GetCellPos());
            PlayerController.SetOrientation(newCell.FloorOrientation);
            StartCoroutine(SpringJump(targetCell, sameCell));
        }

        // unspawn cell
        if (lastCell.GearType != GearType.None)
        {
            lastCell.SetGear(GearType.None);
            DodgeFromLight.GridController.UnSpawnGear(lastCell);
        }

        // walk on sword
        if (!_playerController.IsCastingPet && _playerController.plantedSword != null && newCell.GetCellPos().Equals(_playerController.plantedSwordCell))
        {
            _playerController.RecoverSword();
        }
    }

    IEnumerator TPCell(Cell cell)
    {
        // get random available cell
        List<Cell> availableCells = new List<Cell>();
        DodgeFromLight.GridController.AnimateTP(cell.GetCellPos());
        for (int x = 0; x < DodgeFromLight.CurrentMap.Grid.Width; x++)
            for (int y = 0; y < DodgeFromLight.CurrentMap.Grid.Height; y++)
            {
                Cell c = DodgeFromLight.CurrentMap.Grid.GetCell(x, y);
                if (c.EntityOnCell == null &&
                    !DodgeFromLight.CurrentMap.Grid.IsStart(c) &&
                    !DodgeFromLight.CurrentMap.Grid.IsEnd(c) &&
                    c.IsWalkable)
                    availableCells.Add(DodgeFromLight.CurrentMap.Grid.GetCell(x, y));
            }

        if (availableCells.Count > 0)
        {
            Cell tpCell = availableCells[Random.Range(0, availableCells.Count - 1)];
            PlayerController.PlaceOnCell(tpCell, true);
            yield return new WaitForSeconds(EntitiesMovementDuration);
            Events.Fire_PlayerEndChangeCell(cell, tpCell);
        }
        DodgeFromLight.StopWaitingAction();
    }

    IEnumerator SpringJump(CellPos cell, bool sameCell)
    {
        DodgeFromLight.StartWaitingAction();
        float duration = JumpDuration;
        float enlapsed = 0f;
        Vector3 startPos = PlayerController.transform.position;
        Vector3 endPos = cell.ToVector3(0f);
        Cell lastCell = PlayerController.CurrentCell;
        while (enlapsed < duration)
        {
            Vector3 pos = Vector3.Lerp(startPos, endPos, enlapsed / duration);
            pos.y = SpringAnimationCurve.Evaluate(enlapsed / duration) * YSpringJump;
            PlayerController.transform.position = pos;
            yield return null;
            enlapsed += Time.deltaTime;
        }
        PlayerController.PlaceOnCell(DodgeFromLight.CurrentMap.Grid.GetCell(cell), true);
        if (!sameCell)
            Events.Fire_PlayerEndChangeCell(lastCell, DodgeFromLight.CurrentMap.Grid.GetCell(cell));
        DodgeFromLight.StopWaitingAction();

        if (sameCell)
            yield break;
        foreach (Ennemy ennemy in DodgeFromLight.Ennemies)
        {
            ennemy.RefreshPO();
            ennemy.DoEffect();
        }
        DodgeFromLight.GridController.DrawCellsPO();
    }

    public void StartTimer()
    {
        if (!StopWatch.IsRunning)
            StopWatch.Start();
    }

    public bool TimerStarted()
    {
        return StopWatch.IsRunning;
    }

    /// <summary>
    /// on player start change cell
    /// </summary>
    /// <param name="lastCell"></param>
    /// <param name="newCell"></param>
    private void OnPlayerStartChangeCell(Cell lastCell, Cell newCell)
    {
    }

    private void Events_EnnemySeePlayer(Ennemy obj, Cell cell)
    {
        GameOver(cell);
    }

    private void Events_EntityEndMove(Entity entity)
    {
        DodgeFromLight.EntitiesMoving--;

        if (entity.Name == PlayerController?.Name)
        {
            Events.Fire_PlayerEndChangeCell(entity.LastCell, entity.CurrentCell);
        }
    }

    private void Events_EntityStartMove(Entity entity)
    {
        DodgeFromLight.EntitiesMoving++;

        if (entity.Name == PlayerController?.Name)
        {
            Events.Fire_PlayerStartChangeCell(entity.LastCell, entity.CurrentCell);
        }
    }

    public void PlayerReachEnd()
    {
        StopWatch.Stop();
        DodgeFromLight.StartWaitingAction();
        //Debug.Log("player reach end");
        StartCoroutine(AnimateWin());
    }

    IEnumerator AnimateWin()
    {
        yield return null;
        PlayerController.Animator.Play("Win");
        yield return new WaitForSeconds(1f);
        DodgeFromLight.StartWaitingAction();
        StartCoroutine(EndOfGrid());
    }

    public void PlayerMove(Orientation dir)
    {
        StartCoroutine(PlayerMove_Routine(dir));
    }

    IEnumerator PlayerMove_Routine(Orientation dir)
    {
        Cell targetCell = null;
        PlayerController.Events_TurnStart();
        if (dir != Orientation.None)
            if (PlayerController != null && PlayerController.CurrentTA > 0)
            {
                targetCell = DodgeFromLight.CurrentMap.Grid.GetNextCell(PlayerController.CurrentCell.GetCellPos(), dir, PlayerController.GetCurrentPM(), true, true);

                if (targetCell == null)
                {
                    targetCell = null;
                    // can't go 
                    if (PlayerController.Orientation == dir)
                        yield break;
                }
            }

        Events.Fire_TurnStart();

        if (PlayerController != null && dir != Orientation.None)
        {
            if (PlayerController.CurrentTA > 0)
            {
                if (targetCell == null && PlayerController.Orientation == dir)
                {
                    Events.Fire_TurnEnd();
                    yield break;
                }

                // move player
                if (dir != Orientation.None)
                    PlayerController.SetOrientation(dir);
                if (targetCell != null && dir != Orientation.None)
                    PlayerController.GoToCell(targetCell, EntitiesMovementDuration);
                else
                {
                    Events.Fire_PlayerStartChangeCell(PlayerController.CurrentCell, PlayerController.CurrentCell);
                    Events.Fire_PlayerEndChangeCell(PlayerController.CurrentCell, PlayerController.CurrentCell);
                }
            }
        }

        List<Ennemy> notMovedEnnemies = new List<Ennemy>();

        // move ennemies
        foreach (Ennemy ennemy in DodgeFromLight.Ennemies)
            if (ennemy.GetCurrentPM() > 0 && ennemy.Patrol.NbCells > 0)
                ennemy.UsePM(EntitiesMovementDuration / (float)ennemy.GetCurrentPM());
            else
                notMovedEnnemies.Add(ennemy);

        // move platforms
        foreach (MovingPlatform platform in DodgeFromLight.MovingPlatforms)
            if (platform.CurrentTA > 0 && platform.Patrol.NbCells > 0)
                platform.UsePM(EntitiesMovementDuration / (float)platform.CurrentTA);


        while (DodgeFromLight.HasEntitiesMoving && !playerJustDie)
            yield return null;

        foreach (Ennemy ennemy in notMovedEnnemies)
        {
            ennemy.RefreshPO();
            ennemy.DoEffect();
        }

        while (DodgeFromLight.HasEntitiesMoving && !playerJustDie)
            yield return null;

        // draw cell PO
        DodgeFromLight.GridController.DrawCellsPO();

        NbTurn++;
        Events.Fire_TurnEnd();

        // check if player fall into poinson
        if (PlayerController.CurrentCell.Type == CellType.NotWalkable)
        {
            bool onplatform = false;
            foreach (var platform in DodgeFromLight.MovingPlatforms)
                if (platform.CurrentCell.GetCellPos().Equals(PlayerController.CurrentCell.GetCellPos()))
                {
                    onplatform = true;
                    break;
                }
            if (!onplatform)
                GameOver(PlayerController.CurrentCell, true);
        }
        // check if just walk on not walkable ennemy
        else if (PlayerController.CurrentCell.EntityOnCell != null && PlayerController.CurrentCell.EntityOnCell is Ennemy)
        {
            if (PlayerController.CurrentCell.EntityOnCell.CantWalkOnMe)
            {
                Cell cell = PlayerController.LastCell;
                if (cell.Equals(PlayerController.CurrentCell))
                    cell = PlayerController.CurrentCell.EntityOnCell.LastCell;
                if (cell.Equals(PlayerController.CurrentCell))
                    cell = DodgeFromLight.CurrentMap.Grid.GetNextCell(PlayerController.CurrentCell, PlayerController.CurrentCell.EntityOnCell.Orientation, 1);
                if (cell == null || cell.Equals(PlayerController.CurrentCell))
                    cell = DodgeFromLight.CurrentMap.Grid.GetNextCell(PlayerController.CurrentCell, Orientation.Up, 1);
                if (cell == null || cell.Equals(PlayerController.CurrentCell))
                    cell = DodgeFromLight.CurrentMap.Grid.GetNextCell(PlayerController.CurrentCell, Orientation.Right, 1);
                if (cell == null || cell.Equals(PlayerController.CurrentCell))
                    cell = DodgeFromLight.CurrentMap.Grid.GetNextCell(PlayerController.CurrentCell, Orientation.Down, 1);
                if (cell == null || cell.Equals(PlayerController.CurrentCell))
                    cell = DodgeFromLight.CurrentMap.Grid.GetNextCell(PlayerController.CurrentCell, Orientation.Left, 1);

                if (cell == null || cell.Equals(PlayerController.CurrentCell)) // we can't go anywhere
                {
                    GameOver(PlayerController.CurrentCell, false);
                }
                else // let's bounce on ennemy
                    PlayerMove(DodgeFromLight.CurrentMap.Grid.GetOrientation(PlayerController.CurrentCell.GetCellPos(), cell.GetCellPos()));
            }
        }
        // check if player reach end
        else CheckIfReachEndCell();
        playerJustDie = false;
        DodgeFromLight.TimeStoped--;
    }

    public void CheckIfReachEndCell()
    {
        if (DodgeFromLight.CurrentMap.Grid.IsEnd(_playerController.CurrentCell) && !playerJustDie)
        {
            PlayerReachEnd();
        }
    }

    IEnumerator SetGridRoutine(bool animate = true)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Loading map...");
        yield return null;
        playerJustDie = false;
        NbTurn = 0;
        StopWatch.Stop();
        StopWatch.Reset();
        Time.timeScale = 1f;
        DodgeFromLight.WaitForEndOfAction = 0;
        DodgeFromLight.EntitiesMoving = 0;

        DodgeFromLight.CurrentRules.GetCurrentFullMap((map) =>
        {
            if (map == null)
            {
                DodgeFromLight.SceneTransitions.LoadScene("Lobby");
            }
            else
            {
                DodgeFromLight.CurrentMap = map;
                DodgeFromLight.GridController.ClearGrid();
                DodgeFromLight.GridController.DrawGrid(DodgeFromLight.CurrentMap.Grid);
                Follower.Offset = Follower.BaseOffset;
                _playerController.ThrowGear(GearSide.A);
                _playerController.ThrowGear(GearSide.E);
                _playerController.EndAllAlterations();
                _playerController.TABase = 1;
                _playerController.StartMap();
                _playerController.StopAllCoroutines();
                _playerController.UnlockAnimator();
                _playerController.Play("Idle");
                _playerController.CurrentCell = null;
                _playerController.PlaceOnCell(DodgeFromLight.CurrentMap.Grid.StartCell, true);
                _playerController.SetOrientation(Orientation.Up);
                _playerController.ResetPlayer();
                if (_petController != null)
                    Destroy(_petController.gameObject);
                DodgeFromLight.GridController.SpawnEnnemies(DodgeFromLight.CurrentMap.Grid);
                DodgeFromLight.GridController.SpawnPlatforms(DodgeFromLight.CurrentMap.Grid);
                Follower.Target = _playerController.transform;
                Events.Fire_StartMap();
                DodgeFromLight.UI_WorkerNotifier.Hide();
                if (animate)
                {
                    Follower.AnimateCamera(DodgeFromLight.CurrentMap.Grid.EndCell.GetCellPos().ToVector3(2f),
                        DodgeFromLight.CurrentMap.Grid.StartCell.GetCellPos().ToVector3(2f));
                }
                if (LastCheckPointReached != null)
                    PlayerController.PlaceOnCell(LastCheckPointReached, true);
            }
        });
    }

    public void SetGrid(bool animate = true)
    {
        StartCoroutine(SetGridRoutine(animate));
    }

    bool playerJustDie = false;
    public void GameOver(Cell cell, bool fall = false)
    {
        if (playerJustDie)
            return;

        if (_playerController.IsCastingPet && cell.EntityOnCell == _petController) // pet just die
        {
            DodgeFromLight.StartWaitingAction();
            StartCoroutine(ReduceTimeScale(fall, true));
            _petController.Dead = true;
        }
        else // player just die
        {
            StopWatch.Stop();
            playerJustDie = true;
            PoolManager.Instance.PushBackAllPool(PoolName.POCell);
            DodgeFromLight.WaitForEndOfAction = 9999;
            StartCoroutine(ReduceTimeScale(fall, false));
        }
    }

    IEnumerator ReduceTimeScale(bool fall, bool pet)
    {
        yield return null;
        playerJustDie = true;
        if (pet)
            _petController.Play("Die");
        else
        {
            _playerController.Play("Die");
            Follower.Target = _playerController.transform;
        }
        if (!fall)
            yield return new WaitForSeconds(.5f);
        else
        {
            float dur = 0.1f;
            float enl = 0f;
            Vector3 s = pet ? _petController.transform.position : _playerController.transform.position;
            Vector3 e = s;
            e.y -= 0.8f;
            while (enl < dur)
            {
                if (pet)
                    _petController.transform.position = Vector3.Lerp(s, e, enl / dur);
                else
                    _playerController.transform.position = Vector3.Lerp(s, e, enl / dur);
                yield return null;
                enl += Time.deltaTime;
            }
        }
        float duration = 1f;
        float enlapsed = 0f;
        Vector3 start = Follower.Offset;
        Vector3 end = Follower.Offset / 2f;
        while (enlapsed < duration)
        {
            Time.timeScale = Mathf.Lerp(1f, 0.5f, enlapsed / duration);
            Follower.Offset = Vector3.Lerp(start, end, enlapsed / duration);
            yield return null;
            enlapsed += Time.unscaledDeltaTime;
        }

        _playerController.UncastPet(false, false);
        Time.timeScale = 1f;
        if (!pet)
            RestartGrid();
        else
        {
            Follower.Offset = Follower.BaseOffset;
            DodgeFromLight.StopWaitingAction();
        }
    }

    public void RestartGrid()
    {
        StopAllCoroutines();
        DodgeFromLight.WaitForEndOfAction = 0;
        SetGrid(false);
    }

    public void NextMap()
    {
        if (DodgeFromLight.CurrentRules.DiscoveryMode)
        {
            NextDiscoveryMap();
        }
        else
        {
            if (DodgeFromLight.CurrentRules.Tutorial)
            {
                DodgeFromLight.TutorialController.FinishCurrentMap();
                if (DodgeFromLight.TutorialController.HasFinishTutorial())
                    FinishTutorial();
                else
                {
                    DodgeFromLight.CurrentRules.NextGrid();
                    SetGrid();
                }
            }
            else
            {
                DodgeFromLight.CurrentRules.NextGrid();
                SetGrid();
            }
        }
    }

    public void FinishTutorial()
    {
        InGameUI.Exit();
    }

    IEnumerator EndOfGrid()
    {
        bool saveScore = LastCheckPointReached == null;
        LastCheckPointReached = null;
        StopWatch.Stop();
        bool saving = false;
        if (!DodgeFromLight.CurrentMap.Map.IsFinished() &&
            !DodgeFromLight.CurrentRules.DiscoveryMode &&
            !DodgeFromLight.CurrentRules.Tutorial &&
            DodgeFromLight.CurrentMap.Map.State != MapState.Locked)
        {
            DodgeFromLight.CurrentMap.Map.AlreadyFinished = 0;
            // finish map the first time, must save it
            saving = true;
            DodgeFromLight.UI_WorkerNotifier.Show("Saving map avancement");
            DFLClient.UpdateFinishState(DodgeFromLight.CurrentMap.Map.ID, DodgeFromLight.CurrentMap.Map.AlreadyFinished, (res) =>
            {
                saving = false;
                DodgeFromLight.UI_WorkerNotifier.Hide();
                if (res.Error)
                    DodgeFromLight.UI_Notifications.Notify("Fail : " + res.APIResponse);
            });
        }
        while (saving)
            yield return null;

        if (DFLClient.OnlineState == OnlineState.Online && DFLClient.LoginState == LoginState.LoggedIn && DodgeFromLight.CurrentMap.Map.State == MapState.Locked && saveScore)
        {
            DodgeFromLight.UI_WorkerNotifier.Show("Saving Score");
            string gridID = DodgeFromLight.CurrentRules.DiscoveryMode ? DodgeFromLight.CurrentRules.DiscoveryMapID : DodgeFromLight.CurrentRules.GetCurrentGridID();
            DFLClient.AddScore(gridID, StopWatch.ElapsedMilliseconds, NbTurn, (success, errorMess, winXp) =>
            {
                if (!success)
                    DodgeFromLight.UI_Notifications.Notify(errorMess);
                else if (winXp)
                    DFLClient.AddXP(DodgeFromLight.Databases.RewardData.XPForFirstFinishingMap, (success, errorMess) =>
                    {
                        if (!success)
                            DodgeFromLight.UI_Notifications.Notify(errorMess);
                    });

                if (DodgeFromLight.CurrentRules.DiscoveryMode)
                {
                    DFLClient.HasVote(gridID, DFLClient.CurrentUser.ID, (res, voted) =>
                    {
                        DodgeFromLight.UI_WorkerNotifier.Show("Getting Score");
                        DFLClient.GetScores(gridID, 10, (res, scores) =>
                        {
                            InGameUI.ShowScores(scores, voted);
                            DodgeFromLight.UI_WorkerNotifier.Hide();
                        });
                    });
                }
                else
                {
                    if (!DodgeFromLight.CurrentRules.DontShowScore)
                    {
                        DFLClient.HasVote(gridID, DFLClient.CurrentUser.ID, (success, voted) =>
                        {
                            DodgeFromLight.UI_WorkerNotifier.Show("Getting Score");
                            DFLClient.GetScores(gridID, 10, (success, scores) =>
                            {
                                if (!success)
                                {
                                    Debug.Log("fail getting scores");
                                    NextMap();
                                }
                                else
                                    InGameUI.ShowScores(scores, voted);
                                DodgeFromLight.UI_WorkerNotifier.Hide();
                            });
                        });
                    }
                    else
                    {
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                        NextMap();
                    }
                }
            });
        }
        else
        {
            NextMap();
        }
    }

    private void NextDiscoveryMap()
    {
        StopWatch.Reset();
        NbTurn = 0;
        DodgeFromLight.UI_WorkerNotifier.Show("Getting Discovery Map...");
        DFLClient.GetDiscoveryMapID((res, id) =>
        {
            if (!res.Error)
            {
                DodgeFromLight.UI_WorkerNotifier.Show("Downloading Map...");
                string mapPath = GridManager.Folder;
                DFLClient.DownloadMap(id, mapPath, (res) =>
                {
                    if (!res.Error)
                    {
                        DodgeFromLight.CurrentRules = new GameRules().Discovery(id);
                        DodgeFromLight.CurrentRules.DiscoveryMode = true;
                        SetGrid();
                    }
                    else
                        DodgeFromLight.UI_Notifications.Notify("Error downloading map.");
                    DodgeFromLight.UI_WorkerNotifier.Hide();
                });
            }
            else
            {
                DodgeFromLight.UI_Notifications.Notify("Error getting discovery map");
                DodgeFromLight.UI_WorkerNotifier.Hide();
            }
        });
    }

    public void WinGame()
    {
        Debug.Log("End Of Game");
    }

    public void StartGame()
    {
        DodgeFromLight.WaitForEndOfAction = 0;
        DodgeFromLight.EntitiesMoving = 0;
        SetGrid();
    }

    public void EnterPause()
    {
        Paused = true;
        Time.timeScale = 0f;
        StopWatch.Stop();
    }

    public void ExitPause()
    {
        Paused = false;
        Time.timeScale = 1f;
        StopWatch.Start();
    }
}