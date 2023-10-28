using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CursorManager : MonoBehaviour
{
    public Camera cam;
    public Texture2D Arrow;
    public Texture2D Hand;
    public Texture2D TextCarret;
    public Texture2D ResizeH;
    public Texture2D ResizeV;
    public Texture2D ResizeHandV;
    public Texture2D Rotation;
    public Texture2D RotationAndScale;
    public Texture2D Scale;
    public int NbSetters = 0;
    private CursorType currentCursor;
    private bool isLocked = false;
    private CursorSetter currentCursorSetter;

    private void Awake()
    {
        DodgeFromLight.CursorManager = this;
        SetCursor(CursorType.Arrow);
        NbSetters = 0;
    }

    public void SetCursor(CursorType cursor)
    {
        if (isLocked)
            return;
        switch (cursor)
        {
            default:
            case CursorType.Arrow:
                Cursor.SetCursor(Arrow, new Vector2(16f, 0f), CursorMode.Auto);
                break;
            case CursorType.Hand:
                Cursor.SetCursor(Hand, new Vector2(16f, 0f), CursorMode.Auto);
                break;
            case CursorType.TextCarret:
                Cursor.SetCursor(TextCarret, new Vector2(16f, 16f), CursorMode.Auto);
                break;
            case CursorType.ResizeH:
                Cursor.SetCursor(ResizeH, new Vector2(16f, 16f), CursorMode.Auto);
                break;
            case CursorType.ResizeV:
                Cursor.SetCursor(ResizeV, new Vector2(16f, 16f), CursorMode.Auto);
                break;
            case CursorType.ResizeHandV:
                Cursor.SetCursor(ResizeHandV, new Vector2(16f, 16f), CursorMode.Auto);
                break;
            case CursorType.Rotation:
                Cursor.SetCursor(Rotation, new Vector2(16f, 16f), CursorMode.Auto);
                break;
            case CursorType.RotationAndScale:
                Cursor.SetCursor(RotationAndScale, new Vector2(16f, 16f), CursorMode.Auto);
                break;
            case CursorType.Scale:
                Cursor.SetCursor(Scale, new Vector2(16f, 16f), CursorMode.Auto);
                break;
        }
        currentCursor = cursor;
    }

    public CursorType GetCursor()
    {
        return currentCursor;
    }

    public void Lock()
    {
        isLocked = true;
    }

    public void UnLock()
    {
        isLocked = false;
    }

    private void Update()
    {
        if (currentCursorSetter == null)
            return;

        if (Input.GetMouseButtonDown(0))
            currentCursorSetter.MouseDown();
        else if (Input.GetMouseButtonUp(0))
            currentCursorSetter.MouseUp();
    }

    public void LateUpdate()
    {
        if (isLocked || NbSetters <= 0)
            return;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            var rays = GetEventSystemRaycastResults();
            foreach (var ray in rays)
            {
                var setter = ray.gameObject.GetComponent<CursorSetter>();
                if (setter != null)
                {
                    currentCursorSetter = setter;
                    currentCursorSetter.HoverEnter();
                    SetCursor(setter.PointerEnterCursor);
                    return;
                }
                break;
            }
        }
        else
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                var setter = hit.collider.gameObject.GetComponent<CursorSetter>();
                if (setter != null)
                {
                    currentCursorSetter = setter;
                    currentCursorSetter.HoverEnter();
                    SetCursor(setter.PointerEnterCursor);
                    return;
                }
            }
        }
        if (currentCursorSetter != null)
            currentCursorSetter.HoverExit();

        SetCursor(CursorType.Arrow);
    }
    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}

public enum CursorType
{
    Arrow,
    Hand,
    TextCarret,
    ResizeH,
    ResizeV,
    ResizeHandV,
    Rotation,
    RotationAndScale,
    Scale
}