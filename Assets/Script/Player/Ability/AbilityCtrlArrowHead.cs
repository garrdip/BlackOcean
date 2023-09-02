using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AbilityCtrlArrowHead : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeCardCtrlArrow))]
    public AbilityCtrlArrow abilityCtrlArrow; // 화살표 머리의 부모 오브젝트 클래스

    void Update()
    {
        CheckArrowHeadPosition();
    }

    // 화살표 머리가 타겟에 진입 시 CardCtrlArrow 클래스에 있는 currentTarget 변수에 충돌한 오브젝트 저장
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject") && abilityCtrlArrow != null){
            abilityCtrlArrow.currentTarget = collider.gameObject.transform.parent.GetComponent<TargetObject>();
        }
    }

    // 화살표 머리가 타겟에서 나갈 시 CardCtrlArrow 클래스에 있는 currentTarget 변수 null로 초기화
    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject") && abilityCtrlArrow != null){
            abilityCtrlArrow.currentTarget = null;
        }
    }

    // 화살표 머리의 위치를 체크하여 게임화면 아래부분보다 아래로 가는 경우 화살표 제거 (화살표 머리 부분이 마우스 포인터 위치를 따라 움직이므로 마우스 포인트 위치를 체크)
    private void CheckArrowHeadPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition.y < 0f)
        {
            if(NetworkClient.connection != null && NetworkClient.active && isOwned){
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                AbilityCtrlArrow abilityCtrlArrow = gamePlayerDeck.abilityCtrlArrow;
                abilityCtrlArrow.RemoveAbilityCtrlArrow();
            }
        }
    }

    // --------------------------------------------------------------SyncVar Hook ----------------------------------------------------------------------//

    // 화살표 머리의 부모오브젝트 설정
    public void OnChangeCardCtrlArrow(AbilityCtrlArrow oldAbilityCtrlArrow, AbilityCtrlArrow newAbilityCtrlAroow)
    {
        transform.SetParent(newAbilityCtrlAroow.transform);
        transform.localPosition = new Vector3(0f, 0f, 0f); // 동적으로 부모 설정시 localPosition이 변경되므로 부모와 같은 위치가 되도록 localPosition 0으로 설정
        newAbilityCtrlAroow.arrowNodes.Add(GetComponent<Transform>());
    }
}
