using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Applies hover feedback to an interactive map decoration.
/// </summary>
public sealed class HoverableMapObject : MonoBehaviour
{
    public MeshRenderer Renderer;
    public Material BaseMaterial;
    public Material HoveredMaterial;

    /// <summary>
    /// Applies the hover material while the pointer is over the decoration.
    /// </summary>
    private void OnMouseEnter()
    {
        // Do not override UI pointer interactions with world feedback.
        if (!EventSystem.current.IsPointerOverGameObject())
            Renderer.material = HoveredMaterial;
    }

    /// <summary>
    /// Restores the base material when the pointer leaves the decoration.
    /// </summary>
    private void OnMouseExit()
    {
        // Return the decoration to its authored presentation.
        Renderer.material = BaseMaterial;
    }
}
