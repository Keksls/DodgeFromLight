using DFLCommonNetwork.GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UI_Inventory : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public UI_WorkerNotifier UI_WorkerNotifier;
    public Transform ItemsContainer;
    public RectTransform ScrollViewContent;
    public GameObject ItemPrefab;
    public GameObject SeparatorPrefab;
    public GameObject EmptyItemPrefab;
    public GameObject ornamentItemPrefab;
    public Transform OrnamentsContainer;
    public GameObject OrnamentPanel;
    public GameObject ItemsPanel;
    public Color SelectedItemColor;
    public Color UnselectedItemColor;
    public List<Button> ItemsTypeButtons;
    public Button OrnamentTypeButton;
    public Color SelectedItemButtonColor;
    public Color UnselectedItemButtonColor;
    private Dictionary<SkinType, GameObject> selectedItemsGO;
    private int lastSelectedItemID = -1;
    HashSet<SkinType> cantBeHide = new HashSet<SkinType>()
    {
        SkinType.Boot,
        SkinType.Chest,
        SkinType.Eye,
        SkinType.Face,
        SkinType.Hand,
        SkinType.Glove,
        SkinType.Plant
    };
    Dictionary<SkinType, Button> dicItemsButtons;
    Dictionary<SkinType, HashSet<SkinType>> SkinGroups = new Dictionary<SkinType, HashSet<SkinType>>();
    SkinType[] currentSelectedSkins = new SkinType[1] { SkinType.Ornament };
    private bool initialized = false;

    private void Awake()
    {
        selectedItemsGO = new Dictionary<SkinType, GameObject>();
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

        SkinGroups.Add(SkinType.Helm, new HashSet<SkinType>()
        {
            SkinType.CustomHelmet
        });
        SkinGroups.Add(SkinType.CustomHelmet, new HashSet<SkinType>()
        {
            SkinType.Helm
        });
    }

    private void Start()
    {
        dicItemsButtons = new Dictionary<SkinType, Button>();
        dicItemsButtons.Add(SkinType.Ornament, OrnamentTypeButton);
        foreach (Button btn in ItemsTypeButtons)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                string typeName = btn.name;
                SkinType type = (SkinType)Enum.Parse(typeof(SkinType), typeName);
                Title.text = GetTitle(type);
                if (type == SkinType.Helm || type == SkinType.CustomHelmet)
                    StartCoroutine(BindItems(SkinType.Helm, SkinType.CustomHelmet));
                else
                    StartCoroutine(BindItems(type));
            });

            string typeName = btn.name;
            SkinType type = (SkinType)Enum.Parse(typeof(SkinType), typeName);
            dicItemsButtons.Add(type, btn);

            // set tooltip
            var tt = btn.gameObject.AddComponent<UITooltipSetter>();
            tt.Message = GetTitle(type);
            tt.UsePointerEvent = true;
        }
        initialized = true;
        HideEmptySlots();
        if ((currentSelectedSkins.Length == 1 && currentSelectedSkins[0] == SkinType.Ornament) || currentSelectedSkins.Length == 0)
            BindOrnaments();
        else
            BindItems(currentSelectedSkins);
    }

    private string GetTitle(SkinType type)
    {
        switch (type)
        {
            default:
                return "Inventory";
            case SkinType.Belt:
                return "Belts";
            case SkinType.Boot:
                return "Boots";
            case SkinType.Chest:
                return "Chestplate";
            case SkinType.Glove:
                return "Forearm guards";
            case SkinType.Helm:
                return "Helmets";
            case SkinType.Plant:
                return "Pants";
            case SkinType.Shoulder:
                return "Shoulder pads";
            case SkinType.Hat:
                return "Hats";
            case SkinType.Hand:
                return "Hands";
            case SkinType.Eye:
                return "Eyes";
            case SkinType.Face:
                return "Faces";
            case SkinType.Hair:
                return "Hair cuts";
            case SkinType.Wand:
                return "Magic Wand";
            case SkinType.Sword:
                return "Swords";
            case SkinType.Shield:
                return "Shields";
            case SkinType.Ornament:
                return "Ornaments";
            case SkinType.Pet:
                return "Pets";
            case SkinType.Hammer:
                return "War Hammers";
            case SkinType.CustomHelmet:
                return "Helmets";
            case SkinType.Axe:
                return "War Axes";
            case SkinType.Wings:
                return "Wings";
            case SkinType.Dagger:
                return "Daggers";
            case SkinType.Aureol:
                return "Aureols";
        }
    }

    private void OnEnable()
    {
        Events.SaveGetted -= Events_SaveGetted;
        Events.SaveGetted += Events_SaveGetted;

        if (initialized)
        {
            HideEmptySlots();
            if ((currentSelectedSkins.Length == 1 && currentSelectedSkins[0] == SkinType.Ornament) || currentSelectedSkins.Length == 0)
                BindOrnaments();
            else
                BindItems(currentSelectedSkins);
        }
    }

    private void OnDisable()
    {
        LobbyManager.Instance.StopForceOrnamentEnabled();
        Events.SaveGetted -= Events_SaveGetted;
    }

    private void Events_SaveGetted()
    {
        HideEmptySlots();
        if ((currentSelectedSkins.Length == 1 && currentSelectedSkins[0] == SkinType.Ornament) || currentSelectedSkins.Length == 0)
            BindOrnaments();
        else
            BindItems(currentSelectedSkins);
    }

    public void HideEmptySlots()
    {
        foreach (Button btn in ItemsTypeButtons)
        {
            string typeName = btn.name;
            SkinType type = (SkinType)Enum.Parse(typeof(SkinType), typeName);
            btn.gameObject.SetActive(SaveManager.CurrentSave.GetNbUnlockedPart(type) > 0);
            if (!btn.gameObject.activeSelf && currentSelectedSkins.ToList().Contains(type))
            {
                var list = currentSelectedSkins.ToList();
                list.Remove(type);
                currentSelectedSkins = list.ToArray();
            }
        }
        if (currentSelectedSkins.Length == 0)
            currentSelectedSkins = new SkinType[] { SkinType.Ornament };
    }

    SkinType lastSelectedItemType;
    private void Update()
    {
        if (lastSelectedItemID == -1)
            return;

        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    DodgeFromLight.Databases.SkinsData.SetSkinRarity(lastSelectedItemType, lastSelectedItemID, SkinRarityType.Common);
        //    BindItems();
        //}
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    DodgeFromLight.Databases.SkinsData.SetSkinRarity(lastSelectedItemType, lastSelectedItemID, SkinRarityType.Epic);
        //    BindItems();
        //}
        //if (Input.GetKeyDown(KeyCode.L))
        //{
        //    DodgeFromLight.Databases.SkinsData.SetSkinRarity(lastSelectedItemType, lastSelectedItemID, SkinRarityType.Legendary);
        //    BindItems();
        //}
        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    DodgeFromLight.Databases.SkinsData.SetSkinRarity(lastSelectedItemType, lastSelectedItemID, SkinRarityType.Rare);
        //    BindItems();
        //}
        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    DodgeFromLight.Databases.SkinsData.SetSkinRarity(lastSelectedItemType, lastSelectedItemID, SkinRarityType.Mythical);
        //    BindItems(currentSelectedSkins);
        //}
    }

    #region Bind items
    IEnumerator BindItems(params SkinType[] types)
    {
        UI_WorkerNotifier.Show("Loading Items");
        yield return null;

        Title.text = GetTitle(types[0]);
        OrnamentPanel.SetActive(false);
        ItemsPanel.SetActive(true);
        LobbyManager.Instance.StopForceOrnamentEnabled();
        // unselect last item button
        foreach (var type in currentSelectedSkins)
            if (dicItemsButtons.ContainsKey(type))
                dicItemsButtons[type].GetComponent<Image>().color = UnselectedItemButtonColor;
        currentSelectedSkins = types;

        foreach (Transform child in ItemsContainer)
            Destroy(child.gameObject);
        selectedItemsGO.Clear();
        yield return null;

        int nbItems = 0;
        foreach (SkinType type in types)
        {
            BindItemsByAllRarity(type, ref nbItems);
            yield return null;
        }

        foreach (var type in currentSelectedSkins)
            if (dicItemsButtons.ContainsKey(type))
                dicItemsButtons[type].GetComponent<Image>().color = SelectedItemButtonColor;
        UI_WorkerNotifier.Hide();
    }

    void BindItemsByAllRarity(SkinType type, ref int nbItems)
    {
        List<short> Skins = SaveManager.CurrentSave.GetUnlocked(type);
        if (Skins.Count == 0)
            return;
        BindItemsByRarity(SkinRarityType.Common, type, Skins, ref nbItems);
        BindItemsByRarity(SkinRarityType.Rare, type, Skins, ref nbItems);
        BindItemsByRarity(SkinRarityType.Epic, type, Skins, ref nbItems);
        BindItemsByRarity(SkinRarityType.Legendary, type, Skins, ref nbItems);
        BindItemsByRarity(SkinRarityType.Mythical, type, Skins, ref nbItems);
    }

    void BindItemsByRarity(SkinRarityType rarity, SkinType type, List<short> Skins, ref int nbItems)
    {
        foreach (short id in Skins)
        {
            if (DodgeFromLight.Databases.RewardData.SkinsRarities[rarity].ContainsKey(type) && DodgeFromLight.Databases.RewardData.SkinsRarities[rarity][type].Contains(id))
                BindItem(type, id, ref nbItems);
        }
    }

    void BindItem(SkinType type, short id, ref int nbItems)
    {
        nbItems++;
        Sprite tex = Resources.Load<Sprite>(@"Items/" + type.ToString() + "_" + id);
        GameObject item = Instantiate(ItemPrefab);
        item.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = DodgeFromLight.Databases.SkinsData.GetSkin(type, id).Sprite;
        SkinRarityType rarity = DodgeFromLight.Databases.RewardData.GetRarity(type, id);
        if (rarity != SkinRarityType.None)
            item.transform.GetChild(0).gameObject.GetComponent<Image>().color = DodgeFromLight.Databases.RewardData.GetRarityColor(rarity, 0.2f);
        else
            item.transform.GetChild(0).gameObject.GetComponent<Image>().color = Vector4.zero;

        item.transform.SetParent(ItemsContainer, false);
        item.GetComponent<Button>().onClick.AddListener(() =>
        {
            lastSelectedItemID = id;
            lastSelectedItemType = type;
            if (SaveManager.CurrentSave.GetCurrentPart(type) == id && !cantBeHide.Contains(type))
            {
                if (selectedItemsGO.ContainsKey(type))
                {
                    selectedItemsGO[type].transform.GetChild(2).GetComponent<Image>().color = UnselectedItemColor;
                    selectedItemsGO.Remove(type);
                }
                SaveManager.SetCurrentPart(type, -1);
                DFLClient.EquipSkin(type, -1);
            }
            else
            {
                // disable linked skins
                if (SkinGroups.ContainsKey(type))
                {
                    foreach (SkinType disablingType in SkinGroups[type])
                    {
                        if (SaveManager.CurrentSave.GetCurrentPart(disablingType) != -1)
                        {
                            if (selectedItemsGO.ContainsKey(disablingType))
                            {
                                selectedItemsGO[disablingType].transform.GetChild(2).GetComponent<Image>().color = UnselectedItemColor;
                                selectedItemsGO.Remove(disablingType);
                            }
                            SaveManager.SetCurrentPart(disablingType, -1);
                            DFLClient.EquipSkin(disablingType, -1);
                        }
                    }
                }

                // set current part as active
                if (selectedItemsGO.ContainsKey(type))
                    selectedItemsGO[type].transform.GetChild(2).GetComponent<Image>().color = UnselectedItemColor;
                selectedItemsGO[type] = item;
                SaveManager.SetCurrentPart(type, id);
                item.transform.GetChild(2).GetComponent<Image>().color = SelectedItemColor;
                DFLClient.EquipSkin(type, id);
            }
        });
        if (SaveManager.CurrentSave.GetCurrentPart(type) == id)
        {
            item.transform.GetChild(2).GetComponent<Image>().color = SelectedItemColor;
            if (!selectedItemsGO.ContainsKey(type))
                selectedItemsGO.Add(type, item);
            else
                selectedItemsGO[type] = item;
        }
        else
            item.transform.GetChild(2).GetComponent<Image>().color = UnselectedItemColor;
    }
    #endregion

    #region Bind ornaments
    void BindOrnaments()
    {
        Title.text = GetTitle(SkinType.Ornament);
        foreach (var type in currentSelectedSkins)
            if (dicItemsButtons.ContainsKey(type))
                dicItemsButtons[type].GetComponent<Image>().color = UnselectedItemButtonColor;
        currentSelectedSkins = new SkinType[1] { SkinType.Ornament };
        LobbyManager.Instance.StartForceOrnamentEnabled();
        OrnamentPanel.SetActive(true);
        ItemsPanel.SetActive(false);
        foreach (Transform t in OrnamentsContainer)
            Destroy(t.gameObject);

        foreach (short id in SaveManager.CurrentSave.GetUnlocked(SkinType.Ornament))
        {
            GameObject ornament = Instantiate(ornamentItemPrefab);
            GameObject ornamentInner = DodgeFromLight.Databases.OrnamentData.GetOrnament(id, DFLClient.CurrentUser.Name);
            ornament.transform.SetParent(OrnamentsContainer.transform, false);
            ornamentInner.GetComponent<RectTransform>().SetAndStretchParent(ornament.GetComponent<RectTransform>());

            if (id == SaveManager.CurrentSave.GetCurrentPart(SkinType.Ornament))
                ornament.transform.GetChild(0).GetComponent<Image>().color = Color.green;
            else
                ornament.transform.GetChild(0).GetComponent<Image>().color = Color.white;

            ornament.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                if (DFLClient.OnlineState == OnlineState.Online && DFLClient.LoginState == LoginState.LoggedIn)
                {
                    SaveManager.CurrentSave.SetCurrentPart(SkinType.Ornament, id);
                    DFLClient.EquipSkin(SkinType.Ornament, id);
                }
                else
                {
                    SaveManager.CurrentSave.SetCurrentPart(SkinType.Ornament, id);
                    BindOrnaments();
                }
            });
        }

        foreach (var type in currentSelectedSkins)
            if (dicItemsButtons.ContainsKey(type))
                dicItemsButtons[type].GetComponent<Image>().color = SelectedItemButtonColor;
    }
    #endregion
}