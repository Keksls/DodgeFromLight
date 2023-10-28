using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DFLCommonNetwork.GameEngine;

public class InGameUI : MonoBehaviour
{
    public GameObject PressEnterCheckpointPanel;
    public GameObject PressAEGearPanel;
    public TextMeshProUGUI TurnText;
    public TextMeshProUGUI DurationText;
    public Text GearAText;
    public Text GearEText;
    public TextMeshProUGUI GridNameText;
    public Image GearAImage;
    public Image GearEImage;
    public GameObject GearAImageEmpty;
    public GameObject GearEImageEmpty;
    public Color GearNormalColor;
    public Color GearPressedColor;
    public GameObject PauseMenu;
    public Toggle HideScoresForSession;

    public List<GameObject> InGameUIElements;

    public GameObject ScorePlayerPreview;
    public Camera ScoreCamera;
    public LobbyPlayer_Controller first;
    public LobbyPlayer_Controller second;
    public LobbyPlayer_Controller third;

    public TextMeshProUGUI ScoreTxtRun;
    public TextMeshProUGUI MapName;
    public TextMeshProUGUI Author;
    public TextMeshProUGUI NbLikes;
    public TextMeshProUGUI NbDislikes;
    public Transform ScoreList;
    public GameObject ScoreItemPrefabFirst;
    public GameObject ScoreItemPrefabSecond;
    public GameObject ScoreItemPrefabThird;
    public GameObject ScoreItemPrefab;
    public GameObject LeaderBoard;
    public GameObject VotePanel;
    public GameObject NextMapButton;

    public float HideCursorDuration = 5f;
    public float MouseMovementThreshold = 0.1f;
    public bool AutoHideCursor = true;
    private float lastFrameTime = 0f;
    private Vector3 lastMousePosition;

    void Awake()
    {
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnEnd += Events_TurnEnd;
        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
        Events.SetGear -= Events_SetGear;
        Events.SetGear += Events_SetGear;
        PauseMenu.SetActive(false);
        RefreshGears();
    }

