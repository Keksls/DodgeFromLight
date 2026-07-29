using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Owns the local player save and its disk persistence.
/// </summary>
public static class SaveManager
{
    private const string SaveFileName = "player-save.json";
    private static bool initialized;

    public static PlayerSave CurrentSave { get; private set; }

    /// <summary>
    /// Loads the local player save once per application session.
    /// </summary>
    public static void Initialize()
    {
        // Fall back to a complete default save when no valid disk data exists.
        if (initialized)
            return;

        initialized = true;
        CurrentSave = Load() ?? GetDefaultSave();
        CurrentSave.Normalize();
        OrderSkins();
        Save();
        Events.Fire_SaveGetted();
    }

    /// <summary>
    /// Creates the default progression granted to a new local player.
    /// </summary>
    public static PlayerSave GetDefaultSave()
    {
        // Grant the former tutorial bundle locally instead of requesting it from a server.
        var save = CreateDefaultSave();
        if (GameContext.Databases != null &&
            GameContext.Databases.RewardData != null)
        {
            foreach (var reward in GameContext.Databases.RewardData.GetTutorialRewards())
                save.Unlock(reward.SkinType, (short)reward.SkinID);

            foreach (var reward in GameContext.Databases.RewardData.GetTutorialRewards())
                save.SetCurrentPart(reward.SkinType, (short)reward.SkinID);

            save.Experience = GameContext.Databases.RewardData.GetTutorialXP();
        }

        save.LastRewardLevel = 6;
        save.SetCurrentPart(SkinType.Ornament, 0);
        return save;
    }

    /// <summary>
    /// Creates an empty save with every cosmetic slot initialized.
    /// </summary>
    public static PlayerSave CreateDefaultSave()
    {
        // PlayerSave owns collection normalization, so creation has one source of truth.
        return new PlayerSave();
    }

    /// <summary>
    /// Unlocks a cosmetic and persists the updated inventory.
    /// </summary>
    public static void Unlock(SkinType type, short id)
    {
        // Ignore duplicate unlocks while still keeping disk state current.
        EnsureInitialized();
        CurrentSave.Unlock(type, id);
        Save();
        Events.Fire_SaveUpdated();
    }

    /// <summary>
    /// Determines whether the local player owns a cosmetic.
    /// </summary>
    public static bool IsUnlocked(SkinType type, short id)
    {
        // Delegate ownership checks to the normalized save model.
        EnsureInitialized();
        return CurrentSave.IsUnlocked(type, id);
    }

    /// <summary>
    /// Equips or clears a cosmetic and persists the selection.
    /// </summary>
    public static void SetCurrentPart(SkinType type, short id)
    {
        // Cosmetic changes are local and immediately durable.
        EnsureInitialized();
        CurrentSave.SetCurrentPart(type, id);
        Save();
        Events.Fire_SaveUpdated();
    }

    /// <summary>
    /// Adds experience to the local progression.
    /// </summary>
    public static void AddExperience(int amount)
    {
        // Prevent negative rewards from reducing progression accidentally.
        EnsureInitialized();
        CurrentSave.Experience += Math.Max(0, amount);
        Save();
        Events.Fire_WinXP(amount);
        Events.Fire_SaveUpdated();
    }

    /// <summary>
    /// Persists the current local save to the platform data folder.
    /// </summary>
    public static void Save()
    {
        // Write through a temporary file to avoid truncating the active save on interruption.
        if (CurrentSave == null)
            return;

        var savePath = GetSavePath();
        var temporaryPath = savePath + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
        File.WriteAllText(
            temporaryPath,
            JsonConvert.SerializeObject(CurrentSave, Formatting.Indented));
        File.Copy(temporaryPath, savePath, true);
        File.Delete(temporaryPath);
    }

    /// <summary>
    /// Loads and validates the save stored on disk.
    /// </summary>
    private static PlayerSave Load()
    {
        // Invalid or interrupted saves fall back safely without blocking startup.
        var savePath = GetSavePath();
        if (!File.Exists(savePath))
            return null;

        try
        {
            var save = JsonConvert.DeserializeObject<PlayerSave>(
                File.ReadAllText(savePath));
            save?.Normalize();
            return save;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Unable to load local player save: " + exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Orders owned cosmetics by rarity for stable inventory presentation.
    /// </summary>
    private static void OrderSkins()
    {
        // Skip presentation ordering until the runtime databases are available.
        var rewardData = GameContext.Databases?.RewardData;
        if (rewardData == null)
            return;

        foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
        {
            var ordered = CurrentSave
                .GetUnlocked(type)
                .OrderBy(id => rewardData.GetRarity(type, id))
                .ToList();
            CurrentSave.Unlocked[type] = ordered;
        }
    }

    /// <summary>
    /// Ensures callers can use the save manager before explicit bootstrap.
    /// </summary>
    private static void EnsureInitialized()
    {
        // Preserve the existing static access pattern while centralizing initialization.
        if (!initialized)
            Initialize();
    }

    /// <summary>
    /// Resolves the platform-specific local save path.
    /// </summary>
    private static string GetSavePath()
    {
        // Application.persistentDataPath is compatible with later Steam Cloud syncing.
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }
}
