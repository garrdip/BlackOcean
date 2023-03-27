using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;

public class CardOnHand : NetworkBehaviour
{
    [SyncVar]
    public int index;
    public string cardName;
    public bool isTargetAble;

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

    private Vector3 mousePosition;

    private BoxCollider2D boxCollider2D;


    void Start()
    {
        transform.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        originScale = transform.localScale;
        targetScale = originScale + new Vector3(1f, 1.5f, 0f);
        hoveredPositionY = 0.8f;
    }

    void FixedUpdate()
    {
        if(isOwned){
            if(isMouseOver){
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
                targetPosition = new Vector3(transform.localPosition.x, hoveredPositionY, transform.localPosition.z);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }else{
                SetCardOfHandPositionSymmetry(index);  
            }
        }
    }

/*
    // 오브젝트에 마우스 왼쪽버튼 땠을 때
    private void OnMouseUp()
    {
        isDrag = false;
        isMouseOver = false;
    }

    // 오브젝트에 마우스 왼쪽버튼 누를 때
    private void OnMouseDown()
    {
        if(isTargetAble){
            if(NetworkClient.connection != null){
                Ray ray = M_MapManager.instance.mainCam.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(ray, out RaycastHit raycastHit)){
                    Vector3 movePoint = raycastHit.point;
                    Camera camera = M_MapManager.instance.mainCam;
                    RectTransform canvaasRectTransform = DeckUI.instance.GameCanvas.GetComponent<RectTransform>(); // 게임 화면의 Canvas객체
                    Debug.Log("movePoint : " + movePoint.ToString());
                    Debug.Log("맞은 객체 : " + raycastHit.transform.name);

                    // 월드 좌표를 UI 좌표로 변환
                    Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, movePoint);

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
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector2(mousePosition.x, mousePosition.y);
        }
    }

    // 오브젝트를 마우스로 드래그 중일 때
    private void OnMouseDrag()
    {
        if(!isTargetAble && isDrag){
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector2(mousePosition.x, mousePosition.y);
        }
    }
    */

    // 카드 대칭 위치값, 회전값, 크기값 지정
    private void SetCardOfHandPositionSymmetry(int index)
    {
        // 현재 플레이어의 소유의 카드 갯수
        GamePlayer gamePlayer = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>();
        int count = gamePlayer.cardOnHands.Count;
        
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