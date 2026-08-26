using System.Collections.Generic;

/// <summary>
/// Stores the reversible history of the active map-authoring session.
/// </summary>
public static class UndoableActionsManager
{
    private static readonly Stack<UndoableAction> Undoable =
        new Stack<UndoableAction>();
    private static readonly Stack<UndoableAction> Redoable =
        new Stack<UndoableAction>();

    public static bool CanUndo => Undoable.Count > 0;
    public static bool CanRedo => Redoable.Count > 0;

    /// <summary>
    /// Adds one committed action and invalidates the divergent redo branch.
    /// </summary>
    public static void AddUndoableAction(
        UndoableAction action)
    {
        // A new authoring mutation always starts a fresh linear history branch.
        if (action == null)
            return;
        Undoable.Push(action);
        Redoable.Clear();
    }

    /// <summary>
    /// Clears all history for a newly loaded authoring workspace.
    /// </summary>
    public static void Clear()
    {
        // History never crosses map boundaries.
        Undoable.Clear();
        Redoable.Clear();
    }

    /// <summary>
    /// Restores the previous supported authoring state.
    /// </summary>
    public static void Undo()
    {
        // Empty history is a safe no-op for both shortcuts and buttons.
        if (Undoable.Count == 0)
            return;
        UndoableAction action = Undoable.Pop();
        action.Undo();
        Redoable.Push(action);
    }

    /// <summary>
    /// Reapplies the next supported authoring state.
    /// </summary>
    public static void Redo()
    {
        // Reapplied actions return to the undo stack in chronological order.
        if (Redoable.Count == 0)
            return;
        UndoableAction action = Redoable.Pop();
        action.Redo();
        Undoable.Push(action);
    }
}
