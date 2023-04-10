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
    public Vector3 targetPosition;
    public float hoveredPositionY;

    // 회전값
    public Vector3 originRotation;
    public Vector3 targetRotation;

    // 마우스가 오브젝트 위에 있는지 여부
    public bool isMouseOver = false; 

    // 오브젝트가 드래그 상태인지 여부
    public bool isDrag = false;

    // 오브젝트가 움직이는 상태인지 여부
    public bool isMoving = false;

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
        transform.SetParent(DeckUI.instance.CardPocket.transform);
        transform.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
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
    public void OnCardDragStart(Vector3 cardCenterPosition, CardOnHand cardOnHand)
    {
        if(isOwned && currentPlayerDeck.isLocalPlayer){
            if(card.isTargetable){
                // 타겟팅 카드면 화살표 생성
                currentPlayerDeck.CmdSpawnArrowEmitter(cardCenterPosition, cardOnHand);
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
                CardActionByType(cardOnHand);
                cardOnHand.CardOnHandThrowAwaySequence(cardOnHand);
            }
        }
    }

    // [TEMP]카드 타입에 따라 액션 수행하는 함수
    public void CardActionByType(CardOnHand cardOnHand)
    {
        // TODO : 카드 타입에 따라 액션 수행
        if(NetworkClient.connection != null){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if (gamePlayerDeck.isLocalPlayer){
                if(!cardOnHand.card.isTargetable){
                    gamePlayerDeck.CmdActionToAllTarget(cardOnHand);
                }
            }
        }
    }

    // CardOnHand 오브젝트들의 인덱스값에 따라 순차적인 움직임으로 날아오는 애니매이션 + Moving플래그 변수 조정
    public void CardOnHandDrawSequence(CardOnHand cardOnHand, int index)
    {
        cardOnHand.isMoving = true;
        Transform cardTransform = cardOnHand.gameObject.transform;
        cardTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        // buttonPrefareDeck 월드상 좌표
        cardTransform.position = Camera.main.ScreenToWorldPoint(DeckUI.instance.buttonPrefareDeck.GetComponent<RectTransform>().position);

        // Dotween 애니매이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.5f, 0.8f, 0f), 0.2f));
        sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, 0f), 0.2f));
        sequence.Join(cardTransform
            .DOMove(cardTransform.position + new Vector3(0f, 5f, 0f), 0.2f)
            .SetDelay(index * 0.1f)
            .SetEase(Ease.OutSine)
            .OnComplete(() => {
                cardTransform
                    .DOMove(cardTransform.position, 0.2f)
                    .SetDelay(index * 0.1f)
                    .SetEase(Ease.OutSine)
                    .OnComplete(() => {
                        cardOnHand.isMoving = false;
                    }
                );
            })
        );
    }

    // CardOnHand 오브젝트 멀어지는 애니매이션 + 오브젝트 파괴 커맨드 호출
    public void CardOnHandThrowAwaySequence(CardOnHand cardOnHand)
    {
        DeckUI.instance.buttonEndTurn.interactable = false;        
        cardOnHand.isMoving = true;
        float duration = 0.3f;
        // buttonTrashDeck 월드상 좌표
        Vector3 trashDeckPosition = Camera.main.ScreenToWorldPoint(DeckUI.instance.buttonTrashDeck.GetComponent<RectTransform>().position);
        // Dotween 애니매이션 시퀀스 생성
        Sequence sequence = DOTween.Sequence();
        
        // 시퀸스에 사이즈 축소, 현재위치에서 중앙 0.5f위쪽 위치로 이동 애니매이션 추가
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.5f, 0.8f, 0f), duration));
        sequence.Join(cardOnHand.transform
                            .DOMove(new Vector3(0f, 0.5f, 0f), duration)
                            .SetEase(Ease.OutSine));

        // 시퀀스에 사이즈 축소, 오른쪽으로 90도 회전, 현재위치에서 화면의 우측하단 방향으로 포물선 이동 애니매이션 추가
        sequence.Append(cardOnHand.transform.DOScale(new Vector3(0.5f, 0.8f, 0f), duration));
        sequence.Join(cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), duration));
        sequence.Join(cardOnHand.transform
                            .DOMove(trashDeckPosition, duration)
                            .SetEase(Ease.InOutCirc));
        sequence.OnComplete(() =>
        {
            // 애니매이션 시퀀스 모두 종료 시 카드 삭제 로직 수행   
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if (gamePlayerDeck.isLocalPlayer)
            {
                cardOnHand.isDrag = false;
                cardOnHand.isMoving = false;
                gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                DeckUI.instance.buttonEndTurn.interactable = true;
            }
        });
    }

    // CardOnHand 모두 trashDeck으로 버리는 애니매이션(역순으로 크기, 방향, 위치 변경)
    public void CardOnHandAllThrowAwaySequence(CardOnHand cardOnHand)
    {
        DeckUI.instance.buttonEndTurn.interactable = false;
        GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        if(gamePlayerDeck.isLocalPlayer){
            float delay = (gamePlayerDeck.cardOnHands.Count - cardOnHand.index) * 0.1f;
            // buttonTrashDeck 월드상 좌표
            Vector3 trashDeckPosition = Camera.main.ScreenToWorldPoint(DeckUI.instance.buttonTrashDeck.GetComponent<RectTransform>().position);
            cardOnHand.isMoving = true;
            cardOnHand.transform.DOScale(new Vector3(0.5f, 0.8f, 0f), 0.3f);
            cardOnHand.transform.DORotate(new Vector3(0f, 0f, -90f), 0.3f);
            cardOnHand.transform
                    .DOMove(trashDeckPosition, 0.3f)
                    .SetEase(Ease.OutCirc)
                    .SetDelay(delay)
                    .OnComplete(() => {
                        cardOnHand.isMoving = false;
                        gamePlayerDeck.CmdDestroyCardOnHand(cardOnHand);
                        DeckUI.instance.buttonEndTurn.interactable = true;
                    });
        }    
    }
}