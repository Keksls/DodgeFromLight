using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Reward : MonoBehaviour
{
    // rewardPanel vars
    public GameObject RewardPanel;
    public Button BtnCloseReward;
    public Button BtnOpenReward;
    public Animator RewardAnimator;
    public Image RewardSkinShadow;
    public Image RewardSkin;
    public Image RewardRarityColor;
    public Text RewardRemaningpendingText;
    public Image BtnOpenRewardBackColor;

    public void ShowPendingRewardsPanel()
    {
        RewardAnimator.Play("RewardSkinIdle");
        List<RewardItem> rewards = GameContext.Databases.RewardData.GetPendingRewards(SaveManager.CurrentSave.Experience);
        if (rewards.Count > 0)
        {
            RewardItem reward = rewards[0];
            RewardSkinShadow.enabled = false;
            RewardRarityColor.color = GameContext.Databases.RewardData.GetRarityColor(GameContext.Databases.RewardData.GetRarity(reward.SkinType, reward.SkinID), 1f);
            BtnOpenRewardBackColor.color = RewardRarityColor.color;
            InitPendingPanel();
        }
    }

    private void InitPendingPanel()
    {
        List<RewardItem> rewards = GameContext.Databases.RewardData.GetPendingRewards(SaveManager.CurrentSave.Experience);
        if (rewards.Count > 0)
        {
            RewardPanel.SetActive(true);
            BtnOpenReward.gameObject.SetActive(true);
            RewardRemaningpendingText.text = rewards.Count.ToString();
            RewardItem reward = rewards[0];
            if (reward.Type == RewardType.Skin)
                BtnOpenRewardBackColor.color = GameContext.Databases.RewardData.GetRarityColor(GameContext.Databases.RewardData.GetRarity(reward.SkinType, reward.SkinID), 1f);
            else
                BtnOpenRewardBackColor.color = GameContext.Databases.RewardData.GetRarityColor(SkinRarityType.Legendary, 0.66f);
            BtnOpenReward.onClick.RemoveAllListeners();
            BtnOpenReward.onClick.AddListener(() =>
            {
                BtnOpenReward.gameObject.SetActive(false);
                if (reward.Type == RewardType.Skin)
                {
                    Sprite tex = Resources.Load<Sprite>(@"Items/" + reward.SkinType.ToString() + "_" + reward.SkinID);
                    RewardSkin.sprite = tex;
                    RewardSkin.color = Color.white;
                    RewardSkinShadow.sprite = tex;
                    RewardSkinShadow.enabled = true;
                    RewardRarityColor.color = GameContext.Databases.RewardData.GetRarityColor(GameContext.Databases.RewardData.GetRarity(reward.SkinType, reward.SkinID), 1f);
                }
                else
                {
                    Sprite tex = GameContext.Databases.OrnamentData.GetOrnamentImage(reward.Ornament);
                    OrnamentDataAsset data = GameContext.Databases.OrnamentData.GetOrnamentData(reward.Ornament);
                    RewardSkin.sprite = tex;
                    RewardSkin.color = data.OrnamentColor;
                    RewardSkinShadow.sprite = tex;
                    RewardSkinShadow.enabled = true;
                    RewardRarityColor.color = GameContext.Databases.RewardData.GetRarityColor(SkinRarityType.Legendary, 0.66f);
                }
                RewardAnimator.Play("RewardSkinIdle");
                StartCoroutine(OpenReward(reward));
            });
        }
    }

    IEnumerator OpenReward(RewardItem reward)
    {
        bool saved = false;
        GameContext.Databases.RewardData.AcceptReward(reward, (short)(SaveManager.CurrentSave.LastRewardLevel + 1), (success, errorMess) =>
        {
            if (!success)
                GameContext.UI_Notifications.Notify(errorMess);
            //BindItems();
            RewardAnimator.Play("RewardSkinAnimation");
            saved = true;
        });
        while (!saved)
            yield return null;
        yield return new WaitForSeconds(2f);
        if (GameContext.Databases.RewardData.HasRewards(SaveManager.CurrentSave.Experience))
            InitPendingPanel();
    }
}
