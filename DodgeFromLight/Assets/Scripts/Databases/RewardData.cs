using DFLCommonNetwork.GameEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rewards", menuName = "DodgeFromLight/Rewards", order = 2)]
public class RewardData : ScriptableObject
{
    public Color CommonItemColor;
    public Color RareItemColor;
    public Color EpicItemColor;
    public Color LegendaryItemColor;
    public Color MythicalItemColor;
    public int NbLevelByPalliers = 11;
    public int NbPalliers = 32;
    public int XPForFirstFinishingMap = 15;
    public List<int> XpForLevel;
    public Dictionary<SkinRarityType, Dictionary<SkinType, HashSet<int>>> SkinsRarities;

    public void Initialize()
    {
        SkinsRarities = new Dictionary<SkinRarityType, Dictionary<SkinType, HashSet<int>>>();
        SkinsRarities.Add(SkinRarityType.Common, GetAllSkinsIDByRarity(SkinRarityType.Common));
        SkinsRarities.Add(SkinRarityType.Rare, GetAllSkinsIDByRarity(SkinRarityType.Rare));
        SkinsRarities.Add(SkinRarityType.Epic, GetAllSkinsIDByRarity(SkinRarityType.Epic));
        SkinsRarities.Add(SkinRarityType.Legendary, GetAllSkinsIDByRarity(SkinRarityType.Legendary));
        SkinsRarities.Add(SkinRarityType.Mythical, GetAllSkinsIDByRarity(SkinRarityType.Mythical));

        //XpForLevel = new List<int>();
        //for (int i = 0; i < 11; i++)
        //    XpForLevel.Add(1);
        //int fullxp = 0;
        //for (int i = 0; i < (NbPalliers * NbLevelByPalliers) - 11; i++)
        //{
        //    int xp = (int)Mathf.RoundToInt(Mathf.Pow(Mathf.Pow((i + 1) * 5f, 0.5f), 2.2f) / 10f) * 10;
        //    fullxp += xp;
        //    XpForLevel.Add(fullxp);
        //}
    }

    public Color GetRarityColorForLevel(int Level, float alpha)
    {
        return GetRarityColor(GetRarityForLevel(Level), alpha);
    }

    public Color GetRarityColor(SkinRarityType rarity, float alpha)
    {
        Color col = Color.white;
        switch (rarity)
        {
            case SkinRarityType.Common:
                col = CommonItemColor;
                break;
            case SkinRarityType.Rare:
                col = RareItemColor;
                break;
            case SkinRarityType.Epic:
                col = EpicItemColor;
                break;
            case SkinRarityType.Legendary:
                col = LegendaryItemColor;
                break;
            case SkinRarityType.Mythical:
                col = MythicalItemColor;
                break;
        }
        col.a = alpha;
        return col;
    }

    public List<SkinDataAsset> GetAllSkinsByRarity(SkinRarityType rarity)
    {
        List<SkinDataAsset> skins = new List<SkinDataAsset>();
        foreach (var sr in DodgeFromLight.Databases.SkinsData.Skins)
            if (sr.Rarity == rarity)
                skins.Add(sr);
        return skins;
    }

    public Dictionary<SkinType, HashSet<int>> GetAllSkinsIDByRarity(SkinRarityType rarity)
    {
        Dictionary<SkinType, HashSet<int>> skins = new Dictionary<SkinType, HashSet<int>>();
        foreach (SkinDataAsset skin in DodgeFromLight.Databases.SkinsData.Skins)
        {
            if (skin.Rarity == rarity)
            {
                if (!skins.ContainsKey(skin.Type))
                    skins.Add(skin.Type, new HashSet<int>());
                skins[skin.Type].Add(skin.ID);
            }
        }
        return skins;
    }

    public SkinRarityType GetRarity(SkinType type, int ID)
    {
        foreach (SkinDataAsset skin in DodgeFromLight.Databases.SkinsData.Skins)
            if (skin.Type == type && skin.ID == ID)
                return skin.Rarity;
        return SkinRarityType.None;
    }

    public int GetLevel(int xp)
    {
        for (int i = 0; i < XpForLevel.Count; i++)
            if (xp < XpForLevel[i])
                return i - 1;
        return XpForLevel.Count;
    }

    public int GetXpForLevel(int lvl)
    {
        if (lvl >= XpForLevel.Count)
            return 0;
        return XpForLevel[lvl];
    }

    public int GetPallier(int xp)
    {
        return Mathf.FloorToInt((float)GetLevel(xp) / (float)NbLevelByPalliers);
    }

