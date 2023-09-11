using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;
using ProjectD;

public class CardOnDeck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Card card;

    public CanvasGroup canvasGroup;

    [Header("CardOnDeck Image 컴포넌트")]
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

    [Header("CardOnHand 배경 이미지")]
    public Sprite attackCardBackground;
    public Sprite blessCardBackground;
    public Sprite strategyCardBackground;

    [Header("CardOnHand 내부 일러스트 액자 틀")]
    public Sprite attackCardImageFrame;
    public Sprite blessCardImageFrame;
    public Sprite strategyCardImageFrame;

    [Header("CardOnHand 등급 및 강화 틀")]
    public Sprite enhancedLegendCardGradFrame;
    public Sprite legendCardGradeFrame;
    public Sprite enhancedNormalCardGradFrame;
    public Sprite normalCardGradeFrame;
    public Sprite enhancedRareCardGradFrame;
    public Sprite rareCardGradeFrame;

    [Header("CardOnHand 앰블럼")]
    public Sprite attackEmblem;
    public Sprite blessEmblem;
    public Sprite strategyEmblem;

    [Header("CardOnHand 경험치 바")]
    public Sprite activeExpbar;
    public Sprite inActiveExpbar;
    public GameObject cardExpBar; // 경험치 바 
    public GameObject expBlockPrefab; // 경험치 바 내부 블록 오브젝트 프리팹
    public VerticalLayoutGroup verticalLayoutGroup;
    public List<GameObject> expBlocks = new List<GameObject>(); // 경험치 바 내부 블록 리스트

    private Vector3 originScale;
    private bool isTweening = false; // Dotween 애니매이션 함수들 실행중인지 여부
    public bool isSoldOut; // 판매 완료된 카드인지 여부

    void Start()
    {
        originScale = transform.localScale;
        initCardData();
        InitCardIllust(card);
        InitCardTemplateByCardType(card);
        InitCardTemplateByCardEnhanced(card);
        InitCardExpBar(card);
    }

    private void InitCardTemplateByCardType(Card card)
    {
        if(!card.baseCard.cardNumber.Equals("HA")){
            switch(card.baseCard.cardType){
                case CardType.ATTACK:
                    cardBackground.sprite = attackCardBackground;
                    cardImageFrame.sprite = attackCardImageFrame;
                    cardEmblem.sprite = attackEmblem;
                    break;
                case CardType.BLESS:
                    cardBackground.sprite = blessCardBackground;
                    cardImageFrame.sprite = blessCardImageFrame;
                    cardEmblem.sprite = blessEmblem;
                    break;
                case CardType.STRATEGY:
                    cardBackground.sprite = strategyCardBackground;
                    cardImageFrame.sprite = strategyCardImageFrame;
                    cardEmblem.sprite = strategyEmblem;
                    break;
            }
        }
    }

    // 카드 이미지 세팅
    private void InitCardIllust(Card card)
    {
        if(!string.IsNullOrEmpty(card.baseCard.cardImage)){
            cardIllust.sprite = Resources.Load<Sprite>(card.baseCard.cardImage);
        }
    }

    // 카드 강화 상태 프레임 세팅
    private void InitCardTemplateByCardEnhanced(Card card)
    {
        if(card.isEnhanced){
            cardGradeFrame.sprite = enhancedNormalCardGradFrame;
        }else{
            cardGradeFrame.sprite = normalCardGradeFrame;
        }
    }

    // 카드 경험치 바 초기화 : card 데이터에서 최대 경험치 정보를 가져와 해당 숫자 만큼의 경험치 바 내부 블록 생성
    private void InitCardExpBar(Card card)
    {
        // 철귀 이동카드는 경험치 오브젝트 초기화 제외
        if(!card.baseCard.cardNumber.Equals("HA")){
            // 최대 경험치 만큼 내부 블록 생성
            for(int i=0; i<card.baseCard.maxExperience; i++){
                GameObject expBlock = Instantiate(expBlockPrefab);
                expBlock.transform.SetParent(verticalLayoutGroup.transform, false);
                expBlock.GetComponent<Image>().sprite = inActiveExpbar;
                expBlock.GetComponent<Image>().SetNativeSize();
                expBlocks.Add(expBlock);
            }
            // expBlocks 역순으로 전환(블록이 아래부터 쌓이도록)
            expBlocks.Reverse();
            // 경험치 블록 리스트에서 현재 카드의 경험치 숫자 만큼 블록 생상을 변경
            for(int j=0; j<card.experience; j++){
                expBlocks[j].GetComponent<Image>().sprite = activeExpbar;
                expBlocks[j].GetComponent<Image>().SetNativeSize();
            }
        }   
    }

    void OnDisable()
    {
        DOTween.Kill(transform); // 비활성화 될 때 DoTween 프로세스 킬
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if(!isTweening){
            transform.DOScale(originScale * 1.3f, 0.3f);
        }
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if(!isTweening){
            transform.DOScale(originScale, 0.3f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 전투 결과 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.battleResultPopUp.activeSelf){
            gameObject.SetActive(false);
            HandleClickCardOnDeckOnPopUp(() => {
                PopUpUIManager.instance.HandleHideBattleResultPopUp();
                NetworkClient.connection.identity.GetComponent<GamePlayer>().isRewardDone = true;
            });
        }
        // MercuriusPopUp이 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.mercuriusPopUp.activeSelf){
            ChangeCardOnDeckSoldOutState();
            HandleClickCardOnDeckOnPopUp(() => {
                M_TurnManager.instance.npc_Mercurius.shopCards.Remove(this.card);
            });
        }
    }

    // CardOnDeck SoldOut 상태로 변경 및 컴포넌트들 알파값 0.5 변경
    public void ChangeCardOnDeckSoldOutState()
    {
        isSoldOut = true;
        cardSoldOut.SetActive(true);
        // 캔버스 그룹 요소들 상호작용 이벤트 비활성화 
        //canvasGroup.interactable = false;
        //canvasGroup.blocksRaycasts = false;
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

    // 팝업이 활성화된 상태에서 CardOnDeck 공통 클릭 이벤트
    private void HandleClickCardOnDeckOnPopUp(System.Action callback)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                // 애니매이션용 카드 오브젝트 복사본 생성
                GameObject cardOnHandChoosed = CreateCardOnHandChoosed(this.card);
                    
                // 턴 매니저에 저장된 현재 참가한 플레이어들의 타겟오브젝트 리스트에서 로컬플레이어의 타겟오브젝트 조회
                GamePlayer gamePlayer = gamePlayerDeck.GetComponent<GamePlayer>();
                TargetObject currentPlayer = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);

                // 이동 위치는 현재 플레이어 타겟오브젝트 위치
                Vector3 targetPosition = currentPlayer.avatar.GetComponent<PolygonCollider2D>().bounds.center;
                StartMoveToTarget(cardOnHandChoosed.GetComponent<CardOnHandChoosed>(), targetPosition, () => {
                    callback();
                });
            }
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

    // 포물선을 그리며 타겟 위치로 이동
    private void StartMoveToTarget(CardOnHandChoosed cardOnHandChoosed, Vector3 targetPosition, System.Action callback)
    {
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
                cardOnHandChoosed.isTweening = false;
                M_CardManager.instance.AddCardDataToCurrentPlayerDeck(cardOnHandChoosed.card);
                Destroy(cardOnHandChoosed.gameObject);
                callback();
            });
    }

    // 카드 정보 뷰 설정
    private void initCardData()
    {
        if(card.experience >= card.baseCard.maxExperience)
        {
            textCardName.text = CardData.instance.cards.Find(x => x.cardNumber == card.baseCard.cardNumber + "_E").name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(CardData.instance.cards.Find(x => x.cardNumber == card.baseCard.cardNumber + "_E").description);
        }
        else
        {
            textCardName.text = card.baseCard.name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(card.baseCard.description);
        }

        textCardDescription.text += '\n';
        textCardDescription.text += '\n';
        foreach(CardCharacteristic character in card.baseCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>";
        
        if(card.baseCard.cardCharacteristics.Exists( x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(card.baseCard.cardType == NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().previousCardType)
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
}
