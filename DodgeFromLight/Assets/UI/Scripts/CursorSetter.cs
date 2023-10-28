using UnityEngine;

public class CursorSetter : MonoBehaviour
{
    public CursorType PointerEnterCursor = CursorType.Hand;
    public CursorType PointerClickedCursor = CursorType.Arrow;
    public bool KeepUntilMouseUp = false;
    bool hovered = false;
    bool mouseDown = false;
    bool registered = false;

    private void Start()
    {
        if (registered || DodgeFromLight.CursorManager == null)
            return;
        DodgeFromLight.CursorManager.NbSetters++;
        registered = true;
    }

    private void OnEnable()
    {
        if (registered || DodgeFromLight.CursorManager == null)
            return;
        DodgeFromLight.CursorManager.NbSetters++;
        registered = true;
    }

    private void OnDisable()
    {
        if (!registered || DodgeFromLight.CursorManager == null)
            return;
        DodgeFromLight.CursorManager.NbSetters--;
        registered = false;
    }

    public void HoverEnter()
    {
        hovered = true;
    }

    public void HoverExit()
    {
        hovered = false;
    }

    public void MouseDown()
    {
        if (hovered && KeepUntilMouseUp)
        {
            DodgeFromLight.CursorManager.SetCursor(PointerClickedCursor);
            DodgeFromLight.CursorManager.Lock();
            mouseDown = true;
        }
    }

    public void MouseUp()
    {
        if (hovered && KeepUntilMouseUp)
        {
            DodgeFromLight.CursorManager.UnLock();
            DodgeFromLight.CursorManager.SetCursor(PointerClickedCursor);
            mouseDown = false;
        }
    }

    private void LateUpdate()
    {
        if (hovered && KeepUntilMouseUp && mouseDown)
        {
            mouseDown = false;
            DodgeFromLight.CursorManager.SetCursor(PointerClickedCursor);
        }
    }
}