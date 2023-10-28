using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_MapStar : MonoBehaviour
{
    public Image ListBackImage;
    public GameObject MapStarsList;
    public UI_Resizable ListResizable;
    public UIMapInfo UIMapInfo;
    public Transform ListContainer;
    public GameObject MapStarLineItemPrefab;
    public Button DayMapStartButton;
    public Button WeekMapStartButton;
    public Button MonthMapStartButton;
    [HideInInspector]
    public int OpenedMapListIndex = -1;

    private void Start()
    {
        MapStarsList.SetActive(false);
        ListResizable.OnSmash += ListResizable_OnSmash;
    }

    private void ListResizable_OnSmash()
    {
        HideMapStarList();
    }

    public void HideMapStarList()
    {
        MapStarsList.SetActive(false);
        OpenedMapListIndex = -1;
    }

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
                        if (DodgeFromLight.GameManager != null)
                        {
                            Destroy(DodgeFromLight.GameManager.gameObject);
                            Destroy(DodgeFromLight.GameManager);
                            DodgeFromLight.GameManager = null;
                        }
                        LobbyManager.Instance.LeaveLobby();
                        DodgeFromLight.SceneTransitions.LoadScene("Main");
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

    public void GetMapStars()
    {
        // day map stars
        DFLClient.GetDayMapStar((res, maps) =>
        {
            if (res.Error)
                DodgeFromLight.UI_Notifications.Notify("can't get day map star");
            else
            {
                DayMapStartButton.interactable = true;
                DayMapStartButton.gameObject.GetComponent<UI_ButtonMapStar>().SetMaps(maps);
            }
        });
        // week map stars
        DFLClient.GetWeekMapStar((res, maps) =>
        {
            if (res.Error)
                DodgeFromLight.UI_Notifications.Notify("can't get week map star");
            else
            {
                WeekMapStartButton.interactable = true;
                WeekMapStartButton.gameObject.GetComponent<UI_ButtonMapStar>().SetMaps(maps);
            }
        });
        // month map stars
        DFLClient.GetMonthMapStar((res, maps) =>
        {
            if (res.Error)
                DodgeFromLight.UI_Notifications.Notify("can't get month map star");
            else
            {
                MonthMapStartButton.interactable = true;
                MonthMapStartButton.gameObject.GetComponent<UI_ButtonMapStar>().SetMaps(maps);
            }
        });
    }

    public void BindMaps(List<Map> Maps, VertexGradient gradient, ColorBlock btnColors)
    {
        foreach (Transform t in ListContainer)
            Destroy(t.gameObject);

        foreach (Map map in Maps)
        {
            GameObject item = Instantiate(MapStarLineItemPrefab);
            item.transform.SetParent(ListContainer, false);
            item.GetComponent<UI_MapStarItem>().SetMap(map, gradient);
            item.GetComponent<Button>().onClick.RemoveAllListeners();
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                UIMapInfo.SetMap(map);
            });
            item.GetComponent<UI_MapStarItem>().BtnPlay.colors = btnColors;
            item.GetComponent<Button>().colors = btnColors;
        }
    }
}