    public SkinRarityType GetRarityForLevel(int lvl)
    {
        switch (lvl % NbLevelByPalliers)
        {
            case 0:
                return SkinRarityType.Common;
            case 1:
                return SkinRarityType.Rare;
            case 2:
                return SkinRarityType.Common;
            case 3:
                return SkinRarityType.Common;
            case 4:
                return SkinRarityType.Epic;
            case 5:
                return SkinRarityType.Common;
            case 6:
                return SkinRarityType.Rare;
            case 7:
                return SkinRarityType.Rare;
            case 8:
                return SkinRarityType.Epic;
            case 9:
                return SkinRarityType.Legendary;
            case 10:
                return SkinRarityType.Legendary;
        }
        return SkinRarityType.Legendary;
    }

    public RewardItem GetRewardForLevel(int lvl)
    {
        if (lvl % NbLevelByPalliers == 10)
        {
            RewardItem reward = new RewardItem();
            reward.Type = RewardType.Ornament;
            reward.Ornament = (lvl / NbLevelByPalliers);
            return reward;
        }
        else
            return new RewardItem(GetRandomSkinReward(GetRarityForLevel(lvl % NbLevelByPalliers)));
    }

    public SkinDataAsset GetRandomSkinReward(SkinRarityType rarity)
    {
        List<SkinDataAsset> skins = GetAllSkinsByRarity(rarity);
        List<SkinDataAsset> LockedSkins = new List<SkinDataAsset>();
        foreach (var skin in skins)
        {
            if (!SaveManager.IsUnlocked(skin.Type, (short)skin.ID))
                LockedSkins.Add(skin);
        }
        if (LockedSkins.Count > 0)
            return LockedSkins[UnityEngine.Random.Range(0, LockedSkins.Count)];
        else
            return null;
    }

    public bool HasRewards(int XP)
    {
        return SaveManager.CurrentSave.OnlineLastReward < GetLevel(XP);
    }

    public int GetNbPendingRewards(int XP)
    {
        return GetLevel(XP) - SaveManager.CurrentSave.OnlineLastReward;
    }

    public List<RewardItem> GetPendingRewards(int XP)
    {
        List<RewardItem> rewards = new List<RewardItem>();
        int lvl = GetLevel(XP);
        for (int i = SaveManager.CurrentSave.OnlineLastReward; i < lvl; i++)
        {
            rewards.Add(GetRewardForLevel(i));
        }
        return rewards;
    }

    public int GetTutorialXP()
    {
        return XpForLevel[11] + 1;
    }

    public List<RewardItem> GetTutorialRewards()
    {
        List<RewardItem> rewards = new List<RewardItem>();
        // level 0
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Face,
            SkinID = 0
        });
        // level 1
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Eye,
            SkinID = 0
        });
        // level 2
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Hand,
            SkinID = 0
        });
        // level 3
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Glove,
            SkinID = 12
        });
        // level 4
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Chest,
            SkinID = 24
        });
        // level 5
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Plant,
            SkinID = 3
        });
        // level 6
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Boot,
            SkinID = 19
        });
        // level 7
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Shield,
            SkinID = 0
        });
        // level 8
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Sword,
            SkinID = 0
        });
        // level 9
        rewards.Add(new RewardItem()
        {
            Type = RewardType.Skin,
            SkinType = SkinType.Pet,
            SkinID = 0
        });
        return rewards;
    }

    public void AcceptReward(RewardItem reward, short index, Action<bool, string> Callback)
    {
        SaveManager.CurrentSave.OnlineLastReward = index;
        SkinType type = SkinType.Ornament;
        switch (reward.Type)
        {
            case RewardType.Ornament:
                type = SkinType.Ornament;
                break;

            case RewardType.Skin:
                type = reward.SkinType;
                break;
        }
        SaveManager.Unlock(type, (short)reward.SkinID);
        DFLClient.UnlockSkin(type, reward.SkinID, index, Callback);
    }
}

[Serializable]
public class RewardItem
{
    public RewardType Type;
    public int Ornament;
    public SkinType SkinType;
    public int SkinID;
    public int Gold;

    public RewardItem()
    {

    }

    public RewardItem(SkinDataAsset sr)
    {
        Type = RewardType.Skin;
        SkinType = sr.Type;
        SkinID = sr.ID;
    }
}

public enum RewardType
{
    None = -1,
    Ornament = 0,
    Skin = 2
}

public enum SkinRarityType
{
    None = 0,
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Mythical = 5
}