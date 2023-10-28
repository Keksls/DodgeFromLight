using DFLCommonNetwork.GameEngine;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableMapObject : MonoBehaviour
{
    public MeshRenderer Renderer;
    public Material BaseMaterial;
    public Material HoveredMaterial;
    public string Args;
    public CellPos Cell;

    private void OnMouseEnter()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            Renderer.material = HoveredMaterial;
    }

    private void OnMouseExit()
    {
        Renderer.material = BaseMaterial;
    }
}