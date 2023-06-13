using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;


public class CardCtrlArrow : NetworkBehaviour
{
    [SyncVar]
    public CardOnHand arrowOwnedCardOnHand; // 현재 소환된 화살표의 주인 카드

    public GameObject arrowHeadPrefab;
    public GameObject arrowNodePrefab;

    public float scaleFactor = 1f;

    private const int arrowSortingOrder = 1000; // 카드 Hover상태의 경우 카드의 sortingOrder는 999, 따라서 화살표는 그보다 항상 위
    private Transform origin;
    public List<Transform> arrowNodes = new List<Transform>();
    private List<Vector2> controlPoints = new List<Vector2>();
    private readonly List<Vector2> controlPointFactors = new List<Vector2>{ new Vector2(-0.3f, 0.8f), new Vector2(0.1f, 1.4f) };

    void Start()
    {
        origin = GetComponent<Transform>();
    }

    void Update()
    {
        if(isOwned){          
            HandleArrowAction();
            HandleArrowRemove();
            HandleArrowNodesTrasnform();
        }
    }

    // 화살표 노드들의 위치, 회전값 조절
    private void HandleArrowNodesTrasnform()
    {
        if(controlPoints.Count > 0){
            // P3 is at the mouse position
            Vector3 mousePosition = Input.mousePosition; // 마우스 좌표 가져오기
            mousePosition.z = Camera.main.nearClipPlane; // 카메라가 바라보는 위치로 설정
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition); // 화면 좌표를 월드 좌표로 변환
            this.controlPoints[3] = worldPosition;
            if(arrowNodes.Count > 0){
                for(int i=0; i<this.arrowNodes.Count; i++){
                    // Caculates t.
                    var t = Mathf.Log(1f * i / (this.arrowNodes.Count -1) + 1f, 2f);
                    
                    // Cubic Bezier Curve
                    // B(t) = (1-t)^3 * P0 + 3 * (1-t)^2 * t * P1 + 3 * (1-t) * t^2 * P2 + t^3 * P3
                    this.arrowNodes[i].position =
                        Mathf.Pow(1 - t, 3) * this.controlPoints[0] +
                        3 * Mathf.Pow(1 - t, 2) * t * this.controlPoints[1] +
                        3 * (1 - t) * Mathf.Pow(t, 2) * this.controlPoints[2] + 
                        Mathf.Pow(t, 3) * this.controlPoints[3];

                    // Caculates rotations for each arrow node.
                    if(i > 0){
                        var euler = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, this.arrowNodes[i].position - this.arrowNodes[i - 1].position));
                        this.arrowNodes[i].rotation = Quaternion.Euler(euler);
                    }

                    // The first arrow node's rotation
                    this.arrowNodes[0].transform.rotation = this.arrowNodes[1].transform.rotation;
                }        
            }
            CalculateBezierCurvePoint();
        }
    }

    // 마우스 오른쪽 버튼 클릭 시 화살표 제거
    private void HandleArrowRemove()
    {
        if(Input.GetMouseButtonDown(1)){
            RemoveCardCtrlArrow();
        }
    }

    // 마우스 왼쪽버튼 뗄때 마우스로 타겟팅한 오브젝트에게 액션 수행
    private void HandleArrowAction()
    {
        if(Input.GetMouseButtonUp(0)){
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log("Mouse Up!");
            RaycastHit2D hit;
            if (hit = Physics2D.Raycast(mousePosition, Vector2.zero)){
                if(hit.collider != null)Debug.Log(hit.collider);
                if(hit.collider != null && hit.collider.tag.Equals("TargetObject")){
                    if(NetworkClient.connection != null && NetworkClient.active){
                        GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                        if(gamePlayerDeck.isLocalPlayer && arrowOwnedCardOnHand != null){
                            TargetObject targetObjects = hit.collider.transform.parent.GetComponent<TargetObject>();
                            gamePlayerDeck.CmdEnQueueCardTargetPair(arrowOwnedCardOnHand.card, targetObjects, NetworkClient.connection.identity, this); // 카드와 카드 타겟들을 한 쌍으로 하는 Dictionary 데이터 생성
                        }
                    }
                }
            }
        }
    }

    // 화살표 초기화(위치설정, visible상태 활성화, 베지어 곡선 조작점 설정)
    public void InitCardCtrlArrow(CardOnHand cardOnHand)
    {
        transform.position = cardOnHand.transform.position;
        ChangeArrowVisible(true, cardOnHand.transform);
        InitBezierCurvePoint(cardOnHand);
    }

    // 베지어 곡선 조작점 초기 위치값을 카드의 위치로 설정
    private void InitBezierCurvePoint(CardOnHand cardOnHand)
    {
        controlPoints.Clear();
        for(int i=0; i<4; i++){
            this.controlPoints.Add(cardOnHand.transform.position);
        }
    }

    // 현재 소환된 카드 타겟 화살표 제거, 화살표 소유 카드의 충돌체 크기 변경 및 상태값 변경,
    public void RemoveCardCtrlArrow()
    {
        if(isOwned && arrowOwnedCardOnHand != null){
            ChangeArrowVisible(false, DeckUI.instance.CardOnHandsPanel.transform);
            arrowOwnedCardOnHand.isDrag = false;
            arrowOwnedCardOnHand.isMoving = false;
            arrowOwnedCardOnHand.isMouseOver = false;
            arrowOwnedCardOnHand.transform.GetComponent<SpriteRenderer>().sortingOrder = arrowOwnedCardOnHand.originSortOrder;
            M_CardManager.instance.ChangeCardOnHandColliderSize(arrowOwnedCardOnHand, M_CardManager.instance.cardCollidableSize);
        }
    }

    // 화살표의 활성화 상태 변경 및 부모 오브젝트 설정 변경
    public void ChangeArrowVisible(bool isVisible, Transform parent)
    {
        this.gameObject.SetActive(isVisible);
        transform.SetParent(parent);
    }

    // 베지어 곡선 조작점 계산
    private void CalculateBezierCurvePoint()
    {
        // P0 is at the arrow emitter point
        this.controlPoints[0] = new Vector2(this.origin.position.x, this.origin.position.y);

        // P1, P2 determines by P0 and P3
        // P1 = P0 + (P3 - P0) * Vector2(-0.3f, 0.8f)
        // P2 = P0 + (P3 - P0) * Vector2(0.1f, 1.4f)
        this.controlPoints[1] = this.controlPoints[0] + (this.controlPoints[3] - this.controlPoints[0]) * this.controlPointFactors[0];
        this.controlPoints[2] = this.controlPoints[0] + (this.controlPoints[3] - this.controlPoints[0]) * this.controlPointFactors[1];
    }
     
    // 화살표 노드들의 크기값 세팅
    private void SetArrowNodesScale()
    {
        if(arrowNodes.Count > 0){
            for(int i=0; i<this.arrowNodes.Count; ++i){
                // Calculate scales for each arrow node
                var scale = this.scaleFactor * (0.4f - 0.02f * (this.arrowNodes.Count -1 -i));
                this.arrowNodes[i].localScale = (i == (arrowNodes.Count-1)) 
                    ? new Vector3(scale + 0.2f, scale + 0.2f, 0f) // Arrow Head Scale
                    : new Vector3(scale, scale - 0.2f, 0f); // Arrow Node Scale
            }
        }
    }

    // 화살표 머리와 몸통 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅 및 소유 권한 구분용 색상 변경
    [ClientRpc]
    public void RpcSetArrowParts(List<GameObject> nodes)
    {
        ChangeArrowVisible(isOwned, DeckUI.instance.CardOnHandsPanel.transform);
        foreach(GameObject arrowNode in nodes){
            arrowNode.transform.SetParent(transform);
            arrowNode.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
            arrowNode.GetComponent<SpriteRenderer>().sortingOrder = arrowSortingOrder;
            arrowNodes.Add(arrowNode.GetComponent<Transform>());
        }
        SetArrowNodesScale();
    }

    // 화살표를 소환한 카드의 액션 수행 이벤트 수신
    [ClientRpc]
    public void RpcAcceptCardUse(NetworkIdentity conn)
    {
        if(conn == NetworkClient.connection.identity){
            ChangeArrowVisible(false, DeckUI.instance.CardOnHandsPanel.transform); // 화살표 활성화 상태 변경
            M_CardManager.instance.CardOnHandThrowAwaySequence(arrowOwnedCardOnHand); // 화살표 주인 카드 제거
            M_CardManager.instance.ChangeCardOnHandColliderSize(arrowOwnedCardOnHand, M_CardManager.instance.cardCollidableSize); // 카드 충돌체 크기 변경
        }
    }

}
