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
            HandleClickCardOnDeckOnPopUp(() => {
                PopUpUIManager.instance.HandleHideBattleResultPopUp();
            });
        }
        // MercuriusPopUp이 팝업 활성화 상태에서 카드 클릭 이벤트
        if(PopUpUIManager.instance.mercuriusPopUp.activeSelf){
            HandleClickCardOnDeckOnPopUp(() => {
                PopUpUIManager.instance.HandleMercuriusPopUp(false);
            });
        }
    }

    // 팝업이 활성화된 상태에서 CardOnDeck 공통 클릭 이벤트
    private void HandleClickCardOnDeckOnPopUp(System.Action callback)
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                // 애니매이션용 카드 오브젝트 복사본 생성
                GameObject cardOnDeckChoosed = CreateChoosedCardOnDeck(this.card);
                    
                // 턴 매니저에 저장된 현재 참가한 플레이어들의 타겟오브젝트 리스트에서 로컬플레이어의 타겟오브젝트 조회
                GamePlayer gamePlayer = gamePlayerDeck.GetComponent<GamePlayer>();
                TargetObject currentPlayer;
                if(NetworkServer.activeHost){
                    currentPlayer = NetworkServer.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkServer.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
                }else{
                    currentPlayer = NetworkClient.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkClient.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
                }
                //TargetObject currentPlayer = M_TurnManager.instance.spawnedPlayerSyncList.Find((targetObject) => targetObject.player == gamePlayer);

                // 이동 위치는 현재 플레이어 타겟오브젝트 위치
                Vector3 targetPosition = currentPlayer.transform.position;
                StartMoveToTarget(cardOnDeckChoosed.GetComponent<CardOnDeck>(), targetPosition);
                callback();
            }
        }
    }

    // 애니매이션용으로 사용될 선택된 보상카드의 복사 오브젝트 생성
    private GameObject CreateChoosedCardOnDeck(Card card)
    {
        GameObject cardOnDeckChoosed = Instantiate(PopUpUIManager.instance.CardOnDeckChoosedPrefab);
        cardOnDeckChoosed.GetComponent<CardOnDeck>().card = card;
        cardOnDeckChoosed.GetComponent<CardOnDeck>().isTweening = true;
        cardOnDeckChoosed.transform.SetParent(GameUIManager.instance.RootGameObject.transform);
        cardOnDeckChoosed.transform.position = new Vector3(0f, 0f, 0f);

        return cardOnDeckChoosed;
    }

    // 포물선을 그리며 타겟 위치로 이동
    private void StartMoveToTarget(CardOnDeck cardOnDeckChoosed, Vector3 targetPosition)
    {
        float height = 2f;
        float duration = 1f;
        Vector3 startPos = cardOnDeckChoosed.transform.position;
        Vector3 midPos = (startPos + targetPosition) / 2f;
        midPos.y += height;
        Vector3[] path = new Vector3[] { startPos, midPos, targetPosition };
        
        // DOTween을 사용하여 포물선 이동 애니메이션 생성
        cardOnDeckChoosed.transform.DOScale(new Vector3(0.02f, 0.02f, 0f), 0.5f);
        cardOnDeckChoosed.transform.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.OutQuint)
            .OnComplete(() => {
                cardOnDeckChoosed.GetComponent<CardOnDeck>().isTweening = false;
                M_CardManager.instance.AddCardDataToCurrentPlayerDeck(cardOnDeckChoosed.card);
                Destroy(cardOnDeckChoosed.gameObject);
                NetworkClient.connection.identity.GetComponent<GamePlayer>().isRewardDone = true;
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
