using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    public string[] TutorialMapsFolderNames;
    public Dictionary<string, FullMap> Maps;
    public GameObject GrapplePanel;
    public GameObject BombePanel;
    public GameObject DiscoballPanel;
    public GameObject MirrorPanel;
    public GameObject RemoteControlPanel;
    public GameObject SpeedBootsPanel;
    public GameObject TimeControllerPanel;
    public GameObject KeysPanel;
    public Image Z;
    public Image Q;
    public Image S;
    public Image D;
    public Image Jump;
    public Color PressedKeyColor;
    public Color UnpressedKeyColor;
    public Transform Canvas;

    void Awake()
    {
        //ResetAvancement();
        //SetAvancement(13);
        DodgeFromLight.TutorialController = this;
        Maps = new Dictionary<string, FullMap>();
        foreach (string folder in TutorialMapsFolderNames)
        {
            string folderPath = Application.streamingAssetsPath + @"\" + folder;
            Maps.Add(folder, new FullMap(GridManager.GetGrid(folderPath), GridManager.GetMap(folderPath)));
        }
        HideAllpanels();
    }

    private void OnEnable()
    {
        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
        Events.SetGear -= Events_SetGear;
        Events.SetGear += Events_SetGear;
    }

    private void OnDisable()
    {
        Events.StartMap -= Events_StartMap;
        Events.SetGear -= Events_SetGear;
    }

    private void Update()
    {
        if (DodgeFromLight.CurrentRules == null || !DodgeFromLight.CurrentRules.Tutorial)
            return;
        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Forward).IsPressed())
            Z.color = PressedKeyColor;
        else
            Z.color = UnpressedKeyColor;

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.left).IsPressed())
            Q.color = PressedKeyColor;
        else
            Q.color = UnpressedKeyColor;

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Backward).IsPressed())
            S.color = PressedKeyColor;
        else
            S.color = UnpressedKeyColor;

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Right).IsPressed())
            D.color = PressedKeyColor;
        else
            D.color = UnpressedKeyColor;

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Jump).IsPressed())
            Jump.color = PressedKeyColor;
        else
            Jump.color = UnpressedKeyColor;
    }

    private void Events_SetGear(GearType type)
    {
        if (!DodgeFromLight.CurrentRules.Tutorial)
            return;
        HideAllpanels();
        switch (type)
        {
            case GearType.None:
                KeysPanel.SetActive(true);
                break;
            case GearType.Bomb:
                BombePanel.SetActive(true);
                break;
            case GearType.DiscoBall:
                DiscoballPanel.SetActive(true);
                break;
            case GearType.Grapple:
                GrapplePanel.SetActive(true);
                break;
            case GearType.InvisibilityCloak:
                break;
            case GearType.Mirror:
                MirrorPanel.SetActive(true);
                break;
            case GearType.PowderOfDarkness:
                break;
            case GearType.RemoteControl:
                RemoteControlPanel.SetActive(true);
                break;
            case GearType.SpeedBoots:
                SpeedBootsPanel.SetActive(true);
                break;
            case GearType.TimeController:
                TimeControllerPanel.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void Events_StartMap()
    {
        if (!DodgeFromLight.CurrentRules.Tutorial)
            return;
        HideAllpanels();
        KeysPanel.SetActive(true);
    }

    public void ShowInputPanel()
    {
        HideAllpanels();
    }

    public void HideAllpanels()
    {
        foreach (Transform t in Canvas)
            t.gameObject.SetActive(false);
    }

    public void ResetAvancement()
    {
        PlayerPrefs.DeleteKey("TutorialStepIndex");
        PlayerPrefs.DeleteKey("TutorialRewardsTaken");
    }

    public void SetAvancement(int stepIndex)
    {
        PlayerPrefs.SetInt("TutorialStepIndex", stepIndex);
    }

    public int GetAvancement()
    {

        if (PlayerPrefs.HasKey("TutorialStepIndex"))
            return PlayerPrefs.GetInt("TutorialStepIndex");
        else
            return -1;
    }

    public bool HasStartTutorial()
    {
        return PlayerPrefs.HasKey("TutorialStepIndex");
    }

    public bool HasFinishTutorial()
    {
        return GetAvancement() >= TutorialMapsFolderNames.Length - 1;
    }

    public FullMap GetCurrentMap()
    {
        if (HasStartTutorial())
            return Maps[TutorialMapsFolderNames[GetAvancement()]];
        else
            return null;
    }

    public bool HasTakeRewards()
    {
        if (PlayerPrefs.HasKey("TutorialRewardsTaken"))
            return PlayerPrefs.GetInt("TutorialRewardsTaken") == 1;
        else
            return false;
    }

    public void TakeRewards()
    {
        PlayerPrefs.SetInt("TutorialRewardsTaken", 1);
    }

    /// <summary>
    /// return true if just finished tutorial
    /// </summary>
    /// <returns></returns>
    public void FinishCurrentMap()
    {
        SetAvancement(GetAvancement() + 1);
    }

    public List<string> GetRemaningMapsFolders()
    {
        List<string> folders = new List<string>();
        int av = GetAvancement() + 1;
        for (int i = av; i < TutorialMapsFolderNames.Length; i++)
            folders.Add(TutorialMapsFolderNames[i]);
        return folders;
    }
}