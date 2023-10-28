using UnityEngine;

public class FPSOnGUIText : MonoBehaviour
{
    float updateInterval = 1.0f;   // Current time interval
    private float accumulated = 0.0f;  // Accumulated during this period 
    private float frames = 0;    // Frames drawn within the interval 
    private float timeRemaining;   // The remaining time of the current interval
    private float fps = 15.0f;    // Current FPS
    private float lastSample;
    private GUIStyle style;

    private void Awake()
    {
        style = new GUIStyle
        {
            border = new RectOffset(10, 10, 10, 10),
            fontSize = 24,
        };
    }

    private void OnEnable()
    {
        style = new GUIStyle
        {
            border = new RectOffset(10, 10, 10, 10),
            fontSize = 24,
        };
    }

    void Start()
    {
        timeRemaining = updateInterval;
        lastSample = Time.realtimeSinceStartup; // Real time self starting
    }

    void Update()
    {
        if (DodgeFromLight.GameSettingsManager == null || !DodgeFromLight.GameSettingsManager.CurrentSettings.Global.DisplayFPS)
            return;
        ++frames;
        float newSample = Time.realtimeSinceStartup;
        float deltaTime = newSample - lastSample;
        lastSample = newSample;
        timeRemaining -= deltaTime;
        accumulated += 1.0f / deltaTime;

        if (timeRemaining <= 0.0f)
        {
            fps = accumulated / frames;
            timeRemaining = updateInterval;
            accumulated = 0.0f;
            frames = 0;
        }
    }

    void OnGUI()
    {
        if (!DodgeFromLight.GameSettingsManager.CurrentSettings.Global.DisplayFPS)
            return;
        GUI.Label(new Rect(Screen.width - 152, 32, 152, 32), "<color=#ffffff><size=24>" + "FPS:" + fps.ToString("f1") + "</size></color>", style);
    }
}