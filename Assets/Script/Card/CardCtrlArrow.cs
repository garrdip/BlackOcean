using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;
using ProjectD;


public class CardCtrlArrow : NetworkBehaviour
{
    [SyncVar]
    public CardOnHand arrowOwnedCardOnHand; // 현재 소환된 화살표의 주인 카드
    public GameObject currentTarget; // 현재 소환된 화살표가 타겟으로 잡은 오브젝트

    public float scaleFactor = 1f;
    private Transform origin;
    public List<Transform> arrowNodes = new List<Transform>();
    private readonly List<Vector2> controlPoints = new List<Vector2>();
    private readonly List<Vector2> controlPointFactors = new List<Vector2>{ new Vector2(-0.3f, 0.8f), new Vector2(0.1f, 1.4f) };


    // 공격용 화살표
    public Sprite attackArrowHeadEnemy; // 화살표가 타겟에 진입할 때 헤드 이미지
    public Sprite attackArrowHeadNormal; // 화살표가 타겟에 나갈 때 헤드 이미지
    public Sprite attackArrowNodeEnemy; // 화살표가 타겟에 진입할 때 노드 이미지
    public Sprite attackArrowNodeNormal; // 화살표가 타겟에 나갈 때 노드 이미지
    public GameObject arrowHeadExpanderLeft; // 화살표 헤드 확장 오브젝트 왼쪽 파츠
    public GameObject arrowHeadExpanderRight; // 화살표 헤드 확장 오브젝트 오른쪽 파츠
    public Sprite arrowExpandLeftNormal; // 화살표 헤드 왼쪽 기본 상태 이미지
    public Sprite arrowExpandRightNormal; // 화살표 헤드 오른쪽 기본 상태 이미지
    public Sprite arrowExpandLeftLight; // 화살표 헤드 왼쪽 확장 상태 이미지
    public Sprite arrowExpandRightLight; // 화살표 헤드 오른쪽 확장 상태 이미지


    // 버프용 화살표
    public Sprite buffArrowHeadNormal;
    public Sprite buffArrowHeadEnemy;
    public Sprite buffArrowHeadAlly;
    public Sprite buffArrowNodeNormal;
    public Sprite buffArrowNodeEnemy;
    public Sprite buffArrowNodeAlly;
    public GameObject buffArrowHeadCircle;


    void Start()
    {
        origin = GetComponent<Transform>();
        SetArrowNodesScale();
        ChangeArrowVisible(isOwned, GameUIManager.instance.CardOnHandsPanel.transform);
        M_CardManager.instance.isArrowActive = false; // 생성 시점에는 오브젝트가 활성화 되어있지만(네트워크 오브젝트는 생성시 Active 상태), 활성화 상태 변수값은 false로 초기화
    }

    void Update()
    {
        if(isOwned){   
            HandleArrowPosition();     
            HandleArrowAction();
            HandleArrowRemove();
            HandleArrowNodesTrasnform();
        }
    }

    void OnDestroy()
    {
        arrowHeadExpanderLeft.transform.DOKill();
        arrowHeadExpanderRight.transform.DOKill();
        buffArrowHeadCircle.transform.DOKill();
    }

    private void HandleArrowPosition()
    {
        if(arrowOwnedCardOnHand != null){
            transform.position = arrowOwnedCardOnHand.transform.position;
        }
    }

