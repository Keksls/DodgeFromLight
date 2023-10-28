using DFLCommonNetwork.GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public List<PlayerSkinPartContainer> Containers;
    public PlayerSkinPartContainer LeftDaggerContainer;
    public Dictionary<SkinType, Transform> dicContainers;
    public LitePlayerSave CurrentPlayerSave;
    private Dictionary<SkinType, GameObject> currentSkins = new Dictionary<SkinType, GameObject>();
    private GameObject currentLeftDagger = null;
    private Dictionary<SkinType, int> currentSkinsID = new Dictionary<SkinType, int>();
    private Dictionary<SkinType, HashSet<SkinType>> SkinGroups = new Dictionary<SkinType, HashSet<SkinType>>();

    private void Awake()
    {
        dicContainers = new Dictionary<SkinType, Transform>();
        foreach (PlayerSkinPartContainer cont in Containers)
            dicContainers.Add(cont.Type, cont.Container.transform);

        SkinGroups = new Dictionary<SkinType, HashSet<SkinType>>();
        SkinGroups.Add(SkinType.Sword, new HashSet<SkinType>()
        {
            SkinType.Wand,
            SkinType.Axe,
            SkinType.Hammer,
            SkinType.Dagger
        });
        SkinGroups.Add(SkinType.Wand, new HashSet<SkinType>()
        {
            SkinType.Sword,
            SkinType.Axe,
            SkinType.Hammer,
            SkinType.Dagger
        });
        SkinGroups.Add(SkinType.Axe, new HashSet<SkinType>()
        {
            SkinType.Sword,
            SkinType.Wand,
            SkinType.Hammer,
            SkinType.Dagger
        });
        SkinGroups.Add(SkinType.Hammer, new HashSet<SkinType>()
        {
            SkinType.Sword,
            SkinType.Axe,
            SkinType.Wand,
            SkinType.Dagger
        });
        SkinGroups.Add(SkinType.Dagger, new HashSet<SkinType>()
        {
            SkinType.Sword,
            SkinType.Axe,
            SkinType.Wand,
            SkinType.Hammer
        });
    }

    public void SetSave(LitePlayerSave save)
    {
        CurrentPlayerSave = save;
        ApplySkin();
    }

    public void SetSave(PlayerSave save)
    {
        CurrentPlayerSave = new LitePlayerSave(save);
        ApplySkin();
    }

    public void SetSkinPart(SkinType type, int id)
    {
        switch (type)
        {
            case SkinType.Belt:
            case SkinType.Boot:
            case SkinType.Chest:
            case SkinType.Glove:
            case SkinType.Helm:
            case SkinType.Plant:
            case SkinType.Shoulder:
            case SkinType.Hat:
            case SkinType.Hand:
            case SkinType.Face:
            case SkinType.Hair:
                SetSkinPart_SkinedMesh(type, id);
                break;

            default:
                SetSkinPart_Mesh(type, id);
                break;
        }
        if (!currentSkinsID.ContainsKey(type))
            currentSkinsID.Add(type, id);
        currentSkinsID[type] = id;

        if (type == SkinType.Pet && id != -1)
            InitPet();
    }

    private void SetSkinPart_SkinedMesh(SkinType type, int id)
    {
        if (currentSkinsID.ContainsKey(type) && currentSkinsID[type] != -1)
            dicContainers[type].GetChild(currentSkinsID[type]).gameObject.SetActive(false);
        if (id != -1)
            dicContainers[type].GetChild(id).gameObject.SetActive(true);
    }

    private void SetSkinPart_Mesh(SkinType type, int id)
    {
        // delete old skin
        UnsetSkinPart(type);
        if (SkinGroups.ContainsKey(type) && id != -1)
            foreach (SkinType linked in SkinGroups[type])
                UnsetSkinPart(linked);

        // get new skin data
        SkinDataAsset skin = DodgeFromLight.Databases.SkinsData.GetSkin(type, id);
        if (skin == null)
            return;
        GameObject partPrefab = DodgeFromLight.Databases.SkinsData.GetPrefab(type, id);
        if (partPrefab == null)
            return;

        // instantiate and place skin
        GameObject part = Instantiate(partPrefab);
        part.transform.SetParent(dicContainers[type]);
        part.transform.localPosition = skin.localPos;
        part.transform.localRotation = skin.localRot;
        part.transform.localScale = skin.localScale;
        currentSkins.Add(type, part);

        if (type == SkinType.Dagger)
        {
            currentLeftDagger = Instantiate(partPrefab);
            currentLeftDagger.transform.SetParent(LeftDaggerContainer.Container.transform);
            currentLeftDagger.transform.localPosition = skin.localPos;
            currentLeftDagger.transform.localRotation = skin.localRot;
            currentLeftDagger.transform.localScale = skin.localScale;
        }
    }

    public void ApplySkin()
    {
        foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
            if (type != SkinType.Ornament)
                SetPartFromSave(type);
    }

    public void UnsetSkinPart(SkinType type)
    {
        // delete old skin
        if (currentSkins.ContainsKey(type))
        {
            Destroy(currentSkins[type]);
            currentSkins.Remove(type);
        }

        if (type == SkinType.Dagger && currentLeftDagger != null)
            Destroy(currentLeftDagger);
    }

    public void SetPartFromSave(SkinType type)
    {
        SetSkinPart(type, CurrentPlayerSave.GetCurrentPart(type));
    }

    public GameObject GetInstantiatesSkin(SkinType type)
    {
        if (!currentSkins.ContainsKey(type))
            return null;
        return currentSkins[type];
    }

    public GameObject FastGetCurrentSkin(SkinType type)
    {
        return currentSkins[type];
    }

    public GameObject GetInstantiatedWeaponGameObject()
    {
        if (CurrentPlayerSave.GetCurrentPart(SkinType.Sword) != -1)
            return GetInstantiatesSkin(SkinType.Sword);
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Axe) != -1)
            return GetInstantiatesSkin(SkinType.Axe);
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Hammer) != -1)
            return GetInstantiatesSkin(SkinType.Hammer);
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Wand) != -1)
            return GetInstantiatesSkin(SkinType.Wand);
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Dagger) != -1)
            return GetInstantiatesSkin(SkinType.Dagger);
        else
            return null;
    }

    public WeaponDataAsset GetCurrentWeaponData()
    {
        if (CurrentPlayerSave.GetCurrentPart(SkinType.Sword) != -1)
            return DodgeFromLight.Databases.WeaponData.GetWeaponData(SkinType.Sword, CurrentPlayerSave.GetCurrentPart(SkinType.Sword));
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Axe) != -1)
            return DodgeFromLight.Databases.WeaponData.GetWeaponData(SkinType.Axe, CurrentPlayerSave.GetCurrentPart(SkinType.Axe));
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Hammer) != -1)
            return DodgeFromLight.Databases.WeaponData.GetWeaponData(SkinType.Hammer, CurrentPlayerSave.GetCurrentPart(SkinType.Hammer));
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Wand) != -1)
            return DodgeFromLight.Databases.WeaponData.GetWeaponData(SkinType.Wand, CurrentPlayerSave.GetCurrentPart(SkinType.Wand));
        else if (CurrentPlayerSave.GetCurrentPart(SkinType.Dagger) != -1)
            return DodgeFromLight.Databases.WeaponData.GetWeaponData(SkinType.Dagger, CurrentPlayerSave.GetCurrentPart(SkinType.Dagger));
        else
            return null;
    }

    public void KillPet()
    {
        UnsetSkinPart(SkinType.Pet);
    }

    public void SetLayerOnPart(SkinType type, int layer)
    {
        if (currentSkins.ContainsKey(type))
            currentSkins[type].SetLayer(layer);

        if (type == SkinType.Dagger && currentLeftDagger != null)
            currentLeftDagger.SetLayer(layer);
    }

    public void InitPet()
    {
        GameObject petGo = GetInstantiatesSkin(SkinType.Pet);
        if (petGo == null)
            return;
        petGo.transform.SetParent(null);
        petGo.GetComponent<Pet>().Initialize(GetContainer(SkinType.Pet));
    }

    public Transform GetContainer(SkinType type)
    {
        return dicContainers[type];
    }

    #region Eyes Blinking
    // blink eye
    public float blinkEyeRateMin = 4f;
    public float blinkEyeRateMax = 10f;
    public float blinkEyeSpeed = 0.1f;
    public float doubleBlinkLuck = 0.2f;
    public float doubleBlinkPause = 0.15f;
    public float YOffsetBlinkPause = -0.01f;
    private float blinkEyeRate;
    private float previousBlinkEyeRate;
    private float blinkEyeTime;
    void Update()
    {
        if (Time.time > blinkEyeTime && GetInstantiatesSkin(SkinType.Eye) != null)
        {
            previousBlinkEyeRate = blinkEyeRate;
            blinkEyeTime = Time.time + blinkEyeRate;
            //set a trigger named "blink" in ur animator window and then set that trigger the arrow connectiing eyeIdle to eyeBlink               
            StartCoroutine(BlinkEyes());
            while (previousBlinkEyeRate == blinkEyeRate)
            {
                // Random Rate from 4 secs to 10secs
                blinkEyeRate = UnityEngine.Random.Range(blinkEyeRateMin, blinkEyeRateMax);
            }
        }
    }

    IEnumerator BlinkEyes()
    {
        bool doubleBlink = UnityEngine.Random.Range(0f, 1f) <= doubleBlinkLuck;
        yield return StartCoroutine(BlinkEyesSingle());
        if (doubleBlink)
        {
            yield return new WaitForSeconds(doubleBlinkPause);
            yield return StartCoroutine(BlinkEyesSingle());
        }
    }

    IEnumerator BlinkEyesSingle()
    {
        Vector3 start = Vector3.one;
        Vector3 end = Vector3.one;
        end.y = 0.25f;
        Vector3 startPos = FastGetCurrentSkin(SkinType.Eye).transform.localPosition;
        Vector3 endPos = FastGetCurrentSkin(SkinType.Eye).transform.localPosition;
        endPos.y += YOffsetBlinkPause;
        float enlapsed = 0f;
        while (enlapsed < blinkEyeSpeed)
        {
            FastGetCurrentSkin(SkinType.Eye).transform.localScale = Vector3.Lerp(start, end, enlapsed / blinkEyeSpeed);
            FastGetCurrentSkin(SkinType.Eye).transform.localPosition = Vector3.Lerp(startPos, endPos, enlapsed / blinkEyeSpeed);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        FastGetCurrentSkin(SkinType.Eye).transform.localScale = end;
        FastGetCurrentSkin(SkinType.Eye).transform.localPosition = endPos;
        yield return new WaitForSeconds(blinkEyeSpeed * 0.66f);
        enlapsed = 0f;
        while (enlapsed < blinkEyeSpeed)
        {
            FastGetCurrentSkin(SkinType.Eye).transform.localScale = Vector3.Lerp(end, start, enlapsed / blinkEyeSpeed);
            FastGetCurrentSkin(SkinType.Eye).transform.localPosition = Vector3.Lerp(endPos, startPos, enlapsed / blinkEyeSpeed);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        FastGetCurrentSkin(SkinType.Eye).transform.localScale = start;
        FastGetCurrentSkin(SkinType.Eye).transform.localPosition = startPos;
    }
    #endregion
}

[Serializable]
public class PlayerSkinPartContainer
{
    public SkinType Type;
    public GameObject Container;
}