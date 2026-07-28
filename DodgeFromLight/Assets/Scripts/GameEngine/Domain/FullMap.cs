/// <summary>
/// Groups a puzzle grid with its authored metadata.
/// </summary>
public sealed class FullMap
{
    public Grid Grid;
    public Map Map;

    /// <summary>
    /// Creates a complete map document.
    /// </summary>
    public FullMap(Grid grid, Map map)
    {
        // Keep grid data and metadata together throughout load and save operations.
        Grid = grid;
        Map = map;
    }
}
