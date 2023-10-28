using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI Text;
    public float BaseSize;
    public float HoveredSize;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Text.fontSize = HoveredSize;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Text.fontSize = BaseSize;
    }
}