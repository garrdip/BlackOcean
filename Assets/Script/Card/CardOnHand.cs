using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
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
    public Vector3 targetPosition;
    public float hoveredPositionY;

    // 회전값
    public Vector3 originRotation;
    public Vector3 targetRotation;

    // 마우스가 오브젝트 위에 있는지 여부
    public bool isMouseOver = false; 

    // 오브젝트가 드래그 상태인지 여부
    public bool isDrag = false;

    // 현재 게임 플레이어의 GamePlayerDeck 클래스 참조값
    public GamePlayerDeck currentPlayerDeck;

    private Vector3 trashCardStartPoint;

    private Vector3 trashCardEndPoint;


    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        if(NetworkClient.connection != null){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
        transform.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        transform.localPosition = new Vector3(-20f, 0f, 0f);
        originPosition = transform.localPosition;
        originScale = transform.localScale;
        targetScale = originScale + new Vector3(0.5f, 0.5f, 0f);
        originSortOrder = index + 1;
        hoveredPositionY = 0.8f;
    }

    // 카드에 마우스 진입할 시 이벤트
    public void OnCardMouseIn(CardOnHand cardOnHand)
    {
        if(isOwned && cardOnHand != null){
            isMouseOver = true;
            originSortOrder = index + 1;
            cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder = 999;
        }
    }

    // 마우스가 카드에서 벗어날 시 이벤트
    public void OnCardMouseOut(CardOnHand cardOnHand)
    {
        if(isOwned && cardOnHand != null){
            isMouseOver = false;
            cardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder = originSortOrder;
        }
    }

    // 카드 드래그 시작 시 이벥트
    public void OnCardDragStart(Vector3 cardCenterPosition)
    {
        if(isOwned && currentPlayerDeck.isLocalPlayer){
            if(card.isTargetable){
                // 타겟팅 카드면 화살표 생성
                currentPlayerDeck.CmdSpawnArrowEmitter(cardCenterPosition);
                isDrag = false;
            }else{
                isDrag = true;
            }
        }
    }

    // 카드 드래그 진행 중 이벤트
    public void OnCardDrag()
    {
        if(isOwned){
            if(isDrag){
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.position = new Vector2(mousePosition.x, mousePosition.y);
                transform.localScale = targetScale;
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }

    // 카드 드래그 종료 시 이벤트
    public void OnCardDragEnd(CardOnHand cardOnHand)
    {
        if(isOwned){
            if(isDrag && (Input.mousePosition.y > Screen.height / 2))
            {
                DeckUI.instance.buttonEndTurn.interactable = false;
                // Dotween 애니매이션 시퀀스 생성
                Sequence sequence = DOTween.Sequence();

                // 시퀸스에 사이즈 축소, 오른쪽으로 90도 회전, 현재위치에서 중앙 0.5f위쪽 위치로 이동 애니매이션 추가
                sequence.Append(transform.DOScale(new Vector3(0.5f, 1f, 0f), 0.5f));
                sequence.Join(transform.DORotate(new Vector3(0f, 0f, -90f), 0.5f));
                sequence.Join(transform.DOMove(new Vector3(0f, 0.5f, 0f), 0.5f).SetEase(Ease.OutSine));

                // 시퀀스에 사이즈 축소, 오른쪽으로 90도 회전, 현재위치에서 화면의 우측하단 방향으로 포물선 이동 애니매이션 추가
                sequence.Append(transform.DOScale(new Vector3(0.5f, 1f, 0f), 0.5f));
                sequence.Join(transform.DORotate(new Vector3(0f, 0f, -90f), 0.5f));
                sequence.Join(transform.DOMove(Camera.main.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)), 0.5f).SetEase(Ease.InOutCirc));
                sequence.OnComplete(() =>
                {
                    // 애니매이션 시퀀스 모두 종료 시 카드 삭제 로직 수행   
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    if (gamePlayerDeck.isLocalPlayer)
                    {
                        gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                        isDrag = false;
                        DeckUI.instance.buttonEndTurn.interactable = true;
                    }
                });
            }
        }
    }
}