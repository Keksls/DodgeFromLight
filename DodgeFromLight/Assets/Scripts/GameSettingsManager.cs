using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;

public class GameSettingsManager : MonoBehaviour
{
    #region Variables
    public PostProcessVolume PostProcessVolume;
    public PostProcessLayer PostProcessLayer;
    public AudioMixer Mixer;
    [HideInInspector]
    public GameSettings CurrentSettings { get; private set; }
    private string settingsPath;
    #endregion

    private void Awake()
    {
        DodgeFromLight.GameSettingsManager = this;
        settingsPath = Application.persistentDataPath + @"\settings.dfl";
        LoadSettings();
    }

    private void Start()
    {
        ApplyAllSettings(CurrentSettings, false);
    }

    #region Graphics
    public void SetFullScreen(bool fullscreen, bool save)
    {
        CurrentSettings.Graphics.Fullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
        if (save)
            SaveSettings();
    }

    public void SetResolution(int res, bool fullscreen, bool save)
    {
        CurrentSettings.Graphics.Resolution = res;
        Screen.SetResolution(Screen.resolutions[res].width, Screen.resolutions[res].height, fullscreen);
        if (save)
            SaveSettings();
    }

    public void SetQuality(int index, bool save)
    {
        CurrentSettings.Graphics.Quality = index;
        QualitySettings.SetQualityLevel(index);
        if (save)
            SaveSettings();
    }

    public void SetAntialiasing(PostProcessLayer.Antialiasing AA, bool save)
    {
        PostProcessLayer.antialiasingMode = AA;
        CurrentSettings.Graphics.Antialiasing = AA;
        if (save)
            SaveSettings();
    }

    public void SetPostProcess(bool pp, bool save)
    {
        PostProcessLayer.enabled = pp;
        CurrentSettings.Graphics.PostProcess = pp;
        if (save)
            SaveSettings();
    }

    public void SetTargetFPS(int fps, bool save)
    {
        CurrentSettings.Graphics.TargetFPS = fps;
        Application.targetFrameRate = fps;
        if (save)
            SaveSettings();
    }

    public void SetVSync(bool vsync, bool save)
    {
        CurrentSettings.Graphics.VSync = vsync;
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        if (save)
            SaveSettings();
    }
    #endregion

    #region Inputs
    public InputSettingsData GetInput(InputSettingsType type)
    {
        return CurrentSettings.Inputs.GetInput(type);
    }

    public void SetInput(InputSettingsType type, KeyCode key, bool ctrl, bool shift, bool save)
    {
        CurrentSettings.Inputs.SetInput(type, key, ctrl, shift);
        if (save)
            SaveSettings();
    }

    Dictionary<KeyCode, string> keyNames = null;
    public string GetNiceKeyString(KeyCode Key)
    {
        if (keyNames == null || keyNames.Count == 0)
        {
            keyNames = new Dictionary<KeyCode, string>();
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                if (!keyNames.ContainsKey(k))
                    keyNames.Add(k, k.ToString());

            for (int i = 0; i < 10; i++)
            {
                keyNames[(KeyCode)((int)KeyCode.Alpha0 + i)] = i.ToString();
                keyNames[(KeyCode)((int)KeyCode.Keypad0 + i)] = i.ToString();
            }
            keyNames[KeyCode.Comma] = ",";
            keyNames[KeyCode.Escape] = "Esc";
            keyNames[KeyCode.Backspace] = "Back";
            keyNames[KeyCode.Return] = "Enter";
        }

        return keyNames[Key];
    }
    #endregion

    #region Global
    public void SetDisplayFPS(bool display, bool save)
    {
        CurrentSettings.Global.DisplayFPS = display;
        if (save)
            SaveSettings();
    }

    public void SetAutoHideCursor(bool hideCursor, bool save)
    {
        CurrentSettings.Global.AutoHideCursor = hideCursor;
        Cursor.visible = true;
        if (save)
            SaveSettings();
    }
    #endregion

    #region Audio
    public void SetVolume(float volume, bool save)
    {
        CurrentSettings.Audio.Volume = volume;
        Mixer.SetFloat("MasterVolume", volume);
        if (save)
            SaveSettings();
    }
    #endregion

    #region Apply
    public void ApplyAllSettings(GameSettings settings, bool save)
    {
        ApplyGraphicSettings(settings.Graphics, save);
    }

    public void ApplyGraphicSettings(GraphicsSettings settings, bool save)
    {
        SetFullScreen(settings.Fullscreen, save);
        SetResolution(settings.Resolution, settings.Fullscreen, save);
        SetQuality(settings.Quality, save);
        SetAntialiasing(settings.Antialiasing, save);
        SetPostProcess(settings.PostProcess, save);
        SetTargetFPS(settings.TargetFPS, save);
        SetVSync(settings.VSync, save);
    }
    #endregion

