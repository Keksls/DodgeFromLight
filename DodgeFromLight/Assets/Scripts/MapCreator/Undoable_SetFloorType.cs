/// <summary>
/// Reverses the floor asset assigned to one cell.
/// </summary>
public sealed class Undoable_SetFloorType : UndoableAction
{
    private readonly CellPos _cellPosition;
    private readonly string _previousFloorId;
    private readonly string _newFloorId;

    /// <summary>
    /// Creates a reversible floor-asset change.
    /// </summary>
    public Undoable_SetFloorType(
        CellPos position,
        string previousFloorId,
        string newFloorId)
    {
        // Stable resource identifiers survive every visual board rebuild.
        _cellPosition = position;
        _previousFloorId = previousFloorId;
        _newFloorId = newFloorId;
    }

    /// <summary>
    /// Restores the previous floor resource identifier.
    /// </summary>
    public override void Undo()
    {
        // The active grid supplies the current cell instance.
        GameContext.CurrentMap.Grid
            .GetCell(_cellPosition)
            .FloorID = _previousFloorId;
    }

    /// <summary>
    /// Reapplies the new floor resource identifier.
    /// </summary>
    public override void Redo()
    {
        // Resource identity is the complete serialized floor mutation.
        GameContext.CurrentMap.Grid
            .GetCell(_cellPosition)
            .FloorID = _newFloorId;
    }
}
