using UnityEngine;

public class UI_EnvironmentButton : MonoBehaviour
{
    public BuildInEnvironments Environment;

    private void Awake()
    {
        UITooltipSetter tip = gameObject.AddComponent<UITooltipSetter>();
        tip.UsePointerEvent = true;
        tip.Message = "Environment <b>" + Environment.ToString() + "</b>";
    }

    public void OnClick()
    {
        GetComponentInParent<UI_MapCreator>().SetBuildinEnvironment(Environment);
        if (UI_EnvironmentSettings.Instance != null && UI_EnvironmentSettings.Instance.gameObject.activeInHierarchy)
            UI_EnvironmentSettings.Instance.BindEnvironment(DodgeFromLight.CurrentMap.Grid.Environment);
    }
}