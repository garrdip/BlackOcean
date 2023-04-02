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

    // 오브젝트가 회전 상태인지 여부
    public bool isRotating = false;

    // 현재 게임 플레이어의 GamePlayerDeck 클래스 참조값
    public GamePlayerDeck currentPlayerDeck;

    private Vector3 trashCardStartPoint;

    private Vector3 trashCardEndPoint;


    void Start()
    {
        transform.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        originScale = transform.localScale;
        targetScale = originScale + new Vector3(1f, 1.5f, 0f);
        hoveredPositionY = 0.8f;
    }

    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        if(NetworkClient.connection != null){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
    }

    // 카드에 마우스 진입할 시 이벤트
    public void OnCardMouseIn()
    {
        if(isOwned){
            isMouseOver = true;
            originSortOrder = index;
            transform.GetComponent<SpriteRenderer>().sortingOrder = 999;
        }
    }

    // 마우스가 카드에서 벗어날 시 이벤트
    public void OnCardMouseOut()
    {
        if(isOwned){
            isMouseOver = false;
            transform.GetComponent<SpriteRenderer>().sortingOrder = originSortOrder;
        }
    }

    // 카드 드래그 시작 시 이벥트
    public void OnCardDragStart()
    {
        if(isOwned){
            if(card.isTargetable){
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(ray, out RaycastHit raycastHit)){
                    RectTransform canvaasRectTransform = DeckUI.instance.GameCanvas.GetComponent<RectTransform>(); // 게임 화면의 Canvas객체
                    // 클릭한 카드의 중앙 좌표에 화살표 인디케이터 생성 요청
                    Vector3 cardCenterPosition = raycastHit.collider.bounds.center;
                    currentPlayerDeck.CmdSpawnArrowEmitter(cardCenterPosition);
                }
            }else{
                isDrag = true;
            }
        }
    }

    // 카드 드래그 진행 중 이벤트
    public void OnCardDrag()
    {
        if(isOwned){
            isDrag = true;
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector2(mousePosition.x, mousePosition.y);
            transform.localScale = targetScale;
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    // 카드 드래그 종료 시 이벤트
    public void OnCardDragEnd()
    {
        if(isOwned){
            isDrag = false;
            if (!isRotating && (Input.mousePosition.y > Screen.height / 2))
            {
                Vector3 dropPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3[] points = new Vector3[]{
                    dropPosition,
                    //transform.localPosition,
                    //transform.localPosition,
                    Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f))
                };
                transform
                    .DOMove(points[1], 1f)
                    .SetEase(Ease.InOutCirc)
                    .OnComplete(() => CmdDestroyCardOnHand());
                //transform.DOLocalPath(points, 1f, PathType.CatmullRom, PathMode.Full3D, 10, Color.white);
            }
        }
    }

    // CardOnHand 오브젝트 파괴 및 리스트에서 제거, 댁 카운트 감소
    [Command]
    public void CmdDestroyCardOnHand()
    {
        NetworkServer.Destroy(this.gameObject);
        GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.cardOnHands.Remove(this);
        gamePlayerDeck.currentDeckCount--;
    }
}