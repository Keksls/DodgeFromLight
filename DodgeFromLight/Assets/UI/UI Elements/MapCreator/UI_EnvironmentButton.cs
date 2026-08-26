using UnityEngine;

public class UI_EnvironmentButton : MonoBehaviour
{
    public BuildInEnvironments Environment;

    /// <summary>
    /// Preserves tooltip data on the serialized legacy environment button.
    /// </summary>
    private void Awake()
    {
        // This hierarchy stays disabled while the Fugui environment panel is active.
        UITooltipSetter tip = gameObject.AddComponent<UITooltipSetter>();
        tip.UsePointerEvent = true;
        tip.Message = "Environment <b>" + Environment.ToString() + "</b>";
    }

    /// <summary>
    /// Forwards a serialized legacy environment click to the current controller.
    /// </summary>
    public void OnClick()
    {
        // Keep the fallback functional for a safe prefab migration in the Unity editor.
        GetComponentInParent<MapCreatorController>().SetBuildinEnvironment(Environment);
        if (UI_EnvironmentSettings.Instance != null && UI_EnvironmentSettings.Instance.gameObject.activeInHierarchy)
            UI_EnvironmentSettings.Instance.BindEnvironment(GameContext.CurrentMap.Grid.Environment);
    }
}
