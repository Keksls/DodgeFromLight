using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ButtonMapStar : MonoBehaviour
{
    public UI_MapStar UI_MapStar;
    public List<Map> Maps;
    public Image MapPreviewImage;
    public TextMeshProUGUI MapPreviewName;
    public float SwitchMapDuration = 5f;
    public int Index = 0;
    private Sprite[] Sprites;
    private int currentIndex;

    private void OnEnable()
    {
        if (Maps != null)
        {
            StopAnimation();
            StartCoroutine(AnimateRoutine());
        }
    }

    private void OnDisable()
    {
        if (Maps != null)
            StopAnimation();
    }

    public void SetMaps(List<Map> maps)
    {
        Maps = maps;
        if (maps == null)
            MapPreviewName.text = "wait for new maps.";
        else
        {
            Sprites = new Sprite[maps.Count];
            if (gameObject.activeInHierarchy)
            {
                StopAnimation();
                StartCoroutine(AnimateRoutine());
            }
            GetComponent<Button>().onClick.RemoveAllListeners();
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (UI_MapStar.OpenedMapListIndex == Index)
                {
                    UI_MapStar.MapStarsList.gameObject.SetActive(false);
                    UI_MapStar.OpenedMapListIndex = -1;
                }
                else
                {
                    UI_MapStar.MapStarsList.gameObject.SetActive(true);
                    UI_MapStar.OpenedMapListIndex = Index;
                    UI_MapStar.BindMaps(Maps, MapPreviewName.colorGradient, GetComponent<Button>().colors);
                    UI_MapStar.ListBackImage.color = GetComponent<Button>().colors.highlightedColor;
                }
            });
        }
    }

    public void StopAnimation()
    {
        StopAllCoroutines();
    }

    IEnumerator AnimateRoutine()
    {
        while (true)
        {
            DrawMap(currentIndex);
            yield return new WaitForSeconds(SwitchMapDuration);
            NextIndex();
        }
    }

    private void DrawMap(int index)
    {
        MapPreviewName.text = Maps[index].Name;
        if (Sprites[index] != null)
            MapPreviewImage.sprite = Sprites[index];
        else
        {
            DFLClient.DownloadMapPreview(Maps[index].ID, (res, data) =>
            {
                if (!res.Error)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(data))
                    {
                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                        MapPreviewImage.sprite = s;
                        Sprites[index] = s;
                    }
                    else
                    {
                        MapPreviewImage.sprite = null;
                    }
                }
                else
                    MapPreviewImage.sprite = null;
            });
        }
    }

    private void NextIndex()
    {
        currentIndex++;
        if (currentIndex >= Maps.Count)
            currentIndex = 0;
    }
}