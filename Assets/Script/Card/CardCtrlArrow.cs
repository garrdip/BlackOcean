using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;


public class CardCtrlArrow : NetworkSingletonD<CardCtrlArrow>
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
        this.arrowNodes.ForEach(a => a.GetComponent<Transform>().position = new Vector2(-1000, -1000));
        for(int i=0; i<4; i++){
            this.controlPoints.Add(Vector2.zero);
        }
    }

    void Update()
    {
        if(isOwned){          
            // P3 is at the mouse position
            Vector3 mousePosition = Input.mousePosition; // 마우스 좌표 가져오기
            mousePosition.z = Camera.main.nearClipPlane; // 카메라가 바라보는 위치로 설정
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition); // 화면 좌표를 월드 좌표로 변환
            this.controlPoints[3] = worldPosition;
            HandleArrowAction();
            HandleArrowRemove();
        }

        // P0 is at the arrow emitter point
        this.controlPoints[0] = new Vector2(this.origin.position.x, this.origin.position.y);

        // P1, P2 determines by P0 and P3
        // P1 = P0 + (P3 - P0) * Vector2(-0.3f, 0.8f)
        // P2 = P0 + (P3 - P0) * Vector2(0.1f, 1.4f)
        this.controlPoints[1] = this.controlPoints[0] + (this.controlPoints[3] - this.controlPoints[0]) * this.controlPointFactors[0];
        this.controlPoints[2] = this.controlPoints[0] + (this.controlPoints[3] - this.controlPoints[0]) * this.controlPointFactors[1];

        if(arrowNodes.Count > 0){
            for(int i=0; i<this.arrowNodes.Count; ++i){
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
                // Calculate scales for each arrow node
                var scale = this.scaleFactor * (0.4f - 0.02f * (this.arrowNodes.Count -1 -i));
                this.arrowNodes[i].localScale = (i == (arrowNodes.Count-1)) 
                    ? new Vector3(scale + 0.2f, scale + 0.2f, 0f) // Arrow Head Scale
                    : new Vector3(scale, scale - 0.2f, 0f); // Arrow Node Scale
            }
            // The first arrow node's rotation
            this.arrowNodes[0].transform.rotation = this.arrowNodes[1].transform.rotation;
        }
    }

    // 화살표 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅 및 화살표 오브젝트으 부모를 생성요청한 CardOnHand오브젝트로 설정
    [ClientRpc]
    public void RpcArrowInit(Vector3 position, CardOnHand cardOnHand)
    {
        transform.SetParent(cardOnHand.transform);
        transform.position = position;
    }

    // 화살표 머리 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅 및 소유 권한 구분용 색상 변경
    [ClientRpc]
    public void RpcSetArrowHead(GameObject arrowHead)
    {
        arrowHead.transform.SetParent(transform);
        arrowNodes.Add(arrowHead.GetComponent<Transform>());
        arrowHead.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        arrowHead.GetComponent<SpriteRenderer>().sortingOrder = arrowSortingOrder;
    }
    
    // 화살표 몸통 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅 및 소유 권한 구분용 색상 변경
    [ClientRpc]
    public void RpcSetArrowNode(GameObject arrowNode)
    {
        arrowNode.transform.SetParent(transform);
        arrowNodes.Add(arrowNode.GetComponent<Transform>());
        arrowNode.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        arrowNode.GetComponent<SpriteRenderer>().sortingOrder = arrowSortingOrder;
    }

    // 마우스 오른쪽 버튼 클릭 시 화살표 제거
    public void HandleArrowRemove()
    {
        if(Input.GetMouseButtonDown(1)){
            if(NetworkClient.connection != null && arrowOwnedCardOnHand != null){
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                gamePlayerDeck.CmdDestroyArrowEmitter(this.gameObject);
                arrowOwnedCardOnHand.isDrag = false;
                arrowOwnedCardOnHand.isMoving = false;
            }
        }
    }

    // 마우스 왼쪽버튼 뗄때 마우스로 타겟팅한 오브젝트에게 액션 수행
    private void HandleArrowAction()
    {
        if(Input.GetMouseButtonUp(0)){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)){
                if(hit.collider != null && NetworkClient.connection != null && hit.collider.tag.Equals("TargetObject")){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    if(gamePlayerDeck.isLocalPlayer && arrowOwnedCardOnHand != null){
                        TargetObject targetObject = hit.collider.gameObject.GetComponent<TargetObject>();
                        gamePlayerDeck.CmdActionToTarget(targetObject, arrowOwnedCardOnHand); // 화살표 타겟에 액션 수행
                        gamePlayerDeck.CmdDestroyArrowEmitter(this.gameObject); // 화살표 삭제
                        arrowOwnedCardOnHand.CardOnHandThrowAwaySequence(arrowOwnedCardOnHand); // 화살표 주인 카드 제거
                    }
                }
            }
        }
    }
}
