using UnityEngine;
using UnityEngine.UI;

public class UI_Collapsable : MonoBehaviour
{
    public GameObject Body;
    private bool collapsed = false;

    public void Collapse()
    {
        collapsed = true;
        Body.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
    }

    public void Extends()
    {
        collapsed = false;
        Body.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
    }

    public void ClickOnHeader()
    {
        if (collapsed)
            Extends();
        else
            Collapse();
    }
}