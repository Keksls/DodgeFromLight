using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CellPOData", menuName = "DodgeFromLight/CellPOData", order = 4)]
public class CellPOData : ScriptableObject
{
    public Material CellMaterialPrefab;
    public List<EnnemyColorData> EnnemiesColors;
    [ColorUsage(true, true)]
    public Color GlouttonAttractionColorUpEmission;
    public Color GlouttonAttractionColorUp;
    [ColorUsage(true, true)]
    public Color GlouttonAttractionColorDownEmission;
    public Color GlouttonAttractionColorDown;

    private Dictionary<EnnemyType, Material> CellUpMaterials;
    private Dictionary<EnnemyType, Material> CellDownMaterials;
    private Dictionary<EnnemyType, EnnemyColorData> EnnemiesColorDataDic;
    [HideInInspector]
    public Material AttractionCellUpMaterial;
    [HideInInspector]
    public Material AttractionCellDownMaterial;

    public void Initialize()
    {
        // initialize Materials
        CellUpMaterials = new Dictionary<EnnemyType, Material>();
        CellDownMaterials = new Dictionary<EnnemyType, Material>();
        EnnemiesColorDataDic = new Dictionary<EnnemyType, EnnemyColorData>();
        foreach (EnnemyColorData item in EnnemiesColors)
        {
            EnnemiesColorDataDic.Add(item.Type, item);
        }

        foreach (EnnemyType type in System.Enum.GetValues(typeof(EnnemyType)))
        {
            Material mat = Instantiate(CellMaterialPrefab);
            mat.name = type.ToString() + "_Down";
            mat.SetColor("_EmissionColor", EnnemiesColorDataDic[type].DownColorEmission);
            mat.color = EnnemiesColorDataDic[type].DownColor;
            CellDownMaterials.Add(type, mat);

            Material matUp = Instantiate(CellMaterialPrefab);
            matUp.name = type.ToString() + "_Up";
            matUp.SetColor("_EmissionColor", EnnemiesColorDataDic[type].UpColorEmission);
            matUp.color = EnnemiesColorDataDic[type].UpColor;
            CellUpMaterials.Add(type, matUp);
        }

        AttractionCellDownMaterial = Instantiate(CellMaterialPrefab);
        AttractionCellDownMaterial.SetColor("_EmissionColor", GlouttonAttractionColorDownEmission);
        AttractionCellDownMaterial.color = GlouttonAttractionColorDown;

        AttractionCellUpMaterial = Instantiate(CellMaterialPrefab);
        AttractionCellUpMaterial.color = GlouttonAttractionColorUp;
        AttractionCellUpMaterial.SetColor("_EmissionColor", GlouttonAttractionColorUpEmission);
    }

    public void UpdateColors()
    {
        foreach (EnnemyType type in System.Enum.GetValues(typeof(EnnemyType)))
        {
            CellDownMaterials[type].SetColor("_EmissionColor", EnnemiesColorDataDic[type].DownColorEmission);
            CellDownMaterials[type].color = EnnemiesColorDataDic[type].DownColor;
            CellUpMaterials[type].SetColor("_EmissionColor", EnnemiesColorDataDic[type].UpColorEmission);
            CellUpMaterials[type].color = EnnemiesColorDataDic[type].UpColor;
        }

        AttractionCellDownMaterial.SetColor("_EmissionColor", GlouttonAttractionColorDownEmission);
        AttractionCellDownMaterial.color = GlouttonAttractionColorDown;
        AttractionCellUpMaterial.SetColor("_EmissionColor", GlouttonAttractionColorUpEmission);
        AttractionCellUpMaterial.color = GlouttonAttractionColorUp;
    }

    public EnnemyColorData GetEnnemyColor(EnnemyType type)
    {
        return EnnemiesColorDataDic[type];
    }

    public Material GetUpMaterial(EnnemyType type)
    {
        return CellUpMaterials[type];
    }

    public Material GetDownMaterial(EnnemyType type)
    {
        return CellDownMaterials[type];
    }
}

[System.Serializable]
public class EnnemyColorData
{
    public EnnemyType Type;
    [ColorUsage(true, true)]
    public Color DownColorEmission = Color.white;
    public Color DownColor = Color.white;
    [ColorUsage(true, true)]
    public Color UpColorEmission = Color.white;
    public Color UpColor = Color.white;
}