    #region Serialization
    public void SaveSettings()
    {
        string json = JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented);
        File.WriteAllText(settingsPath, json);
    }

    public void LoadSettings()
    {
        if (!File.Exists(settingsPath))
            CreateDefaultSettings();
        CurrentSettings = JsonConvert.DeserializeObject<GameSettings>(File.ReadAllText(settingsPath));
        CurrentSettings.Inputs.Initialize();
    }

    public void CreateDefaultGraphicSettings()
    {
        // default graphics settings
        CurrentSettings.Graphics = new GraphicsSettings();
        CurrentSettings.Graphics.Antialiasing = PostProcessLayer.Antialiasing.TemporalAntialiasing;
        CurrentSettings.Graphics.Fullscreen = true;
        CurrentSettings.Graphics.PostProcess = true;
        CurrentSettings.Graphics.Quality = 2;
        CurrentSettings.Graphics.Resolution = Screen.resolutions.Length - 1;
        CurrentSettings.Graphics.TargetFPS = 60;
        CurrentSettings.Graphics.VSync = true;
    }

    public void CreateDefaultInputSettings()
    {
        // default input settings
        CurrentSettings.Inputs = new InputSettings();
        CurrentSettings.Inputs.SetInput(InputSettingsType.Backward, KeyCode.S, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.Forward, KeyCode.Z, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.left, KeyCode.Q, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.Right, KeyCode.D, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.Jump, KeyCode.Space, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.Gear1, KeyCode.A, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.Gear2, KeyCode.E, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.PlantSword, KeyCode.T, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.DeployShield, KeyCode.G, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.CastPet, KeyCode.F, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.TakeCheckPoint, KeyCode.Backspace, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.RestartMap, KeyCode.R, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.NextMap, KeyCode.N, false, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.TurnCameraLeft, KeyCode.Q, true, false);
        CurrentSettings.Inputs.SetInput(InputSettingsType.TurnCameraRight, KeyCode.D, true, false);
        CurrentSettings.Inputs.Initialize();
    }

    public void CreateDefaultGlobalSettings()
    {
        // default Global settings
        CurrentSettings.Global = new GlobalSettings();
        CurrentSettings.Global.DisplayFPS = false;
        CurrentSettings.Global.AutoHideCursor = true;
    }

    public void CreateDefaultAudioSettings()
    {
        // audio settings
        CurrentSettings.Audio = new DFLAudioSettings();
        CurrentSettings.Audio.Volume = 1.0f;
    }

    public void CreateDefaultSettings()
    {
        CurrentSettings = new GameSettings();
        CreateDefaultGraphicSettings();
        CreateDefaultInputSettings();
        CreateDefaultGlobalSettings();
        CreateDefaultAudioSettings();
        // save settings
        SaveSettings();
    }
    #endregion
}

public class GameSettings
{
    public GraphicsSettings Graphics { get; set; }
    public InputSettings Inputs { get; set; }
    public GlobalSettings Global { get; set; }
    public DFLAudioSettings Audio { get; set; }
}

public class DFLAudioSettings
{
    public float Volume { get; set; }
}

public class GraphicsSettings
{
    public bool Fullscreen { get; set; }
    public int Resolution { get; set; }
    public int Quality { get; set; }
    public PostProcessLayer.Antialiasing Antialiasing { get; set; }
    public bool PostProcess { get; set; }
    public int TargetFPS { get; set; }
    public bool VSync { get; set; }
}

public class GlobalSettings
{
    public bool DisplayFPS { get; set; }
    public bool AutoHideCursor { get; set; }
}

public class InputSettings
{
    public List<InputSettingsData> Inputs { get; set; }
    private Dictionary<InputSettingsType, InputSettingsData> InputsDic = new Dictionary<InputSettingsType, InputSettingsData>();

    public InputSettings()
    {
        Inputs = new List<InputSettingsData>();
    }

    public void Initialize()
    {
        InputsDic = new Dictionary<InputSettingsType, InputSettingsData>();
        foreach (var input in Inputs)
            InputsDic.Add(input.Type, input);
    }

    public void SetInput(InputSettingsType type, KeyCode key, bool ctrl, bool shift)
    {
        Remove(type);
        Inputs.Add(new InputSettingsData()
        {
            Control = ctrl,
            Key = key,
            Shift = shift,
            Type = type
        });
        Initialize();
    }

    public void Remove(InputSettingsType type)
    {
        int toRem = -1;
        int i = 0;
        foreach (InputSettingsData input in Inputs)
        {
            if (input.Type == type)
                toRem = i;
            i++;
        }
        if (toRem != -1)
            Inputs.RemoveAt(toRem);
    }

    public InputSettingsData GetInput(InputSettingsType type)
    {
        return InputsDic[type];
    }
}

public class InputSettingsData
{
    public InputSettingsType Type { get; set; }
    public KeyCode Key { get; set; }
    public bool Control { get; set; }
    public bool Shift { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        if (Control)
            sb.Append("Ctrl + ");
        if (Shift)
            sb.Append("Shift + ");
        sb.Append(DodgeFromLight.GameSettingsManager.GetNiceKeyString(Key));
        return sb.ToString();
    }

    public bool IsDown()
    {
        if (Control && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            return false;

        if (Shift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            return false;

        return Input.GetKeyDown(Key);
    }

    public bool IsUp()
    {
        if (Control && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            return false;

        if (Shift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            return false;

        return Input.GetKeyUp(Key);
    }

    public bool IsPressed()
    {
        if (Control && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            return false;

        if (Shift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            return false;

        return Input.GetKey(Key);
    }
}

public enum InputSettingsType
{
    Forward,
    Backward,
    left,
    Right,
    Jump,
    Gear1,
    Gear2,
    PlantSword,
    DeployShield,
    CastPet,
    TakeCheckPoint,
    RestartMap,
    NextMap,
    TurnCameraLeft,
    TurnCameraRight
}