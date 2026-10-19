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
        EnsureNameLabel();
    }

    // 장비/물약 드랍 표시 (아이템 개편 Phase 7-5) — 프리팹에 텍스트가 없어 런타임에 이름 라벨을 만든다 (등급 색). 이미 인벤토리에 지급된 항목이라 클릭 = 확인(목록에서 제거)
    void EnsureNameLabel()
    {
        if(reward == null || (reward.reward_Type != Reward_Type.Equip && reward.reward_Type != Reward_Type.Potion)) return;
        GameObject go = new GameObject("NameLabel", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 0f);
        rect.offsetMax = new Vector2(-16f, 0f);
        TMPro.TextMeshProUGUI label = go.AddComponent<TMPro.TextMeshProUGUI>();
        if(M_LanguageManager.currnetFont != null) label.font = M_LanguageManager.currnetFont;
        label.fontSize = 22;
        label.alignment = TMPro.TextAlignmentOptions.Left;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 22;
        label.color = !string.IsNullOrEmpty(reward.rewardColorHex) && ColorUtility.TryParseHtmlString(reward.rewardColorHex, out Color color) ? color : Color.white;
        label.text = (reward.reward_Type == Reward_Type.Equip ? "[장비] " : "[물약] ") + reward.rewardName;
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
            case Reward_Type.Item:   // (구 유물 — 폐기)
            case Reward_Type.Equip:  // 장비/물약 드랍 — 이미 지급됨, 목록에서만 제거 (Phase 7-5)
            case Reward_Type.Potion:
                rewardOwner.GetComponent<GamePlayerDeck>().CmdRewardRemove(reward.guid, reward.reward_Type);
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