    private void OnEnable()
    {
        PressEnterCheckpointPanel.SetActive(false);
        PressAEGearPanel.SetActive(false);
        PressAEGearPanel.GetComponentInChildren<Text>().text = "Press <b>" +
          DodgeFromLight.GameSettingsManager.GetNiceKeyString(DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear1).Key) + "</b> or " +
            "<b>" + DodgeFromLight.GameSettingsManager.GetNiceKeyString(DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear2).Key) + "</b> to equip";
        PressEnterCheckpointPanel.GetComponentInChildren<Text>().text = "Press <b>" + DodgeFromLight.GameSettingsManager.GetNiceKeyString(DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.TakeCheckPoint).Key) + "</b> to save Checkpoint";
        GearAText.text = DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear1).Key.ToString();
        GearEText.text = DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear2).Key.ToString();
    }

    private void Events_SetGear(GearType type)
    {
        RefreshGears();
    }

    private void Events_StartMap()
    {
        RefreshGears();
        GridNameText.text = Path.GetFileNameWithoutExtension(DodgeFromLight.CurrentMap.Map.Name);
        TurnText.text = "Turn : " + DodgeFromLight.GameManager.NbTurn;
    }

    private void OnDestroy()
    {
        Events.TurnEnd -= Events_TurnEnd;
        Events.StartMap -= Events_StartMap;
        Events.SetGear -= Events_SetGear;
    }

    private void Events_TurnEnd()
    {
        TurnText.text = "Turn : " + DodgeFromLight.GameManager.NbTurn;
    }

    public void ShowUI()
    {
        foreach (var go in InGameUIElements)
            gameObject.SetActive(true);
    }

    public void HideUI()
    {
        foreach (var go in InGameUIElements)
            gameObject.SetActive(false);
    }

    public void Like()
    {
        Vote(VoteType.Like);
    }

    public void Dislike()
    {
        Vote(VoteType.Dislike);
    }

    public void SuperLike()
    {
        Vote(VoteType.GoldenLike);
    }

    public void HideScore()
    {
        ScorePlayerPreview.SetActive(false);
        first.gameObject.SetActive(false);
        second.gameObject.SetActive(false);
        third.gameObject.SetActive(false);
        LeaderBoard.SetActive(false);
    }

    public void ShowScores(List<Score> scores, bool alreadyVoted)
    {
        LeaderBoard.SetActive(true);
        // clear leaderboard
        foreach (Transform t in ScoreList)
            Destroy(t.gameObject);
        // add scores items
        int i = 1;
        foreach (Score score in scores)
        {
            GameObject line = null;
            if (i == 1)
                line = Instantiate(ScoreItemPrefabFirst);
            else if (i == 2)
                line = Instantiate(ScoreItemPrefabSecond);
            else if (i == 3)
                line = Instantiate(ScoreItemPrefabThird);
            else
                line = Instantiate(ScoreItemPrefab);
            line.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "# " + i;
            line.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = score.UserName;
            line.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = TimeSpan.FromMilliseconds(score.Time).ToString().Replace("00:", "").Trim('0');
            line.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = score.Turns.ToString();
            line.transform.SetParent(ScoreList, false);
            i++;
        }

        first.gameObject.SetActive(false);
        second.gameObject.SetActive(false);
        third.gameObject.SetActive(false);
        if (scores.Count > 0)
        {
            DodgeFromLight.EnvironmentController.SetEnvironment(DodgeFromLight.EnvironmentController.GetBuildinEnvironment(BuildInEnvironments.Default));
            ScorePlayerPreview.SetActive(true);
            // draw first
            first.gameObject.SetActive(false);
            DFLClient.GetLiteHero(scores[0].UserID, (success, hero, errorMessage) =>
            {
                if (success)
                {
                    first.gameObject.SetActive(true);
                    first.Character.SetSave(hero);
                    first.RefreshOrnament(hero);
                    first.StartForceOrnamentEnabled();
                    first.Character.SetLayerOnPart(SkinType.Pet, LayerMask.NameToLayer("ScoreLayer"));
                    first.gameObject.SetLayer(LayerMask.NameToLayer("ScoreLayer"));
                    first.transform.LookAtY(ScoreCamera.transform);
                    first.OrnamentContainer.forward = -ScoreCamera.transform.forward;
                    first.Animator.Play("Win");
                }
            });
            if (scores.Count > 1)
            {
                second.gameObject.SetActive(false);
                DFLClient.GetLiteHero(scores[1].UserID, (success, hero, errorMessage) =>
                {
                    if (success)
                    {
                        second.gameObject.SetActive(true);
                        second.Character.SetSave(hero);
                        second.RefreshOrnament(hero);
                        second.StartForceOrnamentEnabled();
                        second.Character.SetLayerOnPart(SkinType.Pet, LayerMask.NameToLayer("ScoreLayer"));
                        second.gameObject.SetLayer(LayerMask.NameToLayer("ScoreLayer"));
                        second.transform.LookAtY(ScoreCamera.transform);
                        second.OrnamentContainer.forward = -ScoreCamera.transform.forward;
                        second.Animator.Play("Win");
                    }
                });
            }
            if (scores.Count > 2)
            {
                third.gameObject.SetActive(false);
                DFLClient.GetLiteHero(scores[2].UserID, (success, hero, errorMessage) =>
                {
                    if (success)
                    {
                        third.gameObject.SetActive(true);
                        third.Character.SetSave(hero);
                        third.RefreshOrnament(hero);
                        third.StartForceOrnamentEnabled();
                        third.Character.SetLayerOnPart(SkinType.Pet, LayerMask.NameToLayer("ScoreLayer"));
                        third.gameObject.SetLayer(LayerMask.NameToLayer("ScoreLayer"));
                        third.transform.LookAtY(ScoreCamera.transform);
                        third.OrnamentContainer.forward = -ScoreCamera.transform.forward;
                        third.Animator.Play("Win");
                    }
                });
            }
        }

        if (alreadyVoted)
        {
            NextMapButton.SetActive(true);
            VotePanel.SetActive(false);
        }
        else
        {
            NextMapButton.SetActive(false);
            VotePanel.SetActive(true);
        }

        HideScoresForSession.isOn = DodgeFromLight.CurrentRules.DontShowScore;

        ScoreTxtRun.text = new TimeSpan(DodgeFromLight.GameManager.StopWatch.ElapsedTicks).ToString().Replace("00:", "") + " (" + DodgeFromLight.GameManager.NbTurn + ")";
        MapName.text = DodgeFromLight.CurrentMap.Map.Name;
        NbLikes.text = DodgeFromLight.CurrentMap.Map.Likes.ToString();
        NbDislikes.text = DodgeFromLight.CurrentMap.Map.Dislikes.ToString();
        Author.text = "Map created by <b>" + DodgeFromLight.CurrentMap.Map.Author + "</b>";
    }

    public void NextMap()
    {
        if (DodgeFromLight.CurrentRules.Tutorial)
            return;
        ClosePauseMenu();
        HideScore();
        DodgeFromLight.GameManager.NextMap();
    }

    private void Vote(VoteType type)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Voting...");
        DFLClient.Vote(DodgeFromLight.CurrentRules.DiscoveryMode ? DodgeFromLight.CurrentRules.DiscoveryMapID : DodgeFromLight.CurrentRules.GetCurrentGridID(), DFLClient.CurrentUser.ID, type, (success) =>
        {
            if (!success)
                DodgeFromLight.UI_Notifications.Notify("Vote failed.");
            DodgeFromLight.UI_WorkerNotifier.Hide();
            NextMap();
        });
    }

    private void Update()
    {
        if (DodgeFromLight.GameManager != null)
            DurationText.text = DodgeFromLight.GameManager.StopWatch.Elapsed.ToString(@"m\:ss\.ff");

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear1).IsDown())
        {
            GearAText.color = GearPressedColor;
            GearAImage.transform.localScale = Vector3.one * 1.25f;
        }
        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear1).IsUp())
        {
            GearAText.color = GearNormalColor;
            GearAImage.transform.localScale = Vector3.one;
        }

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear2).IsDown())
        {
            GearEText.color = GearPressedColor;
            GearEImage.transform.localScale = Vector3.one * 1.25f;
        }
        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear2).IsUp())
        {
            GearEText.color = GearNormalColor;
            GearEImage.transform.localScale = Vector3.one;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!DodgeFromLight.GameManager.Paused)
            {
                DodgeFromLight.GameManager.EnterPause();
                PauseMenu.SetActive(true);
                foreach (var btn in PauseMenu.GetComponentsInChildren<UI_PointerHoverAnimations>())
                    btn.PlayExitClip();
            }
            else
            {
                DodgeFromLight.GameManager.ExitPause();
                PauseMenu.SetActive(false);
            }
        }

