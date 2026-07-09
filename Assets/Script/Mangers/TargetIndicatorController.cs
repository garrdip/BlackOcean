using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// 타겟 인디케이터(카드/몬스터 액션 타겟 표시) 뷰 컨트롤러.
// M_TurnManager에서 분리된 순수 클라이언트 뷰 로직 — RPC/SyncVar 없음.
// M_TurnManager와 같은 GameObject에 부착되며, 프리팹/컨테이너 참조는 인스펙터에서 할당.
public class TargetIndicatorController : InstanceD<TargetIndicatorController>
{
    [Header("타겟 인디케이터")]
    public GameObject targetIndicatorContainer;
    public GameObject targetIndicatorPrefab; // 타겟 인디케이터 프리팹
    public List<GameObject> targetIndicators = new List<GameObject>(); // 카드 액션 및 몬스터의 액션 타겟 표시 오브젝트 리스트
    public List<GameObject> targetIndicatorCadidates = new List<GameObject>(); // 타겟 인디케이터 후보군 리스트

    // 슬롯 위치에 타겟 인디케이터 생성 (M_TurnManager의 SyncList 콜백에서 호출)
    public void CreateIndicator(uint netId, Vector3 position)
    {
        GameObject targetIndicatorObject = Instantiate(targetIndicatorPrefab, position + new Vector3(0f, 3f, 0f), Quaternion.identity, targetIndicatorContainer.transform);
        targetIndicatorObject.GetComponent<TargetIndicator>().netId = netId;
        targetIndicators.Add(targetIndicatorObject);
    }

