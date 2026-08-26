using UnityEngine;
using UnityEngine.UI;

public class UI_GearButton : MonoBehaviour
{
    public GearType Gear;

    /// <summary>
    /// Preserves the serialized legacy gear button until the disabled Canvas hierarchy is reserialized.
    /// </summary>
    private void Awake()
    {
        // The listener remains valid for prefab compatibility, although Fugui owns normal interaction.
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GetComponentInParent<MapCreatorController>().SetGearOnCell(Gear);
        });

        UITooltipSetter tip = gameObject.AddComponent<UITooltipSetter>();
        tip.Message = "Gear <b>" + Gear.ToString() + "</b>";
        tip.UsePointerEvent = true;
    }
}
