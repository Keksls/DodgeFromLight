using System.Collections.Generic;

/// <summary>
/// Stores bounded whole-document revisions so every authoring gesture can be reversed.
/// </summary>
public static class UndoableActionsManager
{
    private const int Capacity = 64;
    private static readonly LinkedList<string> Undoable = new LinkedList<string>();
    private static readonly Stack<string> Redoable = new Stack<string>();
    private static bool _lastWasCoalesced;
    private static long _lastRecordTime;
    public static bool CanUndo => Undoable.Count > 0;
    public static bool CanRedo => Redoable.Count > 0;

    /// <summary>
    /// Records the previous revision only when authored document data actually changed.
    /// </summary>
    public static void Record(string before, string after, bool coalesce = false)
    {
        // One revision represents a complete paint stroke, rectangle, or contextual property edit.
        if (string.IsNullOrEmpty(before) || before == after)
            return;
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        // Continuous slider updates retain the first revision of the gesture instead of filling the history.
        if (!coalesce || !_lastWasCoalesced || now - _lastRecordTime > System.Diagnostics.Stopwatch.Frequency * 0.3)
            Undoable.AddLast(before);
        _lastWasCoalesced = coalesce;
        _lastRecordTime = now;
        while (Undoable.Count > Capacity)
            Undoable.RemoveFirst();
        Redoable.Clear();
    }

    /// <summary>
    /// Starts a history isolated from every previous map.
    /// </summary>
    public static void Clear()
    {
        // A newly opened workspace cannot undo changes made to another document.
        Undoable.Clear();
        Redoable.Clear();
        _lastWasCoalesced = false;
    }

    /// <summary>
    /// Returns the preceding revision while saving the current revision for redo.
    /// </summary>
    public static string Undo(string current)
    {
        // Boundary operations leave the document unchanged.
        if (!CanUndo)
            return current;
        Redoable.Push(current);
        _lastWasCoalesced = false;
        string result = Undoable.Last.Value;
        Undoable.RemoveLast();
        return result;
    }

    /// <summary>
    /// Returns the next revision and makes the current document undoable again.
    /// </summary>
    public static string Redo(string current)
    {
        // Redo is invalidated automatically when a different mutation is recorded.
        if (!CanRedo)
            return current;
        Undoable.AddLast(current);
        _lastWasCoalesced = false;
        return Redoable.Pop();
    }
}