#if !UNITY_EDITOR
        // auto hide cursor
        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Global.AutoHideCursor)
        {
            Vector3 mousePosition = Input.mousePosition;
            if (Vector3.Distance(mousePosition, lastMousePosition) > MouseMovementThreshold) // mouse move
            {
                if (!Cursor.visible)
                    Cursor.visible = true;
                lastFrameTime = Time.time;
            }
            else // mouse don't move
            {
                if (lastFrameTime + HideCursorDuration <= Time.time) // must hide the mouse cursor
                    if (Cursor.visible)
                        Cursor.visible = false;
            }
            lastMousePosition = mousePosition;
        }
#endif
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    public void RefreshGears()
    {
        if (DodgeFromLight.GameManager == null || DodgeFromLight.GameManager._playerController == null ||
            DodgeFromLight.GameManager._playerController.GearA == null)
        {
            GearAImage.enabled = false;
            GearAImageEmpty.SetActive(true);
        }
        else
        {
            GearAImage.enabled = true;
            GearAImageEmpty.SetActive(false);
            Texture2D tex = DodgeFromLight.Databases.GearData.GetGearData(DodgeFromLight.GameManager._playerController.GearA.Type).Image; ;
            GearAImage.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }

        if (DodgeFromLight.GameManager == null || DodgeFromLight.GameManager._playerController == null ||
            DodgeFromLight.GameManager._playerController.GearE == null)
        {
            GearEImage.enabled = false;
            GearEImageEmpty.SetActive(true);
        }
        else
        {
            GearEImage.enabled = true;
            GearEImageEmpty.SetActive(false);
            Texture2D tex = DodgeFromLight.Databases.GearData.GetGearData(DodgeFromLight.GameManager._playerController.GearE.Type).Image;
            GearEImage.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }

    public void chkHideScoreForSession()
    {
        DodgeFromLight.CurrentRules.DontShowScore = HideScoresForSession.isOn;
    }

    public void Exit()
    {
        PoolManager.Instance.PushBackAllPool(PoolName.POCell);
        DodgeFromLight.GameManager.ExitPause();
        PauseMenu.SetActive(false);
        if (PlayerPrefs.HasKey("ForceGrid"))
        {
            StartCoroutine(Exit_routine("MapCreator"));
        }
        else
        {
            PlayerPrefs.DeleteKey("ForceGrid");
            StartCoroutine(Exit_routine((DodgeFromLight.CurrentRules.Tutorial && !DodgeFromLight.TutorialController.HasFinishTutorial()) ? "MainMenu" : "Lobby"));
        }
    }

    IEnumerator Exit_routine(string sceneName)
    {
        yield return null;
        Destroy(DodgeFromLight.GameManager.gameObject);
        Destroy(DodgeFromLight.GameManager);
        DodgeFromLight.GameManager = null;
        DodgeFromLight.SceneTransitions.LoadScene(sceneName);
    }

    public void RestartMap()
    {
        DodgeFromLight.GameManager.ExitPause();
        PauseMenu.SetActive(false);
        if (DodgeFromLight.WaitForEndOfAction > 0)
            return;
        DodgeFromLight.GameManager.RestartGrid();
    }

    public void ClosePauseMenu()
    {
        DodgeFromLight.GameManager.ExitPause();
        PauseMenu.SetActive(false);
    }
}