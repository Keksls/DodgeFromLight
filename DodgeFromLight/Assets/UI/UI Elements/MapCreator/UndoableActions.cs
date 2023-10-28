using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;

public abstract class UndoableAction
{
    public abstract void Undo();

    public abstract void Redo();
}

public class Undoable_SetCellType : UndoableAction
{
    CellPos CellPos;
    CellType PreviewCellType;
    CellType NewCellType;

    public Undoable_SetCellType(CellPos pos, CellType previewsCellType, CellType newCellType)
    {
        CellPos = pos;
        PreviewCellType = previewsCellType;
        NewCellType = newCellType;
    }

    public override void Redo()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(CellPos);
        cell.SetType(NewCellType);
        cell.SetWalkable(NewCellType != CellType.NotWalkable);
    }

    public override void Undo()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(CellPos);
        cell.SetType(PreviewCellType);
        cell.SetWalkable(PreviewCellType != CellType.NotWalkable);
    }
}

public class Undoable_SetFloorType : UndoableAction
{
    CellPos CellPos;
    string PreviewCellType;
    string NewCellType;

    public Undoable_SetFloorType(CellPos pos, string previewsCellType, string newCellType)
    {
        CellPos = pos;
        PreviewCellType = previewsCellType;
        NewCellType = newCellType;
    }

    public override void Redo()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(CellPos);
        cell.FloorID = NewCellType;
    }

    public override void Undo()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(CellPos);
        cell.FloorID = PreviewCellType;
    }
}

public class Undoable_SetElement : UndoableAction
{
    CellPos CellPos;
    string elementID;
    string newElementID;

    public Undoable_SetElement(CellPos pos, string prevID, string newID)
    {
        CellPos = pos;
        elementID = prevID;
        newElementID = newID;
    }

    public override void Redo()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(CellPos);
        if (string.IsNullOrEmpty(newElementID))
            cell.RemoveElement();
        else
            cell.SetElement(newElementID);
    }

    public override void Undo()
    {
        Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(CellPos);
        if (string.IsNullOrEmpty(elementID))
            cell.RemoveElement();
        else
            cell.SetElement(elementID);
    }
}

public static class UndoableActionsManager
{
    public static Stack<UndoableAction> Undoable;
    public static Stack<UndoableAction> Redoable;

    static UndoableActionsManager()
    {
        Undoable = new Stack<UndoableAction>();
        Redoable = new Stack<UndoableAction>();
    }

    public static void AddUndoableAction(UndoableAction action)
    {
        Undoable.Push(action);
    }

    public static void Clear()
    {
        Undoable.Clear();
        Redoable.Clear();
    }

    public static void Undo()
    {
        if (Undoable.Count == 0)
            return;
        UndoableAction undoable = Undoable.Pop();
        Redoable.Push(undoable);
        undoable.Undo();
    }

    public static void Redo()
    {
        if (Redoable.Count == 0)
            return;
        UndoableAction undoable = Redoable.Pop();
        Undoable.Push(undoable);
        undoable.Redo();
    }
}