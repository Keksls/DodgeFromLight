using DFLCommonNetwork.GameEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SaveManager
{
    public static PlayerSave CurrentSave { get; private set; }
    private static bool initialized = false;
    public static bool SaveGetted { get; set; }

    public static void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        SaveGetted = false;
        CurrentSave = GetDefaulSave();
        OrderSkins(DodgeFromLight.Databases.RewardData);
        if (DFLClient.OnlineState == OnlineState.Online && DFLClient.LoginState == LoginState.LoggedIn)
            Events_UserLogin();
        else
        {
            Events.UserLogin -= Events_UserLogin;
            Events.UserLogin += Events_UserLogin;
        }
    }

    public static void SetSave(PlayerSave ps)
    {
        CurrentSave = ps;
        SaveGetted = true;
        OrderSkins(DodgeFromLight.Databases.RewardData);
        Events.Fire_SaveGetted();
    }

    public static PlayerSave GetDefaulSave()
    {
        var rewards = DodgeFromLight.Databases.RewardData.GetTutorialRewards();
        CurrentSave = CreateDefaultSave();
        foreach (var reward in rewards)
        {
            Unlock(reward.SkinType, (short)reward.SkinID);
            SetCurrentPart(reward.SkinType, (short)reward.SkinID);
        }
        CurrentSave.XP = DodgeFromLight.Databases.RewardData.GetTutorialXP();
        CurrentSave.OnlineLastReward = 6;
        CurrentSave.SetCurrentPart(SkinType.Ornament, 0);
        return CurrentSave;
    }

    private static void Events_UserLogin()
    {
        CurrentSave = DFLClient.CurrentUser.PlayerSave;
        SaveGetted = true;
        OrderSkins(DodgeFromLight.Databases.RewardData);
        Events.Fire_SaveGetted();
    }

    private static void OrderSkins(RewardData reward)
    {
        Array types = Enum.GetValues(typeof(SkinType));
        foreach (SkinType type in types)
            if (CurrentSave.Unlocked.ContainsKey((byte)type))
                CurrentSave.Unlocked[(byte)type] = CurrentSave.Unlocked[(byte)type].OrderBy(id => reward.GetRarity(type, id)).ToList();
    }

    public static PlayerSave CreateDefaultSave()
    {
        int OnHead = UnityEngine.Random.Range(0, 3);
        Array types = Enum.GetValues(typeof(SkinType));
        PlayerSave save = new PlayerSave();
        foreach (SkinType type in types)
        {
            save.Unlocked.Add((byte)type, new List<short>());
            save.Equiped.Add((byte)type, -1);
        }
        CurrentSave = save;
        return save;
    }

    public static void Unlock(SkinType type, short ID)
    {
        if (!CurrentSave.Unlocked[(byte)type].Contains(ID))
            CurrentSave.Unlocked[(byte)type].Add(ID);
    }

    public static bool IsUnlocked(SkinType type, short ID)
    {
        return CurrentSave.Unlocked[(byte)type].Contains(ID);
    }

    public static void SetCurrentPart(SkinType type, short id)
    {
        CurrentSave.Equiped[(byte)type] = id;
    }
}