using UnityEngine;

/// <summary>
/// Temporarily binds neutral material instances to a renderer without modifying its project assets.
/// </summary>
public sealed class MapCreatorMaterialOverride
{
    private readonly Renderer _renderer;
    private readonly Material[] _authored;
    private readonly Material[] _work;

    /// <summary>
    /// Captures the authored slots and their cache-owned neutral replacements.
    /// </summary>
    public MapCreatorMaterialOverride(Renderer renderer, Material[] authored, Material[] work)
    {
        _renderer = renderer;
        _authored = authored;
        _work = work;
    }

    /// <summary>
    /// Binds the neutral slots for this camera render only.
    /// </summary>
    public void Apply()
    {
        if (_renderer != null) _renderer.sharedMaterials = _work;
    }

    /// <summary>
    /// Restores the exact original material references after the work camera has rendered.
    /// </summary>
    public void Restore()
    {
        if (_renderer != null) _renderer.sharedMaterials = _authored;
    }
}
