using DFLCommonNetwork.GameEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkinsData", menuName = "DodgeFromLight/SkinsData", order = 6)]
public class SkinsData : ScriptableObject
{
    public List<SkinDataAsset> Skins;
    [HideInInspector]
    public Dictionary<string, SkinDataAsset> skinsDic;

    public void Initialize()
    {
        skinsDic = new Dictionary<string, SkinDataAsset>();
        foreach (var skin in Skins)
        {
            skinsDic.Add(skin.Type + "_" + skin.ID, skin);
        }
    }

    public SkinDataAsset GetSkin(SkinType Type, int SkinID)
    {
        if (skinsDic.ContainsKey(Type + "_" + SkinID))
            return skinsDic[Type + "_" + SkinID];
        return null;
    }

    public GameObject GetPrefab(SkinType Type, int SkinID)
    {
        string path = "Items/Prefabs/" + Type + "_" + SkinID;
        return Resources.Load<GameObject>(path);
    }

    public bool HasSkin(SkinType Type, int SkinID)
    {
        return skinsDic.ContainsKey(Type + "_" + SkinID);
    }

    public void SetSkinRarity(SkinType skin, int skinID, SkinRarityType rarity)
    {
        SkinDataAsset Skin = new SkinDataAsset(skin, skinID, rarity);
        // remove if already exist in line
        int toRemindex = -1;
        int i = 0;
        foreach (SkinDataAsset skinR in Skins)
        {
            if (skinR.Type == Skin.Type && skinR.ID == skinID)
                toRemindex = i;
            i++;
        }
        if (toRemindex != -1)
            Skins.RemoveAt(toRemindex);
        // add skin rarity
        Skin.Sprite = Resources.Load<Sprite>(@"Items/" + Skin.Type.ToString() + "_" + Skin.ID);
        Skins.Insert(toRemindex, Skin);
    }
}

[Serializable]
public class SkinDataAsset
{
    public Sprite Sprite;
    public SkinType Type;
    public int ID;
    public SkinRarityType Rarity;
    public Vector3 localPos;
    public Vector3 localScale;
    public Quaternion localRot;

    public SkinDataAsset(SkinType skin, int skinID, SkinRarityType rarity)
    {
        Type = skin;
        ID = skinID;
        Rarity = rarity;
    }

    public SkinDataAsset()
    {
    }
}