    // 화살표 노드들의 위치, 회전값 조절
    private void HandleArrowNodesTrasnform()
    {
        if(controlPoints.Count > 0){
            Vector3 mousePosition = Input.mousePosition; // 마우스 좌표 가져오기
            mousePosition.z = Camera.main.nearClipPlane; // 카메라가 바라보는 위치로 설정
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition); // 화면 좌표를 월드 좌표로 변환

            SpriteRenderer arrowHeadSpriteRenderer = this.arrowNodes[this.arrowNodes.Count - 1].GetComponent<SpriteRenderer>(); // 화살표 머리 오브젝트의 스프라이트 랜더러
            float arrowHeadHeight = arrowHeadSpriteRenderer.bounds.size.y; // 화살표 머리의 스프라이트 랜더러 높이
            Vector3 controlPoint2 = new Vector3(this.controlPoints[2].x, this.controlPoints[2].y, 0);
            Vector3 arrowDirection = (worldPosition - controlPoint2).normalized; // 화살표가 향하는 방향
            this.controlPoints[3] = worldPosition - arrowDirection * (arrowHeadHeight); //  P3(controlPoints[3])는 마우스 위치(화살표 머리의 위쪽 끝이 마우스 위치에 오도록 위치 보정)
            
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

                    // 화살표 머리와 바로 이전 노드사이 위치 1.5배 더 떨어지도록 조절
                    var lastNode = arrowNodes[arrowNodes.Count - 1];
                    var prevLastNode = arrowNodes[arrowNodes.Count - 2];
                    Vector3 direction = (lastNode.position - prevLastNode.position).normalized;
                    float distance = Vector3.Distance(prevLastNode.position, lastNode.position);
                    lastNode.position = prevLastNode.position + direction * distance * 1.5f;
                    var finalEuler = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, lastNode.position - prevLastNode.position));
                    lastNode.rotation = Quaternion.Euler(finalEuler);
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
        if(Input.GetMouseButtonUp(0) && currentTarget != null){
            if(NetworkClient.connection != null && NetworkClient.active){
                GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                if(gamePlayerDeck.isOwned && arrowOwnedCardOnHand != null){
                    if(arrowOwnedCardOnHand.isUsed == false)
                    {
                        TargetObject targetObject = currentTarget.transform.parent.GetComponent<TargetObject>();
                        //카드 사용 유무 판단 위치
                        if(targetObject == null)return;
                        if(arrowOwnedCardOnHand.card.baseCard.isTargetable)
                        {
                            switch(arrowOwnedCardOnHand.card.baseCard.validTarget)
                            {
                                case ValidTarget.ENEMY :
                                    if(targetObject.objectType != ObjectType.ENEMY) return;
                                    break;
                                case ValidTarget.MEMBER :
                                    if(targetObject.objectType == ObjectType.ENEMY)
                                        return;
                                    if(targetObject.player == GetComponent<GamePlayer>())
                                        return;
                                    break;
                                case ValidTarget.TEAM :
                                    if(targetObject.objectType != ObjectType.PLAYER)
                                        return;
                                    break;
                            }
                        }
                        if(gamePlayerDeck.GetTotalCostOfCardOnHand(arrowOwnedCardOnHand) > gamePlayerDeck.currentIchi) // 카드 코스트 계산 하는곳
                            return;
                        //
                        arrowOwnedCardOnHand.isUsed = true;
                        arrowOwnedCardOnHand.isMoving = false;
                        CmdEnQueueCardData(gamePlayerDeck, arrowOwnedCardOnHand,targetObject); // 카드와 카드 타겟들을 한 쌍으로 하는 Dictionary 데이터 생성
                        AcceptCardUse();
                    }
                }
            }
        }
    }


    [Command]
    void CmdEnQueueCardData(GamePlayerDeck gamePlayerDeck, CardOnHand cardOnHand, TargetObject tar)
    {
        gamePlayerDeck.serverCardPredictQueue.Enqueue((cardOnHand, tar));
    }

    // 화살표 초기화(위치설정, visible상태 활성화, 베지어 곡선 조작점 설정, 화살표 활성화 상태 변수 변경, 마우스 커서 보이지 안게 변경)
    public void InitCardCtrlArrow(CardOnHand cardOnHand)
    {
        transform.position = cardOnHand.transform.position;
        ChangeArrowVisible(true, cardOnHand.transform);
        InitBezierCurvePoint(cardOnHand);
        Cursor.visible = false;
        SetArrowByValidTarget(cardOnHand);
    }

    // 베지어 곡선 조작점 초기 위치값을 카드의 위치로 설정
    private void InitBezierCurvePoint(CardOnHand cardOnHand)
    {
        controlPoints.Clear();
        for(int i=0; i<4; i++){
            this.controlPoints.Add(cardOnHand.transform.position);
        }
    }

    // 현재 소환된 카드 타겟 화살표 제거, 화살표 소유 카드의 상태값 변경, 마우스 커서 보이게 변경
    public void RemoveCardCtrlArrow()
    {
        if(isOwned && arrowOwnedCardOnHand != null){
            ChangeArrowVisible(false, GameUIManager.instance.CardOnHandsPanel.transform);
            arrowOwnedCardOnHand.isDrag = false;
            arrowOwnedCardOnHand.isMoving = false;
            arrowOwnedCardOnHand.isMouseOver = false;
            M_CardManager.instance.ChangeCardOnHandShiftState(arrowOwnedCardOnHand, false);
            Cursor.visible = true;
            arrowOwnedCardOnHand = null;
            TargetIndicatorController.instance.DisableTargetIndicator();
        }
    }

    // 화살표의 활성화 상태 변경 및 부모 오브젝트 설정 변경
    public void ChangeArrowVisible(bool isVisible, Transform parent)
    {
        this.gameObject.SetActive(isVisible);
        M_CardManager.instance.isArrowActive = isVisible;
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
                    ? new Vector3(scale + 0.5f, scale + 0.5f, 0f) // Arrow Head Scale
                    : new Vector3(scale + 0.35f, scale + 0.35f, 0f); // Arrow Node Scale
            }
        }
    }

    // 화살표를 소환한 카드의 액션 수행 이벤트 수신
    public void AcceptCardUse()
    {
        ChangeArrowVisible(false, GameUIManager.instance.CardOnHandsPanel.transform); // 화살표 활성화 상태 변경
        M_CardManager.instance.CardOnHandThrowAwaySequence(arrowOwnedCardOnHand); // 화살표 주인 카드 제거
        Cursor.visible = true;
        arrowOwnedCardOnHand = null;
        TargetIndicatorController.instance.DisableTargetIndicator();
    }

    // 타겟 유형에 따라 화살표 리소스 설정
    private void SetArrowByValidTarget(CardOnHand cardOnHand)
    {
        if(cardOnHand.card.baseCard.isTargetable){
            switch(cardOnHand.card.baseCard.validTarget)
            {
                case ValidTarget.ENEMY:
                    // 단일 적 공격 카드
                    for(int i=0; i<this.arrowNodes.Count; i++){
                        SpriteRenderer spriteRenderer = arrowNodes[i].GetComponent<SpriteRenderer>();
                        if(i == (arrowNodes.Count-1)){
                            spriteRenderer.sprite = attackArrowHeadNormal;
                        }else{
                            spriteRenderer.sprite = attackArrowNodeNormal;
                        }
                    }
                    arrowHeadExpanderLeft.SetActive(true);
                    arrowHeadExpanderRight.SetActive(true);
                    buffArrowHeadCircle.SetActive(false);
                    break;
                case ValidTarget.ENEMY_ALL:
                    // 전체 적 공격 카드
                    for(int i=0; i<this.arrowNodes.Count; i++){
                        SpriteRenderer spriteRenderer = arrowNodes[i].GetComponent<SpriteRenderer>();
                        if(i == (arrowNodes.Count-1)){
                            spriteRenderer.sprite = attackArrowHeadNormal;
                        }else{
                            spriteRenderer.sprite = attackArrowNodeNormal;
                        }
                    }
                    arrowHeadExpanderLeft.SetActive(true);
                    arrowHeadExpanderRight.SetActive(true);
                    buffArrowHeadCircle.SetActive(false);
                    break;
                case ValidTarget.MEMBER:
                    // 단일 아군 버프 카드
                    for(int i=0; i<this.arrowNodes.Count; i++){
                        SpriteRenderer spriteRenderer = arrowNodes[i].GetComponent<SpriteRenderer>();
                        if(i == (arrowNodes.Count-1)){
                            spriteRenderer.sprite = buffArrowHeadNormal;
                        }else{
                            spriteRenderer.sprite = buffArrowNodeNormal;
                        }
                    }
                    arrowHeadExpanderLeft.SetActive(false);
                    arrowHeadExpanderRight.SetActive(false);
                    buffArrowHeadCircle.SetActive(true);
                    break;
                case ValidTarget.TEAM:
                    // 전체 아군 버프 카드
                    for(int i=0; i<this.arrowNodes.Count; i++){
                        SpriteRenderer spriteRenderer = arrowNodes[i].GetComponent<SpriteRenderer>();
                        if(i == (arrowNodes.Count-1)){
                            spriteRenderer.sprite = buffArrowHeadNormal;
                        }else{
                            spriteRenderer.sprite = buffArrowNodeNormal;
                        }
                    }
                    arrowHeadExpanderLeft.SetActive(false);
                    arrowHeadExpanderRight.SetActive(false);
                    buffArrowHeadCircle.SetActive(true);
                    break;
                case ValidTarget.ALL:
                    // 아군 및 적군 모두 적용 가능한 버프 카드
                    for(int i=0; i<this.arrowNodes.Count; i++){
                        SpriteRenderer spriteRenderer = arrowNodes[i].GetComponent<SpriteRenderer>();
                        if(i == (arrowNodes.Count-1)){
                            spriteRenderer.sprite = buffArrowHeadNormal;
                        }else{
                            spriteRenderer.sprite = buffArrowNodeNormal;
                        }
                    }
                    arrowHeadExpanderLeft.SetActive(false);
                    arrowHeadExpanderRight.SetActive(false);
                    buffArrowHeadCircle.SetActive(true);
                    break;
            }
        }
    }

    // 화살표 노드들 이미지를 타겟에 진입 or 벗어날 때 상태에 따라 다른 이미지 설정
    public void ChangeArrowStateByValidTarget(bool isEnter, GameObject target)
    {
        if(arrowOwnedCardOnHand.card.baseCard.isTargetable){
            TargetObject targetObject = target.transform.parent.GetComponent<TargetObject>();
            SetArrowNodesByValidTarget(isEnter, targetObject);
            TargetIndicatorController.instance.EnableTargetIndiCatorByArrow(arrowOwnedCardOnHand.card.baseCard.validTarget, isEnter, targetObject);
        }
    }

    // 타겟에 따라 화살표 헤드 및 노드의 이미지 구분 적용
    private void SetArrowNodesByValidTarget(bool isEnter, TargetObject targetObject)
    {
        switch(arrowOwnedCardOnHand.card.baseCard.validTarget)
        {
            case ValidTarget.ENEMY :
                // 공격 카드
                if(targetObject.objectType == ObjectType.ENEMY){
                    SetArrowNodesSprite(isEnter, attackArrowHeadEnemy,attackArrowHeadNormal, attackArrowNodeEnemy, attackArrowNodeNormal);
                    SetAttackArrowExpanderState(isEnter);
                }
                break;
            case ValidTarget.ALL :
                // 아군 및 적군 모두 적용가능한 버프 카드
                if(targetObject.objectType == ObjectType.ENEMY){
                    SetArrowNodesSprite(isEnter, buffArrowHeadEnemy,buffArrowHeadNormal, buffArrowNodeEnemy, buffArrowNodeNormal);
                }else{
                    SetArrowNodesSprite(isEnter, buffArrowHeadAlly, buffArrowHeadNormal, buffArrowNodeAlly, buffArrowNodeNormal);
                }
                SetBuffArrowCircleRotateLoop(isEnter);
                break;
            case ValidTarget.MEMBER:
                // 아군 단일 적용 버프 카드
                if(targetObject.objectType == ObjectType.PLAYER){
                    SetArrowNodesSprite(isEnter, buffArrowHeadAlly, buffArrowHeadNormal, buffArrowNodeAlly, buffArrowNodeNormal);
                    SetBuffArrowCircleRotateLoop(isEnter);
                }
                break;
            case ValidTarget.TEAM:
                // 아군 전체 적용 버프 카드
                if(targetObject.objectType == ObjectType.PLAYER){
                    SetArrowNodesSprite(isEnter, buffArrowHeadAlly, buffArrowHeadNormal, buffArrowNodeAlly, buffArrowNodeNormal);
                    SetBuffArrowCircleRotateLoop(isEnter);
                }
                break;
        }
    }

    // 화살표를 소환한 카드의 ValidTarget에 따라 화살표 머리와 몸통 스프라이트 이미지 구별 적용
    private void SetArrowNodesSprite(bool isEnter, Sprite activeHeadSprite, Sprite normalHeadSprite, Sprite activeNodeSprite, Sprite normalNodeSPrite)
    {
        for(int i=0; i<this.arrowNodes.Count; i++){
            SpriteRenderer spriteRenderer = arrowNodes[i].GetComponent<SpriteRenderer>();
            if(i == (arrowNodes.Count-1)){
                spriteRenderer.sprite = isEnter ? activeHeadSprite : normalHeadSprite; // 화살표 머리
            }else{
                spriteRenderer.sprite = isEnter ? activeNodeSprite : normalNodeSPrite; // 화살표 몸통
            }
        }
    }

    // 공격 화살표 확장 오브젝트 위치 조정 트위닝
    private void SetAttackArrowExpanderState(bool isEnter)
    {
        arrowHeadExpanderLeft.GetComponent<SpriteRenderer>().sprite = isEnter ? arrowExpandLeftLight : arrowExpandLeftNormal;
        arrowHeadExpanderRight.GetComponent<SpriteRenderer>().sprite = isEnter ? arrowExpandRightLight : arrowExpandRightNormal;
        arrowHeadExpanderLeft.transform.DOLocalMoveX(isEnter ? -0.3f : -0.21f, 0.2f);
        arrowHeadExpanderRight.transform.DOLocalMoveX(isEnter ? 0.3f : 0.21f, 0.2f);
    }

    // 버프 화살표 원형 오브젝트 회전 트위닝
    private void SetBuffArrowCircleRotateLoop(bool isEnter)
    {
        if(isEnter){
            buffArrowHeadCircle.transform
                .DORotate(new Vector3(0, 0, 360), 5f, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1);
        }else{
            buffArrowHeadCircle.transform.DOKill();
        }
    }
}
