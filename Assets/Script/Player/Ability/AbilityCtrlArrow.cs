using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using ProjectD;


public class AbilityCtrlArrow : NetworkBehaviour
{
    public TargetObject currentTarget; // 현재 소환된 화살표가 타겟으로 잡은 오브젝트
    public float scaleFactor = 1f;
    private Transform origin;
    public List<Transform> arrowNodes = new List<Transform>();
    public Vector2[] controlPoints = new Vector2[2];

    void Start()
    {
        origin = GetComponent<Transform>();
        ChangeArrowVisible(isOwned, GameUIManager.instance.CardOnHandsPanel.transform);
        M_CardManager.instance.isArrowActive = false; // 생성 시점에는 오브젝트가 활성화 되어있지만(네트워크 오브젝트는 생성시 Active 상태), 활성화 상태 변수값은 false로 초기화
        RemoveAbilityCtrlArrow();
    }

    void Update()
    {
        if(isOwned){       
            SetArrowNodesScale();
            HandleArrowAction();
            HandleArrowRemove();
            HandleArrowNodesTrasnform();
        }
    }

    // 화살표 노드들의 위치, 회전값 조절
    private void HandleArrowNodesTrasnform()
    {
        if(controlPoints.Length > 0){
            // P3 is at the mouse position
            Vector3 mousePosition = Input.mousePosition; // 마우스 좌표 가져오기
            mousePosition.z = Camera.main.nearClipPlane; // 카메라가 바라보는 위치로 설정
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition); // 화면 좌표를 월드 좌표로 변환
            this.controlPoints[1] = worldPosition;
            if(arrowNodes.Count > 0){
                for(int i=0; i<this.arrowNodes.Count; i++){

                    // Cubic Bezier Curve
                    // B(t) = (1-t)^3 * P0 + 3 * (1-t)^2 * t * P1 + 3 * (1-t) * t^2 * P2 + t^3 * P3
                    this.arrowNodes[i].position = controlPoints[0] - (controlPoints[0] - controlPoints[1])*i/(arrowNodes.Count - 1);

                    // Caculates rotations for each arrow node.
                    if(i > 0){
                        var euler = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, this.arrowNodes[i].position - this.arrowNodes[i - 1].position));
                        this.arrowNodes[i].rotation = Quaternion.Euler(euler);
                    }

                    // The first arrow node's rotation
                    this.arrowNodes[0].transform.rotation = this.arrowNodes[1].transform.rotation;
                }        
            }
        }
    }

    // 마우스 오른쪽 버튼 클릭 시 화살표 제거
    private void HandleArrowRemove()
    {
        if(Input.GetMouseButtonDown(1)){
            RemoveAbilityCtrlArrow();
        }
    }

    // 마우스 왼쪽버튼 뗄때 마우스로 타겟팅한 오브젝트에게 액션 수행
    private void HandleArrowAction()
    {
        if(Input.GetMouseButtonUp(0) && currentTarget != null){
            if(NetworkClient.connection != null && NetworkClient.active){
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.GetComponent<GamePlayerDeck>();
                CmdEnQueueCardData(gamePlayerDeck, currentTarget, NetworkClient.connection.identity);
                RemoveAbilityCtrlArrow();
            }
        }
    }

    [Command]
    void CmdEnQueueCardData(GamePlayerDeck gamePlayerDeck, TargetObject tar, NetworkIdentity conn)
    {
        gamePlayerDeck.serverCardPredictQueue.Enqueue((gamePlayerDeck.abilityCard, tar, conn));
    }

    // 화살표 초기화(위치설정, visible상태 활성화, 베지어 곡선 조작점 설정, 화살표 활성화 상태 변수 변경)
    public void InitCardCtrlArrow(AbilityButton abilityButton)
    {
        transform.position = abilityButton.transform.position;
        ChangeArrowVisible(true, abilityButton.transform);
        InitBezierCurvePoint(abilityButton);
    }

    // 베지어 곡선 조작점 초기 위치값을 카드의 위치로 설정
    private void InitBezierCurvePoint(AbilityButton abilityButton)
    {
        controlPoints[0] = abilityButton.transform.position;
    }

    // 현재 소환된 카드 타겟 화살표 제거, 화살표 소유 카드의 상태값 변경,
    public void RemoveAbilityCtrlArrow()
    {
        ChangeArrowVisible(false, GameUIManager.instance.CardOnHandsPanel.transform);
    }

    // 화살표의 활성화 상태 변경 및 부모 오브젝트 설정 변경
    public void ChangeArrowVisible(bool isVisible, Transform parent)
    {
        this.gameObject.SetActive(isVisible);
        M_CardManager.instance.isAbilityArrowActive = isVisible;
        transform.SetParent(parent);
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

}
