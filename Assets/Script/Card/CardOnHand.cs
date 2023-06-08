using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using ProjectD;
using Mirror;
using DG.Tweening;
using TMPro;

public class CardOnHand : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeCardData))]
    public Card card;

    [SyncVar]
    public int index;
    
    [Header("CardOnHand Transform 및 컴포넌트 관련 값들")]
    // 랜더링 순서값
    public int originSortOrder; // 초기값

    // 초기 위치값
    public Vector3 originPosition;

    // 화살표 소환된 카드의 위치값(화면 중앙 하단)
    public Vector3 arrowSpawnedCardPosition;

    // 마우스 오버 시 Y좌표값
    public float hoveredPositionY;


    [Header("CardOnHand 상태 변수값들")]
    // 마우스가 오브젝트 위에 있는지 여부
    public bool isMouseOver = false; 

    // 카드 오브젝트가 드래그 상태인지 여부
    public bool isDrag = false;

    // 카드 오브젝트가 움직이는 상태인지 여부
    public bool isMoving = false;

    // 카드 오브젝트가 밀려난 상태인지 여부
    public bool isShifted = false;

    // 카드 선택 모드 창에서 선택된 상태인지 여부
    public bool isChoosed = false;


    [Header("현재 게임 플레이어의 GamePlayerDeck 클래스 참조값")]
    public GamePlayerDeck currentPlayerDeck;

    [Header("CardOnHand UI 컴포넌트")]
    public Canvas cardOnHandCanvas;
    public TextMeshProUGUI textCardName;
    public TextMeshProUGUI textCardInfo;


    void Start()
    {
        cardOnHandCanvas.worldCamera = Camera.main;
    }

    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
        hoveredPositionY = 1.2f;
    }

    // 오브젝트에 마우스 포인터 진입할 때 이벤트
    void OnMouseEnter()
    {
        if(isOwned && !isMoving && !IsDeckListPopUpActive() && !isChoosed){
            isMouseOver = true;
            originSortOrder = index;
            transform.GetComponent<SpriteRenderer>().sortingOrder =  M_CardManager.instance.maxSortOrder;
            cardOnHandCanvas.sortingOrder =  M_CardManager.instance.maxSortOrder;
            M_CardManager.instance.ChangeCardOnHandColliderSize(this, M_CardManager.instance.cardNoneCollidableSize);
            M_CardManager.instance.ChangeCardOnHandShiftState(this, true);
        }
    }

    // 오브젝트에서 마우스 포인터 나갈 때 이벤트
    void OnMouseExit()
    {
        if(isOwned && !isMoving && !IsDeckListPopUpActive()){
            isMouseOver = false;
            transform.GetComponent<SpriteRenderer>().sortingOrder =  originSortOrder;
            cardOnHandCanvas.sortingOrder = originSortOrder;
            M_CardManager.instance.ChangeCardOnHandColliderSize(this, M_CardManager.instance.cardCollidableSize);
            M_CardManager.instance.ChangeCardOnHandShiftState(this, false);
        }
    }

    // 오브젝트에 마우스 왼쪽버튼 누를 때 이벤트
    void OnMouseDown()
    {
        if(isOwned && currentPlayerDeck.isLocalPlayer){
            // 덱 [목록] 팝업창이 뜬 경우에 마우스 왼쪽 버튼 클릭 시
            if(!IsDeckListPopUpActive()){
                isDrag = true;
                arrowSpawnedCardPosition = transform.position; // 드래그 시작전 마우스 클릭 시점에 카드의 절대 위치값 저장(이 시점의 카드 위치는 중앙 하단). 화살표 소환 시 카드를 다시 중앙 하단으로 이동시키기 위함.
            }
            // 덱 [제거] 팝업창이 뜬 경우에 마우스 왼쪽 버튼 클릭 시
            if(IsDeckRemovePopUpActive()){
                isChoosed = true;
                currentPlayerDeck.cardOnHands.Remove(this);
                M_CardManager.instance.choosedCardOnHand = this;
            }
        }
    }

    // 오브젝트를 마우스로 드래그 할 때 이벤트
    void OnMouseDrag()
    {
        if(isOwned && isDrag && !IsDeckListPopUpActive() && !IsDeckRemovePopUpActive()){
            DragCardOnHand(this);
            MovePositionArrowSpawnedCardOnHand(this);
        }
    }

    // 오브젝트에서 마우스 왼쪽버튼 뗄 때 이벤트
    void OnMouseUp()
    {
        if(isOwned && isDrag && !IsDeckListPopUpActive()){
            // Targetable 카드가 아닌 경우 마우스 뗄 때 위치가 화면 중앙을 넘어갈 경우 액션 수행
            if(!card.baseCard.isTargetable && (Input.mousePosition.y > Screen.height / 2) && !IsDeckRemovePopUpActive()){
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    if (gamePlayerDeck.isLocalPlayer){
                        gamePlayerDeck.CmdEnQueueCardTargetPair(card, null, NetworkClient.connection.identity, null);
                    }
                }
                M_CardManager.instance.CardOnHandThrowAwaySequence(this);
            }
            isDrag = false;
            isMoving = false;
            isMouseOver = false;
        }
    }

    // 마우스 좌표에 따라 카드 오브젝트 드래그
    private void DragCardOnHand(CardOnHand cardOnHand)
    {
        // 드래그 중 오브젝트의 정렬값은 최대값. 항상 맨 위에 랜더링
        cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder =  M_CardManager.instance.maxSortOrder;
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
        if(cardOnHand.card.baseCard.isTargetable && (Input.mousePosition.y > Screen.height / 3)){
            cardOnHand.isDrag = false;
            cardOnHand.isMoving = true;
            cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder = M_CardManager.instance.maxSortOrder;
            currentPlayerDeck.cardCtrlArrow.InitCardCtrlArrow(cardOnHand);
            currentPlayerDeck.CmdSetArrowOwnCardOnHand(cardOnHand);
            cardOnHand.transform
                .DOMove(new Vector3(0f, arrowSpawnedCardPosition.y, arrowSpawnedCardPosition.z), 0.4f)
                .SetEase(Ease.OutSine);
        }
    }

    // DeckList PopUP 활성화 여부 확인 함수
    private bool IsDeckListPopUpActive()
    {
        return DeckUI.instance.DeckListPopUp.activeSelf;
    }

    // DeckRemove PopUp 활성화 여부 확인 함수
    private bool IsDeckRemovePopUpActive()
    {
        return DeckUI.instance.DeckRemovePopUp.activeSelf;
    }

    // 카드 생성 이벤트 수신 (카드의 위치 및 부모 오브젝트 설정)
    [ClientRpc]
    public void RpcSpawnedCardOnHand(CardPocket cardPocket)
    {
        if(isOwned){
            transform.position = DeckUI.instance.buttonPrefareDeck.transform.position;
        }else{
            transform.position = new Vector3(-100f, 0f, 0f);
        }
        transform.SetParent(cardPocket.transform);
    }

    // 카드 정렬값 이벤트 수신
    [ClientRpc]
    public void RpcSortOrder(int index)
    {
        transform.GetComponent<SpriteRenderer>().sortingOrder = index;
        cardOnHandCanvas.sortingOrder = index;
        transform.SetSiblingIndex(index);
    }

    // --------------------------------------------------------------- SyncVar Hook -----------------------------------------------------------------//

    // 카드 정보 뷰 업데이트
    public void OnChangeCardData(Card oldCard, Card newCard)
    {
        textCardName.text = card.baseCard.name;
        textCardInfo.text = card.baseCard.cardType.ToString();
    }
}