using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using DFLCommonNetwork.GameEngine;

public class Entity : MonoBehaviour
{
    public Cell CurrentCell;
    public Cell LastCell;
    public string Name;
    public Orientation Orientation;
    public bool Moving { get; internal set; }
    public int TABase = 1;
    internal int CurrentTA;
    public int PO;
    public Animator Animator;
    public bool AnimatorLocked { get; private set; }
    private string NextClipToPlay = "Idle";
    public bool Invisible = false;
    public bool CantWalkOnMe = false;
    private bool initialized = false;
    internal Action StartMove;
    internal Action EndMove;

    private List<Alteration> Alterations;

    public virtual void Awake()
    {
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

        Events.TurnStart -= Events_TurnStart;
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnStart += Events_TurnStart;
        Events.TurnEnd += Events_TurnEnd;

        Alterations = new List<Alteration>();
    }

    public void StartMap()
    {
        Alterations = new List<Alteration>();
        CurrentTA = TABase;
    }

    internal virtual void Events_TurnEnd()
    {
        CheckUnDoAlterations();
        CurrentTA = TABase;
    }

    internal virtual void Events_TurnStart()
    {
        CurrentTA = TABase;
    }

    public void AddAlteration(Alteration alteration)
    {
        Alterations.Add(alteration);
        alteration.DoAlteration(this);
    }

    public void EndAllAlterations()
    {
        if (Alterations != null && Alterations.Count > 0)
        {
            foreach (Alteration alteration in Alterations)
                alteration.UnDoAlteration(this);
            Alterations.Clear();
        }
    }

    public void CheckUnDoAlterations()
    {
        if (Alterations.Count > 0)
        {
            List<Alteration> toRem = new List<Alteration>();
            foreach (Alteration alteration in Alterations)
            {
                alteration.NbTurnRemaning--;
                if (alteration.NbTurnRemaning <= 0)
                {
                    alteration.UnDoAlteration(this);
                    toRem.Add(alteration);
                }
            }
            foreach (Alteration alteration in toRem)
                Alterations.Remove(alteration);
        }
    }

    /// <summary>
    /// place the entity on the cell
    /// </summary>
    /// <param name="cell">cell to place on</param>
    public void PlaceOnCell(Cell cell, bool placeTransform)
    {
        if (CurrentCell != null)
            DodgeFromLight.CurrentMap.Grid.GetCell(CurrentCell.X, CurrentCell.Y).EntityOnCell = null;
        LastCell = CurrentCell;
        CurrentCell = cell;
        if (placeTransform)
            transform.position = new Vector3(cell.X, 0f, cell.Y);
        DodgeFromLight.CurrentMap.Grid.GetCell(CurrentCell.X, CurrentCell.Y).EntityOnCell = this;
        Play("Idle");
    }

    /// <summary>
    /// set the entity orientation
    /// </summary>
    /// <param name="orientation">orientation</param>
    public void SetOrientation(Orientation orientation)
    {
        Orientation = orientation;
        transform.localEulerAngles = DodgeFromLight.CurrentMap.Grid.GetOrientationVector(Orientation);
        Play("Idle");
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
        // fire walking event
        Events.Fire_EntityStartMove(this);
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
        StartMove?.Invoke();
        Play("Walk");
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
        Events.Fire_EntityEndMove(this);
        Moving = false;
        Callback?.Invoke();
        Play("Idle");
        EndMove?.Invoke();
    }

    /// <summary>
    /// Play animation clip
    /// </summary>
    /// <param name="Clip">clip to play</param>
    /// <param name="lockAnimator">wait for end on clip</param>
    public void Play(string Clip, bool lockAnimator = false, float duration = -1f, bool smooth = true, float smoothDuration = 0.1f, bool lockedWaitEnd = true)
    {
        if (Animator == null)
            return;

        if (!AnimatorLocked)
        {
            if (smooth)
                Animator.CrossFade(Clip, smoothDuration);
            else
                Animator.Play(Clip);
            if (lockAnimator)
            {
                AnimatorLocked = true;
                if (duration == -1f)
                    StartCoroutine(UnlockAnimator(AnimationLength(Clip), lockedWaitEnd));
                else
                    StartCoroutine(UnlockAnimator(duration, lockedWaitEnd));
                NextClipToPlay = "Idle";
            }
        }
        else
            NextClipToPlay = Clip;
    }

    internal float AnimationLength(string name)
    {
        float time = 0;
        RuntimeAnimatorController ac = Animator.runtimeAnimatorController;

        for (int i = 0; i < ac.animationClips.Length; i++)
            if (ac.animationClips[i].name.ToLower().Contains(name.ToLower()))
                time = ac.animationClips[i].length;

        return time;
    }

    public void LookAtEntity(Entity entity)
    {
        transform.LookAt(entity.transform);
        Vector3 rot = transform.localEulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.localEulerAngles = rot;
    }

    public void LookAtCell(CellPos pos)
    {
        transform.LookAt(pos.ToVector3(0f));
        Vector3 rot = transform.localEulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.localEulerAngles = rot;
    }

    public void UnlockAnimator()
    {
        StopCoroutine("UnlockAnimator");
        AnimatorLocked = false;
        Play(NextClipToPlay);
    }

    IEnumerator UnlockAnimator(float duration, bool lockedWaitEnd)
    {
        if (lockedWaitEnd)
            DodgeFromLight.StartWaitingAction();
        yield return new WaitForSeconds(duration);
        AnimatorLocked = false;
        Play(NextClipToPlay);
        if (lockedWaitEnd)
            DodgeFromLight.StopWaitingAction();
    }

    #region Stats
    /// <summary>
    /// get entity current PM
    /// </summary>
    /// <returns></returns>
    public int GetCurrentPM()
    {
        return CurrentTA;
    }
    #endregion
}

public class EntityStats
{
    public int PM;
    public int PO;

    public static EntityStats operator +(EntityStats a, EntityStats b)
    {
        EntityStats c = new EntityStats();
        c.PM = a.PM + b.PM;
        c.PO = a.PO + b.PO;
        return c;
    }
}

public enum AlterationType
{
    PM,
    PO
}

public class Alteration
{
    public AlterationType Type;
    public int Value;
    public int NbTurnRemaning;
    public Action RemoveAlterationCallback;

    public Alteration(AlterationType type, int value, int nbTurnRemaning, Action callback = null)
    {
        Type = type;
        Value = value;
        NbTurnRemaning = nbTurnRemaning;
        RemoveAlterationCallback = callback;
    }

    public void DoAlteration(Entity entity)
    {
        switch (Type)
        {
            case AlterationType.PM:
                entity.TABase += Value;
                break;
            case AlterationType.PO:
                entity.PO += Value;
                break;
        }
    }

    public void UnDoAlteration(Entity entity)
    {
        switch (Type)
        {
            case AlterationType.PM:
                entity.TABase -= Value;
                break;
            case AlterationType.PO:
                entity.PO -= Value;
                break;
        }
        RemoveAlterationCallback?.Invoke();
    }
}