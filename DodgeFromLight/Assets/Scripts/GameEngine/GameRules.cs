using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the ordered local maps and traversal behavior for one play session.
/// </summary>
public sealed class GameRules
{
    public bool Endless;
    public bool Random;
    public bool Tutorial;
    public List<string> Grids { get; private set; }
    public int CurrentGrid { get; private set; }
    public int NbGrids => Grids.Count;
    public event Action OnEnd;

    /// <summary>
    /// Creates an empty finite local ruleset.
    /// </summary>
    public GameRules()
        : this(false, false, new List<string>())
    {
        // Delegate initialization to the complete constructor.
    }

    /// <summary>
    /// Creates an empty local ruleset with traversal options.
    /// </summary>
    public GameRules(bool endless, bool random)
        : this(endless, random, new List<string>())
    {
        // Delegate initialization to the complete constructor.
    }

    /// <summary>
    /// Creates a local ruleset containing one map.
    /// </summary>
    public GameRules(bool endless, bool random, string gridName)
        : this(endless, random, new List<string> { gridName })
    {
        // Keep the single-map entry point used by editor playtesting.
    }

    /// <summary>
    /// Creates a local ruleset containing an ordered map list.
    /// </summary>
    public GameRules(bool endless, bool random, List<string> grids)
    {
        // Copy the supplied list so session traversal cannot mutate caller state.
        Grids = grids == null ? new List<string>() : new List<string>(grids);
        Endless = endless;
        Random = random;
        Tutorial = false;
        CurrentGrid = 0;
    }

    /// <summary>
    /// Replaces the ordered local map list.
    /// </summary>
    public GameRules SetGrids(List<string> grids)
    {
        // Reset traversal when a new authored sequence is selected.
        Grids = grids == null ? new List<string>() : new List<string>(grids);
        CurrentGrid = 0;
        return this;
    }

    /// <summary>
    /// Configures a finite ordered tutorial session.
    /// </summary>
    public GameRules SetTutorial(List<string> gridFolders)
    {
        // Tutorial identifiers resolve inside the built-in StreamingAssets folder.
        SetGrids(gridFolders);
        Random = false;
        Tutorial = true;
        Endless = false;
        return this;
    }

    /// <summary>
    /// Appends one local map identifier to the session.
    /// </summary>
    public GameRules AddGrid(string grid)
    {
        // Preserve authored order for deterministic runs and future speedrun timing.
        Grids.Add(grid);
        return this;
    }

    /// <summary>
    /// Advances to the next map according to the local traversal options.
    /// </summary>
    public void NextGrid()
    {
        // End finite sessions or wrap and optionally randomize endless sessions.
        if (NbGrids == 0)
        {
            OnEnd?.Invoke();
            return;
        }

        CurrentGrid++;
        if (!Endless && CurrentGrid >= NbGrids)
        {
            CurrentGrid = NbGrids - 1;
            OnEnd?.Invoke();
            return;
        }

        if (Endless)
            CurrentGrid %= NbGrids;
        if (Random)
            CurrentGrid = UnityEngine.Random.Range(0, NbGrids);
    }

    /// <summary>
    /// Gets the stable identifier of the active map.
    /// </summary>
    public string GetCurrentGridID()
    {
        // Return the selected authored identifier without network indirection.
        return Grids[CurrentGrid];
    }

    /// <summary>
    /// Loads the active built-in tutorial or locally authored map.
    /// </summary>
    public void GetCurrentFullMap(Action<FullMap> callback)
    {
        // Resolve tutorial content in place and local content through the workspace.
        if (NbGrids == 0)
        {
            callback?.Invoke(null);
            return;
        }

        if (Tutorial)
        {
            var tutorialFolder = System.IO.Path.Combine(
                Application.streamingAssetsPath,
                "Tutorial",
                GetCurrentGridID());
            callback?.Invoke(GridManager.GetFullMap(tutorialFolder));
            return;
        }

        DodgeFromLight.UI_WorkerNotifier?.Show("Loading local map...");
        var opened = LocalMapRepository.Open(GetCurrentGridID());
        callback?.Invoke(opened ? GridManager.GetFullMap() : null);
        DodgeFromLight.UI_WorkerNotifier?.Hide();
    }
}
