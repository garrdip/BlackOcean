using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;
using AYellowpaper.SerializedCollections;
using ProjectD;

public class CardOnDeck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Card card;
    public GamePlayer cardOwner;
    public CanvasGroup canvasGroup;

    [Header("CardOnDeck Image 컴포넌트")]
    public List<GameObject> cardImages = new List<GameObject>();
    public List<Material> cardDissolveMaterials = new List<Material>();
    public Image cardBackground;
    public Image cardIllust;
    public Image cardImageFrame;
    public Image cardGradeFrame;
    public Image cardEmblem;

    [Header("CardOnDeck Text 컴포넌트")]
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardDescription;
    public TextMeshProUGUI textCardCost;
    public GameObject cardSoldOut;

    [Header("CardOnHand 경험치 바")]
    public GameObject cardExpBar; // 경험치 바 
    public GameObject expBlockPrefab; // 경험치 바 내부 블록 오브젝트 프리팹
    public VerticalLayoutGroup verticalLayoutGroup;
    public List<GameObject> expBlocks = new List<GameObject>(); // 경험치 바 내부 블록 리스트

    private Vector3 originScale;
    public bool isShopCardInfo = false; // 카드 상점에서 정보 자세히 보기용 카드인지 여부(카드 상점 팝업창에 미리 생성되어있는 카드 정보용 오브젝트만 true로 설정되어있음)
    public bool isEnhancedPreviewCard = false;
    public bool isRemovePreviewCard = false;

    void Start()
    {
        originScale = isShopCardInfo ? new Vector3(1.8f, 1.8f, 1.8f) : Vector3.one;
        initCardData(card);
        InitCardTemplateByCharacter(card);
        if(card.isSoldout){
            ChangeCardOnDeckSoldOutState();
        }
    }

    // CardData의 스프라이트 데이터로부터 선택한 캐릭터의 카드 이미지 세팅
    public void InitCardTemplateByCharacter(Card card)
    {
        switch(card.baseCard.character){
            case Character.GEORK:
                SerializedDictionary<string, Sprite> georkCardSprites = CardData.instance.characterCardTemplate[Character.GEORK];
                InitCardTemplateByCardType(card, georkCardSprites);
                InitCardIllust(card, georkCardSprites);
                InitCardTemplateByCardEnhanced(card, georkCardSprites);
                InitCardExpBar(card, georkCardSprites);
                break;
            case Character.ERIS:
                SerializedDictionary<string, Sprite> erisCardSprites = CardData.instance.characterCardTemplate[Character.ERIS];
                InitCardTemplateByCardType(card, erisCardSprites);
                InitCardIllust(card, erisCardSprites);
                InitCardTemplateByCardEnhanced(card, erisCardSprites);
                InitCardExpBar(card, erisCardSprites);
                break;
            case Character.HONGDANHYANG:
                SerializedDictionary<string, Sprite> danhyangCardSprites = CardData.instance.characterCardTemplate[Character.HONGDANHYANG];
                InitCardTemplateByCardType(card, danhyangCardSprites);
                InitCardIllust(card, danhyangCardSprites);
                InitCardTemplateByCardEnhanced(card, danhyangCardSprites);
                InitCardExpBar(card, danhyangCardSprites);
                break;
        }
    }

    // 카드 타입에 따라 외형 틀 세팅
    private void InitCardTemplateByCardType(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(!card.baseCard.cardNumber.Equals("HA")){
            switch(card.baseCard.cardType){
                case CardType.ATTACK:
                    cardBackground.sprite = sprites[Const.ATTACK_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.ATTACK_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.ATTACK_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.BLESS:
                    cardBackground.sprite = sprites[Const.BLESS_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.BLESS_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.BLESS_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.STRATEGY:
                    cardBackground.sprite = sprites[Const.STRATEGY_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.STRATEGY_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.STRATEGY_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.HERO:
                    cardBackground.sprite = sprites[Const.HERO_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.HERO_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.HERO_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
                case CardType.CURSE:
                    cardBackground.sprite = sprites[Const.CURSE_CARD_BG];
                    cardImageFrame.sprite = sprites[Const.CURSE_IMAGE_FRAME];
                    cardEmblem.sprite = sprites[Const.CURSE_EMBLEM];
                    cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
                    break;
            }
        }
    }

    // 카드 일러스트 세팅
    private void InitCardIllust(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(!card.baseCard.cardNumber.Contains("HA")){
            if(card.baseCard.cardNumber.Contains("_E")){
                // 강화카드의 경우 _E 문자열을 제거하여 아틀라스에서 스프라이트 조회
                int idx = card.baseCard.cardNumber.IndexOf("_E");
                if(idx != -1){
                    string cardNumber = card.baseCard.cardNumber.Substring(0, idx);
                    cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(cardNumber);
                }
            }else{
                cardIllust.sprite = CardData.instance.cardIllustAtlas.GetSprite(card.baseCard.cardNumber);
            }
        }
    }

    // 카드 강화 상태 프레임 세팅
    private void InitCardTemplateByCardEnhanced(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        if(card.isEnhanced){
            cardGradeFrame.sprite = sprites[Const.ENHANCE_NORMAL_GRADE_FRAME];
        }else{
            cardGradeFrame.sprite = sprites[Const.NORMAL_GRADE_FRAME];
        }
    }

    // 카드 경험치 바 초기화 : card 데이터에서 최대 경험치 정보를 가져와 해당 숫자 만큼의 경험치 바 내부 블록 생성
    private void InitCardExpBar(Card card, SerializedDictionary<string, Sprite> sprites)
    {
        // 철귀 이동카드는 경험치 오브젝트 초기화 제외
        if(!card.baseCard.cardNumber.Equals("HA")){
            // 최대 경험치 만큼 내부 블록 생성
            for(int i=0; i<card.baseCard.maxExperience; i++){
                GameObject expBlock = Instantiate(expBlockPrefab);
                expBlock.transform.SetParent(verticalLayoutGroup.transform, false);
                expBlock.GetComponent<Image>().sprite = sprites[Const.EXP_BAR_INACTIVE];
                expBlocks.Add(expBlock);
            }
            // expBlocks 역순으로 전환(블록이 아래부터 쌓이도록)
            expBlocks.Reverse();
            // 경험치 블록 리스트에서 현재 카드의 경험치 숫자 만큼 블록 생상을 변경
            for(int j=0; j<card.experience; j++){
                expBlocks[j].GetComponent<Image>().sprite = sprites[Const.EXP_BAR_ACTIVE];
            }
        }   
    }

    void OnDisable()
    {
        DOTween.Kill(transform); // 비활성화 될 때 DoTween 프로세스 킬
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if(!isShopCardInfo){
            if(PopUpUIManager.instance.isCardEnhancePopUpOpen || PopUpUIManager.instance.isCardRemovePopUpOpen || PopUpUIManager.instance.cardRemovePopUp.GetComponent<CardRemovePopUp>().cardRemovePreview.activeSelf){
                return;
            }else if(PopUpUIManager.instance.isMercuriusPopUpOpen){
                MercuriusPopUp mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
                mercuriusPopUp.ShowHoverdCardInfo(card);
            }else{
                transform.DOScale(originScale * 1.3f, 0.3f);
            }
            GraphicRaycaster graphicRaycaster = textCardDescription.GetComponentInParent<GraphicRaycaster>();
            TextDetector.instance.StartTextDetect(graphicRaycaster);
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("event_cardstore_mouseover_2"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if(!isShopCardInfo){
            if(PopUpUIManager.instance.isCardEnhancePopUpOpen || PopUpUIManager.instance.isCardRemovePopUpOpen || PopUpUIManager.instance.cardRemovePopUp.GetComponent<CardRemovePopUp>().cardRemovePreview.activeSelf){
                return;
            }else if(PopUpUIManager.instance.isMercuriusPopUpOpen){
                MercuriusPopUp mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
                mercuriusPopUp.HideHoverdCardInfo();
            }else{
                transform.DOScale(originScale, 0.3f);
            }
            TextDetector.instance.StopTextDetect();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 전투 결과 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.isBattleResultPopUpOpen){
            if(cardOwner != null){
                RequsetCardReward();
                ChangeCardOnDeckRewardedState(M_TurnManager.instance.rewardCardObjects);
                CardOnDeckClickAnimation();
                AudioClip rewardCardAudio = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_game_win_reward"));
                M_SoundManager.instance.PlaySFX(rewardCardAudio, rewardCardAudio.length);
            }
        }
        // MercuriusPopUp이 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.isMercuriusPopUpOpen && !card.isSoldout){
            RequestCardPurchase();
            ChangeCardOnDeckSoldOutState();
            CardOnDeckClickAnimation();
            AudioClip shopCardPurchaseAudio = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("event_cardstore_purchase"));
            M_SoundManager.instance.PlaySFX(shopCardPurchaseAudio, shopCardPurchaseAudio.length);
        }
        //DeckDrawPopUp이 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.isDeckDrawPopUpOpen){
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            GamePlayerDeck gamePlayerDeck = playerInterface.currentGamePlayer.GetComponent<GamePlayerDeck>();
            PopUpUIManager.instance.HandleHideDeckDrawPopUp();
            DeckDrawPopUp deckDrawPopUp = PopUpUIManager.instance.deckDrawPopUp.GetComponent<DeckDrawPopUp>();
            int index = deckDrawPopUp.addtionDrawCardObjects.FindIndex((cardObject) =>  cardObject.GetComponent<CardOnDeck>() == this); // 팝업에서 선택한 카드의 인덱스 조회
            gamePlayerDeck.CmdSpawnAddtionDrawCard(card, index);
        }
        if(PopUpUIManager.instance.isCardEnhancePopUpOpen){
            CardEnhancePopUp.instance.HandleCardEnhancePreviewOpen(); // 선택한 카드 강화 프리뷰 팝업창 호출
            if(CardEnhancePopUp.instance.cardEnhancePreview.activeSelf && !isEnhancedPreviewCard){
                CardEnhancePopUp.instance.CreateEnhancePreviewCard(card);
                CardEnhancePopUp.instance.selectCardGuid = card.guid;
            }
        }
        if(PopUpUIManager.instance.isCardRemovePopUpOpen){
            CardRemovePopUp.instance.HandleCardRemovePreviewOpen(); // 선택한 카드 제거 프리뷰 팝업창 호출
            if(CardRemovePopUp.instance.cardRemovePreview.activeSelf && !isRemovePreviewCard){
                CardRemovePopUp.instance.CreateRemovePreviewCard(card);
                CardRemovePopUp.instance.selectCardGuid = card.guid;
            }
        }
    }

    // 보상카드 선택 커맨드 전송 및 오브젝트 정리
    private void RequsetCardReward()
    {
        int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == cardOwner.netId);
        BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
        battleResultPopUp.ChangeRewardLayoutState(index, false);
        cardOwner.GetComponent<GamePlayerDeck>().CmdRewardRemove(card.guid, Reward_Type.Card);
        GameObject rewardObject = M_TurnManager.instance.rewardObjects.Find((rewardObject) => rewardObject.GetComponent<RewardListItem>().reward.guid == card.guid);
        M_TurnManager.instance.RemoveRewardListItem(rewardObject);
    }

    // 보상카드중 선택한 카드의 탭에 있는 카드들은 모두 선택불가 및 알파값 0.5 설정
    private void ChangeCardOnDeckRewardedState(List<GameObject> rewardCards)
    {
        foreach(GameObject gameObject in rewardCards){
            CardOnDeck cardOnDeck = gameObject.GetComponent<CardOnDeck>();
            if(cardOnDeck.cardOwner == cardOwner){
                cardOnDeck.canvasGroup.interactable = false;
                cardOnDeck.canvasGroup.blocksRaycasts = false;
                cardOnDeck.canvasGroup.alpha = 0.5f;
            }
        }
    }

    // 선택한 카드 구매 커맨드 전송
    private void RequestCardPurchase()
    {
        List<Card> cards = M_TurnManager.instance.npc_Mercurius.shopCardDictionary[cardOwner];
        int index = cards.FindIndex((c) => c.guid.Equals(card.guid));
        if(index != -1){
            M_TurnManager.instance.CmdChangeShopCardSoldOut(cardOwner, index);
        }
    }

    // CardOnDeck SoldOut 상태로 변경 및 컴포넌트들 알파값 0.5 변경
    public void ChangeCardOnDeckSoldOutState()
    {
        if(!isShopCardInfo){
            cardSoldOut.SetActive(true);
            // 캔버스 그룹 요소들 상호작용 이벤트 비활성화 
            canvasGroup.interactable = false;
            // cardSoldOut 오브젝트도 canvasGroup에 포함되기 때문에 카드요소들 하나씩 직접 알파값 변경
            cardBackground.color = new Color(cardBackground.color.r, cardBackground.color.g, cardBackground.color.b, 0.5f);
            cardIllust.color = new Color(cardIllust.color.r, cardIllust.color.g, cardIllust.color.b, 0.5f);
            cardImageFrame.color = new Color(cardImageFrame.color.r, cardImageFrame.color.g, cardImageFrame.color.b, 0.5f);
            cardGradeFrame.color = new Color(cardGradeFrame.color.r, cardGradeFrame.color.g, cardGradeFrame.color.b, 0.5f);
            cardEmblem.color = new Color(cardEmblem.color.r, cardEmblem.color.g, cardEmblem.color.b, 0.5f);
            textCardName.color = new Color(textCardName.color.r, textCardName.color.g, textCardName.color.b, 0.5f);
            textCardDescription.color = new Color(textCardDescription.color.r, textCardDescription.color.g, textCardDescription.color.b, 0.5f);
            textCardCost.color = new Color(textCardCost.color.r, textCardCost.color.g, textCardCost.color.b, 0.5f);
            // 카드 경험치바 하위요소도 캔버스 그룹으로 묶어 한번에 알파값 변경
            cardExpBar.GetComponent<CanvasGroup>().alpha = 0.5f;
        }
    }

    // 팝업이 활성화된 상태에서 CardOnDeck 공통 클릭 이벤트
    private void CardOnDeckClickAnimation()
    {
        if(cardOwner != null){
            // 카드 인스턴스 생성
            Card card = this.card.CardDeepCopy(false);
            // 애니매이션용 카드 오브젝트 복사본 생성
            GameObject cardOnHandChoosed = CreateCardOnHandChoosed(card);
                
            // 턴 매니저에 저장된 현재 참가한 플레이어들의 타겟오브젝트 리스트에서 로컬플레이어의 타겟오브젝트 조회
            TargetObject currentPlayer = M_TurnManager.instance.GetCurrentPlayerTargetObject(cardOwner);

            // 이동 위치는 현재 플레이어 타겟오브젝트 위치
            Vector3 targetPosition = currentPlayer.avatar.GetComponent<PolygonCollider2D>().bounds.center;
            float height = 2f;
            float duration = 1f;
            Vector3 startPos = cardOnHandChoosed.transform.position;
            Vector3 midPos = (startPos + targetPosition) / 2f;
            midPos.y += height;
            Vector3[] path = new Vector3[] { startPos, midPos, targetPosition };
            
            // DOTween을 사용하여 포물선 이동 애니메이션 생성
            cardOnHandChoosed.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.5f);
            cardOnHandChoosed.transform.DOPath(path, duration, PathType.CatmullRom)
                .SetEase(Ease.OutQuint)
                .OnComplete(() => {
                    cardOnHandChoosed.GetComponent<CardOnHandChoosed>().isTweening = false;
                    cardOwner.GetComponent<GamePlayerDeck>().CmdAddDeck(card);
                    Destroy(cardOnHandChoosed);
                });
        }
    }

    // 애니매이션용으로 사용될 선택된 보상카드의 복사 오브젝트 생성
    private GameObject CreateCardOnHandChoosed(Card card)
    {
        GameObject cardOnHandChoosed = Instantiate(PopUpUIManager.instance.CardOnHandChoosedPrefab);
        cardOnHandChoosed.GetComponent<CardOnHandChoosed>().card = card;
        cardOnHandChoosed.GetComponent<CardOnHandChoosed>().isTweening = true;
        cardOnHandChoosed.transform.SetParent(GameUIManager.instance.RootGameObject.transform);
        cardOnHandChoosed.transform.position = new Vector3(0f, 0f, 0f);

        return cardOnHandChoosed;
    }

    // 카드 정보 뷰 설정
    public void initCardData(Card card)
    {
        if(card.experience >= card.baseCard.maxExperience || card.isEnhanced || card.tempEnhanced)
        {
            CardBase cardBase = CardData.instance.cards.Find(x => x.cardNumber == card.baseCard.cardNumber + "_E");
            if(cardBase != null){
                textCardName.text = cardBase.name;
                textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(cardBase.description);
            }
        }
        else
        {
            textCardName.text = card.baseCard.name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(card.baseCard.description);
        }

        textCardDescription.text += '\n';
        textCardDescription.text += '\n';
        foreach(CardCharacteristic character in card.baseCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>" + '\n';
        foreach(CardCharacteristic character in card.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>" + '\n';
        
        if(card.baseCard.cardCharacteristics.Exists( x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(card.baseCard.cardType == NetworkClient.connection.identity.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>().previousCardType)
            {
                textCardCost.text = "<b><color=green>" +((card.baseCard.cost + card.costAddition - 1) <= 0 ? "0" : (card.baseCard.cost + card.costAddition - 1).ToString()) + "</color></b>";
            }
            else
            {
                textCardCost.text = "<b><color=red>"+ (card.baseCard.cost + card.costAddition + 1).ToString() + "</color></b>";
            }
        }
        else textCardCost.text = (card.baseCard.cost + card.costAddition).ToString();
    }

    // CardOnDeck Dissolve 효과
    public IEnumerator CardOnDeckDissolve(System.Action callback = null)
    {
        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            float dissolveRatio = timer / duration;
            float reverseRatio = 1 - (dissolveRatio);
            for(int i=0; i<cardImages.Count; i++){
                Image image = cardImages[i].GetComponent<Image>();
                Material dissolveMaterial = new Material(cardDissolveMaterials[i]);
                image.material = dissolveMaterial;
                dissolveMaterial.SetFloat("_Level", dissolveRatio);
            }
            foreach(GameObject expBlock in expBlocks){
                Image image = expBlock.GetComponent<Image>();
                image.color = new Color(image.color.r, image.color.g, image.color.b, reverseRatio);
            }
            textCardCost.alpha = reverseRatio;
            textCardDescription.alpha = reverseRatio;
            textCardName.alpha = reverseRatio;
            timer += Time.deltaTime;
            yield return null;
        }
        if(callback != null){
            callback();
        }
    }
}
