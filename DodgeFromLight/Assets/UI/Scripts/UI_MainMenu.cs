using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class UI_MainMenu : MonoBehaviour
{
    public Transform Container;
    public bool EscToOpen = true;
    public Button.ButtonClickedEvent OnClose;
    private bool opened = true;

    private void Awake()
    {
        Hide(false);
    }

    /// <summary>
    /// Toggles the local settings menu and captures input rebinding.
    /// </summary>
    private void Update()
    {
        // Chat focus no longer participates because the network console was removed.
        if (Input.GetKeyDown(KeyCode.Escape) && EscToOpen)
        {
            if (opened)
                Hide(true);
            else
                Show();
        }
        AssignateInput();
    }

    public void Show()
    {
        Container.gameObject.SetActive(true);
        BindGraphicsSettings();
        BindInputs();
        BindGlobalSettings();
        BindAudioSettings();
        opened = true;
    }

    public void Hide(bool fireOnCloseEvent)
    {
        Container.gameObject.SetActive(false);
        opened = false;
        if (fireOnCloseEvent)
            OnClose?.Invoke();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    #region Global
    public Toggle DisplayFPSToggle;
    public Toggle AutoHideCursorToggle;

    public void BindGlobalSettings()
    {
        if (GameContext.GameSettingsManager.CurrentSettings.Global == null)
            GameContext.GameSettingsManager.CreateDefaultGlobalSettings();
        DisplayFPSToggle.isOn = GameContext.GameSettingsManager.CurrentSettings.Global.DisplayFPS;
        AutoHideCursorToggle.isOn = GameContext.GameSettingsManager.CurrentSettings.Global.AutoHideCursor;
    }

    public void ClearCache()
    {
        GameContext.UI_Modal.SetTitle("you are going to clear the cache")
            .SetButtonLeft("Apply", () =>
            {
                GameContext.UI_WorkerNotifier.Show("Restarting...");
                int rt = 0, TutorialStepIndex = 0;
                if (PlayerPrefs.HasKey("TutorialRewardsTaken"))
                    rt = PlayerPrefs.GetInt("TutorialRewardsTaken");
                if (PlayerPrefs.HasKey("TutorialStepIndex"))
                    TutorialStepIndex = PlayerPrefs.GetInt("TutorialStepIndex");
                PlayerPrefs.DeleteAll();
                PlayerPrefs.SetInt("TutorialRewardsTaken", rt);
                PlayerPrefs.SetInt("TutorialStepIndex", TutorialStepIndex);
                GameContext.RestartGame();
                GameContext.UI_WorkerNotifier.Hide();
            })
        .Show("This action need to restart the game.");
    }

    public void RedoTutorial()
    {
        GameContext.UI_Modal.SetTitle("you are going to clear Tutorial avancement")
            .SetButtonLeft("Apply", () =>
            {
                GameContext.UI_WorkerNotifier.Show("Restarting...");
                PlayerPrefs.DeleteKey("TutorialStepIndex");
                PlayerPrefs.DeleteKey("TutorialRewardsTaken");
                GameContext.RestartGame();
                GameContext.UI_WorkerNotifier.Hide();
            })
        .Show("This action need to restart the game.");
    }

    public void ResetDefaultSettings()
    {
        GameContext.UI_Modal.SetTitle("you are going to restore default settings")
            .SetButtonLeft("Apply", () =>
            {
                GameContext.UI_WorkerNotifier.Show("Restarting...");
                GameContext.GameSettingsManager.CreateDefaultSettings();
                GameContext.RestartGame();
                GameContext.UI_WorkerNotifier.Hide();
            })
        .Show("This action need to restart the game.");
    }

    public void DisplayFPSToggleValueChange(bool val)
    {
        GameContext.GameSettingsManager.SetDisplayFPS(val, true);
    }

    public void AutoHideCursorToggleValueChange(bool val)
    {
        GameContext.GameSettingsManager.SetAutoHideCursor(val, true);
    }
    #endregion

    #region Audio
    public Slider AudioVolumeSlider;

    public void BindAudioSettings()
    {
        if (GameContext.GameSettingsManager.CurrentSettings.Audio == null)
            GameContext.GameSettingsManager.CreateDefaultAudioSettings();
        AudioVolumeSlider.value = GameContext.GameSettingsManager.CurrentSettings.Audio.Volume;
    }

    public void VolumeChange(float volume)
    {
        GameContext.GameSettingsManager.SetVolume(volume, true);
    }
    #endregion

    #region Graphics
    public Toggle FullscreenToggle;
    public Dropdown ResolutionDropdown;
    public Slider QualitySlider;
    public Dropdown AntialiasingDropdown;
    public Toggle PostprocessToggle;
    public Toggle VSyncToggle;
    public Slider TargetFPSSlider;
    public Text QualityText;
    public Text FPSText;
    public Toggle FPSToggle;

    private void BindGraphicsSettings()
    {
        if (GameContext.GameSettingsManager.CurrentSettings.Graphics == null)
            GameContext.GameSettingsManager.CreateDefaultGraphicSettings();

        FullscreenToggle.isOn = GameContext.GameSettingsManager.CurrentSettings.Graphics.Fullscreen;
        VSyncToggle.isOn = GameContext.GameSettingsManager.CurrentSettings.Graphics.VSync;
        PostprocessToggle.isOn = GameContext.GameSettingsManager.CurrentSettings.Graphics.PostProcess;
        QualitySlider.value = GameContext.GameSettingsManager.CurrentSettings.Graphics.Quality;
        FPSToggle.isOn = GameContext.GameSettingsManager.CurrentSettings.Graphics.TargetFPS > -1;
        TargetFPSSlider.gameObject.SetActive(FPSToggle.isOn);
        if (FPSToggle.isOn)
            TargetFPSSlider.value = GameContext.GameSettingsManager.CurrentSettings.Graphics.TargetFPS;

        // bind resolutions
        ResolutionDropdown.options.Clear();
        foreach (var res in Screen.resolutions)
            ResolutionDropdown.options.Add(new Dropdown.OptionData(res.width + " x " + res.height));
        ResolutionDropdown.value = GameContext.GameSettingsManager.CurrentSettings.Graphics.Resolution;

        // bind Antialiasing
        AntialiasingDropdown.options.Clear();
        foreach (AntialiasingMode res in Enum.GetValues(typeof(AntialiasingMode)))
            AntialiasingDropdown.options.Add(new Dropdown.OptionData(GetAAName(res)));
        AntialiasingDropdown.value = (int)GameContext.GameSettingsManager.CurrentSettings.Graphics.Antialiasing;
    }

    /// <summary>
    /// Returns the concise label displayed for an URP anti-aliasing mode.
    /// </summary>
    private string GetAAName(AntialiasingMode antialiasing)
    {
        // Keep the existing UI labels while using URP's native enum.
        switch (antialiasing)
        {
            case AntialiasingMode.None:
                return "None";
            case AntialiasingMode.FastApproximateAntialiasing:
                return "FXAA";
            case AntialiasingMode.SubpixelMorphologicalAntiAliasing:
                return "SMAA";
            case AntialiasingMode.TemporalAntiAliasing:
                return "TAA";
            default:
                return "None";
        }
    }

    public void VSyncToggleValueChange(bool val)
    {
        GameContext.GameSettingsManager.SetVSync(val, true);
    }

    public void TargetFPSValueChange(float index)
    {
        GameContext.GameSettingsManager.SetTargetFPS((int)index, true);
        FPSText.text = "(" + (int)index + " FPS)";
    }

    public void FPSToggleValueChange(bool val)
    {
        if (val)
        {
            TargetFPSSlider.gameObject.SetActive(true);
            GameContext.GameSettingsManager.SetTargetFPS(60, true);
            TargetFPSSlider.value = GameContext.GameSettingsManager.CurrentSettings.Graphics.TargetFPS;
            FPSText.text = "(" + 60 + " FPS)";
        }
        else
        {
            TargetFPSSlider.gameObject.SetActive(false);
            GameContext.GameSettingsManager.SetTargetFPS(-1, true);
            FPSText.text = "(Unlimited FPS)";
        }
    }

    public void FullscreenValueChange(bool val)
    {
        GameContext.GameSettingsManager.SetFullScreen(val, true);
    }

    public void ResolutionValueChange(int index)
    {
        GameContext.GameSettingsManager.SetResolution(index, GameContext.GameSettingsManager.CurrentSettings.Graphics.Fullscreen, true);
    }

    public void QualityValueChange(float index)
    {
        GameContext.GameSettingsManager.SetQuality((int)index, true);
        switch ((int)index)
        {
            case 0:
                QualityText.text = "(Low quality)";
                break;
            case 1:
                QualityText.text = "(Medium quality)";
                break;
            case 2:
                QualityText.text = "(High quality)";
                break;
        }
    }

    /// <summary>
    /// Applies the URP anti-aliasing mode selected in the settings dropdown.
    /// </summary>
    public void AntialiasingValueChange(int index)
    {
        // Dropdown entries follow the enum declaration order.
        GameContext.GameSettingsManager.SetAntialiasing((AntialiasingMode)index, true);
    }

    public void PostprocessValueChange(bool val)
    {
        GameContext.GameSettingsManager.SetPostProcess(val, true);
    }
    #endregion

    #region Inputs
    public GameObject InputLinePrefab;
    public Transform InputContainer;
    private int selectedInputID = -1;
    private InputSettingsType selectedInputType;

    public void BindInputs()
    {
        if (GameContext.GameSettingsManager.CurrentSettings.Inputs == null)
            GameContext.GameSettingsManager.CreateDefaultInputSettings();

        // clear container
        foreach (Transform t in InputContainer)
            Destroy(t.gameObject);
        // bind container
        int i = 0;
        var types = Enum.GetValues(typeof(InputSettingsType));
        foreach (InputSettingsType type in types)
        {
            InputSettingsData input = GameContext.GameSettingsManager.GetInput(type);
            GameObject line = Instantiate(InputLinePrefab);
            line.transform.SetParent(InputContainer, false);
            line.transform.GetChild(0).GetComponent<Text>().text = input.Type.ToString();
            line.transform.GetChild(1).GetComponentInChildren<Text>().text = input.ToString();
            line.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                line.transform.GetChild(1).GetComponentInChildren<Text>().text = "press key...";
                selectedInputID = i;
                selectedInputType = input.Type;
            });
            i++;
        }
    }

    void AssignateInput()
    {
        if (selectedInputID != -1)
        {
            for (int i = (int)KeyCode.A; i <= (int)KeyCode.Z; i++)
                AssignateHotKey(i);
            for (int i = (int)KeyCode.F1; i <= (int)KeyCode.F15; i++)
                AssignateHotKey(i);
            for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++)
                AssignateHotKey(i);
            AssignateHotKey((int)KeyCode.Backspace);
            AssignateHotKey((int)KeyCode.Return);
            AssignateHotKey((int)KeyCode.KeypadEnter);
            AssignateHotKey((int)KeyCode.Space);
            AssignateHotKey((int)KeyCode.DownArrow);
            AssignateHotKey((int)KeyCode.LeftArrow);
            AssignateHotKey((int)KeyCode.RightArrow);
            AssignateHotKey((int)KeyCode.UpArrow);
        }
    }

    void AssignateHotKey(int i)
    {
        if (Input.GetKeyDown((KeyCode)i)) // press a key
        {
            selectedInputID = -1;
            GameContext.GameSettingsManager.SetInput(selectedInputType, (KeyCode)i,
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), true);
            BindInputs();
        }
    }
    #endregion
}
