using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GamePreparationUI : MonoBehaviour
{
    #region Variables
    // maps vars
    // avancement vars
    public GameObject LevelPanel;
    public Text CurrentLevelText;
    public Text NextLevelText;
    public Image NextLevelShadow;
    public Image CurrentLevelShadow;
    public Image SliderImage;
    public Slider XPSlider;
    public Button BtnPendingRewards;


    public Transform LocalMapsContainer;
    public GameObject MapItemPrefab;
    public Color SelectLocalMapColor;
    public Color UnselectLocalMapColor;
    public InputField SearchLocalMapField;
    #endregion

    #region Map Start
    #endregion

    #region Initialization
    private void Start()
    {
        //    LevelPanel.SetActive(false);
        //    DodgeFromLight.Databases.RewardData.Initialize();
        //    BindLocalMaps("", true);
        //    DodgeFromLight.EnvironmentController.SetEnvironment(DodgeFromLight.EnvironmentController.GetBuildinEnvironment(BuildInEnvironments.Default));
        //    MapInfoPanel.OnDownloadMap = OnRemoteMapDownloaded;

        //    SearchLocalMapField.onValueChanged.AddListener((val) => { SearchLocalMap(); });
        //    SearchRemoteMapField.onSubmit.AddListener((val) => { SearchRemoteMap(); SearchRemoteMapField.Select(); });

        //    LogginPanel.gameObject.SetActive(false);
        //    LogginPanel.OnConnect = (success) =>
        //    {
        //        LogginPanel.gameObject.SetActive(false);
        //        if (success)
        //        {
        //            foreach (var of in OnlineFeatures)
        //                of.SetActive(true);

        //            if (DFLClient.LoginState == LoginState.LoggedIn)
        //            {
        //                BindRemoteMaps("");
        //                GetMapStars();
        //            }
        //        }
        //    };

        //    if (DFLClient.OnlineState == OnlineState.NotChecked)
        //    {
        //        UI_WorkerNotifier.Show("Checking online status...");
        //        DFLClient.CheckIfOnline((status) =>
        //        {
        //            UI_WorkerNotifier.Hide();
        //            if (status == OnlineState.Online)
        //            {
        //                if (DFLClient.LoginState != LoginState.LoggedIn)
        //                {
        //                    LogginPanel.gameObject.SetActive(true);
        //                    LogginPanel.ShowConnectionPanel();
        //                }
        //                else
        //                {
        //                    foreach (var of in OnlineFeatures)
        //                        of.SetActive(true);
        //                    BindRemoteMaps("");
        //                    GetMapStars();
        //                }
        //            }
        //        });
        //    }
        //    else if (DFLClient.OnlineState == OnlineState.Online)
        //    {
        //        if (DFLClient.LoginState != LoginState.LoggedIn)
        //        {
        //            LogginPanel.gameObject.SetActive(true);
        //            LogginPanel.ShowConnectionPanel();
        //        }
        //        else
        //        {
        //            foreach (var of in OnlineFeatures)
        //                of.SetActive(true);
        //            BindRemoteMaps("");
        //            GetMapStars();
        //        }
        //    }
    }

    private void Events_SaveGetted()
    {
        LevelPanel.SetActive(true);
    }
    #endregion

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        //{
        //    if (SearchRemoteMapField.isFocused)
        //        SearchRemoteMap();
        //    else if (SearchLocalMapField.isFocused)
        //        SearchLocalMap();
        //}
    }

    #region Buttons
    public void BtnDiscovery()
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Getting Discovery Map...");
        DFLClient.GetDiscoveryMapID((res, id) =>
        {
            if (!res.Error)
            {
                DodgeFromLight.UI_WorkerNotifier.Show("Downloading Map...");
                DFLClient.DownloadMap(id, GridManager.Folder, (res) =>
                {
                    if (!res.Error)
                    {
                        DodgeFromLight.CurrentRules = new GameRules().Discovery(id);
                        DodgeFromLight.CurrentRules.DiscoveryMode = true;
                        Play();
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

    public void Play()
    {
        if (DodgeFromLight.GameManager != null)
        {
            Destroy(DodgeFromLight.GameManager.gameObject);
            Destroy(DodgeFromLight.GameManager);
            DodgeFromLight.GameManager = null;
        }
        DodgeFromLight.SceneTransitions.LoadScene("Main");
    }

    public void Exit()
    {
        PlayerPrefs.DeleteKey("ForceGrid");
        StartCoroutine(Exit_routine("MainMenu"));
    }

    IEnumerator Exit_routine(string sceneName)
    {
        yield return null;
        DodgeFromLight.SceneTransitions.LoadScene(sceneName);
    }
    #endregion

    //#region Local Maps
    //public void BindLocalMaps(string kw, bool reloadGrids)
    //{
    //    // clear container
    //    for (int i = 0; i < LocalMapsContainer.childCount; i++)
    //        Destroy(LocalMapsContainer.GetChild(i).gameObject);

    //    if (reloadGrids)
    //    {
    //        UI_WorkerNotifier.Show("Loading maps ...");
    //        GridManager.LoadGrids(() =>
    //        {
    //            // remove local selection if no present on local maps list
    //            List<string> selected = GetLocalSelectedMaps();
    //            foreach (string selectedMap in selected)
    //            {
    //                if (!GridManager.HasLocalMap(selectedMap))
    //                    RemoveLocalMapFromSelection(selectedMap);
    //            }

    //            // Add In Work Maps
    //            int nbItems = AddLocalMaps(GridManager.GetAllMaps(), kw);
    //            RectTransform rect = LocalMapsContainer.GetComponent<RectTransform>();
    //            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(620f, nbItems * 160f / 4f));

    //            UI_WorkerNotifier.Hide();
    //        }, (map, loaded, loading) =>
    //        {
    //            UI_WorkerNotifier.Show("Loading maps : " + map.Name + " (" + loaded + "/" + loading + ")");
    //        });
    //    }
    //    else
    //    {
    //        // Add In Work Maps
    //        int nbItems = AddLocalMaps(GridManager.GetAllMaps(), kw);
    //        RectTransform rect = LocalMapsContainer.GetComponent<RectTransform>();
    //        rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(LocalMapsContainer.transform.parent.GetComponent<RectTransform>().sizeDelta.y, nbItems * 164f / 3f));
    //    }
    //}

    //int nbAdded = 0;
    //private int AddLocalMaps(Dictionary<string, Map> Maps, string filter)
    //{
    //    filter = filter.ToLower();
    //    nbAdded = 0;
    //    var selected = new HashSet<string>(GetLocalSelectedMaps());
    //    if (selected.Count > 0)
    //        LocalMapSeparator("Selected Maps");
    //    // bind selected
    //    foreach (var map in Maps)
    //    {
    //        if (!map.Value.Name.ToLower().Contains(filter) || !selected.Contains(map.Key))
    //            continue;
    //        AddLocalMapItem(map, true);
    //        nbAdded++;
    //    }
    //    if (selected.Count > 0)
    //    {
    //        EndLocalMapLine(4 - (nbAdded % 4));
    //        LocalMapSeparator("Local Maps");
    //    }
    //    // bind unselected
    //    foreach (var map in Maps)
    //    {
    //        if (!map.Value.Name.ToLower().Contains(filter) || selected.Contains(map.Key))
    //            continue;
    //        AddLocalMapItem(map);
    //        nbAdded++;
    //    }
    //    return nbAdded;
    //}

    //void EndLocalMapLine(int items)
    //{
    //    if (items < 4)
    //    {
    //        for (int i = 0; i < items; i++)
    //        {
    //            GameObject item = Instantiate(EmptyItemPrefab);
    //            item.transform.SetParent(LocalMapsContainer, false);
    //            nbAdded++;
    //        }
    //    }
    //}

    //void LocalMapSeparator(string title)
    //{
    //    for (int i = 0; i < 1; i++)
    //    {
    //        GameObject item = Instantiate(EmptyItemPrefab);
    //        item.transform.SetParent(LocalMapsContainer, false);
    //    }

    //    GameObject sep = Instantiate(SeparatorPrefab);
    //    sep.transform.SetParent(LocalMapsContainer, false);
    //    sep.GetComponentInChildren<Text>().text = title;

    //    for (int i = 0; i < 2; i++)
    //    {
    //        GameObject item = Instantiate(EmptyItemPrefab);
    //        item.transform.SetParent(LocalMapsContainer, false);
    //    }

    //    nbAdded += 4;
    //}

    //private void AddLocalMapItem(KeyValuePair<string, Map> map, bool selected = false)
    //{
    //    GameObject item = Instantiate(MapItemPrefab);
    //    item.transform.SetParent(LocalMapsContainer);
    //    item.GetComponentInChildren<Text>().text = map.Value.Name;
    //    string prevPath = GridManager.GetFolderPath(map.Key) + @"\preview.png";
    //    if (File.Exists(prevPath))
    //    {
    //        byte[] bytes = File.ReadAllBytes(prevPath);
    //        Texture2D tex = new Texture2D(2, 2);
    //        tex.LoadImage(bytes);
    //        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
    //        item.transform.GetChild(0).GetComponent<Image>().sprite = s;
    //    }
    //    item.transform.localScale = Vector3.one;
    //    item.GetComponent<Button>().onClick.AddListener(() =>
    //    {
    //        MapInfoPanel.SetMap(map.Value, false, true);
    //    });

    //    //checkBox select
    //    Toggle chk = item.transform.GetChild(3).GetComponent<Toggle>();
    //    chk.isOn = IsLocalMapSelected(map.Key);
    //    chk.onValueChanged.AddListener((val) =>
    //    {
    //        // add it to the gamerule list
    //        if (IsLocalMapSelected(map.Key))
    //            RemoveLocalMapFromSelection(map.Key);
    //        else
    //            AddLocalMapToSelection(map.Key);
    //        BindLocalMaps("", false);
    //    });

    //    // btn delete
    //    Button btnDelete = item.transform.GetChild(2).GetComponent<Button>();
    //    btnDelete.onClick.RemoveAllListeners();
    //    btnDelete.onClick.AddListener(() =>
    //    {
    //        UI_Modal.Show("Are you sure you want to delete this map ? After its deletion you will no longer be able to play with it. However you can always download it again in the 'download' tab.",
    //            "Delete", () =>
    //            {
    //                GridManager.DeleteGrid(map.Key);
    //                BindLocalMaps("", true);
    //            }, "Cancel", UI_Modal.Hide);
    //    });
    //    UIMouseHoverDetector hoverDetector = item.AddComponent<UIMouseHoverDetector>()
    //        .OnMouseEnter(() => { btnDelete.gameObject.SetActive(true); chk.gameObject.SetActive(true); })
    //        .OnMouseExit(() => { btnDelete.gameObject.SetActive(false); chk.gameObject.SetActive(false); });
    //    hoverDetector.ForceMouseExit();

    //    if (selected)
    //    {
    //        var cb = item.GetComponent<Button>().colors;
    //        cb.normalColor = SelectedItemColor;
    //        item.GetComponent<Button>().colors = cb;
    //    }
    //}

    //public void PlayLocalMap()
    //{
    //    List<string> maps = GetLocalSelectedMaps();
    //    if (maps.Count > 0)
    //    {
    //        DodgeFromLight.CurrentRules = new GameRules(true, false, maps);
    //        Play();
    //    }
    //    else
    //    {
    //        UI_Notifications.Notify("You must select at least one map");
    //    }
    //}

    //public void SearchLocalMap()
    //{
    //    string kw = SearchLocalMapField.text;
    //    BindLocalMaps(kw, false);
    //}

    //private void AddLocalMapToSelection(string gridID)
    //{
    //    List<string> maps = GetLocalSelectedMaps();
    //    if (!maps.Contains(gridID))
    //        maps.Add(gridID);
    //    PlayerPrefs.SetString("localMapsSelected", JsonConvert.SerializeObject(maps));
    //}

    //private void RemoveLocalMapFromSelection(string gridID)
    //{
    //    List<string> maps = GetLocalSelectedMaps();
    //    if (maps.Contains(gridID))
    //        maps.Remove(gridID);
    //    PlayerPrefs.SetString("localMapsSelected", JsonConvert.SerializeObject(maps));
    //}

    //private bool IsLocalMapSelected(string gridID)
    //{
    //    List<string> maps = GetLocalSelectedMaps();
    //    return maps.Contains(gridID);
    //}

    //private List<string> GetLocalSelectedMaps()
    //{
    //    List<string> maps = new List<string>();
    //    if (PlayerPrefs.HasKey("localMapsSelected"))
    //        maps = JsonConvert.DeserializeObject<List<string>>(PlayerPrefs.GetString("localMapsSelected"));
    //    return maps;
    //}
    //#endregion

    //#region Remote Maps
    //public void SearchRemoteMap()
    //{
    //    string kw = SearchRemoteMapField.text;
    //    BindRemoteMaps(kw);
    //}

    //public void OnRemoteMapDownloaded(string gridID)
    //{
    //    AddLocalMapItem(new KeyValuePair<string, Map>(gridID, GridManager.GetMap(gridID)));
    //}

    //public void BindRemoteMaps(string kw)
    //{
    //    UpdateLevelPanel();
    //    RemoteMapsWorker.Show("Getting Remote Maps");
    //    // clear container
    //    for (int i = 0; i < RemoteMapsContainer.childCount; i++)
    //        Destroy(RemoteMapsContainer.GetChild(i).gameObject);

    //    DFLClient.GetMapsList(kw, (res, Maps) =>
    //    {
    //        foreach (var map in Maps)
    //        {
    //            GameObject item = Instantiate(MapItemPrefab);
    //            item.transform.SetParent(RemoteMapsContainer, false);
    //            item.GetComponentInChildren<Text>().text = map.Name;
    //            // download Image Async
    //            DFLClient.DownloadMapPreview(map.ID, (r, data) =>
    //            {
    //                if (!r.Error && item)
    //                {
    //                    Texture2D tex = new Texture2D(2, 2);
    //                    if (tex.LoadImage(data))
    //                    {
    //                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
    //                        item.transform.GetChild(0).GetComponent<Image>().sprite = s;
    //                    }
    //                    else
    //                    {
    //                        // fail !
    //                    }
    //                }
    //            });
    //            item.transform.localScale = Vector3.one;
    //            item.GetComponent<Button>().onClick.AddListener(() =>
    //            {
    //                bool hasLocalMap = GridManager.HasLocalMap(map.ID);
    //                MapInfoPanel.SetMap(map, !hasLocalMap, false);
    //            });
    //        }
    //        RectTransform rect = RemoteMapsContainer.GetComponent<RectTransform>();
    //        rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(672f, (float)Maps.Count * 176f / 9f + 32f));
    //        RemoteMapsWorker.Hide();
    //    });
    //}

    //#endregion
}