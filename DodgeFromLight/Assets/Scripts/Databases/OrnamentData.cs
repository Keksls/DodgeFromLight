using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "OrnamentData", menuName = "DodgeFromLight/OrnamentData", order = 9)]
public class OrnamentData : ScriptableObject
{
    public GameObject OrnamentPrefab;
    public Sprite[] Ornaments;
    public Sprite OrnamentsBG;
    public List<OrnamentDataAsset> OrnamentsData;

    public GameObject GetOrnament(int ornamentID, string userName)
    {
        int rank = ornamentID % Ornaments.Length;
        OrnamentType type = (OrnamentType)((int)((ornamentID / Ornaments.Length)) + 1);
        return GetOrnament(type, rank, userName);
    }

    public GameObject GetOrnament(OrnamentType type, int rank, string userName)
    {
        OrnamentDataAsset data = GetOrnamentData(type);
        Sprite img = GetOrnamentSprite(rank);
        if (data == null || img == null)
            return null;

        GameObject ornament = Instantiate(OrnamentPrefab);
        ornament.transform.GetChild(0).GetComponent<Image>().sprite = OrnamentsBG;
        ornament.transform.GetChild(0).GetComponent<Image>().color = data.BackColor;
        ornament.transform.GetChild(0).GetComponent<Image>().preserveAspect = true;
        ornament.transform.GetChild(2).GetComponent<Image>().sprite = img;
        ornament.transform.GetChild(2).GetComponent<Image>().color = data.OrnamentColor;
        ornament.transform.GetChild(2).GetComponent<Image>().preserveAspect = true;
        ornament.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = userName;
        ornament.transform.GetChild(1).GetComponent<TextMeshProUGUI>().colorGradient = data.TextColor;
        ornament.transform.GetChild(1).GetComponent<AnimateTextGradient>().Initialize();
        return ornament;
    }

    public Sprite GetOrnamentImage(int ornamentID)
    {
        int rank = ornamentID % Ornaments.Length;
        OrnamentType type = (OrnamentType)((int)((ornamentID / Ornaments.Length)) + 1);
        return GetOrnamentSprite(rank);
    }

    public OrnamentDataAsset GetOrnamentData(int ornamentID)
    {
        int rank = ornamentID % Ornaments.Length;
        OrnamentType type = (OrnamentType)((int)((ornamentID / Ornaments.Length)) + 1);
        return GetOrnamentData(type);
    }

    private Sprite GetOrnamentSprite(int rank)
    {
        if (Ornaments.Length > rank && rank >= 0 && Ornaments.Length > 0)
            return Ornaments[rank];
        else
            return null;
    }

    private OrnamentDataAsset GetOrnamentData(OrnamentType type)
    {
        foreach (var orn in OrnamentsData)
            if (orn.Type == type)
                return orn;
        return null;
    }

    public List<short> GetAllOrnamentsID()
    {
        List<short> IDs = new List<short>();
        short index = 0;
        foreach (var orn in OrnamentsData)
            for (short i = 0; i < Ornaments.Length; i++)
            {
                IDs.Add(index);
                index++;
            }
        return IDs;
    }
}

[Serializable]
public class OrnamentDataAsset
{
    public OrnamentType Type;
    public Color OrnamentColor;
    public Color BackColor;
    public VertexGradient TextColor;
}

public enum OrnamentType
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Diamond = 4,
    Cormaline = 5,
    Saphir = 6,
    Mythical = 7,
    Master = 8,
}