using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class CardCtrlArrowHead : MonoBehaviour
{
    public CardCtrlArrow cardCtrlArrow; // 화살표 머리의 부모 오브젝트 클래스
    public Sprite targetEnterStateArrowHead; // 화살표가 타겟에 진입할 때 헤드 이미지
    public Sprite targetExitStateArrowHead; // 화살표가 타겟에 나갈 때 헤드 이미지
    public Sprite targetEnterStateArrowNode; // 화살표가 타겟에 진입할 때 노드 이미지
    public Sprite targetExitStateArrowNode; // 화살표가 타겟에 나갈 때 노드 이미지
    public GameObject arrowHeadExpanderLeft; // 화살표 헤드 확장 오브젝트 왼쪽 파츠
    public GameObject arrowHeadExpanderRight; // 화살표 헤드 확장 오브젝트 오른쪽 파츠
    public Sprite arrowExpandLeftNormal; // 화살표 헤드 왼쪽 기본 상태 이미지
    public Sprite arrowExpandRightNormal; // 화살표 헤드 오른쪽 기본 상태 이미지
    public Sprite arrowExpandLeftLight; // 화살표 헤드 왼쪽 확장 상태 이미지
    public Sprite arrowExpandRightLight; // 화살표 헤드 오른쪽 확장 상태 이미지

    void Update()
    {
        CheckArrowHeadPosition();
    }

    void OnDestroy()
    {
        arrowHeadExpanderLeft.transform.DOKill();
        arrowHeadExpanderRight.transform.DOKill();
    }

    // 화살표 머리가 타겟에 진입 시 CardCtrlArrow 클래스에 있는 currentTarget 변수에 충돌한 오브젝트 저장
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject") && cardCtrlArrow != null){
            cardCtrlArrow.currentTarget = collider.gameObject;
            ChangeArrowNodesColor(true);
            arrowHeadExpanderLeft.GetComponent<SpriteRenderer>().sprite = arrowExpandLeftLight;
            arrowHeadExpanderRight.GetComponent<SpriteRenderer>().sprite = arrowExpandRightLight;
            arrowHeadExpanderLeft.transform.DOLocalMoveX(-0.3f, 0.2f);
            arrowHeadExpanderRight.transform.DOLocalMoveX(0.3f, 0.2f);

        }
    }

    // 화살표 머리가 타겟에서 나갈 시 CardCtrlArrow 클래스에 있는 currentTarget 변수 null로 초기화
    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider != null && collider.tag.Equals("TargetObject") && cardCtrlArrow != null){
            cardCtrlArrow.currentTarget = null;
            ChangeArrowNodesColor(false);
            arrowHeadExpanderLeft.GetComponent<SpriteRenderer>().sprite = arrowExpandLeftNormal;
            arrowHeadExpanderRight.GetComponent<SpriteRenderer>().sprite = arrowExpandRightNormal;
            arrowHeadExpanderLeft.transform.DOLocalMoveX(-0.21f, 0.2f);
            arrowHeadExpanderRight.transform.DOLocalMoveX(0.21f, 0.2f);
        }
    }

    // 화살표 머리의 위치를 체크하여 게임화면 아래부분보다 아래로 가는 경우 화살표 제거 (화살표 머리 부분이 마우스 포인터 위치를 따라 움직이므로 마우스 포인트 위치를 체크)
    private void CheckArrowHeadPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        if(mousePosition.y < 0f){
            if(cardCtrlArrow != null && cardCtrlArrow.isOwned){
                cardCtrlArrow.RemoveCardCtrlArrow();
            }
        }
    }

    // 화살표 노드들 이미지를 타겟에 진입 or 벗어날 때 상태에 따라 다른 이미지 설정
    public void ChangeArrowNodesColor(bool isEnter)
    {
        if(cardCtrlArrow != null && cardCtrlArrow.arrowNodes.Count > 0){
            for(int i=0; i<cardCtrlArrow.arrowNodes.Count; i++){
                SpriteRenderer spriteRenderer = cardCtrlArrow.arrowNodes[i].GetComponent<SpriteRenderer>();
                if(i == cardCtrlArrow.arrowNodes.Count-1){
                    spriteRenderer.sprite = isEnter ? targetEnterStateArrowHead : targetExitStateArrowHead;
                }else{
                    spriteRenderer.sprite = isEnter ? targetEnterStateArrowNode : targetExitStateArrowNode;
                }
            }
        }
    }
}
