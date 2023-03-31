using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;


public class CardCtrlArrow : NetworkSingletonD<CardCtrlArrow>, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public GameObject arrowHeadPrefab;
    public GameObject arrowNodePrefab;

    public float scaleFactor = 1f;

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
        RemoveArrowOnMouseRightClicked();
    }

    // 클라이언트에 화살표 오브젝트 생성 시 오브젝트의 부모오브젝트를 GameCanvas로 설정
    public override void OnStartClient()
    {
        transform.SetParent(DeckUI.instance.DeckListPanel.transform);
    }

    // 화살표 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅
    [ClientRpc]
    public void RpcArrowInit(Vector3 position)
    {
        transform.position = position;
        //transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // 화살표 머리 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅 및 소유 권한 구분용 색상 변경
    [ClientRpc]
    public void RpcSetArrowHead(GameObject arrowHead)
    {
        arrowHead.transform.SetParent(transform);
        arrowNodes.Add(arrowHead.GetComponent<Transform>());
        arrowHead.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        arrowHead.GetComponent<SpriteRenderer>().sortingOrder = 2;
    }
    
    // 화살표 몸통 네트워크 오브젝트 생성되면 클라이언트별로 생성 위치 세팅 및 소유 권한 구분용 색상 변경
    [ClientRpc]
    public void RpcSetArrowNode(GameObject arrowNode)
    {
        arrowNode.transform.SetParent(transform);
        arrowNodes.Add(arrowNode.GetComponent<Transform>());
        arrowNode.GetComponent<SpriteRenderer>().color = isOwned ? Color.red : Color.white;
        arrowNode.GetComponent<SpriteRenderer>().sortingOrder = 2;
    }

    // 마우스 오른쪽 버튼 클릭 시 화살표 제거
    public void RemoveArrowOnMouseRightClicked()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if(NetworkClient.connection != null){
                GamePlayer gamePlayer = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>();
                gamePlayer.CmdDestroyArrowEmitter(this.gameObject);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        
    }

    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {   
        
    }

    // 드래그 진행중
    public void OnDrag(PointerEventData eventData)
    {
        
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        
    }

    // 드랍 이벤트
    public void OnDrop(PointerEventData eventData)
    {
        
    }
}
