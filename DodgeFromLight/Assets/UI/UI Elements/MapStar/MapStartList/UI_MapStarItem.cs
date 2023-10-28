using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_MapStarItem : MonoBehaviour
{
    public Image PreviewImage;
    public TextMeshProUGUI MapName;
    public TextMeshProUGUI AuthorName;
    public Button BtnPlay;

    public void SetMap(Map map, VertexGradient gradient)
    {
        DFLClient.DownloadMapPreview(map.ID, (res, data) =>
        {
            if (!gameObject)
                return;
            if (!res.Error)
            {
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                    PreviewImage.sprite = s;
                }
                else
                {
                    PreviewImage.sprite = null;
                }
            }
            else
                PreviewImage.sprite = null;
        });

        MapName.text = map.Name;
        MapName.colorGradient = gradient;
        BtnPlay.GetComponentInChildren<TextMeshProUGUI>().colorGradient = gradient;
        AuthorName.text = "by " + map.Author;
        BtnPlay.onClick.AddListener(() =>
        {
            DodgeFromLight.CurrentRules = new GameRules(true, false, map.ID);
            if (DodgeFromLight.GameManager != null)
            {
                Destroy(DodgeFromLight.GameManager.gameObject);
                Destroy(DodgeFromLight.GameManager);
                DodgeFromLight.GameManager = null;
            }
            LobbyManager.Instance.LeaveLobby();
            DodgeFromLight.SceneTransitions.LoadScene("Main");
        });
    }
}