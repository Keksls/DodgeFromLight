using System.Collections.Generic;
using UnityEngine;

public class MaterialChanger
{
    private List<MaterialPair> Pairs;

    public MaterialChanger()
    {
        Pairs = new List<MaterialPair>();
    }

    public void AddRenderer(Renderer mr, Material[] baseMat)
    {
        Pairs.Add(new MaterialPair(mr, baseMat));
    }

    public void ResetAllRenderers()
    {
        foreach (var pair in Pairs)
            if (pair.Renderer)
                pair.Renderer.materials = pair.BaseMaterials;
    }

    public void SetMaterial(Material mat)
    {
        foreach (var pair in Pairs)
            if (pair.Renderer != null)
            {
                Material[] mats = new Material[pair.BaseMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                pair.Renderer.materials = mats;
            }
    }

    public void Clear()
    {
        Pairs.Clear();
    }
}

public class MaterialPair
{
    public Renderer Renderer;
    public Material[] BaseMaterials;

    public MaterialPair(Renderer renderer, Material[] baseMaterial)
    {
        Renderer = renderer;
        BaseMaterials = baseMaterial;
    }
}