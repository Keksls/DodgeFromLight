using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public List<GameObject> InGameUIElements;

    public float HideCursorDuration = 5f;
    public float MouseMovementThreshold = 0.1f;

#if !UNITY_EDITOR
    private float lastMouseMovementTime;
    private Vector3 lastMousePosition;
#endif

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
          GameContext.GameSettingsManager.GetNiceKeyString(GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear1).Key) + "</b> or " +
            "<b>" + GameContext.GameSettingsManager.GetNiceKeyString(GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear2).Key) + "</b> to equip";
        PressEnterCheckpointPanel.GetComponentInChildren<Text>().text = "Press <b>" + GameContext.GameSettingsManager.GetNiceKeyString(GameContext.GameSettingsManager.GetInput(InputSettingsType.TakeCheckPoint).Key) + "</b> to save Checkpoint";
        GearAText.text = GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear1).Key.ToString();
        GearEText.text = GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear2).Key.ToString();
    }

    private void Events_SetGear(GearType type)
    {
        RefreshGears();
    }

    private void Events_StartMap()
    {
        RefreshGears();
        GridNameText.text = Path.GetFileNameWithoutExtension(GameContext.CurrentMap.Map.Name);
        TurnText.text = "Turn : " + GameContext.GameManager.NbTurn;
    }

    private void OnDestroy()
    {
        Events.TurnEnd -= Events_TurnEnd;
        Events.StartMap -= Events_StartMap;
        Events.SetGear -= Events_SetGear;
    }

    private void Events_TurnEnd()
    {
        TurnText.text = "Turn : " + GameContext.GameManager.NbTurn;
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

    /// <summary>
    /// Advances to the next local map.
    /// </summary>
    public void NextMap()
    {
        // Continue the active local ruleset without leaderboard or voting gates.
        if (GameContext.CurrentRules.Tutorial)
            return;
        ClosePauseMenu();
        GameContext.GameManager.NextMap();
    }

    /// <summary>
    /// Refreshes the run HUD, pause input, gear feedback and runtime cursor visibility.
    /// </summary>
    private void Update()
    {
        // Keep the displayed run state synchronized with the local game manager.
        if (GameContext.GameManager != null)
            DurationText.text = GameContext.GameManager.StopWatch.Elapsed.ToString(@"m\:ss\.ff");

        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear1).IsDown())
        {
            GearAText.color = GearPressedColor;
            GearAImage.transform.localScale = Vector3.one * 1.25f;
        }
        else if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear1).IsUp())
        {
            GearAText.color = GearNormalColor;
            GearAImage.transform.localScale = Vector3.one;
        }

        if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear2).IsDown())
        {
            GearEText.color = GearPressedColor;
            GearEImage.transform.localScale = Vector3.one * 1.25f;
        }
        else if (GameContext.GameSettingsManager.GetInput(InputSettingsType.Gear2).IsUp())
        {
            GearEText.color = GearNormalColor;
            GearEImage.transform.localScale = Vector3.one;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!GameContext.GameManager.Paused)
            {
                GameContext.GameManager.EnterPause();
                PauseMenu.SetActive(true);
                foreach (var btn in PauseMenu.GetComponentsInChildren<UI_PointerHoverAnimations>())
                    btn.PlayExitClip();
            }
            else
            {
                GameContext.GameManager.ExitPause();
                PauseMenu.SetActive(false);
            }
        }

#if !UNITY_EDITOR
        // Hide the cursor only in player builds after the configured inactivity delay.
        if (GameContext.GameSettingsManager.CurrentSettings.Global.AutoHideCursor)
        {
            Vector3 mousePosition = Input.mousePosition;
            if (Vector3.Distance(mousePosition, lastMousePosition) > MouseMovementThreshold) // mouse move
            {
                if (!Cursor.visible)
                    Cursor.visible = true;
                lastMouseMovementTime = Time.time;
            }
            else // mouse don't move
            {
                if (lastMouseMovementTime + HideCursorDuration <= Time.time) // must hide the mouse cursor
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
        if (GameContext.GameManager == null || GameContext.GameManager._playerController == null ||
            GameContext.GameManager._playerController.GearA == null)
        {
            GearAImage.enabled = false;
            GearAImageEmpty.SetActive(true);
        }
        else
        {
            GearAImage.enabled = true;
            GearAImageEmpty.SetActive(false);
            Texture2D tex = GameContext.Databases.GearData.GetGearData(GameContext.GameManager._playerController.GearA.Type).Image; ;
            GearAImage.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }

        if (GameContext.GameManager == null || GameContext.GameManager._playerController == null ||
            GameContext.GameManager._playerController.GearE == null)
        {
            GearEImage.enabled = false;
            GearEImageEmpty.SetActive(true);
        }
        else
        {
            GearEImage.enabled = true;
            GearEImageEmpty.SetActive(false);
            Texture2D tex = GameContext.Databases.GearData.GetGearData(GameContext.GameManager._playerController.GearE.Type).Image;
            GearEImage.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }

    /// <summary>
    /// Leaves the current local play session.
    /// </summary>
    public void Exit()
    {
        // Return playtests to the editor and normal sessions to the main menu.
        PoolManager.Instance.PushBackAllPool(PoolName.POCell);
        GameContext.GameManager.ExitPause();
        PauseMenu.SetActive(false);
        if (PlayerPrefs.HasKey("ForceGrid"))
        {
            StartCoroutine(Exit_routine("MapCreator"));
        }
        else
        {
            PlayerPrefs.DeleteKey("ForceGrid");
            StartCoroutine(Exit_routine("MainMenu"));
        }
    }

    /// <summary>
    /// Releases the active gameplay manager before loading the requested local scene.
    /// </summary>
    private IEnumerator Exit_routine(string sceneName)
    {
        // Defer destruction by one frame so current UI callbacks can finish safely.
        yield return null;
        Destroy(GameContext.GameManager.gameObject);
        Destroy(GameContext.GameManager);
        GameContext.GameManager = null;
        GameContext.SceneTransitions.LoadScene(sceneName);
    }

    public void RestartMap()
    {
        GameContext.GameManager.ExitPause();
        PauseMenu.SetActive(false);
        if (GameContext.WaitForEndOfAction > 0)
            return;
        GameContext.GameManager.RestartGrid();
    }

    public void ClosePauseMenu()
    {
        GameContext.GameManager.ExitPause();
        PauseMenu.SetActive(false);
    }
}
