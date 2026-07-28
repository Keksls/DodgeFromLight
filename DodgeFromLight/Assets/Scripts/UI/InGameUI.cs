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

    /// <summary>
    /// Advances to the next local map.
    /// </summary>
    public void NextMap()
    {
        // Continue the active local ruleset without leaderboard or voting gates.
        if (DodgeFromLight.CurrentRules.Tutorial)
            return;
        ClosePauseMenu();
        DodgeFromLight.GameManager.NextMap();
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

    /// <summary>
    /// Leaves the current local play session.
    /// </summary>
    public void Exit()
    {
        // Return playtests to the editor and normal sessions to the main menu.
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
