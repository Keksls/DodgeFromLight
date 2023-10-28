using DFLCommonNetwork.GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Patrol Patrol;
    public bool invertWayPoints;
    public CellPos StartCell;
    public Orientation StartOrientation;
    public List<CellPos> WayPoints;
    public int NbPMUsed = 0;
    public bool Moving = false;
    public int CurrentTA = 0;
    public int TABase = 1;
    public Cell CurrentCell;
    public Cell LastCell;
    public Orientation Orientation;
    public bool HasPlayerOnIt { get { return DodgeFromLight.GameManager.PlayerController.CurrentCell.Equals(CurrentCell); } }
    private bool initialized = false;
    private int oldBaseTA;
    private int nbTurnsFreezeRemaning = 0;

    private void Awake()
    {
        invertWayPoints = false;
        NbPMUsed = 0;
        Initialize();
    }
    public virtual void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart;
        Events.TurnEnd -= Events_TurnEnd;
    }

    public void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        Moving = false;
        CurrentTA = TABase;
        oldBaseTA = TABase;

        Events.TurnStart -= Events_TurnStart;
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnStart += Events_TurnStart;
        Events.TurnEnd += Events_TurnEnd;
    }

    public void StartMap()
    {
        CurrentTA = TABase;
    }

    private void Events_TurnEnd()
    {
        if (nbTurnsFreezeRemaning > 0)
        {
            nbTurnsFreezeRemaning--;
            if (nbTurnsFreezeRemaning == 0)
                TABase = oldBaseTA;
        }
        CurrentTA = TABase;
    }

    internal virtual void Events_TurnStart()
    {
        CurrentTA = TABase;
    }

    public void Freeze(int nbTurns)
    {
        TABase = 0;
        nbTurnsFreezeRemaning = nbTurns;
    }

    /// <summary>
    /// Use all PM for the ennemy
    /// </summary>
    public void UsePM(float speed)
    {
        StopCoroutine("UsePM_Routine");
        StartCoroutine(UsePM_Routine(speed));
    }

    /// <summary>
    /// place the ennemy on the first cell of his patrol routine
    /// </summary>
    public void PlaceOnFirstCell()
    {
        if (Patrol.NbCells == 0)
        {
            Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(StartCell);
            PlaceOnCell(cell, true);
            SetOrientation(StartOrientation);
        }
        else
        {
            Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(Patrol.FirstCell);
            PlaceOnCell(cell, true);
            SetOrientation(Patrol.FirstOrientation);
        }
    }

    /// <summary>
    /// will this ennemy change cell on next move
    /// </summary>
    /// <returns></returns>
    public bool WillChangeCellNextMove()
    {
        if (CurrentTA <= 0)
            return false;
        return !Patrol.SimulateLastCell(CurrentTA, Orientation).Equals(CurrentCell);
    }

    IEnumerator UsePM_Routine(float speed)
    {
        bool refreshAllPO = CurrentTA > 1;

        while (CurrentTA > 0 && Patrol.NbCells > 0)
        {
            CellPos nextCell = Patrol.NextCell();// next cell is in ennemy orientation, just walk toward
                                                 //if (nextCell.IsInOrientation(CurrentCell.GetCellPos(), Orientation))
                                                 //{
            Cell cell = DodgeFromLight.CurrentMap.Grid.GetCell(nextCell);
            if (cell == null)
                yield break;

            // fire walking event
            DodgeFromLight.StartWaitingAction();
            Moving = true;
            // cancel current movement
            StopCoroutine("MoveToCell_Routine");
            PlaceOnCell(cell, false);
            // move to target cell
            yield return StartCoroutine(MoveToCell_Routine(cell, speed, null, null));
            PlaceOnCell(cell, true);
            DodgeFromLight.StopWaitingAction();
            //}
            //else // next cell is not orientation, need to turn
            //{
            //    Patrol.CancelNextCell();
            //    DodgeFromLight.StartWaitingAction();
            //    //float enlapsed = 0f;
            //    //Vector3 startRot = transform.localEulerAngles;
            //    //Vector3 endRot = DodgeFromLight.CurrentMap.Grid.GetOrientationVector(
            //    //    DodgeFromLight.CurrentMap.Grid.GetOrientation(CurrentCell.GetCellPos(), nextCell));
            //    //float rotWeight = Mathf.Abs(endRot.y - startRot.y);
            //    //float inversedRotWeight = Mathf.Abs((360 + endRot.y) - startRot.y);
            //    //if (inversedRotWeight < rotWeight)
            //    //    endRot.y += 360;
            //    //while (enlapsed < speed)
            //    //{
            //    //    transform.localEulerAngles = Vector3.Lerp(startRot, endRot, enlapsed / speed);
            //    //    enlapsed += Time.deltaTime;
            //    //    yield return null;
            //    //}
            //    SetOrientation(DodgeFromLight.CurrentMap.Grid.GetOrientation(CurrentCell.GetCellPos(), nextCell));
            //    CurrentTA--;
            //    DodgeFromLight.StopWaitingAction();
            //}
            NbPMUsed++;
        }
    }
    /// <summary>
    /// place the entity on the cell
    /// </summary>
    /// <param name="cell">cell to place on</param>
    public void PlaceOnCell(Cell cell, bool placeTransform)
    {
        LastCell = CurrentCell;
        CurrentCell = cell;
        if (placeTransform)
            transform.position = new Vector3(cell.X, 0f, cell.Y);
    }

    /// <summary>
    /// set the entity orientation
    /// </summary>
    /// <param name="orientation">orientation</param>
    public void SetOrientation(Orientation orientation)
    {
        Orientation = orientation;
        transform.localEulerAngles = DodgeFromLight.CurrentMap.Grid.GetOrientationVector(Orientation);
    }

    /// <summary>
    /// move entity to the given direction
    /// </summary>
    /// <param name="dir">direction to move</param>
    public void MoveToDirection(Orientation orientation)
    {
        // get next cell
        Cell targetCell = DodgeFromLight.CurrentMap.Grid.GetNextCell(CurrentCell, orientation, CurrentTA);
        GoToCell(targetCell, DodgeFromLight.GameManager.EntitiesMovementDuration);
    }

    public void GoToCell(Cell cell, float duration, Action Callback = null, Action EachFrameCallback = null)
    {
        if (cell == null)
        {
            Callback?.Invoke();
            return;
        }

        PlaceOnCell(cell, false);
        Moving = true;
        // cancel current movement
        StopCoroutine("MoveToCell_Routine");
        // move to target cell
        StartCoroutine(MoveToCell_Routine(cell, duration, Callback, EachFrameCallback));
    }

    /// <summary>
    /// Stop movements for this turn
    /// </summary>
    public void StopMovementsForThisTurn()
    {
        CurrentTA = 0;
    }

    /// <summary>
    /// smooth movement of entity based on target cell
    /// </summary>
    /// <param name="cell">target cell</param>
    /// <param name="duration">movement duration</param>
    /// <returns></returns>
    internal IEnumerator MoveToCell_Routine(Cell cell, float duration, Action Callback = null, Action EachFrameCallback = null)
    {
        DodgeFromLight.StartWaitingAction();
        float enlapsed = 0.0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(cell.X, 0f, cell.Y);
        while (enlapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, enlapsed / duration);
            enlapsed += Time.deltaTime;
            yield return null;
            EachFrameCallback?.Invoke();
        }
        CurrentTA--;
        transform.position = endPos;
        DodgeFromLight.StopWaitingAction();
        Moving = false;
        Callback?.Invoke();
    }

    /// <summary>
    /// Draw ennemy Path (Must be call on a script attached to a Camera component that implement OnPostRender)
    /// </summary>
    public void DrawPath(Transform tfrm)
    {
        GL.PushMatrix();
        DodgeFromLight.GridController.lineMaterial.SetPass(0);
        GL.MultMatrix(tfrm.localToWorldMatrix);

        // Draw lines
        if (WayPoints.Count > 1)
        {
            GL.Begin(GL.LINES);
            for (int i = 0; i < WayPoints.Count; i++)
            {
                GL.Color(Color.black);
                CellPos startCell = WayPoints[i % WayPoints.Count];
                CellPos endCell = WayPoints[(i + 1) % WayPoints.Count];
                GL.Vertex(new Vector3(startCell.X, .04f, startCell.Y));
                GL.Vertex(new Vector3(endCell.X, .04f, endCell.Y));
            }
            GL.End();
        }

        // Draw Quad
        GL.Begin(GL.QUADS);
        float minSize = DodgeFromLight.GridController.PathSquareSize / DodgeFromLight.GridController.PathSquareOffsetSize / 6f;
        float maxSize = DodgeFromLight.GridController.PathSquareSize * DodgeFromLight.GridController.PathSquareOffsetSize / 6f;
        for (float i = 0; i < WayPoints.Count; i++)
        {
            float size = Mathf.Lerp(minSize, maxSize, i / (float)WayPoints.Count);

            GL.Color(Color.black);
            CellPos startCell = WayPoints[(int)i];

            GL.Vertex(new Vector3(startCell.X - size, .04f, startCell.Y + size));
            GL.Vertex(new Vector3(startCell.X + size, .04f, startCell.Y + size));
            GL.Vertex(new Vector3(startCell.X + size, .04f, startCell.Y - size));
            GL.Vertex(new Vector3(startCell.X - size, .04f, startCell.Y - size));
        }
        GL.End();

        GL.PopMatrix();
    }

    /// <summary>
    /// return the inverse of the giver orientation
    /// </summary>
    /// <param name="dir">orientation to inverse</param>
    /// <returns>inversed orientation</returns>
    public Orientation GetInversedOrientation(Orientation dir)
    {
        switch (dir)
        {
            default:
            case Orientation.None:
                return Orientation.None;

            case Orientation.Up:
                return Orientation.Down;
            case Orientation.Right:
                return Orientation.Left;
            case Orientation.Down:
                return Orientation.Up;
            case Orientation.Left:
                return Orientation.Right;
        }
    }

    public SerializableMovingPlatform ToSerializable()
    {
        SerializableMovingPlatform serializable = new SerializableMovingPlatform()
        {
            StartOrientation = StartOrientation,
            StartPos = StartCell,
            TA = TABase,
            WayPoints = WayPoints
        };

        return serializable;
    }
}

public class SerializableMovingPlatform
{
    public Orientation StartOrientation { get; set; }
    public CellPos StartPos { get; set; }
    public List<CellPos> WayPoints { get; set; }
    public int TA { get; set; }

    public SerializableMovingPlatform() { }

    public MovingPlatform ToMovingPlatform(GameObject go, Grid grid)
    {
        MovingPlatform platform = null;
        platform = go.AddComponent<MovingPlatform>();

        platform.StartCell = StartPos;
        platform.StartOrientation = StartOrientation;
        platform.Patrol = new Patrol();
        platform.Patrol.SetWayPoints(WayPoints, grid);
        platform.WayPoints = WayPoints;
        platform.TABase = TA;
        return platform;
    }
}