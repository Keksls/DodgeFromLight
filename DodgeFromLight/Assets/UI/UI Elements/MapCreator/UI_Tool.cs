using UnityEngine;
public class UI_Tool : MonoBehaviour
{
    public ToolType Type;
    public bool AutoRegisterButton = true;

    /// <summary>
    /// Keeps the serialized legacy button harmless while the Fugui interface owns the visible tool palette.
    /// </summary>
    private void Awake()
    {
        // Preserve prefab compatibility until Unity can safely reserialize the disabled Canvas hierarchy.
        UITooltipSetter tip = gameObject.AddComponent<UITooltipSetter>();
        tip.UsePointerEvent = true;
        tip.Message = "Tool <b>" + Type.ToString() + "</b>";
        GetComponentInParent<MapCreatorController>().RegisterToolButton(Type, gameObject);
    }

    /// <summary>
    /// Forwards a serialized legacy button click to the current map creator controller.
    /// </summary>
    public void OnClick()
    {
        // This path is inactive in normal use because Fugui handles the visible interface.
        GetComponentInParent<MapCreatorController>().SetTool(Type);
    }
}
