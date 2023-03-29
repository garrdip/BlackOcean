using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;

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
    private bool isMouseOver = false; 

    // 오브젝트가 드래그 상태인지 여부
    private bool isDrag = false;

    // 현재 게임 플레이어
    public GamePlayer currentPlayer;


    void Start()
    {
        transform.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        originScale = transform.localScale;
        targetScale = originScale + new Vector3(1f, 1.5f, 0f);
        hoveredPositionY = 0.8f;
    }

    void FixedUpdate()
    {
        if(NetworkClient.connection != null && isOwned){
            if(isMouseOver){
                SetCardOfHandPositionOrigin();
            }else{
                SetCardOfHandPositionSymmetry(currentPlayer.currentDeckCount, index);  
            }
        }   
    }

    // 클라이언트에서 생성 시 현재 플레이어 참조값 미리 캐싱
    public override void OnStartClient()
    {
        currentPlayer = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>();
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
                if(NetworkClient.connection != null){
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(ray, out RaycastHit raycastHit)){
                    RectTransform canvaasRectTransform = DeckUI.instance.GameCanvas.GetComponent<RectTransform>(); // 게임 화면의 Canvas객체
                    // 클릭한 카드의 중앙 좌표
                    Vector3 cardCenterPosition = raycastHit.collider.bounds.center;
                    
                    // 월드 좌표를 UI 좌표로 변환
                    Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, cardCenterPosition);

                    // UI 좌표를 캔버스 좌표로 변환후 canvasPosition에 저장
                    Vector2 canvasPosition;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvaasRectTransform, screenPosition, null, out canvasPosition);
                    
                    // 게임월드와 UI의 동일한 클릭위치(클릭한 카드의 위치)에 화살표 인디케이터 생성 
                    GamePlayer gamePlayer = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>();
                    gamePlayer.CmdSpawnArrowEmitter(canvasPosition);
                }
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
        }
    }

    // 카드 대칭 위치값, 회전값, 크기값 초기화
    private void SetCardOfHandPositionOrigin()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
        targetPosition = new Vector3(transform.localPosition.x, hoveredPositionY, transform.localPosition.z);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    // 카드 대칭 위치값, 회전값, 크기값 지정
    private void SetCardOfHandPositionSymmetry(int count, int index)
    {
        // 대칭 위치값 계산
        int leftCount = (count - 1) / 2;
        int rightCount = count - leftCount - 1;
        float symmetryPosition = (count % 2 == 0) ? ((index - leftCount) * 1.5f - 0.75f) : ((index - leftCount) * 1.5f + 0f);
        
        // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
        Vector3 position = new Vector3(symmetryPosition, -Mathf.Abs(symmetryPosition) * 0.15f, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, position, Time.deltaTime * 10f);
        originPosition = position;

        // 회전값
        Quaternion rotation = Quaternion.Euler(0f, 0f, -symmetryPosition);
        transform.localRotation = rotation;
        originRotation = new Vector3(0f, 0f, -symmetryPosition * 1.5f);

        // 크기값
        transform.localScale = Vector3.Lerp(transform.localScale, originScale, Time.deltaTime * 10f);  
    }
}