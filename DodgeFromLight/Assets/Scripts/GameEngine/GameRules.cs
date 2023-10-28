using System;
using System.Collections.Generic;
using UnityEngine;

public class GameRules
{
    public bool Endless;
    public bool DiscoveryMode;
    public bool DontShowScore;
    public string DiscoveryMapID;
    public List<string> Grids { get; private set; }
    public bool Tutorial;
    public bool Random;
    public int NbGrids { get { return Grids.Count; } }
    public int CurrentGrid { get; private set; }
    public event Action OnEnd;

    public GameRules()
    {
        Grids = new List<string>();
        Endless = false;
        Random = false;
        CurrentGrid = 0;
        Tutorial = false;
    }

    public GameRules(bool endless, bool random)
    {
        Grids = new List<string>();
        Endless = endless;
        Random = random;
        CurrentGrid = 0;
        Tutorial = false;
    }

    public GameRules(bool endless, bool random, string gridName)
    {
        Grids = new List<string>();
        Grids.Add(gridName);
        Endless = endless;
        Random = random;
        Tutorial = false;
        CurrentGrid = 0;
    }

    public GameRules(bool endless, bool random, List<string> grids)
    {
        Grids = grids;
        Endless = endless;
        Random = random;
        CurrentGrid = 0;
        Tutorial = false;
    }

    public GameRules SetGrids(List<string> grids)
    {
        Grids = grids;
        CurrentGrid = 0;
        return this;
    }

    public GameRules SetTutorial(List<string> gridFolders)
    {
        Grids = gridFolders;
        Random = false;
        Tutorial = true;
        Endless = false;
        CurrentGrid = 0;
        return this;
    }

    public GameRules AddGrid(string grid)
    {
        if (Grids == null)
            Grids = new List<string>();
        Grids.Add(grid);
        return this;
    }

    public GameRules Discovery(string gridID)
    {
        DiscoveryMapID = gridID;
        DiscoveryMode = true;
        Endless = false;
        Random = false;
        Grids.Add("tmp_discovery");
        return this;
    }

    public void NextGrid()
    {
        CurrentGrid++;
        if (!Endless && CurrentGrid == NbGrids)
            OnEnd?.Invoke();
        else
        {
            if (Endless)
                CurrentGrid %= NbGrids;
            if (Random)
                CurrentGrid = UnityEngine.Random.Range(0, NbGrids);
        }
    }

    public string GetCurrentGridID()
    {
        return Grids[CurrentGrid];
    }

    public void GetCurrentFullMap(Action<FullMap> Callback)
    {
        if (Tutorial)
            Callback?.Invoke(GridManager.GetFullMap(Application.streamingAssetsPath + @"\Tutorial\" + Grids[CurrentGrid]));
        else
        {
            // get map before play it
            DodgeFromLight.UI_WorkerNotifier?.Show("Getting map...");
            DFLClient.DownloadMap(DiscoveryMode ? DiscoveryMapID : Grids[CurrentGrid], GridManager.Folder, (res) =>
            {
                if (res.Error)
                {
                    DodgeFromLight.UI_Notifications?.Notify("Error getting map");
                    Callback?.Invoke(null);
                }
                else
                {
                    Callback?.Invoke(GridManager.GetFullMap());
                }
                DodgeFromLight.UI_WorkerNotifier?.Hide();
            }); ;
        }
    }
}