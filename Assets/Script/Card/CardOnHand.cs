using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using ProjectD;
using Mirror;
using DG.Tweening;
using TMPro;

public class CardOnHand : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnChangeCardData))]
    public Card card;

    [SyncVar (hook = nameof(OnChangeIndex))]
    public int index;

    [SyncVar (hook = nameof(OnChangeParent))]
    public CardPocket parent;

    [Header("CardOnHand Transform 및 컴포넌트 관련 값들")]
    // 랜더링 순서값
    public int originSortOrder; // 초기값

    // 초기 위치값
    public Vector3 originPosition;

    // 화살표 소환된 카드의 위치값(화면 중앙 하단)
    public Vector3 arrowSpawnedCardPosition;

    [Header("CardOnHand 상태 변수값들")]
    // 마우스가 오브젝트 위에 있는지 여부
    public bool isMouseOver = false; 

    // 카드 오브젝트가 드래그 상태인지 여부
    public bool isDrag = false;

    // 카드 오브젝트가 움직이는 상태인지 여부
    public bool isMoving = false;

    // 카드 오브젝트가 밀려난 상태인지 여부
    public bool isShifted = false;

    // 카드 제거 팝업창에서 선택한 상태인지 여부
    public bool isChoosed = false;

    public bool isUsed = false;

    [Header("현재 게임 플레이어의 GamePlayerDeck 클래스 참조값")]
    public GamePlayerDeck currentPlayerDeck;

    [Header("CardOnHand UI Canvas 컴포넌트")]
    public Canvas cardOnHandCanvas;

    [Header("CardOnHand Sprite 컴포넌트")]
    public SpriteRenderer cardBackground;
    public SpriteRenderer cardIllust;
    public SpriteRenderer cardImageFrame;
    public SpriteRenderer cardGradeFrame;
    public SpriteRenderer cardEmblem;

    [Header("CardOnHand Text 컴포넌트")]
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

    public delegate void CardInfoChanged();
    public CardInfoChanged CardInfoChangedEvent;

    void Start()
    {
        cardOnHandCanvas.worldCamera = Camera.main;
        CardInfoChangedEvent += OnChangedCardInfo;
    }

    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            InitCardIllust(card);
            InitCardTemplateByCardType(card);
            InitCardTemplateByCardEnhanced(card);
            InitCardExpBar(card);
        }
    }

    // 카드 타입에 따라 외형 틀 세팅
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
                expBlocks.Add(expBlock);
            }
            // expBlocks 역순으로 전환(블록이 아래부터 쌓이도록)
            expBlocks.Reverse();
            // 경험치 블록 리스트에서 현재 카드의 경험치 숫자 만큼 블록 생상을 변경
            for(int j=0; j<card.experience; j++){
                expBlocks[j].GetComponent<Image>().sprite = activeExpbar;
            }
        }   
    }

    // 오브젝트에 마우스 포인터 진입할 때 이벤트
    void OnMouseEnter()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(!isUsed && !isMoving && !isChoosed && !IsArrowActive() && !IsCardControllablePopUpActive()){
                isMouseOver = true;
                originSortOrder = index;
                transform.GetComponent<SortingGroup>().sortingOrder =  M_CardManager.instance.maxSortOrder;
                cardOnHandCanvas.sortingOrder =  M_CardManager.instance.maxSortOrder;
                M_CardManager.instance.ChangeCardOnHandShiftState(this, true);
            }
        }
    }

    // 오브젝트에서 마우스 포인터 나갈 때 이벤트
    void OnMouseExit()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(!isUsed && !isMoving && !IsArrowActive() && !IsCardControllablePopUpActive()){
                isMouseOver = false;
                transform.GetComponent<SortingGroup>().sortingOrder =  originSortOrder;
                cardOnHandCanvas.sortingOrder = originSortOrder;
                M_CardManager.instance.ChangeCardOnHandShiftState(this, false);
            }
        }
    }

    // 오브젝트에 마우스 왼쪽버튼 누를 때 이벤트
    void OnMouseDown()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(!isUsed && !isMoving && !IsArrowActive()){
                // 덱 [목록] 팝업창이 뜬 경우에 마우스 왼쪽 버튼 클릭 시
                if(!IsCardControllablePopUpActive()){
                    isDrag = true;
                    arrowSpawnedCardPosition = transform.position; // 드래그 시작전 마우스 클릭 시점에 카드의 절대 위치값 저장(이 시점의 카드 위치는 중앙 하단). 화살표 소환 시 카드를 다시 중앙 하단으로 이동시키기 위함.
                }
                // 덱 [제거] 팝업창이 뜬 경우에 마우스 왼쪽 버튼 클릭 시
                if(IsCardOnHandRemovePopUpActive()){
                    if(isChoosed){
                        currentPlayerDeck.RemoveChoosedCardOnHands(this); // 클릭한 카드를 제거용 카드 배열에서 제거
                    }else{
                        currentPlayerDeck.AddChoosedCardOnHands(this); // 클릭한 카드를 제거용 카드 배열에 추가
                    }  
                }
            }
        }
    }

    // 오브젝트를 마우스로 드래그 할 때 이벤트
    void OnMouseDrag()
    {
        if(!isUsed &&isOwned && M_TurnManager.instance.IsActivePhase()){
            if(isDrag && !IsCardControllablePopUpActive() && !IsCardOnHandRemovePopUpActive()){
                DragCardOnHand(this);
                MovePositionArrowSpawnedCardOnHand(this);
            }
        }
    }

    // 오브젝트에서 마우스 왼쪽버튼 뗄 때 이벤트
    void OnMouseUp()
    {
        if(isOwned && M_TurnManager.instance.IsActivePhase()){
            if(isDrag && !IsCardControllablePopUpActive() && !IsCardOnHandRemovePopUpActive()){
                // Targetable 카드가 아닌 경우 마우스 뗄 때 위치가 화면 중앙을 넘어갈 경우 액션 수행
                if(!card.baseCard.isTargetable && (Input.mousePosition.y > Screen.height / 2)){
                    int totalCost = 0;
                    if(card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
                    {
                        if(card.baseCard.cardType == NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().previousCardType)
                        {
                            totalCost = ( card.baseCard.cost + card.costAddition - 1 );
                            if(totalCost < 0)totalCost = 0;
                        }
                        else
                            totalCost = ( card.baseCard.cost + card.costAddition + 1 );
                    }
                    else
                        totalCost = card.baseCard.cost + card.costAddition ;
                    if(totalCost > NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().currentIchi) // 카드 코스트 계산 하는곳
                    {
                        isDrag = false;
                        isMoving = false;
                        isMouseOver = false;
                        return;
                    }

                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    CmdEnQueueCardData(gamePlayerDeck,NetworkClient.connection.identity);
                    M_CardManager.instance.CardOnHandThrowAwaySequence(this);
                }
                else
                {
                    isDrag = false;
                    isMoving = false;
                    isMouseOver = false;
                }
            }
        }
    }

    [Command]
    void CmdEnQueueCardData(GamePlayerDeck gamePlayerDeck, NetworkIdentity conn)
    {
        gamePlayerDeck.serverCardPredictQueue.Enqueue((this, null, conn));
    }

    // 마우스 좌표에 따라 카드 오브젝트 드래그
    private void DragCardOnHand(CardOnHand cardOnHand)
    {
        // 드래그 중 오브젝트의 정렬값은 최대값. 항상 맨 위에 랜더링
        cardOnHand.transform.GetComponent<SortingGroup>().sortingOrder =  M_CardManager.instance.maxSortOrder;
        cardOnHand.cardOnHandCanvas.sortingOrder = M_CardManager.instance.maxSortOrder;
        // 오브젝트 위치는 마우스 커서 위치
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cardOnHand.transform.position = new Vector2(mousePosition.x, mousePosition.y);
        cardOnHand.transform.localScale = M_CardManager.instance.cardOverSize;
        cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    // 타겟팅 카드일 경우, 드래그중 위치가 화면 하단부 3분의1을 넘어가면 화살표 생성 후 카드의 위치를 중앙으로 이동
    private void MovePositionArrowSpawnedCardOnHand(CardOnHand cardOnHand)
    {
        if( Input.mousePosition.y > Screen.height / 3 ){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.GetTotalCostOfCardOnHand(cardOnHand) > NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().currentIchi) 
            {
                cardOnHand.isMoving = false;
                cardOnHand.isDrag = false;
                TargetObject currentPlayer = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayerDeck.GetComponent<GamePlayer>());
                currentPlayer.ShowCostNotReaminBubble();
            }
            else if(cardOnHand.card.baseCard.isTargetable)
            {
                cardOnHand.isMoving = true;
                cardOnHand.isDrag = false;
                cardOnHand.transform.GetComponent<SortingGroup>().sortingOrder = M_CardManager.instance.maxSortOrder;
                currentPlayerDeck.cardCtrlArrow.InitCardCtrlArrow(cardOnHand);
                currentPlayerDeck.CmdSetArrowOwnCardOnHand(cardOnHand);
                cardOnHand.transform
                    .DOMove(new Vector3(0f, arrowSpawnedCardPosition.y, arrowSpawnedCardPosition.z), 0.4f)
                    .SetEase(Ease.OutSine);
            }
        }
    }

    // 팝업 활성화 상태일 때 카드 제어가 가능한 팝업의 활성화 여부 확인 함수
    private bool IsCardControllablePopUpActive()
    {
        // PrefareDeckPopUp, TrashDeckPopUp, BattleResultPopUp은 팝업 활성화 상태에서 카드 제어가 안되야 하므로 체크.
        return PopUpUIManager.instance.deckListPopUp.activeSelf || PopUpUIManager.instance.battleResultPopUp.activeSelf;
    }

    // CardOnHandRemove PopUp 활성화 여부 확인 함수
    private bool IsCardOnHandRemovePopUpActive()
    {
        return PopUpUIManager.instance.cardOnHandRemovePopUp.activeSelf;
    }

    // 화살표 활성화 여부 확인 함수
    private bool IsArrowActive()
    {
        return M_CardManager.instance.isArrowActive;
    }

    // 카드 정렬값 이벤트 수신
    [ClientRpc]
    public void RpcSortOrder(int index)
    {
        transform.GetComponent<SortingGroup>().sortingOrder = index;
        cardOnHandCanvas.sortingOrder = index;
        transform.SetSiblingIndex(index);
    }

    // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
    [ClientRpc]
    public void RpcCardOnHandSetParent(GameObject cardPocket)
    {
        transform.SetParent(cardPocket.transform);
    }


    // --------------------------------------------------------------- SyncVar Hook -----------------------------------------------------------------//

    // 카드 정보 뷰 업데이트
    public void OnChangeCardData(Card oldCard, Card newCard)
    {
        // 정상적으로 Card가 동기화 되지 않았을경우 업데이트 취소
        if(newCard == null)return;
        if(newCard.baseCard == null)return;
        if(newCard.baseCard.name == null)return;
        if(newCard.baseCard.name == "")return;

        if(newCard.experience >= newCard.baseCard.maxExperience)
        {
            textCardName.text = CardData.instance.cards.Find(x => x.cardNumber == newCard.baseCard.cardNumber + "_E").name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(CardData.instance.cards.Find(x => x.cardNumber == newCard.baseCard.cardNumber + "_E").description);
        }
        else
        {
            textCardName.text = newCard.baseCard.name;
            textCardDescription.text = M_CardManager.instance.GetAdditionalValueFromDescription(newCard.baseCard.description);
        }

        textCardDescription.text += '\n';
        textCardDescription.text += '\n';
        foreach(CardCharacteristic character in newCard.baseCard.cardCharacteristics)
            textCardDescription.text += "<b><color=yellow>" + character.ToString() + "</color></b>";
        
        if(newCard.baseCard.cardCharacteristics.Exists( x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(newCard.baseCard.cardType == NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().previousCardType)
            {
                textCardCost.text = "<b><color=green>" +((newCard.baseCard.cost + newCard.costAddition - 1) <= 0 ? "0" : (newCard.baseCard.cost + newCard.costAddition - 1).ToString()) + "</color></b>";
            }
            else
            {
                textCardCost.text = "<b><color=red>"+ (newCard.baseCard.cost + newCard.costAddition + 1).ToString() + "</color></b>";
            }
        }
        else textCardCost.text = (newCard.baseCard.cost + newCard.costAddition).ToString();
    }

    void OnChangedCardInfo()
    {
        OnChangeCardData(card,card);
    }

    // 카드 부모오브젝트인 CardPocket 참조값 변경 이벤트 수신
    public void OnChangeParent(CardPocket oldCardPocket, CardPocket newCardPocket)
    {
        transform.SetParent(newCardPocket.transform);
        transform.position = GameUIManager.instance.buttonPrefareDeck.transform.position;
    }

    // 카드 인덱스값 변경 이벤트 수신
    public void OnChangeIndex(int oldValue, int newValue)
    {
        transform.GetComponent<SortingGroup>().sortingOrder = index;
        cardOnHandCanvas.sortingOrder = index;
        transform.SetSiblingIndex(index);
    }
}