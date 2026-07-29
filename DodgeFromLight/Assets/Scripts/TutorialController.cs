using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Owns the local tutorial map catalogue and persisted progression.
/// </summary>
public class TutorialController : MonoBehaviour
{
    private const string ProgressKey = "TutorialStepIndex";

    public string[] TutorialMapsFolderNames = System.Array.Empty<string>();

    private readonly Dictionary<string, FullMap> _maps =
        new Dictionary<string, FullMap>();

    /// <summary>
    /// Registers the tutorial service and loads its authored local maps.
    /// </summary>
    private void Awake()
    {
        // Tutorial visuals are rendered by the solo DrawList presenter.
        GameContext.TutorialController = this;
        _maps.Clear();
        foreach (string folderName in TutorialMapsFolderNames)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                Debug.LogWarning("An empty tutorial map folder was ignored.");
                continue;
            }

            if (_maps.ContainsKey(folderName))
            {
                Debug.LogWarning(
                    "The duplicate tutorial map folder '" +
                    folderName +
                    "' was ignored.");
                continue;
            }

            string folderPath =
                Path.Combine(Application.streamingAssetsPath, folderName);
            _maps.Add(
                folderName,
                new FullMap(
                    GridManager.GetGrid(folderPath),
                    GridManager.GetMap(folderPath)));
        }
    }

    /// <summary>
    /// Removes all persisted tutorial progression.
    /// </summary>
    public void ResetProgress()
    {
        // Resetting returns the next local session to the first tutorial map.
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Persists the completed tutorial step index.
    /// </summary>
    public void SetProgress(int stepIndex)
    {
        // Progress is clamped to preserve a valid catalogue index.
        int maximum = Mathf.Max(-1, TutorialMapsFolderNames.Length - 1);
        PlayerPrefs.SetInt(ProgressKey, Mathf.Clamp(stepIndex, -1, maximum));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Gets the completed tutorial step index, or minus one before the first completion.
    /// </summary>
    public int GetProgress()
    {
        // Absence of the key represents a pristine tutorial.
        return PlayerPrefs.HasKey(ProgressKey)
            ? PlayerPrefs.GetInt(ProgressKey)
            : -1;
    }

    /// <summary>
    /// Reports whether at least one tutorial map has been completed.
    /// </summary>
    public bool HasStartedTutorial()
    {
        // Persisted progress is the only source of tutorial advancement.
        return GetProgress() >= 0;
    }

    /// <summary>
    /// Reports whether the complete authored tutorial sequence has been finished.
    /// </summary>
    public bool HasFinishedTutorial()
    {
        // An empty catalogue cannot represent a completed tutorial.
        return TutorialMapsFolderNames.Length > 0 &&
               GetProgress() >= TutorialMapsFolderNames.Length - 1;
    }

    /// <summary>
    /// Gets the latest completed tutorial map when one exists.
    /// </summary>
    public FullMap GetCurrentMap()
    {
        // Invalid or pristine progress returns no map instead of indexing outside the catalogue.
        int progress = GetProgress();
        if (progress < 0 || progress >= TutorialMapsFolderNames.Length)
            return null;

        string folderName = TutorialMapsFolderNames[progress];
        return _maps.TryGetValue(folderName, out FullMap map)
            ? map
            : null;
    }

    /// <summary>
    /// Commits completion of the current tutorial map.
    /// </summary>
    public void CompleteCurrentMap()
    {
        // Completion advances by one while SetProgress applies the catalogue bound.
        SetProgress(GetProgress() + 1);
    }

    /// <summary>
    /// Gets the ordered tutorial folders that still need to be completed.
    /// </summary>
    public List<string> GetRemainingMapFolders()
    {
        // The next folder begins immediately after the latest completed step.
        var folders = new List<string>();
        int nextIndex = Mathf.Max(0, GetProgress() + 1);
        for (int index = nextIndex;
             index < TutorialMapsFolderNames.Length;
             index++)
        {
            folders.Add(TutorialMapsFolderNames[index]);
        }

        return folders;
    }
}
