/// <summary>
/// Reverses a cell-type and walkability change.
/// </summary>
public sealed class Undoable_SetCellType : UndoableAction
{
    private readonly CellPos _cellPosition;
    private readonly CellType _previousType;
    private readonly CellType _newType;

    /// <summary>
    /// Creates a reversible cell-type change.
    /// </summary>
    public Undoable_SetCellType(
        CellPos position,
        CellType previousType,
        CellType newType)
    {
        // Positions remain stable even when the board visuals are rebuilt.
        _cellPosition = position;
        _previousType = previousType;
        _newType = newType;
    }

    /// <summary>
    /// Restores the previous cell type and matching walkability.
    /// </summary>
    public override void Undo()
    {
        // Ordinary and mechanic cells are walkable; only explicit void is not.
        Apply(_previousType);
    }

    /// <summary>
    /// Reapplies the new cell type and matching walkability.
    /// </summary>
    public override void Redo()
    {
        // Redo uses the same invariant-preserving mutation as Undo.
        Apply(_newType);
    }

    /// <summary>
    /// Applies one cell type while preserving its walkability invariant.
    /// </summary>
    private void Apply(CellType type)
    {
        // The active grid remains authoritative for the cell instance.
        Cell cell =
            GameContext.CurrentMap.Grid.GetCell(_cellPosition);
        cell.SetType(type);
        cell.SetWalkable(type != CellType.NotWalkable);
    }
}
