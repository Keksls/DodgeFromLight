using DFLCommonNetwork.GameEngine;
using SlyvekGameEngine.GameEngine.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    public GameObject ClickOnCellVFX;
    public EventSystem EventSystem;
    public GameObject RefreshSaveVFX;
    public GameObject SpawnVFX;
    public GameObject UnspawnVFX;
    public UI_MapStar UI_MapStar;
    public UI_Console Console;
    public Camera Camera;
    public GridController GridController;
    public GameObject PlayerPrefab;
    public int? CurrentLobby = 0;
    public GameObject LobbyItemPrefab;
    public GameObject ListOfLobbiesPanel;
    public GameObject LocalMapsPanel;
    public Transform LobbiesContainer;
    public Grid CurrentGrid;
    ClickableMapObject currentClickable = null;
    Cell lastClickableCell;
    public bool Visiting = false;
    private Dictionary<int, LitePlayerSave> ClientsOnLobby = new Dictionary<int, LitePlayerSave>();
    private Dictionary<int, LobbyPlayer_Controller> PlayersOnLobby = new Dictionary<int, LobbyPlayer_Controller>();

    private void Awake()
    {
        Instance = this;
        Events.ClickOnClickableObject -= Events_ClickOnClickableObject;
        Events.ClickOnClickableObject += Events_ClickOnClickableObject;
    }

    private void OnDestroy()
    {
        Events.ClickOnClickableObject -= Events_ClickOnClickableObject;
    }

    private void Events_ClickOnClickableObject(string obj)
    {
        if (obj == "Portal")
            ShowListOfLobbies();
        if (obj == "GamePillar")
            UI_MapStar.gameObject.SetActive(true);
        else if (obj == "CraftingTable")
            LocalMapsPanel.SetActive(true);
    }

    private void Start()
    {
        if (DFLClient.LoginState == LoginState.LoggedIn)
            JoinLobby(-DFLClient.CurrentUser.ID, (success) =>
            {
                if (!success)
                    DodgeFromLight.UI_Notifications.Notify("Fail enter Hub");
            });
        UI_MapStar.GetMapStars();
    }

    private void Update()
    {
        // check Click on cell
        if (!CurrentLobby.HasValue || EventSystem.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0)) // click on cell
        {
            CellPos? cellPos = GetClickedCell();
            if (cellPos.HasValue)
            {
                GameObject vfx = Instantiate(ClickOnCellVFX);
                Vector3 pos = cellPos.Value.ToVector3(0.05f);
                vfx.transform.position = pos;
                if (CurrentController().CurrentCell.GetCellPos().Equals(cellPos.Value))
                {
                    if (currentClickable != null)
                        Events.Fire_ClickOnClickableObject(currentClickable.Args);
                    return;
                }
                if (currentClickable != null)
                    CurrentController().WalkToCell(cellPos.Value, () =>
                    {
                        if (currentClickable != null)
                            Events.Fire_ClickOnClickableObject(currentClickable.Args);
                    });
                else
                    CurrentController().WalkToCell(cellPos.Value);
                ChangeCell(cellPos.Value);
            }
        }

        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Inputs.GetInput(InputSettingsType.Forward).IsDown())
        {
            TrySetOrientation(Orientation.Up);
        }

        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Inputs.GetInput(InputSettingsType.Backward).IsDown())
        {
            TrySetOrientation(Orientation.Down);
        }

        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Inputs.GetInput(InputSettingsType.Right).IsDown())
        {
            TrySetOrientation(Orientation.Right);
        }

        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Inputs.GetInput(InputSettingsType.left).IsDown())
        {
            TrySetOrientation(Orientation.Left);
        }
    }

    private void TrySetOrientation(Orientation targetDir)
    {
        Vector3 snappedControllerPos = CurrentController().transform.position;
        snappedControllerPos.y = 0f;
        Vector3 snappedCameraPos = Camera.transform.position;
        snappedCameraPos.y = 0f;
        Vector3 aligmentForward = snappedControllerPos - snappedCameraPos;
        aligmentForward = aligmentForward.AxisRound();
        Orientation dir = CurrentGrid.GetOrientationFromVector(aligmentForward);
        if (dir == Orientation.Right)
            dir = Orientation.Left;
        else if (dir == Orientation.Left)
            dir = Orientation.Right;

        // get final player orientation
        Orientation finalOrientation = CurrentGrid.AddOrientations(targetDir, dir);
        if (finalOrientation != CurrentController().Orientation)
        {
            CurrentController().SetOrientation(finalOrientation);
            if (ClientsOnLobby.Count > 1)
                DFLClient.SetOrientation(finalOrientation);
        }
    }

    private CellPos? GetClickedCell()
    {
        RaycastHit hit;
        Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            var pos = hit.transform.position;

            // if clickable object clicked
            currentClickable = hit.transform.GetComponentInChildren<ClickableMapObject>();
            if (currentClickable != null)
            {
                lastClickableCell = CurrentGrid.GetCell(currentClickable.Cell);
                // get nearset cell to walk on clickable
                CellPos cell = currentClickable.Cell;
                CellPos nearestCell = cell;
                int lowestDist = int.MaxValue;
                for (int i = 1; i <= 4; i++)
                {
                    Orientation dir = (Orientation)i;
                    Cell c = CurrentGrid.GetNeighbor(cell, dir, 1);
                    if (c != null && c.Walkable && !c.HasElement)
                    {
                        int nbCell = PathFinding.FindPath(CurrentGrid, CurrentController().CurrentCell, c, true).Count;
                        if (nbCell < lowestDist)
                        {
                            lowestDist = nbCell;
                            nearestCell = c.GetCellPos();
                        }
                    }
                }

                return nearestCell;
            }
            else
            {
                string cellName = hit.transform.gameObject.name;
                if (cellName.Contains("_"))
                {
                    int x = -1, y = -1;
                    int.TryParse(cellName.Split('_')[0], out x);
                    int.TryParse(cellName.Split('_')[1], out y);
                    Cell cell = CurrentGrid.GetCell(x, y);
                    if (cell != null)
                        return cell.GetCellPos();
                }
            }
        }
        return null;
    }

    public void StartForceOrnamentEnabled()
    {
        var controller = CurrentController();
        if (controller != null)
            controller.StartForceOrnamentEnabled();
    }

    public void StopForceOrnamentEnabled()
    {
        if (DFLClient.LoginState == LoginState.LoggedIn && PlayersOnLobby.ContainsKey(DFLClient.CurrentUser.ID))
            CurrentController().StopForceOrnamentEnabled();
    }

    #region Join / Leave Lobby
    public void JoinLobby(int LobbyID, Action<bool> Callback)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Entering a new world...");
        GetLobbyGrid(LobbyID, (success) =>
        {
            bool isHub = LobbyID < 0;

            CurrentGrid = GridManager.GetGrid();
            DFLClient.EnterLobby(LobbyID, CurrentGrid.StartCell.GetCellPos(), (success, client) =>
            {
                Callback?.Invoke(success);
                if (!success)
                {
                    DodgeFromLight.UI_Notifications.Notify("Fail to enter lobby");
                    return;
                }
                CurrentLobby = LobbyID;
                DFLClient.SetState(isHub ? PlayerState.InHub : PlayerState.InLobby);

                GridController.ClearGrid();
                GridController.DrawGrid(CurrentGrid, true);
                GridController.AddCellsColiders(CurrentGrid, false);
                SpawnClient(client);
                Camera.gameObject.GetComponent<CharacterCameraController>().target = CurrentController().transform;

                string Name = "";
                DFLClient.CurrentMessage.Get(ref Name);
                int maxClients = 0, nbClients = 0;
                DFLClient.CurrentMessage.Get(ref maxClients);
                DFLClient.CurrentMessage.Get(ref nbClients);
                Events.Fire_EnterLobby(client, Name, maxClients, nbClients);

                DFLClient.GetClientsOnLobby(CurrentLobby.Value, (success, clients) =>
                {
                    if (success)
                        foreach (var client in clients)
                            SpawnClient(client);
                });

                // walk forward portal
                CellPos cell = CurrentGrid.StartCell.GetCellPos();
                CellPos nearestCell = cell;
                int lowestDist = int.MaxValue;
                for (int i = 0; i <= 4; i++)
                {
                    Orientation dir = (Orientation)i;
                    Cell c = CurrentGrid.GetNeighbor(cell, dir, 1);
                    if (c != null && c.Walkable && !c.HasElement)
                    {
                        int nbCell = PathFinding.FindPath(CurrentGrid, CurrentController().CurrentCell, c, true).Count;
                        if (nbCell < lowestDist)
                        {
                            lowestDist = nbCell;
                            nearestCell = c.GetCellPos();
                        }
                    }
                }
                CurrentController().WalkToCell(nearestCell);
                ChangeCell(nearestCell);

                DodgeFromLight.EnvironmentController.SetEnvironment(CurrentGrid.Environment);
                DodgeFromLight.UI_WorkerNotifier.Hide();
            });
        });
    }

    public void VisitHub(int hubID, string friendName)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Entering a new world...");
        LeaveLobby((success) =>
        {
            if (success)
                GetLobbyGrid(hubID, (success) =>
                {
                    if (!success)
                    {
                        DodgeFromLight.UI_Notifications.Notify("Fail to enter Hub");
                        return;
                    }
                    CurrentGrid = GridManager.GetGrid();
                    Visiting = true;
                    CurrentLobby = hubID;
                    DFLClient.SetState(PlayerState.InGame);

                    GridController.ClearGrid();
                    GridController.DrawGrid(CurrentGrid, true);
                    GridController.AddCellsColiders(CurrentGrid, false);

                    var player = new LitePlayerSave(SaveManager.CurrentSave);
                    player.ID = DFLClient.CurrentUser.ID;
                    player.Name = DFLClient.CurrentUser.Name;
                    player.Pos = CurrentGrid.StartCell.GetCellPos();
                    SpawnClient(player);
                    Camera.gameObject.GetComponent<CharacterCameraController>().target = CurrentController().transform;
                    UI_LobbyInfos.Instance.EnterLobby(friendName + "'s Hub <i>visitor</i>", 1, 1);

                    // walk forward portal
                    CellPos cell = CurrentGrid.StartCell.GetCellPos();
                    CellPos nearestCell = cell;
                    int lowestDist = int.MaxValue;
                    for (int i = 0; i <= 4; i++)
                    {
                        Orientation dir = (Orientation)i;
                        Cell c = CurrentGrid.GetNeighbor(cell, dir, 1);
                        if (c != null && c.Walkable && !c.HasElement)
                        {
                            int nbCell = PathFinding.FindPath(CurrentGrid, CurrentController().CurrentCell, c, true).Count;
                            if (nbCell < lowestDist)
                            {
                                lowestDist = nbCell;
                                nearestCell = c.GetCellPos();
                            }
                        }
                    }
                    CurrentController().WalkToCell(nearestCell);
                    ChangeCell(nearestCell);

                    DodgeFromLight.EnvironmentController.SetEnvironment(CurrentGrid.Environment);
                    DodgeFromLight.UI_WorkerNotifier.Hide();
                });
        });
    }

    public void GetLobbyGrid(int lobbyID, Action<bool> Callback)
    {
        bool myHub = lobbyID == -DFLClient.CurrentUser.ID;
        if (!myHub) // download lobby
        {
            if (lobbyID < 0)
            {
                GridManager.CleanFolder();
                DFLClient.DownloadHub(-lobbyID, (res) =>
                {
                    if (res.Error) // error getting hub
                    {
                        DodgeFromLight.UI_Notifications.Notify("Fail getting hub data");
                        Debug.Log(res.ToString());
                    }
                    Callback?.Invoke(!res.Error);
                });
            }
            else
            {
                GridManager.CleanFolder();
                DFLClient.DownloadLobby(lobbyID, (res) =>
                {
                    if (res.Error) // error getting lobby
                    {
                        DodgeFromLight.UI_Notifications.Notify("Fail getting lobby data");
                        Debug.Log(res.ToString());
                    }
                    Callback?.Invoke(!res.Error);
                });
            }
        }
        else // create and download your hub
        {
            GridManager.CleanFolder();
            // check if has hub
            DFLClient.HasHub((success, hasHub) =>
            {
                if (!success) // error getting has hub
                {
                    DodgeFromLight.UI_Notifications.Notify("Fail getting hub status");
                    return;
                }

                if (!hasHub) // no hub for now
                {
                    GridManager.InitDefaultHub();
                    DFLClient.UploadHub((res) =>
                    {
                        if (res.Error)
                        {
                            DodgeFromLight.UI_Notifications.Notify("fail creating hub");
                            Debug.Log(res.APIResponse);
                            return;
                        }
                        JoinLobby(-DFLClient.CurrentUser.ID, (success) =>
                        {
                            if (!success)
                                DodgeFromLight.UI_Notifications.Notify("Fail enter Hub");
                        });
                    });
                    return;
                }

                DFLClient.DownloadHub(-lobbyID, (res) =>
                {
                    if (res.Error) // error getting hub
                    {
                        DodgeFromLight.UI_Notifications.Notify("Fail getting hub data");
                        Debug.Log(res.ToString());
                    }
                    Callback?.Invoke(!res.Error);
                });
            });
        }
    }

    public void LeaveLobby(Action<bool> Callback = null)
    {
        if (!Visiting)
        {
            DFLClient.LeaveLobby((success) =>
            {
                if (success)
                {
                    List<int> IDs = PlayersOnLobby.Keys.ToList();
                    foreach (int id in IDs)
                        UnspawnClient(id, false);
                    ClientsOnLobby.Clear();
                    PlayersOnLobby.Clear();
                    GridController.ClearGrid();
                    CurrentLobby = null;
                    Events.Fire_LeaveLobbyt();
                }
                Callback?.Invoke(success);
            });
        }
        else
        {
            List<int> IDs = PlayersOnLobby.Keys.ToList();
            foreach (int id in IDs)
                UnspawnClient(id, false);
            ClientsOnLobby.Clear();
            PlayersOnLobby.Clear();
            GridController.ClearGrid();
            CurrentLobby = null;
            Events.Fire_LeaveLobbyt();
            Callback?.Invoke(true);
        }
        Visiting = false;
    }

    public void SwitchLobby(int lobbyID)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Entering a new world...");
        LeaveLobby((success) =>
        {
            if (success)
                JoinLobby(lobbyID, (success) =>
                {
                    if (!success)
                        DodgeFromLight.UI_Notifications.Notify("Fail enter lobby");
                });
            else
                DodgeFromLight.UI_Notifications.Notify("Fail leave current lobby");
        });
    }
    #endregion

    public void ShowListOfLobbies()
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Getting Lobbies...");
        DFLClient.GiveMeLobbiesList((lobbies) =>
        {
            ListOfLobbiesPanel.SetActive(true);
            foreach (Transform t in LobbiesContainer)
                Destroy(t.gameObject);

            if (CurrentLobby.HasValue && CurrentLobby.Value != -DFLClient.CurrentUser.ID)
            {
                GameObject line = Instantiate(LobbyItemPrefab);
                line.GetComponentInChildren<TextMeshProUGUI>().text = "Back to HUB";
                line.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    SwitchLobby(-DFLClient.CurrentUser.ID);
                    ListOfLobbiesPanel.SetActive(false);
                });
                line.transform.SetParent(LobbiesContainer, false);
            }

            foreach (var lobby in lobbies)
            {
                GameObject line = Instantiate(LobbyItemPrefab);
                line.GetComponentInChildren<TextMeshProUGUI>().text = lobby.Name + " - (" + lobby.NbClients + " / " + lobby.MaxClients + ")";
                int id = lobby.ID;
                line.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    SwitchLobby(id);
                    ListOfLobbiesPanel.SetActive(false);
                });
                line.transform.SetParent(LobbiesContainer, false);
            }

            DodgeFromLight.UI_WorkerNotifier.Hide();
        });
    }

    #region toClient TCP methods
    public void RefreshSave()
    {
        PlayerSave save = null;
        DFLClient.CurrentMessage.RestartRead();
        DFLClient.CurrentMessage.GetObject<PlayerSave>(ref save);
        GameObject vfx = Instantiate(RefreshSaveVFX);
        Vector3 pos = CurrentController().OrnamentContainer.position;
        vfx.transform.position = pos;
    }

    public void ClientEnterLobby()
    {
        LitePlayerSave client = null;
        DFLClient.CurrentMessage.GetObject<LitePlayerSave>(ref client);
        SpawnClient(client);
        if (ClientsOnLobby.Count == 2) // send my psition if any other player comme for the first time (after that, server will know it)
            ChangeCell(CurrentController().CurrentCell.GetCellPos());
        UI_LobbyInfos.Instance.ClientEnterLobby();
    }

    public void ClientLeaveLobby()
    {
        int clientID = 0;
        DFLClient.CurrentMessage.Get(ref clientID);
        if (!ClientsOnLobby.ContainsKey(clientID))
            return;

        string playerName = ClientsOnLobby[clientID].Name;
        UnspawnClient(clientID, true);
        // Owner of the current hub just leave the hub, let's get back into my own hub
        if (-clientID == CurrentLobby)
        {
            SwitchLobby(-DFLClient.CurrentUser.ID);
            DodgeFromLight.UI_Notifications.Notify(playerName + " just leave his Hub.");
        }
        UI_LobbyInfos.Instance.ClientLeaveLobby();
    }

    public void ClientChangeCell()
    {
        int clientID = 0;
        short posX = 0, posY = 0;
        DFLClient.CurrentMessage.Get(ref clientID);
        DFLClient.CurrentMessage.Get(ref posX);
        DFLClient.CurrentMessage.Get(ref posY);
        CellPos pos = new CellPos(posX, posY);
        if (PlayersOnLobby.ContainsKey(clientID))
            PlayersOnLobby[clientID].WalkToCell(pos);
    }

    public void ClientEquipSkin()
    {
        int clientID = 0;
        DFLClient.CurrentMessage.Get(ref clientID);
        byte skinType = 0;
        DFLClient.CurrentMessage.Get(ref skinType);
        short SkinID = 0;
        DFLClient.CurrentMessage.Get(ref SkinID);

        // it's me
        if (clientID == DFLClient.CurrentUser.ID)
        {
            SaveManager.CurrentSave.SetCurrentPart((SkinType)skinType, SkinID);
            Events.Fire_SaveUpdated();
        }
        if (ClientsOnLobby.ContainsKey(clientID))
        {
            ClientsOnLobby[clientID].SetCurrentPart((SkinType)skinType, SkinID);
            PlayerCharacter player = PlayersOnLobby[clientID].Character;
            player.CurrentPlayerSave.SetCurrentPart((SkinType)skinType, SkinID);

            if ((SkinType)skinType == SkinType.Ornament)
                PlayersOnLobby[clientID].RefreshOrnament(ClientsOnLobby[clientID]);
            else
                player.SetPartFromSave((SkinType)skinType);
        }
    }

    public void PlayAnimation(string animationName)
    {
        DFLClient.PlayAnnimation(animationName);
    }

    public void ClientPlayAnimation()
    {
        int clientID = 0;
        string animationName = "";
        DFLClient.CurrentMessage.Get(ref clientID);
        DFLClient.CurrentMessage.Get(ref animationName);
        PlayersOnLobby[clientID].Play(animationName, smoothDuration:0.025f);
    }

    public void ClientSpeak(int clientID, string text)
    {
        if (PlayersOnLobby.ContainsKey(clientID))
            PlayersOnLobby[clientID].PlayerSpeak(text);
    }

    public void ClientEmote()
    {
        int clientID = 0;
        byte emoteType = 0;
        short emoteID = 0;
        DFLClient.CurrentMessage.Get(ref clientID);
        DFLClient.CurrentMessage.Get(ref emoteType);
        DFLClient.CurrentMessage.Get(ref emoteID);
        PlayersOnLobby[clientID].PlayerEmote((EmoteType)emoteType, emoteID);
    }

    public void ClientSetOrientation()
    {
        int clientID = 0;
        byte dir = 0;
        DFLClient.CurrentMessage.Get(ref clientID);
        DFLClient.CurrentMessage.Get(ref dir);
        if (PlayersOnLobby.ContainsKey(clientID))
            PlayersOnLobby[clientID].SetOrientation((Orientation)dir);
    }

    public void ForceEnterLobby()
    {
        int lobbyID = 0;
        DFLClient.CurrentMessage.Get(ref lobbyID);
        SwitchLobby(lobbyID);
    }

    public void AskEnterHub()
    {
        int senderID = 0;
        DFLClient.CurrentMessage.Get(ref senderID);
        string senderName = "";
        DFLClient.CurrentMessage.Get(ref senderName);
        DodgeFromLight.UI_Modal.SetButtonLeft("Refuse", () =>
        {
            DFLClient.AwnserEnterHub(senderID, false);
        })
            .SetButtonRight("Accept", () =>
            {
                DFLClient.AwnserEnterHub(senderID, true);
            })
            .SetTitle("authorization request")
            .Show(senderName + " would like to enter your hub with you.");
    }
    #endregion

    public void UnspawnClient(int clientID, bool playVFX)
    {
        if (playVFX)
        {
            GameObject vfx = Instantiate(UnspawnVFX);
            Vector3 pos = PlayersOnLobby[clientID].transform.position;
            pos.y = .25f;
            vfx.transform.position = pos;
        }
        PlayersOnLobby[clientID].Unspawn();
        PlayersOnLobby.Remove(clientID);
        ClientsOnLobby.Remove(clientID);
    }

    public void SpawnClient(LitePlayerSave client)
    {
        ClientsOnLobby.Add(client.ID, client);
        GameObject player = Instantiate(PlayerPrefab);
        LobbyPlayer_Controller controller = player.GetComponent<LobbyPlayer_Controller>();
        controller.Character.SetSave(client);
        controller.SpawnOnGrid(CurrentGrid, client, Camera);
        controller.PlaceOnCell(client.Pos);
        controller.SetOrientation(Orientation.Up);
        PlayersOnLobby.Add(client.ID, controller);

        // spawn vfx
        GameObject vfx = Instantiate(SpawnVFX);
        Vector3 pos = controller.transform.position;
        pos.y = 0.2f;
        vfx.transform.position = pos;
    }

    public LobbyPlayer_Controller CurrentController()
    {
        return PlayersOnLobby[DFLClient.CurrentUser.ID];
    }

    public LitePlayerSave CurrentLobbyClient()
    {
        return ClientsOnLobby[DFLClient.CurrentUser.ID];
    }

    private void ChangeCell(CellPos pos)
    {
        if (ClientsOnLobby.Count > 1)
            DFLClient.ChangeCell(pos);
    }
}

public enum PlayerEmplacement
{
    None = 0,
    InHub = 1,
    InLobby = 2
}