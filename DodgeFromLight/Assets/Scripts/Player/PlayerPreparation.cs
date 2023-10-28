using DFLCommonNetwork.GameEngine;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class PlayerPreparation : MonoBehaviour
{
    public bool SetSprites = true;
    public bool generateFullyUnlockedSave = true;
    public bool ignorePrefabs = true;
    public bool ignoreScreenshot = true;
    public bool generateFullData = false;
    public float CameraFOV = 1f;
    public int ScreenshotLayer = 0;
    public int StartAtID = 17;
    public Camera cam;
    public List<PlayerSkinPartContainer> Containers;
    public SkinsData baseData;
    public SkinsData backedData;
    private byte[] previewData;

    private void Start()
    {
        baseData.Initialize();
        StartCoroutine(Global());
    }

    IEnumerator Global()
    {
        if (generateFullData)
            yield return StartCoroutine(BakePlayerSkins());
        if (SetSprites)
            foreach (var skin in backedData.Skins)
                skin.Sprite = Resources.Load<Sprite>("Items/Previews/" + skin.Type + "_" + skin.ID);
        if (generateFullyUnlockedSave)
        {
            backedData.Initialize();
            PlayerSave save = SaveManager.GetDefaulSave();
            foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
            {
                if (type == SkinType.Ornament)
                    save.Unlocked[(byte)SkinType.Ornament] = DodgeFromLight.Databases.OrnamentData.GetAllOrnamentsID();
                else
                {
                    int nbSkins = backedData.Skins.Where(s => s.Type == type).ToList().Count;
                    List<short> skins = new List<short>();
                    for (short i = 0; i < nbSkins; i++)
                        skins.Add(i);
                    save.Unlocked[(byte)type] = skins;

                }
            }
            string savePath = Application.streamingAssetsPath + @"\fullyUnlockedSave.json";
            File.WriteAllText(savePath, JsonConvert.SerializeObject(save));
        }
    }

    /// <summary>
    /// must be run with square game render (1024x1024)
    /// </summary>
    /// <returns></returns>
    public IEnumerator BakePlayerSkins()
    {
        ScreenshotLayer = LayerMask.NameToLayer("PreviewGenerator");
        //cam.cullingMask = ScreenshotLayer;
        yield return null;
        // hide everything
        foreach (Transform tr in transform)
            tr.gameObject.SetActive(false);
        List<SkinDataAsset> Skins = new List<SkinDataAsset>();
        yield return null;

        int ci = -1;
        foreach (PlayerSkinPartContainer cont in Containers)
        {
            ci++;
            if (ci < StartAtID)
                continue;

            int nbChilds = cont.Container.transform.childCount;
            for (int i = 0; i < nbChilds; i++)
            {
                Transform t = cont.Container.transform.GetChild(i);
                SkinDataAsset data = new SkinDataAsset();
                data.ID = i;
                data.Type = cont.Type;
                data.localPos = t.localPosition;
                data.localRot = t.localRotation;
                data.localScale = t.localScale;

                // get rariry
                if (baseData.HasSkin(cont.Type, i))
                    data.Rarity = baseData.GetSkin(cont.Type, i).Rarity;
                else
                    data.Rarity = SkinRarityType.Mythical;

#if UNITY_EDITOR
                if (!ignorePrefabs)
                {
                    // Save Prefab
                    string prefabPath = "Assets/Resources/Items/Prefabs/" + data.Type + "_" + data.ID + ".prefab";
                    if (!File.Exists(Environment.CurrentDirectory + @"\" + prefabPath))
                    {
                        t.gameObject.SetActive(true);
                        t.gameObject.SetLayer(0);
                        t.position = Vector3.zero;
                        t.rotation = Quaternion.identity;
                        PrefabUtility.SaveAsPrefabAsset(t.gameObject, prefabPath);
                    }
                }
#endif

                if (!ignoreScreenshot)
                {
                    // screenshot, save it to resources, and assignate it to skinData
                    yield return StartCoroutine(ScreenShotItems(t, cont.Type, i));
                    string savePath = "Assets/Resources/Items/Previews/" + data.Type + "_" + data.ID + ".png";
                    if (File.Exists(Environment.CurrentDirectory + savePath))
                        File.Delete(Environment.CurrentDirectory + savePath);
                    File.WriteAllBytes(savePath, previewData);
                    data.Sprite = Resources.Load<Sprite>("Items/Previews/" + data.Type + "_" + data.ID);
                }

                if (!ignoreScreenshot || !ignorePrefabs)
                    t.gameObject.SetActive(false);
                Skins.Add(data);
            }
            yield return null;
        }
        backedData.Skins = Skins;
    }

    Bounds b = new Bounds();
    IEnumerator ScreenShotItems(Transform t, SkinType type, int id)
    {
        t.SetParent(null);
        t.position = Vector3.zero;
        t.gameObject.SetActive(true);
        int baseLayer = t.gameObject.layer;
        t.gameObject.SetLayer(ScreenshotLayer);

        // get unskinned mesh
        GameObject unskinned = t.gameObject.GetUnskined();
        if (unskinned != null)
        {
            unskinned.SetLayer(ScreenshotLayer);
            t.gameObject.SetActive(false);
            b = unskinned.GetBounds();
        }
        else
            b = t.gameObject.GetBounds();

        Vector3 center = b.center;
        var size = b.size;
        float max = Mathf.Max(size.x, size.y, size.z);
        GetFieldOfView(center, Mathf.Max(size.x, size.y, size.z));
        switch (type)
        {
            case SkinType.Sword:
            case SkinType.Dagger:
            case SkinType.Wand:
            case SkinType.Axe:
                center = new Vector3(-max / 4f, max / 4f, 0f);
                break;

            case SkinType.Hammer:
                center = new Vector3(-max / 8f, max / 4f - 0.1f, 0f);
                break;

            case SkinType.Pet:
                center = new Vector3(0f, center.y + (max / 2f) + 0.2f, 0f);
                break;
        }

        cam.transform.position = new Vector3(center.x, center.y + 0.2f, 2);

        if (type == SkinType.Pet || type == SkinType.Wings)
        {
            unskinned.transform.eulerAngles = new Vector3(-90, 0, 0);
        }
        else if (type == SkinType.Shield)
        {
            if (id < 1)
                t.eulerAngles = new Vector3(0, -35, 0);
            else if (id < 41)
                t.eulerAngles = new Vector3(90, -110, 0);
            else
                t.eulerAngles = new Vector3(0, -12, 0);
        }
        else if (type == SkinType.Sword)
        {
            if (id < 1)
                t.eulerAngles = new Vector3(-45, -80, 20);
            else if (id < 41)
                t.eulerAngles = new Vector3(-45, -80, 20);
            else if (id < 152)
                t.eulerAngles = new Vector3(-25, 30, 40);
            else if (id < 99999)
                t.eulerAngles = new Vector3(-25, 30, 40);
        }
        else if (type == SkinType.Dagger)
        {
            t.eulerAngles = new Vector3(-20, 200, -50);
        }
        else if (type == SkinType.Wand)
        {
            if (id < 10)
                t.eulerAngles = new Vector3(-45, -90, 20);
            else if (id < 99999)
                t.eulerAngles = new Vector3(-45, -90, 20);
        }
        else if (type == SkinType.Axe)
        {
            if (id < 30)
                t.eulerAngles = new Vector3(-45, -90, 20);
            else if (id < 31)
                t.eulerAngles = new Vector3(0, 180, -50);
            else if (id < 99999)
                t.eulerAngles = new Vector3(4, -4, 45);
        }
        else if (type == SkinType.Hammer)
        {
            t.eulerAngles = new Vector3(-45, -90, 20);
        }

        cam.transform.LookAt(center);
        cam.clearFlags = CameraClearFlags.Skybox;
        yield return null;
        cam.clearFlags = CameraClearFlags.Depth;
        yield return null;
        // take screenshot
        int resWidth = cam.pixelWidth;
        int resHeight = cam.pixelHeight;

        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        previewData = screenShot.EncodeToPNG();
        rt.Release();
        Destroy(screenShot);

        if (unskinned != null)
        {
            Destroy(unskinned);
            t.gameObject.SetActive(true);
        }
        t.gameObject.SetLayer(baseLayer);
    }

    private void OnDrawGizmos()
    {
        if (b != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }

    void GetFieldOfView(Vector3 objectPosition, float size)
    {
        //float x = w * 0.5f - 0.5f;
        //float y = h * 0.5f - 0.4f;

        //cam.transform.position = new Vector3(x, y, 10f);

        //cam.orthographicSize = ((w > h * cam.aspect) ? (float)w / (float)cam.pixelWidth * cam.pixelHeight : h) / 2;

        float x = Vector3.Distance(objectPosition, cam.transform.position);
        float y = (size * CameraFOV) / 2f;
        float requiredFOV = Mathf.Atan(y / x) * Mathf.Rad2Deg;
        cam.fieldOfView = requiredFOV * 2f;

        // return 2f * Mathf.Atan((Mathf.Max(w, h) / 2f) / Vector3.Distance(objectPosition, cam.transform.position)) * CameraFOV;
        //Vector3 diff = objectPosition - cam.transform.position;
        //float distance = Vector3.Dot(diff, cam.transform.forward);
        //float angle = Mathf.Atan((Mathf.Max(w, h) / 2f) * CameraFOV) / distance;
        //cam.fieldOfView = angle * 2f * Mathf.Rad2Deg;
    }
}