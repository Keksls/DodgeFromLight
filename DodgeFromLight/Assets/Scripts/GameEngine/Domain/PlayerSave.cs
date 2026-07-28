using System;
using System.Collections.Generic;

/// <summary>
/// Stores the local player's progression and cosmetic choices.
/// </summary>
[Serializable]
public sealed class PlayerSave
{
    public string DisplayName { get; set; }
    public int Experience { get; set; }
    public short LastRewardLevel { get; set; }
    public Dictionary<SkinType, short> Equipped { get; set; }
    public Dictionary<SkinType, List<short>> Unlocked { get; set; }

    /// <summary>
    /// Creates an empty, structurally valid local save.
    /// </summary>
    public PlayerSave()
    {
        // Initialize every collection so callers never depend on server-side defaults.
        DisplayName = "Player";
        Equipped = new Dictionary<SkinType, short>();
        Unlocked = new Dictionary<SkinType, List<short>>();
        Normalize();
    }

    /// <summary>
    /// Repairs missing values after loading a partial or older local save.
    /// </summary>
    public void Normalize()
    {
        // Ensure all cosmetic slots exist before gameplay or UI reads them.
        if (string.IsNullOrWhiteSpace(DisplayName))
            DisplayName = "Player";
        if (Equipped == null)
            Equipped = new Dictionary<SkinType, short>();
        if (Unlocked == null)
            Unlocked = new Dictionary<SkinType, List<short>>();

        foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
        {
            if (!Equipped.ContainsKey(type))
                Equipped[type] = -1;
            if (!Unlocked.ContainsKey(type) || Unlocked[type] == null)
                Unlocked[type] = new List<short>();
        }
    }

    /// <summary>
    /// Gets the equipped cosmetic identifier for a slot.
    /// </summary>
    public int GetCurrentPart(SkinType type)
    {
        // Missing slots are represented by the same sentinel as unequipped slots.
        return Equipped.TryGetValue(type, out var id) ? id : -1;
    }

    /// <summary>
    /// Equips or clears a cosmetic slot.
    /// </summary>
    public void SetCurrentPart(SkinType type, short id)
    {
        // Store the selection locally; SaveManager owns disk persistence.
        Equipped[type] = id;
    }

    /// <summary>
    /// Unlocks a cosmetic when it is not already owned.
    /// </summary>
    public void Unlock(SkinType type, short id)
    {
        // Avoid duplicate identifiers in reward and inventory views.
        if (!Unlocked.TryGetValue(type, out var items))
        {
            items = new List<short>();
            Unlocked[type] = items;
        }

        if (!items.Contains(id))
            items.Add(id);
    }

    /// <summary>
    /// Determines whether a cosmetic is owned.
    /// </summary>
    public bool IsUnlocked(SkinType type, short id)
    {
        // Treat missing slots as locked instead of throwing.
        return Unlocked.TryGetValue(type, out var items) && items.Contains(id);
    }

    /// <summary>
    /// Gets the owned cosmetic identifiers for a slot.
    /// </summary>
    public List<short> GetUnlocked(SkinType type)
    {
        // Normalize on demand for saves created before the slot existed.
        if (!Unlocked.TryGetValue(type, out var items))
        {
            items = new List<short>();
            Unlocked[type] = items;
        }

        return items;
    }

    /// <summary>
    /// Gets the number of owned cosmetics in a slot.
    /// </summary>
    public int GetNbUnlockedPart(SkinType type)
    {
        // Reuse the normalized slot collection for a stable count.
        return GetUnlocked(type).Count;
    }
}
