using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CampaingData", menuName = "DodgeFromLight/CampaingData", order = 8)]
public class CampaingData : ScriptableObject
{
    public List<CampaignDataAsset> Levels;
}

[System.Serializable]
public class CampaignDataAsset
{
    public string Name;
    public string Description;
    public string LevelName;
    public List<RewardItem> Rewards;
}