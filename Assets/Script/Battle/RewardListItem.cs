using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class RewardListItem : MonoBehaviour
{
    public Reward reward;
    public GamePlayer rewardOwner;
    private RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public GameObject itemBarLight;
    public GameObject coinIcon;
    public GameObject coinIconLight;
    public Image rewardItemIcon;



    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ChangeRewardIconActiveByType(true);
    }

    void OnDestroy()
    {
        canvasGroup.DOKill();
        rectTransform.DOKill();
    }

    public void OnPointerClickRewardListItem()
    {
        canvasGroup.DOFade(0f, 0.5f);
        rectTransform.DOAnchorPosX(Screen.width, 0.5f).OnComplete(() =>
        {
            ChangeRewardListItemStateByType(gameObject, rewardOwner, reward);
        });
    }

    public void OnPointerEnterRewardListItem()
    {
        ChangeRewardIconLightActiveByType(true);
    }

    public void OnPointerExitRewardListItem()
    {
        ChangeRewardIconLightActiveByType(false);
    }

    private void ChangeRewardListItemStateByType(GameObject rewardObject, GamePlayer rewardOwner, Reward reward)
    {
        switch(reward.reward_Type){
            case Reward_Type.Item:  // TODO : 선택한 유물 보상 데이터를 플레이어 데이터에 추가
                rewardOwner.GetComponent<GamePlayerDeck>().CmdRewardRemove(reward.guid, Reward_Type.Item);
                RewardService.instance.RemoveRewardListItem(rewardObject);
                AudioClip itemSound = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "event_cardstore_purchase");
                M_SoundManager.instance.PlaySFX(itemSound, itemSound.length);
                break;
            case Reward_Type.Gold: // 골드 수령 — 서버가 소유 골드에 가산
                rewardOwner.GetComponent<GamePlayerDeck>().CmdRewardRemove(reward.guid, Reward_Type.Gold);
                RewardService.instance.RemoveRewardListItem(rewardObject);
                AudioClip coinSound = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "event_cardstore_purchase");
                M_SoundManager.instance.PlaySFX(coinSound, coinSound.length);
                break;
        }
    }

    private void ChangeRewardIconActiveByType(bool isActive)
    {
        switch(reward.reward_Type){
            case Reward_Type.Item:
                break;
            case Reward_Type.Gold:
                coinIcon.SetActive(isActive);
                break;
        }
    }

    private void ChangeRewardIconLightActiveByType(bool isActive)
    {
        switch(reward.reward_Type){
            case Reward_Type.Item:
                break;
            case Reward_Type.Gold:
                coinIconLight.SetActive(isActive);
                break;
        }
        itemBarLight.SetActive(isActive);
    }

}
