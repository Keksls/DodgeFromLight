using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GearData", menuName = "DodgeFromLight/GearData", order = 7)]
public class GearData : ScriptableObject
{
    public List<GearDataAsset> Gears;
    private Dictionary<GearType, GearDataAsset> dicGears;

    public void Initialize()
    {
        dicGears = new Dictionary<GearType,GearDataAsset>();
        foreach(var gear in Gears)
            dicGears.Add(gear.Type, gear);
    }

    public GearDataAsset GetGearData(GearType type)
    {
        return dicGears[type];
    }
}

[System.Serializable]
public class GearDataAsset
{
    public GearType Type;
    public Texture2D Image;
    public string Name;
    public string Description;
}