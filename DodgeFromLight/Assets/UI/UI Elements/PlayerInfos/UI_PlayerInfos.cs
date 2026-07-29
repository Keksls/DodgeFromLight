using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UI_PlayerInfos : MonoBehaviour
{
    public GameObject PreviewGeneratorContainer;
    public GameObject PreviewGeneratorPlayer;
    public TextMeshProUGUI LvlText;
    public TextMeshProUGUI NameText;
    public RawImage PlayerPrev;
    public Slider XPSlider;
    public Button BtnPendingRewards;

    private void Start()
    {
        Events.SaveGetted -= Events_SaveGetted;
        Events.SaveGetted += Events_SaveGetted;
        Events.SaveUpdated -= Events_SaveUpdated;
        Events.SaveUpdated += Events_SaveUpdated;

        UpdatePlayerInfos();
    }

    private void Events_SaveUpdated()
    {
        UpdatePlayerInfos();
    }

    private void Events_SaveGetted()
    {
        UpdatePlayerInfos();
    }

    private void OnDestroy()
    {
        Events.SaveGetted -= Events_SaveGetted;
        Events.SaveUpdated -= Events_SaveUpdated;
    }

    public void UpdatePlayerInfos()
    {
        // set slider avancement
        int currentLvl = GameContext.Databases.RewardData.GetLevel(SaveManager.CurrentSave.Experience);
        float xp = SaveManager.CurrentSave.Experience;
        float startXp = GameContext.Databases.RewardData.GetXpForLevel(currentLvl);
        float nextXp = GameContext.Databases.RewardData.GetXpForLevel(currentLvl + 1);
        StopAllCoroutines();
        StartCoroutine(AnimateXpSlider((xp - startXp) / (nextXp - startXp)));

        // display rewards button
        if (GameContext.Databases.RewardData.HasRewards(SaveManager.CurrentSave.Experience))
            BtnPendingRewards.gameObject.SetActive(true);
        else
            BtnPendingRewards.gameObject.SetActive(false);

        // set texts
        LvlText.text = "Lvl " + currentLvl;
        NameText.text = SaveManager.CurrentSave.DisplayName;
        NameText.colorGradient = GameContext.Databases.OrnamentData.GetOrnamentData(SaveManager.CurrentSave.GetCurrentPart(SkinType.Ornament)).TextColor;
        NameText.gameObject.GetComponent<AnimateTextGradient>().Initialize();

        // update preview
        UpdatePlayerPrev();
        SetTooltips();
    }

    public void UpdatePlayerPrev()
    {
        PreviewGeneratorContainer.SetActive(true);
        // set current save
        PreviewGeneratorPlayer.GetComponent<PlayerCharacter>().SetSave(SaveManager.CurrentSave);
        PreviewGeneratorPlayer.SetLayer(LayerMask.NameToLayer("PreviewGenerator"));
    }

    IEnumerator AnimateXpSlider(float val)
    {
        float enlapsed = 0f;
        while (enlapsed < .5f)
        {
            XPSlider.value = Mathf.Lerp(0f, val, enlapsed / .5f);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        XPSlider.value = val;
    }

    private void SetTooltips()
    {
        UITooltipSetter xpTP = XPSlider.gameObject.AddComponent<UITooltipSetter>();
        xpTP.MessageFunc = () =>
        {
            int currentLvl = GameContext.Databases.RewardData.GetLevel(SaveManager.CurrentSave.Experience);
            float xp = SaveManager.CurrentSave.Experience;
            float startXp = GameContext.Databases.RewardData.GetXpForLevel(currentLvl);
            float nextXp = GameContext.Databases.RewardData.GetXpForLevel(currentLvl + 1);
            float percent = (xp - startXp) / (nextXp - startXp) * 100f;
            return ((int)xp - (int)startXp) + " / " + ((int)nextXp - (int)startXp) + " (" + (int)percent + " %)";
        };

        UITooltipSetter pendingRewardsTP = BtnPendingRewards.gameObject.AddComponent<UITooltipSetter>();
        pendingRewardsTP.MessageFunc = () =>
        {
            if (GameContext.Databases.RewardData.HasRewards(SaveManager.CurrentSave.Experience))
                return GameContext.Databases.RewardData.GetNbPendingRewards(SaveManager.CurrentSave.Experience) + " Pending Rewards";
            else
                return "No pending rewards";
        };
    }
}
