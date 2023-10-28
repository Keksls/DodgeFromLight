using UnityEngine;
using UnityEngine.UI;

public class UI_GearButton : MonoBehaviour
{
    public GearType Gear;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GetComponentInParent<UI_MapCreator>().SetGearOnCell(Gear);
        });

        UITooltipSetter tip = gameObject.AddComponent<UITooltipSetter>();
        tip.Message = "Gear <b>" + Gear.ToString() + "</b>";
        tip.UsePointerEvent = true;
    }
}