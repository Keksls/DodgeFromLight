using System;
using System.Collections.Generic;

/// <summary>
/// Creates the local gameplay session used by direct and menu-driven launches.
/// </summary>
public static class LocalGameSessionFactory
{
    /// <summary>
    /// Creates rules for the tutorial remainder, the latest local map, or tutorial replay.
    /// </summary>
    public static GameRules CreateDefault(TutorialController tutorialController)
    {
        // Main requires at least one map, so every valid startup path returns a populated ruleset.
        if (tutorialController == null)
            throw new ArgumentNullException(nameof(tutorialController));

        if (!tutorialController.HasFinishedTutorial())
        {
            return new GameRules()
                .SetTutorial(tutorialController.GetRemainingMapFolders());
        }

        Map latestLocalMap = LocalMapRepository.GetMostRecent();
        if (latestLocalMap != null)
            return new GameRules(false, false, latestLocalMap.ID);

        var tutorialReplay = new List<string>(
            tutorialController.TutorialMapsFolderNames);
        if (tutorialReplay.Count == 0)
        {
            throw new InvalidOperationException(
                "No tutorial or local map is available for direct gameplay.");
        }

        // Replay the complete built-in sequence without advancing finished save progression.
        return new GameRules().SetTutorial(tutorialReplay, false);
    }
}
