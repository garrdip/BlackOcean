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
    public GameObject cardIcon;
    public GameObject cardIconLight;
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
            case Reward_Type.Card: // 카드 보상 선택 팝업 호출
                int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == rewardOwner.netId);
                BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
                battleResultPopUp.ChangeRewardLayoutState(index, true);
                AudioClip cardSound = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_discard"));
                M_SoundManager.instance.PlaySFX(cardSound, cardSound.length);
                break;
            case Reward_Type.Item:  // TODO : 선택한 유물 보상 데이터를 플레이어 데이터에 추가
                rewardOwner.GetComponent<GamePlayerDeck>().CmdRewardRemove(reward.guid, Reward_Type.Item);
                RewardService.instance.RemoveRewardListItem(rewardObject);
                AudioClip itemSound = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("event_cardstore_purchase"));
                M_SoundManager.instance.PlaySFX(itemSound, itemSound.length);
                break;
            case Reward_Type.Gold: // TODO : 선택한 골드 보상 데이터를 플레이어 데이터에 추가
                rewardOwner.GetComponent<GamePlayerDeck>().CmdRewardRemove(reward.guid, Reward_Type.Gold);
                RewardService.instance.RemoveRewardListItem(rewardObject);
                AudioClip coinSound = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("event_cardstore_purchase"));
                M_SoundManager.instance.PlaySFX(coinSound, coinSound.length);
                break;
        }
    }

    private void ChangeRewardIconActiveByType(bool isActive)
    {
        switch(reward.reward_Type){
            case Reward_Type.Item:
                break;
            case Reward_Type.Card:
                cardIcon.SetActive(isActive);
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
            case Reward_Type.Card:
                cardIconLight.SetActive(isActive);
                break;
            case Reward_Type.Gold:
                coinIconLight.SetActive(isActive);
                break;
        }
        itemBarLight.SetActive(isActive);
    }
    
}
