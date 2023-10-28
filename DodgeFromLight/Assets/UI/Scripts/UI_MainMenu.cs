using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && EscToOpen && !UI_Console.IsTyping)
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
        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Global == null)
            DodgeFromLight.GameSettingsManager.CreateDefaultGlobalSettings();
        DisplayFPSToggle.isOn = DodgeFromLight.GameSettingsManager.CurrentSettings.Global.DisplayFPS;
        AutoHideCursorToggle.isOn = DodgeFromLight.GameSettingsManager.CurrentSettings.Global.AutoHideCursor;
    }

    public void ClearCache()
    {
        DodgeFromLight.UI_Modal.SetTitle("you are going to clear the cache")
            .SetButtonLeft("Apply", () =>
            {
                DodgeFromLight.UI_WorkerNotifier.Show("Restarting...");
                int rt = 0, TutorialStepIndex = 0;
                if (PlayerPrefs.HasKey("TutorialRewardsTaken"))
                    rt = PlayerPrefs.GetInt("TutorialRewardsTaken");
                if (PlayerPrefs.HasKey("TutorialStepIndex"))
                    TutorialStepIndex = PlayerPrefs.GetInt("TutorialStepIndex");
                PlayerPrefs.DeleteAll();
                PlayerPrefs.SetInt("TutorialRewardsTaken", rt);
                PlayerPrefs.SetInt("TutorialStepIndex", TutorialStepIndex);
                DodgeFromLight.RestartGame();
                DodgeFromLight.UI_WorkerNotifier.Hide();
            })
        .Show("This action need to restart the game.");
    }

    public void RedoTutorial()
    {
        DodgeFromLight.UI_Modal.SetTitle("you are going to clear Tutorial avancement")
            .SetButtonLeft("Apply", () =>
            {
                DodgeFromLight.UI_WorkerNotifier.Show("Restarting...");
                PlayerPrefs.DeleteKey("TutorialStepIndex");
                PlayerPrefs.DeleteKey("TutorialRewardsTaken");
                DodgeFromLight.RestartGame();
                DodgeFromLight.UI_WorkerNotifier.Hide();
            })
        .Show("This action need to restart the game.");
    }

    public void ResetDefaultSettings()
    {
        DodgeFromLight.UI_Modal.SetTitle("you are going to restore default settings")
            .SetButtonLeft("Apply", () =>
            {
                DodgeFromLight.UI_WorkerNotifier.Show("Restarting...");
                DodgeFromLight.GameSettingsManager.CreateDefaultSettings();
                DodgeFromLight.RestartGame();
                DodgeFromLight.UI_WorkerNotifier.Hide();
            })
        .Show("This action need to restart the game.");
    }

    public void DisplayFPSToggleValueChange(bool val)
    {
        DodgeFromLight.GameSettingsManager.SetDisplayFPS(val, true);
    }

    public void AutoHideCursorToggleValueChange(bool val)
    {
        DodgeFromLight.GameSettingsManager.SetAutoHideCursor(val, true);
    }
    #endregion

    #region Audio
    public Slider AudioVolumeSlider;

    public void BindAudioSettings()
    {
        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Audio == null)
            DodgeFromLight.GameSettingsManager.CreateDefaultAudioSettings();
        AudioVolumeSlider.value = DodgeFromLight.GameSettingsManager.CurrentSettings.Audio.Volume;
    }

    public void VolumeChange(float volume)
    {
        DodgeFromLight.GameSettingsManager.SetVolume(volume, true);
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
        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics == null)
            DodgeFromLight.GameSettingsManager.CreateDefaultGraphicSettings();

        FullscreenToggle.isOn = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.Fullscreen;
        VSyncToggle.isOn = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.VSync;
        PostprocessToggle.isOn = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.PostProcess;
        QualitySlider.value = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.Quality;
        FPSToggle.isOn = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.TargetFPS > -1;
        TargetFPSSlider.gameObject.SetActive(FPSToggle.isOn);
        if (FPSToggle.isOn)
            TargetFPSSlider.value = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.TargetFPS;

        // bind resolutions
        ResolutionDropdown.options.Clear();
        foreach (var res in Screen.resolutions)
            ResolutionDropdown.options.Add(new Dropdown.OptionData(res.width + " x " + res.height));
        ResolutionDropdown.value = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.Resolution;

        // bind Antialiasing
        AntialiasingDropdown.options.Clear();
        foreach (PostProcessLayer.Antialiasing res in Enum.GetValues(typeof(PostProcessLayer.Antialiasing)))
            AntialiasingDropdown.options.Add(new Dropdown.OptionData(GetAAName(res)));
        AntialiasingDropdown.value = (int)DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.Antialiasing;
    }

    private string GetAAName(PostProcessLayer.Antialiasing aa)
    {
        switch (aa)
        {
            case PostProcessLayer.Antialiasing.None:
                return "None";
            case PostProcessLayer.Antialiasing.FastApproximateAntialiasing:
                return "FXAA";
            case PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing:
                return "SMAA";
            case PostProcessLayer.Antialiasing.TemporalAntialiasing:
                return "TAA";
            default:
                return "None";
        }
    }

    public void VSyncToggleValueChange(bool val)
    {
        DodgeFromLight.GameSettingsManager.SetVSync(val, true);
    }

    public void TargetFPSValueChange(float index)
    {
        DodgeFromLight.GameSettingsManager.SetTargetFPS((int)index, true);
        FPSText.text = "(" + (int)index + " FPS)";
    }

    public void FPSToggleValueChange(bool val)
    {
        if (val)
        {
            TargetFPSSlider.gameObject.SetActive(true);
            DodgeFromLight.GameSettingsManager.SetTargetFPS(60, true);
            TargetFPSSlider.value = DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.TargetFPS;
            FPSText.text = "(" + 60 + " FPS)";
        }
        else
        {
            TargetFPSSlider.gameObject.SetActive(false);
            DodgeFromLight.GameSettingsManager.SetTargetFPS(-1, true);
            FPSText.text = "(Unlimited FPS)";
        }
    }

    public void FullscreenValueChange(bool val)
    {
        DodgeFromLight.GameSettingsManager.SetFullScreen(val, true);
    }

    public void ResolutionValueChange(int index)
    {
        DodgeFromLight.GameSettingsManager.SetResolution(index, DodgeFromLight.GameSettingsManager.CurrentSettings.Graphics.Fullscreen, true);
    }

    public void QualityValueChange(float index)
    {
        DodgeFromLight.GameSettingsManager.SetQuality((int)index, true);
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

    public void AntialiasingValueChange(int index)
    {
        DodgeFromLight.GameSettingsManager.SetAntialiasing((PostProcessLayer.Antialiasing)index, true);
    }

    public void PostprocessValueChange(bool val)
    {
        DodgeFromLight.GameSettingsManager.SetPostProcess(val, true);
    }
    #endregion

    #region Inputs
    public GameObject InputLinePrefab;
    public Transform InputContainer;
    private int selectedInputID = -1;
    private InputSettingsType selectedInputType;

    public void BindInputs()
    {
        if (DodgeFromLight.GameSettingsManager.CurrentSettings.Inputs == null)
            DodgeFromLight.GameSettingsManager.CreateDefaultInputSettings();

        // clear container
        foreach (Transform t in InputContainer)
            Destroy(t.gameObject);
        // bind container
        int i = 0;
        var types = Enum.GetValues(typeof(InputSettingsType));
        foreach (InputSettingsType type in types)
        {
            InputSettingsData input = DodgeFromLight.GameSettingsManager.GetInput(type);
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
            DodgeFromLight.GameSettingsManager.SetInput(selectedInputType, (KeyCode)i,
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), true);
            BindInputs();
        }
    }
    #endregion
}