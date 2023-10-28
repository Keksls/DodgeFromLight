using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapsController : MonoBehaviour
{
    public UI_MainMenu UI_MainMenu;
    public TMP_InputField CodeField;
    public GameObject CodePanel;
    public GameObject SelectedMapPanel;
    public Button btnShare;
    public GameObject OnlinePanel;
    public Image Preview;
    public TextMeshProUGUI MapName;
    public TextMeshProUGUI MapAuthor;
    public TextMeshProUGUI MapUpdate;
    public TextMeshProUGUI Likes;
    public TextMeshProUGUI Dislikes;
    public TextMeshProUGUI Size;
    public Button btnEdit;
    public Button btnDelete;
    public Color SelectedMapColor;

    public GameObject MapItemPrefab;
    public GameObject CreateMapItemPrefab;
    public GameObject EditHubItemPrefab;
    public Transform MapItemsContainer;
    public GameObject MainContainer;
    public GameObject CreateMapPanel;
    public TMP_InputField InputMapName;
    public TMP_InputField InputMapWidth;
    public TMP_InputField InputMapHeight;
    private string currentMapCode = "";
    private Map selectedMap;

    private void Start()
    {
        HideCreateMap();
        UnselectMap();
        BindMaps();

        CodeField.onSubmit.AddListener((val) => EnterCode());
        CodeField.GetComponentInChildren<TMP_SelectionCaret>().raycastTarget = false;
        CodeField.onValidateInput += delegate (string s, int i, char c) { return char.ToUpper(c); };
    }

    private void OnEnable()
    {
        HideCreateMap();
        UnselectMap();
        BindMaps();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O) && selectedMap != null && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift))
        {
            System.Diagnostics.Process.Start(GridManager.Folder);
        }

        if (Input.GetKeyDown(KeyCode.E) && selectedMap != null && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)) // save Env
        {
            EnvironmentSettings env = GridManager.GetGrid(selectedMap.ID).Environment;
            File.WriteAllText(Application.streamingAssetsPath + @"\Environments\" + selectedMap.Name + ".json", JsonConvert.SerializeObject(env));
        }

        // temps download map
        if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftControl) && selectedMap != null)
        {
            string path = Application.streamingAssetsPath + @"\" + selectedMap.Name;
            DFLClient.DownloadMap(selectedMap.ID, path, (res) =>
            {
                if (res.Error)
                {
                    DodgeFromLight.UI_Notifications.Notify("fail !");
                    Debug.Log(res.APIResponse);
                }
                else
                    DodgeFromLight.UI_Notifications.Notify("success");
            });
        }
    }

    #region map list
    bool isLoadingWorkingMaps = false;
    public void BindMaps()
    {
        if (DFLClient.LoginState != LoginState.LoggedIn || DFLClient.OnlineState != OnlineState.Online || isLoadingWorkingMaps)
            return;
        isLoadingWorkingMaps = true;
        for (int i = 0; i < MapItemsContainer.childCount; i++)
            Destroy(MapItemsContainer.GetChild(i).gameObject);

        // edit item hub
        GameObject editHub = Instantiate(EditHubItemPrefab);
        editHub.transform.SetParent(MapItemsContainer, false);
        editHub.transform.localScale = Vector3.one;
        editHub.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlayerPrefs.SetInt("EditingHub", 1);
            LobbyManager.Instance.LeaveLobby();
            DodgeFromLight.SceneTransitions.LoadScene("MapCreator");
        });

        // new map item
        GameObject newItem = Instantiate(CreateMapItemPrefab);
        newItem.transform.SetParent(MapItemsContainer, false);
        newItem.transform.localScale = Vector3.one;
        newItem.GetComponent<Button>().onClick.AddListener(() =>
        {
            ShowCreateMap();
        });

        DodgeFromLight.UI_WorkerNotifier.Show("Getting remote maps");
        DFLClient.GetWorkingMapsList(DFLClient.CurrentUser.ID, (res, Maps) =>
        {
            if (res.Error)
            {
                DodgeFromLight.UI_Notifications.Notify("Error getting maps");
                DodgeFromLight.UI_WorkerNotifier.Hide();
                return;
            }
            foreach (var map in Maps)
            {
                GameObject item = Instantiate(MapItemPrefab);
                item.transform.SetParent(MapItemsContainer, false);
                item.GetComponentInChildren<TextMeshProUGUI>().text = map.Name;
                // download Image Async
                DFLClient.DownloadMapPreview(map.ID, (r, data) =>
                {
                    if (!item)
                        return;
                    if (!r.Error)
                    {
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                        if (tex.LoadImage(data))
                        {
                            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                            item.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = s;
                        }
                        else
                        {
                            // fail !
                        }
                    }
                });
                item.transform.localScale = Vector3.one;
                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectMap(map, item.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite);
                });
            }
            RectTransform rect = MapItemsContainer.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(700f, MapItemsContainer.childCount * 160f / 3f + 16f));
            isLoadingWorkingMaps = false;
            DodgeFromLight.UI_WorkerNotifier.Hide();
        });
    }
    #endregion

    #region Create maps
    public void HideCreateMap()
    {
        CreateMapPanel.SetActive(false);
    }

    public void ShowCreateMap()
    {
        InputMapHeight.text = "";
        InputMapWidth.text = "";
        InputMapName.text = "";
        CreateMapPanel.SetActive(true);
        InputMapName.GetComponentInChildren<TMP_SelectionCaret>().raycastTarget = false;
        InputMapWidth.GetComponentInChildren<TMP_SelectionCaret>().raycastTarget = false;
        InputMapHeight.GetComponentInChildren<TMP_SelectionCaret>().raycastTarget = false;
    }

    public void CreateMap()
    {
        int width = int.Parse(InputMapWidth.text);
        int height = int.Parse(InputMapHeight.text);
        // check if map data are OK
        if (width > 0 && width <= 64 &&
            height > 0 && height <= 64 &&
            !string.IsNullOrEmpty(InputMapName.text))
        {
            CreateMap(InputMapName.text, width, height);
        }
        else
        {
            DodgeFromLight.UI_Notifications.Notify("Map size failed or Name already Exist");
        }
    }
    #endregion

    public void UnselectMap()
    {
        selectedMap = null;
        SelectedMapPanel.SetActive(false);
    }

    private void SelectMap(Map map, Sprite sprite)
    {
        SelectedMapPanel.SetActive(true);
        selectedMap = map;
        btnShare.gameObject.SetActive(false);
        CodePanel.SetActive(false);
        currentMapCode = string.Empty;
        MapName.text = map.Name;
        Size.text = map.Width + " x " + map.Height;
        MapUpdate.text = "Update : " + map.UploadDate.Replace("-", " / ");
        MapAuthor.text = "by <i>" + map.Author + "</i>";
        Likes.text = map.Likes.ToString();
        Dislikes.text = map.Dislikes.ToString();

        DFLClient.GetMapCode(map.ID, (res, code) =>
        {
            if (!res.Error)
            {
                CodePanel.SetActive(true);
                CodeField.text = code;
                currentMapCode = code;
            }
        });

        Preview.sprite = sprite;
        CodeField.text = currentMapCode;
    }

    public void EnterCode()
    {
        string code = CodeField.text.ToUpper();
        DodgeFromLight.UI_WorkerNotifier.Show("Getting map...");
        DFLClient.GetMapIDFromCode(code, (res, id) =>
        {
            if (!res.Error)
            {
                DFLClient.AddWorkingMap(id, DFLClient.CurrentUser.ID, (res) =>
                {
                    if (res.Error)
                        DodgeFromLight.UI_Notifications.Notify(res.APIResponse);
                    else
                    {
                        BindMaps();
                        DodgeFromLight.UI_Notifications.Notify("Map added !");
                    }
                    DodgeFromLight.UI_WorkerNotifier.Hide();
                });
            }
            else
            {
                DodgeFromLight.UI_Notifications.Notify(res.APIResponse);
                DodgeFromLight.UI_WorkerNotifier.Hide();
            }
        });
    }

    public void BtnEdit()
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Checking if map locked...");
        DFLClient.IsMapLocked(selectedMap.ID, (res, locked) =>
        {
            if (!locked) // map not locked, go work on it !
            {
                GridManager.CleanFolder();
                DFLClient.DownloadMap(selectedMap.ID, GridManager.Folder, (res) =>
                {
                    if (res.Error) // error DL map
                    {
                        DFLClient.StopWorkingOnMap(selectedMap.ID, (res, locked) => { });
                        DodgeFromLight.UI_Notifications.Notify("Error getting map");
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                    }
                    else // map downloaded
                    {
                        DFLClient.WorkOnMap(selectedMap.ID, (res, locked) =>
                        {
                            if (locked)
                            {
                                PlayerPrefs.SetInt("EditingHub", 0);
                                LobbyManager.Instance.LeaveLobby();
                                DodgeFromLight.SceneTransitions.LoadScene("MapCreator");
                                DodgeFromLight.UI_WorkerNotifier.Hide();
                            }
                            else
                            {
                                Debug.Log(res.ToString());
                                DodgeFromLight.UI_Notifications.Notify("can't lock map");
                                DodgeFromLight.UI_WorkerNotifier.Hide();
                            }
                        });
                    }
                });
            }
            else // map locked
            {
                DodgeFromLight.UI_Notifications.Notify("Someone is working on this map.");
                DodgeFromLight.UI_WorkerNotifier.Hide();
            }
        });
    }

    public void BtnPlay()
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Getting Map");
        DFLClient.DownloadMap(selectedMap.ID, GridManager.Folder, (res) =>
        {
            if (res.Error)
                DodgeFromLight.UI_Notifications.Notify("error getting map");
            else
            {
                DodgeFromLight.CurrentRules = new GameRules(true, false, selectedMap.ID);
                LoadScene("Main");
                UnselectMap();
            }
        });
        DodgeFromLight.UI_WorkerNotifier.Hide();
    }

    public void BtnDelete()
    {
        DodgeFromLight.UI_Modal.SetTitle("Are you shure you want to delete this map?").
            SetButtonLeft("Delete", () =>
            {
                GridManager.CleanFolder();
                if (selectedMap.AuthorID == DFLClient.CurrentUser.ID)
                {
                    DodgeFromLight.UI_WorkerNotifier.Show("Deleting remote map");
                    DFLClient.DeleteMap(selectedMap.ID, (res, ok) =>
                    {
                        BindMaps();
                        UnselectMap();
                        if (res.Error)
                            DodgeFromLight.UI_Notifications.Notify(res.APIResponse);
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                    });
                }
                else
                {
                    DodgeFromLight.UI_WorkerNotifier.Show("Deleting remote map");
                    DFLClient.DeleteWorkingMap(selectedMap.ID, DFLClient.CurrentUser.ID, (res) =>
                    {
                        BindMaps();
                        UnselectMap();
                        if (res.Error)
                            DodgeFromLight.UI_Notifications.Notify(res.APIResponse);
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                    });
                }
            })
            .SetButtonRight("Cancel", DodgeFromLight.UI_Modal.Hide)
            .Show("If this is an online map, only the creator can delete it from the server.\n If you are not the creator, the map will just be unlinked to your profile.");
    }

    public void CreateMap(string name, int width, int height)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Creating Map...");

        // create local map
        Map map = new Map(name, width, height);
        map.SetAuthor(DFLClient.CurrentUser);
        map.SetState(MapState.InWork);
        Grid grid = new Grid(width, height);
        grid.Environment = DodgeFromLight.EnvironmentController.GetBuildinEnvironment(BuildInEnvironments.Default);
        GridManager.SaveFullMap(new FullMap(grid, map), new byte[0]);

        // upload map
        DFLClient.UploadMap(map.ID, (res) =>
        {
            if (!res.Error)
            {
                DodgeFromLight.UI_WorkerNotifier.Show("Preparing Map...");
                DFLClient.AddWorkingMap(map.ID, DFLClient.CurrentUser.ID, (res) =>
                {
                    if (res.Error)
                    {
                        GridManager.CleanFolder();
                        BindMaps();
                        UnselectMap();
                        DodgeFromLight.UI_Notifications.Notify("Fail Adding map...");
                    }
                    else
                    {
                        PlayerPrefs.SetInt("EditingHub", 0);
                        LobbyManager.Instance.LeaveLobby();
                        DodgeFromLight.SceneTransitions.LoadScene("MapCreator");
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                    }
                });
            }
            else
            {
                DodgeFromLight.UI_Notifications.Notify("Upload Error.");
                Debug.Log(res.ToString());
            }
        });
    }

    public void BtnSubmit()
    {
        if (!selectedMap.IsFinished())
        {
            DodgeFromLight.UI_Modal.SetTitle("Please play map before sumbiting it")
                .Show("This map has been modified since last time you finish it, or has never been played.\nYou must play and finish the map at least one time before submit it.");
        }
        else
        {
            DodgeFromLight.UI_Modal.SetTitle("Are you sure you want to submit this map ?")
                .SetButtonLeft("Submit", () =>
                {
                    if (selectedMap.AuthorID == DFLClient.CurrentUser.ID)
                    {
                        DodgeFromLight.UI_WorkerNotifier.Show("Uploading Map...");

                        DFLClient.DownloadMap(selectedMap.ID, GridManager.Folder, (res) =>
                        {
                            if (res.Error) // can't get map
                            {
                                Debug.Log(res.ToString());
                                DodgeFromLight.UI_Notifications.Notify("Can't get map");
                                DodgeFromLight.UI_WorkerNotifier.Hide();
                            }
                            else // map downloaded
                            {
                                submitMap();
                            }
                        });
                    }
                    else
                    {
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                        DodgeFromLight.UI_Notifications.Notify("Only " + selectedMap.Author + " can submit this map.");
                    }
                })
            .SetButtonRight("Cancel", DodgeFromLight.UI_Modal.Hide)
                .Show("You are going to submit your map to discovery mode.\n It will appear for all players and will be voted on.\nIf it is validated, it can be downloaded by everyone, if not, it will come back in this interface so that you can modify it.\n\n WARNING, after submitting a map, you will no longer be able to modify it.");
        }
    }

    void submitMap()
    {
        DFLClient.UploadMap(selectedMap.ID, (res) =>
        {
            if (!res.Error)
            {
                DodgeFromLight.UI_Notifications.Notify("Map Uploaded.");
                DFLClient.UpdateState(selectedMap.ID, MapState.Locked, (r) =>
                {
                    DodgeFromLight.UI_WorkerNotifier.Hide();
                    if (!r.Error)
                    {
                        DodgeFromLight.UI_Notifications.Notify("Map Submited.");
                        UnselectMap();
                        BindMaps();
                    }
                    else
                    {
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                        DodgeFromLight.UI_Notifications.Notify("Upload Error.");
                        Debug.Log(r.ToString());
                    }
                    GridManager.CleanFolder();
                });
            }
            else
            {
                DodgeFromLight.UI_WorkerNotifier.Hide();
                DodgeFromLight.UI_Notifications.Notify("Upload Error.");
                Debug.Log(res.ToString());
            }
        });
    }

    public void BtnCopyCode()
    {
        if (CodeField.text != "no code")
            GUIUtility.systemCopyBuffer = CodeField.text;
    }

    public void LoadScene(string SceneName)
    {
        if (DodgeFromLight.GameManager != null)
        {
            Destroy(DodgeFromLight.GameManager.gameObject);
            Destroy(DodgeFromLight.GameManager);
            DodgeFromLight.GameManager = null;
        }
        LobbyManager.Instance.LeaveLobby();
        DodgeFromLight.SceneTransitions.LoadScene(SceneName);
    }

    internal enum LogginPanelState
    {
        Loggin,
        Register
    }
}