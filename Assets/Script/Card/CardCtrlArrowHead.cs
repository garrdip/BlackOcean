using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardCtrlArrowHead : NetworkBehaviour
{
    void Update()
    {
        CheckArrowHeadPosition();
    }

    // 화살표 인디케이터 헤드가 TargetObject로 Enter 감지해서 붉은색으로 변경
    private void OnTriggerEnter(Collider collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject")){
            // TODO : 화살표가 타겟으로 잡는 오브젝트에 타겟팅 효과 표시
        }
    }

    // 화살표 인디케이터 헤드가 TargetObject로 Exit 감지해서 흰색으로 변경
    private void OnTriggerExit(Collider collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject")){
            // TODO : 화살표가 타겟으로 잡았던 오브젝트의 타겟팅 효과 제거
        }
    }

    // 화살표 머리의 위치를 체크하여 게임화면 아래부분보다 아래로 가는 경우 화살표 제거 (화살표 머리 부분이 마우스 포인터 위치를 따라 움직이므로 마우스 포인트 위치를 체크)
    private void CheckArrowHeadPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition.y < 0f)
        {
            if(NetworkClient.connection != null){
                GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                CardCtrlArrow cardCtrlArrow = gamePlayerDeck.cardCtrlArrow;
                cardCtrlArrow.RemoveCardCtrlArrow();
            }
        }
    }
}
