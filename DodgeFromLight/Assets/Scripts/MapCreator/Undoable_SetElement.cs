/// <summary>
/// Reverses the decoration assigned to one cell.
/// </summary>
public sealed class Undoable_SetElement : UndoableAction
{
    private readonly CellPos _cellPosition;
    private readonly string _previousElementId;
    private readonly string _newElementId;

    /// <summary>
    /// Creates a reversible decoration assignment.
    /// </summary>
    public Undoable_SetElement(
        CellPos position,
        string previousElementId,
        string newElementId)
    {
        // Stable resource identifiers preserve authored asset identity.
        _cellPosition = position;
        _previousElementId = previousElementId;
        _newElementId = newElementId;
    }

    /// <summary>
    /// Restores the previous decoration assignment.
    /// </summary>
    public override void Undo()
    {
        // Empty identifiers remove the decoration and reset its transform data.
        Apply(_previousElementId);
    }

    /// <summary>
    /// Reapplies the new decoration assignment.
    /// </summary>
    public override void Redo()
    {
        // Redo shares the same empty-value handling as Undo.
        Apply(_newElementId);
    }

    /// <summary>
    /// Applies one decoration identifier to the current grid cell.
    /// </summary>
    private void Apply(string elementId)
    {
        // Element helpers preserve the cell's serialized scale and rotation defaults.
        Cell cell =
            GameContext.CurrentMap.Grid.GetCell(_cellPosition);
        if (string.IsNullOrEmpty(elementId))
            cell.RemoveElement();
        else
            cell.SetElement(elementId);
    }
}
