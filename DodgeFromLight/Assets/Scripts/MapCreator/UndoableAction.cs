/// <summary>
/// Defines a reversible map-authoring mutation.
/// </summary>
public abstract class UndoableAction
{
    /// <summary>
    /// Restores the state that existed before the authoring mutation.
    /// </summary>
    public abstract void Undo();

    /// <summary>
    /// Reapplies the authoring mutation after it was undone.
    /// </summary>
    public abstract void Redo();
}
