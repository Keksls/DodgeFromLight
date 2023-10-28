using UnityEngine;
public class UI_Tool : MonoBehaviour
{
    public ToolType Type;
    public bool AutoRegisterButton = true;

    private void Awake()
    {
        UITooltipSetter tip = gameObject.AddComponent<UITooltipSetter>();
        tip.UsePointerEvent = true;
        tip.Message = "Tool <b>" + Type.ToString() + "</b>";
        GetComponentInParent<UI_MapCreator>().RegisterToolButton(Type, gameObject);
    }

    public void OnClick()
    {
        GetComponentInParent<UI_MapCreator>().SetTool(Type);
    }
}