    // 해당 플레이어의 캐릭터 오브젝트를 마우스 오버 및 클릭 할 수 있는 상태로 변경(isSelectable 플래그 변수의 상태값에 따라 작동)
    public void SetPlayerSelectable(bool isSelectable)
    {
        for(int i=0; i<M_TurnManager.instance.playerOrder.Count; i++){
            uint netId = M_TurnManager.instance.playerOrder[i];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.isSelectable = isSelectable;
            }
        }
    }

    // 화살표가 타겟오브젝트에 Enter/Exit 될 때 해당 타겟오브젝트와 카드의 ValidTarget에 따라 타겟 인디케이터 활성화 상태 변경
    public void EnableTargetIndiCatorByArrow(ValidTarget validTarget, bool isEnter, TargetObject targetObject = null)
    {
        switch(validTarget){
            case ValidTarget.NONE:
                // 플레이어 본인 타겟 활성화
                GamePlayer gamePlayer = PlayerRegistry.Local.currentGamePlayer;
                foreach(GameObject targetIndicatorObject in targetIndicators){
                    TargetIndicator targetIndicator = targetIndicatorObject.GetComponent<TargetIndicator>();
                    if(targetIndicator.netId == M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer).netId){
                        if(isEnter){
                            targetIndicator.OnTargetEnable();
                        }else{
                            targetIndicator.OnTargetDisable(true);
                        }
                    }
                }
                break;
            case ValidTarget.ENEMY:
                // 몬스터 중 해당 몬스터의 타겟 인디케이터 활성화
                foreach(GameObject targetIndicatorObject in targetIndicators){
                    TargetIndicator targetIndicator = targetIndicatorObject.GetComponent<TargetIndicator>();
                    if(targetObject != null && targetObject.objectType == ObjectType.ENEMY && targetIndicator.netId == targetObject.netId ){
                        if(isEnter){
                            targetIndicator.OnTargetEnable();
                        }else{
                            targetIndicator.OnTargetDisable(true);
                        }
                    }
                }
                break;
            case ValidTarget.ENEMY_ALL:
                // 모든 몬스터 타겟 인디케이터 활성화
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i != 0 && i != 1 && i != 2){
                        if(isEnter){
                            targetIndicators[i].GetComponent<TargetIndicator>().OnTargetEnable();
                        }else{
                            targetIndicators[i].GetComponent<TargetIndicator>().OnTargetDisable(true);
                        }
                    }
                }
                break;
            case ValidTarget.MEMBER:
                // 팀 플레이어 중 해당 플레이어 타겟 인디케이터 활성화
                foreach(GameObject targetIndicatorObject in targetIndicators){
                    TargetIndicator targetIndicator = targetIndicatorObject.GetComponent<TargetIndicator>();
                    if(targetObject != null && targetObject.objectType == ObjectType.PLAYER && targetIndicator.netId == targetObject.netId){
                        if(isEnter){
                            targetIndicator.OnTargetEnable();
                        }else{
                            targetIndicator.OnTargetDisable(true);
                        }
                    }
                }
                break;
            case ValidTarget.TEAM:
                // 팀 플레이어 모든 타겟 인디케이터 활성화
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i == 0 || i == 1 || i == 2){
                        if(isEnter){
                            targetIndicators[i].GetComponent<TargetIndicator>().OnTargetEnable();
                        }else{
                            targetIndicators[i].GetComponent<TargetIndicator>().OnTargetDisable(true);
                        }
                    }
                }
                break;
            case ValidTarget.ALL:
                // 팀이면 모든 팀 플레이어 or 몬스터면 모든 몬스터 타겟 인디케이터 활성화
                if(targetObject != null && targetObject.objectType == ObjectType.PLAYER){
                    for(int i=0; i<targetIndicators.Count; i++){
                        if(i == 0 || i == 1 || i == 2){
                            if(isEnter){
                                targetIndicators[i].GetComponent<TargetIndicator>().OnTargetEnable();
                            }else{
                                targetIndicators[i].GetComponent<TargetIndicator>().OnTargetDisable(true);
                            }
                        }
                    }
                }else{
                    for(int i=0; i<targetIndicators.Count; i++){
                        if(i != 0 && i != 1 && i != 2){
                            if(isEnter){
                                targetIndicators[i].GetComponent<TargetIndicator>().OnTargetEnable();
                            }else{
                                targetIndicators[i].GetComponent<TargetIndicator>().OnTargetDisable(true);
                            }
                        }
                    }
                }
                break;
        }
    }

    // 사용하려는 카드의 ValidTarget에 따라 타겟 인디케이터 후보군 상태로 설정(카드에 마우스 오버시 호출)
    public void CandidatedTargetIndicatorByCard(ValidTarget validTarget)
    {
        switch(validTarget){
            case ValidTarget.NONE:
                // 플레이어 본인 타겟 후보로 설정
                GamePlayer gamePlayer = PlayerRegistry.Local.currentGamePlayer;
                TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
                foreach(GameObject targetIndicatorObject in targetIndicators){
                    TargetIndicator targetIndicator = targetIndicatorObject.GetComponent<TargetIndicator>();
                    if(targetIndicator.netId == targetObject.netId){
                        targetIndicator.OnTargetCandidated();
                        targetIndicatorCadidates.Add(targetIndicator.gameObject);
                    }
                }
                break;
            case ValidTarget.ENEMY:
                // 모든 몬스터 타겟 후보로 설정
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i != 0 && i != 1 && i != 2){
                        targetIndicators[i].GetComponent<TargetIndicator>().OnTargetCandidated();
                        targetIndicatorCadidates.Add(targetIndicators[i]);
                    }
                }
                break;
            case ValidTarget.ENEMY_ALL:
                // 모든 몬스터 타겟 후보로 설정
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i != 0 && i != 1 && i != 2){
                        targetIndicators[i].GetComponent<TargetIndicator>().OnTargetCandidated();
                        targetIndicatorCadidates.Add(targetIndicators[i]);
                    }
                }
                break;
            case ValidTarget.MEMBER:
                // 팀 플레이어들 모두 타겟 후보로 설정
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i == 0 || i == 1 || i == 2){
                        targetIndicators[i].GetComponent<TargetIndicator>().OnTargetCandidated();
                        targetIndicatorCadidates.Add(targetIndicators[i]);
                    }
                }
                break;
            case ValidTarget.TEAM:
                // 팀 플레이어들 모두 타겟 후보로 설정
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i == 0 || i == 1 || i == 2){
                        targetIndicators[i].GetComponent<TargetIndicator>().OnTargetCandidated();
                        targetIndicatorCadidates.Add(targetIndicators[i]);
                    }
                }
                break;
            case ValidTarget.ALL:
                // 팀 플레이어, 몬스터 모두 타겟 후보로 설정
                foreach(GameObject targetIndicatorObject in targetIndicators){
                    TargetIndicator targetIndicator = targetIndicatorObject.GetComponent<TargetIndicator>();
                    targetIndicator.OnTargetCandidated();
                    targetIndicatorCadidates.Add(targetIndicator.gameObject);
                }
                break;
        }
    }

    // 몬스터의 ActionTarget에 따라 타겟 인디케이터 활성화 상태로 설정(몬스터에 마우스 오버시 호출)
    public void EnalbleTargetIndicatorByMonster(ActionTarget nextTarget, uint targetNetId)
    {
        switch(nextTarget){
            case ActionTarget.NONE:
                // 몬스터 본인 타겟 활성화
                foreach(GameObject targetIndicatorObject in targetIndicators){
                    TargetIndicator targetIndicator = targetIndicatorObject.GetComponent<TargetIndicator>();
                    if(targetIndicator.netId == targetNetId){
                        targetIndicator.OnTargetEnable();
                    }
                }
                break;
            case ActionTarget.FRONT:
                // 전열 플레이어 타겟 인디케이터 활성화
                targetIndicators[2].GetComponent<TargetIndicator>().OnTargetEnable();
                break;
            case ActionTarget.FRONT_MIDDLE:
                // 전열, 중열 플레이어 타겟 인디케이터 활성화
                targetIndicators[1].GetComponent<TargetIndicator>().OnTargetEnable();
                targetIndicators[2].GetComponent<TargetIndicator>().OnTargetEnable();
                break;
            case ActionTarget.FRONT_BACK:
                // 전열 후열 플레이어 타겟 인디케이터 활성화
                targetIndicators[0].GetComponent<TargetIndicator>().OnTargetEnable();
                targetIndicators[2].GetComponent<TargetIndicator>().OnTargetEnable();
                break;
            case ActionTarget.MIDDLE:
                // 중열 플레이어 타겟 인디케이터 활성화
                targetIndicators[1].GetComponent<TargetIndicator>().OnTargetEnable();
                break;
            case ActionTarget.MIDDLE_BACK:
                // 중열 후열 플레이어 타겟 인디케이터 활성화
                targetIndicators[0].GetComponent<TargetIndicator>().OnTargetEnable();
                targetIndicators[1].GetComponent<TargetIndicator>().OnTargetEnable();
                break;
            case ActionTarget.BACK:
                // 후열 플레이어 타겟 인디케이터 활성화
                targetIndicators[0].GetComponent<TargetIndicator>().OnTargetEnable();
                break;
            case ActionTarget.WHOLE:
                // 플레이어 전체 타겟 인디케이터 활성화
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i == 0 || i == 1 || i == 2){
                        targetIndicators[i].GetComponent<TargetIndicator>().OnTargetEnable();
                    }
                }
                break;
            case ActionTarget.WHOLE_ALLY:
                // 몬스터 전체 타겟 인디케이터 활성화
                for(int i=0; i<targetIndicators.Count; i++){
                    if(i != 0 && i != 1 && i != 2){
                        targetIndicators[i].GetComponent<TargetIndicator>().OnTargetEnable();
                    }
                }
                break;
        }
    }

    // 타겟 인디케이터 모두 비활성화
    public void DisableTargetIndicator()
    {
        foreach(GameObject targetIndicator in targetIndicators){
            targetIndicator.GetComponent<TargetIndicator>().OnTargetDisable(false);
        }
        targetIndicatorCadidates.Clear();
    }

    // 타겟 인디케이터 모두 제거
    public void ClearTargetIndicators()
    {
        for(int i=targetIndicators.Count-1; i>=0; i--){
            Destroy(targetIndicators[i]);
            targetIndicators.RemoveAt(i);
        }
    }

    // Synclist에서 오더 인덱스 변경 이벤트 수신하여 타겟 인디케이터에 할당된 netId값 갱신
    public void SetTargetIndicatorOrder(uint targetObjectNetId, int index)
    {
        if(targetIndicators.Count > 0){
            targetIndicators[index].GetComponent<TargetIndicator>().netId = targetObjectNetId;
        }
    }
}
