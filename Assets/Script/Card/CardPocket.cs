using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardPocket : NetworkBehaviour
{
    private Vector3 hidePosition;
    private Vector3 showPosition;

    public Vector3 mousePosition;
    public Vector3 targetPosition;

    public Vector3 targetScale;
    public float hoveredPositionY;
    public Camera mainCamera;

    public readonly SyncList<CardOnHand> cards = new SyncList<CardOnHand>();

    public GameObject dragTarget;


    void Start()
    {
        transform.SetParent(DeckUI.instance.DeckListPanel.transform);
        hidePosition = transform.localPosition + new Vector3(-20f, -3.5f, 0f);
        showPosition = transform.localPosition + new Vector3(20f, -3.5f, 0f);
        targetScale = new Vector3(3f, 4f, 0f) + new Vector3(1f, 1.5f, 0f);
        mainCamera = Camera.main;
        hoveredPositionY = 0.8f;
    }

    void Update()
    {   
        if(isOwned){
            HandleCardDragStart();
            HandleCardDrag();
            HandleCardDragEnd();
        }
    }

    void FixedUpdate()
    {
        ChangePocketPositionByTurn();
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(mousePos + new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f));

        Debug.DrawRay(mousePos + new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f), Color.red);
        float minDistance = float.MaxValue;
        Collider closestCollider = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.GetComponent<CardOnHand>() != null)
            {
                float distance = Vector3.Distance(mousePos, hit.collider.GetComponent<CardOnHand>().originPosition);
                if (distance < minDistance){
                    minDistance = distance;
                    closestCollider = hit.collider;
                }
            }
        }

        if (closestCollider != null){
            GameObject collisionGameObject = closestCollider.gameObject;
            collisionGameObject.GetComponent<CardOnHand>().OnCardMouseIn();
            foreach(CardOnHand cardOnHand in cards){
                if(collisionGameObject.GetComponent<CardOnHand>() == cardOnHand){
                    cardOnHand.OnCardMouseIn();
                    dragTarget = collisionGameObject; // Update의 드래그 이벤트용 raycast가 안먹는 경우가 있어서, 임시로 마우스 진입할때도 드래그 타겟 설정
                }else{
                    cardOnHand.OnCardMouseOut();
                }
            }
        }else{
            foreach(CardOnHand cardOnHand in cards){
                cardOnHand.OnCardMouseOut();
            }
        }
    }

    // 드래그 시작
    private void HandleCardDragStart()
    {
        if(Input.GetMouseButtonDown(0)){
            dragTarget = null;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)){
                if (hit.collider != null && hit.collider.gameObject.GetComponent<CardOnHand>() != null){
                    CardOnHand cardOnHand = hit.collider.gameObject.GetComponent<CardOnHand>();
                    if(cardOnHand.isOwned){
                        dragTarget = hit.collider.gameObject;
                        dragTarget.GetComponent<CardOnHand>().OnCardDragStart();
                    }  
                }
            }
        }
    }

    // 드래그 진행중
    private void HandleCardDrag()
    {
        if(Input.GetMouseButton(0)){
            if(dragTarget != null){
                CardOnHand cardOnHand = dragTarget.GetComponent<CardOnHand>();
                if(cardOnHand.isOwned){
                    cardOnHand.OnCardDrag();
                }
            }
        }
    }

    // 드래그 종료
    private void HandleCardDragEnd()
    {
        if(Input.GetMouseButtonUp(0)){
            if(dragTarget != null){
                CardOnHand cardOnHand = dragTarget.GetComponent<CardOnHand>();
                if(cardOnHand.isOwned){
                    cardOnHand.OnCardDragEnd();
                    dragTarget = null; 
                }
            }  
        }
    }

    // 턴값에 따라 로컬 유저의 카드 더미 위치 변경
    private void ChangePocketPositionByTurn()
    {
        if(M_TurnManager.instance.isMyTurn){
            transform.localPosition = showPosition;
        }else{
            transform.localPosition = hidePosition;
        }
    }
}