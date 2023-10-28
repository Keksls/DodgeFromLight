using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "DodgeFromLight/WeaponData", order = 5)]
public class WeaponData : ScriptableObject
{
    public GameObject PlantWeaponVFXPrefab;
    public GameObject TranspositionVFXPrefab;
    public List<WeaponDataAsset> Weapons;
    private Dictionary<string, WeaponDataAsset> weaponsDic;

    public void Initialize()
    {
        weaponsDic = new Dictionary<string, WeaponDataAsset>();

        //BindDefaultMissingWeapons(SkinType.Sword);
        //BindDefaultMissingWeapons(SkinType.Wand);
        //BindDefaultMissingWeapons(SkinType.Shield);
        //BindDefaultMissingWeapons(SkinType.Hammer);
        //BindDefaultMissingWeapons(SkinType.Axe);
        //BindDefaultMissingWeapons(SkinType.Dagger);

        foreach (WeaponDataAsset weapon in Weapons)
        {
            weaponsDic.Add(weapon.Type + "_" + weapon.SkinID, weapon);
        }
    }

    public void BindDefaultMissingWeapons(SkinType Type)
    {
        foreach (SkinDataAsset skin in DodgeFromLight.Databases.SkinsData.Skins)
        {
            if (skin.Type == Type)
            {
                if (!weaponsDic.ContainsKey(skin.Type + "_" + skin.ID)) // we don't already have this weapon
                {
                    WeaponDataAsset weapon = new WeaponDataAsset()
                    {
                        Type = Type,
                        SkinID = skin.ID,
                        Scale = new Vector3(6f, 6f, 6f),
                        Image = skin.Sprite,
                        YPos = Type == SkinType.Sword ? 6f : 2f,
                        Rotation = Type == SkinType.Sword ? new Vector3(90, 0, 0) : new Vector3(-90, 0, 0)
                    };

                    string path = System.Environment.CurrentDirectory + @"\Assets\Resources\Items\Previews\" + Type + "_" + skin.ID + ".png";
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(System.IO.File.ReadAllBytes(path));

                    weapon.ColorJump = ColorHelpers.AverageColorFromTexture(tex);
                    weapon.ColorMagical1 = weapon.ColorJump;
                    weapon.ColorMagical2 = weapon.ColorJump;
                    weapon.ColorMagical1 = ColorHelpers.SetSaturation(weapon.ColorMagical1, 1f);
                    weapon.ColorMagical2 = ColorHelpers.SetSaturation(weapon.ColorMagical2, 0.7f);
                    weapon.ColorMagical2 = ColorHelpers.SetV(weapon.ColorMagical2, .1f);
                    weapon.ColorJump = ColorHelpers.SetSaturation(weapon.ColorJump, 1f);
                    weapon.ColorMagical1 = ColorHelpers.MultiplyIntensity(weapon.ColorMagical1, 6f);
                    weapon.ColorMagical2 = ColorHelpers.MultiplyIntensity(weapon.ColorMagical2, 6f);

                    weapon.ColorJump = ColorHelpers.MultiplyIntensity(weapon.ColorJump, 1f);

                    Weapons.Add(weapon);
                }
            }
        }
    }

    public WeaponDataAsset GetWeaponData(SkinType type, int ID)
    {
        return weaponsDic[type + "_" + ID];
    }

    public GameObject GetPlantedVFX(SkinType type, int ID)
    {
        WeaponDataAsset data = GetWeaponData(type, ID);
        if (data == null) return null;

        GameObject VFX = Instantiate(PlantWeaponVFXPrefab);
        SetPSColor(VFX, data.ColorMagical1); // main Circle
        SetPSColor(VFX.transform.GetChild(0).gameObject, data.ColorMagical2); // Lines
        SetPSColor(VFX.transform.GetChild(1).gameObject, data.ColorMagical1); // Stars
        SetPSColor(VFX.transform.GetChild(2).gameObject, data.ColorMagical1); // Small Circle
        return VFX;
    }

    public GameObject GetTranspositionWeaponVFX(SkinType type, int ID)
    {
        WeaponDataAsset data = GetWeaponData(type, ID);
        if (data == null) return null;

        GameObject VFX = Instantiate(TranspositionVFXPrefab);
        foreach (Transform t in VFX.transform)
        {
            ParticleSystem ps = t.gameObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var col = ps.colorOverLifetime;
                Gradient grad = new Gradient();
                var colKeys = new GradientColorKey[col.color.gradient.colorKeys.Length];
                int i = 0;
                foreach (var gc in col.color.gradient.colorKeys)
                {
                    colKeys[i] = new GradientColorKey(data.ColorJump, gc.time);
                    i++;
                }
                grad.SetKeys(colKeys, col.color.gradient.alphaKeys);
                col.color = grad;
            }
        }
        return VFX;
    }

    private void SetPSColor(GameObject go, Color col, bool copyMaterial = true, bool allColor = false)
    {
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        if (psr == null)
            return;
        if (copyMaterial)
        {
            Material mat = Instantiate(psr.material);
            psr.material = mat;
        }
        psr.material.SetColor("_EmissionColor", col);
        psr.material.SetColor("_TintColor", col);
        if (allColor)
            psr.material.color = col;
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = col;
    }
}

[System.Serializable]
public class WeaponDataAsset
{
    public Sprite Image;
    public SkinType Type;
    public int SkinID;
    public float YPos;
    public Vector3 Rotation;
    public Vector3 Scale;
    [ColorUsage(true, true)]
    public Color ColorMagical1;
    [ColorUsage(true, true)]
    public Color ColorMagical2;
    [ColorUsage(true, true)]
    public Color ColorJump;
}