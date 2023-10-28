using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ServerMap : MonoBehaviour
{
    public UIMapInfo UIMapInfo;
    public Transform RemoteMapsContainer;
    public UI_WorkerNotifier RemoteMapsWorker;
    public GameObject RemoteMapItemPrefab;
    public TMP_InputField SearchRemoteMapField;

    public void SearchRemoteMap()
    {
        string kw = SearchRemoteMapField.text;
        BindRemoteMaps(kw);
        SearchRemoteMapField.GetComponentInChildren<TMP_SelectionCaret>().raycastTarget = false;
    }

    public void BindRemoteMaps(string kw)
    {
        RemoteMapsWorker.Show("Getting Remote Maps");
        // clear container
        for (int i = 0; i < RemoteMapsContainer.childCount; i++)
            Destroy(RemoteMapsContainer.GetChild(i).gameObject);

        DFLClient.GetMapsList(kw, (res, Maps) =>
        {
            foreach (var map in Maps)
            {
                GameObject item = Instantiate(RemoteMapItemPrefab);
                item.transform.SetParent(RemoteMapsContainer, false);
                item.GetComponentInChildren<TextMeshProUGUI>().text = map.Name;
                // download Image Async
                DFLClient.DownloadMapPreview(map.ID, (r, data) =>
                {
                    if (!r.Error && item)
                    {
                        Texture2D tex = new Texture2D(2, 2);
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
                    UIMapInfo.SetMap(map);
                });
            }
            RectTransform rect = RemoteMapsContainer.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(672f, (float)Maps.Count * 176f / 9f + 32f));
            RemoteMapsWorker.Hide();
        });
    }
}