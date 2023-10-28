using UnityEngine;

public class Databases : MonoBehaviour
{
    public ResourcesData ResourcesData;
    public WeaponData WeaponData;
    public CellPOData CellPOData;
    public RewardData RewardData;
    public GlossaryData GlossaryData;
    public SkinsData SkinsData;
    public GearData GearData;
    public OrnamentData OrnamentData;

    //public PlayerCharacter player;
    //int index = 0;
    //GameObject vfx;
    //GameObject jumpFX;
    //GameObject weapon;

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.LeftArrow))
    //    {
    //        index--;
    //        if (index < 0)
    //            index = WeaponData.Weapons.Count - 1;
    //        DrawWeapon();
    //    }
    //    else if (Input.GetKeyDown(KeyCode.RightArrow))
    //    {
    //        index++;
    //        if (index >= WeaponData.Weapons.Count - 1)
    //            index = 0;
    //        DrawWeapon();
    //    }
    //}

    //void DrawWeapon()
    //{
    //    if (vfx != null)
    //        Destroy(vfx);
    //    if (jumpFX && jumpFX != null)
    //        try { Destroy(jumpFX); } catch { }
    //    if (weapon != null)
    //        Destroy(weapon);

    //    var wda = WeaponData.Weapons[index];
    //    vfx = WeaponData.GetPlantedVFX(wda.Type, wda.SkinID);
    //    jumpFX = WeaponData.GetTranspositionWeaponVFX(wda.Type, wda.SkinID);
    //    jumpFX.transform.position = new Vector3(12, 0, 0);
    //    vfx.transform.position = Vector3.zero;

    //    if(player != null)
    //    {
    //        var sp = player.GetSkinPart(wda.Type, wda.SkinID);
    //        weapon = Instantiate(sp.GameObject);
    //        weapon.SetActive(true);
    //        weapon.transform.position = new Vector3(0, wda.YPos, 0);
    //        weapon.transform.eulerAngles = wda.Rotation;
    //        weapon.transform.localScale = wda.Scale;
    //    }
    //}

    void Awake()
    {
        DodgeFromLight.Databases = this;

        GearData.Initialize();
        ResourcesData.Initialize();
        CellPOData.Initialize();
        SkinsData.Initialize();
        RewardData.Initialize();
        WeaponData.Initialize();
    }
}