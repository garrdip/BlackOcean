using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardPocket : NetworkBehaviour
{
    private Vector3 hidePosition;
    private Vector3 showPosition;
    public Camera mainCamera;
    public GameObject dragTarget;
    public GamePlayerDeck currentPlayerDeck;


    void Start()
    {
        transform.SetParent(DeckUI.instance.DeckListPanel.transform);
        hidePosition = transform.localPosition + new Vector3(-20f, -3.5f, 0f);
        showPosition = transform.localPosition + new Vector3(0, -3.5f, 0f);
        mainCamera = Camera.main;
        if(NetworkClient.connection != null){
            currentPlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        }
    }

    void Update()
    {   
        if(isOwned){
            HandleCardDragStart();
            HandleCardDrag();
            HandleCardDragEnd();
            HandleMouseInOut();
        }
    }

    void FixedUpdate()
    {
        if(isOwned){
            SetCardOfHandPositionSymmetry();
            ChangePocketPositionByTurn();
        }
    }

    // 현재 플레이어의 CardOnHands 리스트를 통해 각 카드들의 위치, 회전, 크기 제어
    public void SetCardOfHandPositionSymmetry()
    {
        GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
        int count = gamePlayerDeck.cardOnHands.Count;
        for(int i=0; i<count; i++){      
            CardOnHand cardOnHand =  gamePlayerDeck.cardOnHands[i];
            if(cardOnHand.isMouseOver){
                Vector3 targetPosition = new Vector3(cardOnHand.transform.localPosition.x, cardOnHand.hoveredPositionY, cardOnHand.transform.localPosition.z);
                cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, targetPosition, Time.deltaTime * 10f);
                cardOnHand.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                cardOnHand.transform.localScale = Vector3.Lerp(cardOnHand.transform.localScale, cardOnHand.targetScale, Time.deltaTime * 10f);
            }else{
                // 대칭 위치값 계산
                int leftCount = (count - 1) / 2;
                int rightCount = count - leftCount - 1;
                float symmetryPosition = (count % 2 == 0) ? ((i - leftCount) * 1.5f - 0.75f) : ((i - leftCount) * 1.5f + 0f);

                // 위치값(카드 개수에 따라 좌우 대칭값 계산하여 각 카드의 x, y 좌표 설정)
                Vector3 position = new Vector3(symmetryPosition, -Mathf.Abs(symmetryPosition) * 0.15f, 0f);
                cardOnHand.transform.localPosition = Vector3.Lerp(cardOnHand.transform.localPosition, position, Time.deltaTime * 10f);

                // 회전값
                Quaternion rotation = Quaternion.Euler(0f, 0f, -symmetryPosition);
                cardOnHand.transform.localRotation = rotation;
                cardOnHand.originRotation = new Vector3(0f, 0f, -symmetryPosition * 1.5f);

                // 크기값
                cardOnHand.transform.localScale = Vector3.Lerp(cardOnHand.transform.localScale, cardOnHand.originScale, Time.deltaTime * 10f);  
            }
        }
    }

    // 마우스 In, Out 이벤트
    private void HandleMouseInOut()
    {
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
            foreach(CardOnHand cardOnHand in currentPlayerDeck.cardOnHands){
                if(collisionGameObject.GetComponent<CardOnHand>() == cardOnHand){
                    cardOnHand.OnCardMouseIn();
                }else{
                    cardOnHand.OnCardMouseOut();
                }
            }
        }else{
            foreach(CardOnHand cardOnHand in currentPlayerDeck.cardOnHands){
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
                        dragTarget.GetComponent<CardOnHand>().OnCardDragStart(hit.collider.bounds.center);
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