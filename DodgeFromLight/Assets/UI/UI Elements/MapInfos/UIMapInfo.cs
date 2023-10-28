using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMapInfo : MonoBehaviour
{
    public UI_WorkerNotifier UI_WorkerNotifier;
    public UI_Notifications UI_Notifications;
    public Transform ScoresContainer;
    public GameObject ScoreLinePrefab;
    public TextMeshProUGUI MapName;
    public TextMeshProUGUI AuthorName;
    public Image Preview;
    public TextMeshProUGUI Size;
    public TextMeshProUGUI Likes;
    public TextMeshProUGUI Dislikes;
    private Map map;

    public void SetMap(Map _map)
    {
        map = _map;
        gameObject.SetActive(true);
        MapName.text = map.Name;
        AuthorName.text = map.Author;
        Size.text = map.Width + " x " + map.Height;
        Likes.text = map.Likes.ToString();
        Dislikes.text = map.Dislikes.ToString();

        DFLClient.GetMap(map.ID, (res, m) =>
        {
            BindScoresAndPreview(_map);
        });
    }

    private void BindScoresAndPreview(Map _map)
    {
        DFLClient.DownloadMapPreview(map.ID, (res, data) =>
        {
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                Preview.sprite = s;
            }
        });

        DFLClient.GetScores(map.ID, 5, (success, scores) =>
        {
            if (success)
            {
                foreach (Transform t in ScoresContainer)
                    Destroy(t.gameObject);
                int rank = 1;
                foreach (var score in scores)
                {
                    GameObject line = Instantiate(ScoreLinePrefab);
                    line.transform.GetChild(1).GetComponent<Text>().text = "#" + rank.ToString();
                    line.transform.GetChild(2).GetComponent<Text>().text = score.UserName.ToString();
                    line.transform.GetChild(3).GetComponent<Text>().text = TimeSpan.FromMilliseconds(score.Time).ToString().Replace("00:", "").Trim('0');
                    line.transform.GetChild(4).GetComponent<Text>().text = score.Turns.ToString();
                    line.transform.SetParent(ScoresContainer, false);
                    rank++;
                }
            }
        });
    }

    public void PlayMap()
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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        DodgeFromLight.CursorManager.SetCursor(CursorType.Arrow);
    }
}
