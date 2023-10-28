using UnityEngine;

public class GearRingController : MonoBehaviour
{
    public Material baseMat;
    public SpriteRenderer Render1;
    public SpriteRenderer Render2;

    public void SetGear(GearType type)
    {
        Texture2D tex = DodgeFromLight.Databases.GearData.GetGearData(type).Image;
        Material mat = Instantiate(baseMat);
        mat.SetTexture("_MainTex", tex);
        mat.SetTexture("_EmissionMap", tex);
        Sprite s = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        Render1.sprite = s;
        Render2.sprite = s;
        Render1.material = mat;
        Render2.material = mat;

        Destroy(this);
    }
}