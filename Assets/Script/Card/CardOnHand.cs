using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using ProjectD;
using Mirror;
using DG.Tweening;

public class CardOnHand : NetworkBehaviour
{
    [SyncVar]
    public Card card;

    [SyncVar]
    public int index;
    
    public SpriteRenderer spriteRenderer;
    
    // 랜더링 순서값
    public int originSortOrder;

    // 크기값
    public Vector3 originScale;
    public Vector3 targetScale;

    // 위치값
    public Vector3 originPosition;
    public float hoveredPositionY;


    // 마우스가 오브젝트 위에 있는지 여부
    public bool isMouseOver = false; 

    // 오브젝트가 드래그 상태인지 여부
    public bool isDrag = false;

    // 오브젝트가 움직이는 상태인지 여부
    public bool isMoving = false;

    // 현재 게임 플레이어의 GamePlayerDeck 클래스 참조값
    public GamePlayerDeck currentPlayerDeck;


    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        if(NetworkClient.connection != null){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
        transform.SetParent(DeckUI.instance.CardPocket.transform);
        originScale = transform.localScale;
        targetScale = originScale + new Vector3(0.05f, 0.05f, 0f);
        transform.GetComponent<SpriteRenderer>().sortingOrder = index + 1;
        originSortOrder = index + 1;
        hoveredPositionY = 1.2f;
    }

    // 카드에 마우스 진입할 시 이벤트
    public void OnCardMouseIn(CardOnHand cardOnHand)
    {
        if(isOwned && cardOnHand != null && !M_CardManager.instance.IsArrowSpawned()){
            cardOnHand.isMouseOver = true;
            cardOnHand.originSortOrder = index + 1;
            cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder = 999;
        }
    }

    // 마우스가 카드에서 벗어날 시 이벤트
    public void OnCardMouseOut(CardOnHand cardOnHand)
    {
        if(isOwned && cardOnHand != null && !M_CardManager.instance.IsArrowSpawned()){
            cardOnHand.isMouseOver = false;
            cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder =  cardOnHand.originSortOrder;
        }
    }

    // 카드 드래그 시작 시 이벥트
    public void OnCardDragStart(Vector3 cardCenterPosition, CardOnHand cardOnHand)
    {
        if(isOwned && currentPlayerDeck.isLocalPlayer && !M_CardManager.instance.IsArrowSpawned()){
            cardOnHand.isDrag = true;
            cardOnHand.originPosition = cardOnHand.transform.position;
        }
    }

    // 카드 드래그 진행 중 이벤트
    public void OnCardDrag(Vector3 cardCenterPosition, CardOnHand cardOnHand)
    {
        if(isOwned && cardOnHand.isDrag && !M_CardManager.instance.IsArrowSpawned()){
            DragCardOnHand(cardOnHand);
            MovePositionArrowSpawnedCardOnHand(cardOnHand);
        }
    }

    // 카드 드래그 종료 시 이벤트
    // 타겟팅 카드가 아닐 경우, 드래그 종료 위치가 화면의 2분의1을 넘어가면 카드 액션 수행 및 카드 버리기 애니매이션 실행
    public void OnCardDragEnd(CardOnHand cardOnHand)
    {
        if(isOwned && cardOnHand.isDrag && !M_CardManager.instance.IsArrowSpawned()){
            if(!cardOnHand.card.isTargetable && cardOnHand.isDrag && (Input.mousePosition.y > Screen.height / 2)){
                if(NetworkClient.connection != null){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    if (gamePlayerDeck.isLocalPlayer){
                        gamePlayerDeck.CmdEnQueueCardTargetPair(cardOnHand.card, null);
                    }
                }
                M_CardManager.instance.CardOnHandThrowAwaySequence(cardOnHand);
            }
        }
    }

    // 마우스 좌표에 따라 카드 오브젝트 드래그
    private void DragCardOnHand(CardOnHand cardOnHand)
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cardOnHand.transform.position = new Vector2(mousePosition.x, mousePosition.y);
        cardOnHand.transform.localScale = targetScale;
        cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    // 타겟팅 카드일 경우, 드래그중 위치가 화면 하단부 3분의1을 넘어가면 화살표 생성 후 카드의 위치를 중앙으로 이동
    private void MovePositionArrowSpawnedCardOnHand(CardOnHand cardOnHand)
    {
        if(cardOnHand.card.isTargetable && (Input.mousePosition.y > Screen.height / 3)){
            cardOnHand.isDrag = false;
            cardOnHand.isMoving = true;
            currentPlayerDeck.CmdSpawnArrowEmitter(transform.position, cardOnHand);
            cardOnHand.transform
                .DOMove(new Vector3(0f, originPosition.y, originPosition.z), 0.4f)
                .SetEase(Ease.OutSine);
        }
    }
}