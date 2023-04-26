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
        hoveredPositionY = 1.2f;
    }

    // 오브젝트에 마우스 포인터 진입할 때 이벤트
    void OnMouseEnter()
    {
        if(isOwned && !M_CardManager.instance.IsArrowSpawned()){
            isMouseOver = true;
            originSortOrder = index;
            transform.GetComponent<SpriteRenderer>().sortingOrder = 999;
            M_CardManager.instance.ChangeCardOnHandColliderSize(this, M_CardManager.instance.cardNoneCollidableSize);
            CmdChangeCardOnHandSortOrder(999);
        }
    }

    // 오브젝트에서 마우스 포인터 나갈 때 이벤트
    void OnMouseExit()
    {
        if(isOwned && !M_CardManager.instance.IsArrowSpawned()){
            isMouseOver = false;
            transform.GetComponent<SpriteRenderer>().sortingOrder =  originSortOrder;
            M_CardManager.instance.ChangeCardOnHandColliderSize(this, M_CardManager.instance.cardCollidableSize);
            CmdChangeCardOnHandSortOrder(originSortOrder);
        }
    }

    // 오브젝트에 마우스 왼쪽버튼 누를 때 이벤트
    void OnMouseDown()
    {
        if(isOwned && currentPlayerDeck.isLocalPlayer && !M_CardManager.instance.IsArrowSpawned()){
            isDrag = true;
            originPosition = transform.position;
        }
    }

    // 오브젝트를 마우스로 드래그 할 때 이벤트
    void OnMouseDrag()
    {
        if(isOwned && isDrag && !M_CardManager.instance.IsArrowSpawned()){
            DragCardOnHand(this);
            MovePositionArrowSpawnedCardOnHand(this);
        }
    }

    // 오브젝트에서 마우스 왼쪽버튼 뗄 때 이벤트
    void OnMouseUp()
    {
         if(isOwned && isDrag && !M_CardManager.instance.IsArrowSpawned()){
            if(!card.isTargetable && (Input.mousePosition.y > Screen.height / 2)){
                if(NetworkClient.connection != null){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    if (gamePlayerDeck.isLocalPlayer){
                        gamePlayerDeck.CmdEnQueueCardTargetPair(card, null);
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

    // 스프라이트 랜더링 정렬값 변경 요청
    [Command]
    public void CmdChangeCardOnHandSortOrder(int sortOrder)
    {
        RpcChangeCardOnHandSortOrder(sortOrder);
    }

    // 변경된 스프라이트 랜더링 정렬값에 따라 CardOnHand 랜더링 순서 변경
    [ClientRpc]
    public void RpcChangeCardOnHandSortOrder(int sortOrder)
    {
        transform.GetComponent<SpriteRenderer>().sortingOrder = sortOrder;
    }
}