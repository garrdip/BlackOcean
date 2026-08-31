using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using TMPro;
using DG.Tweening;

public class NextActionIndicator : MonoBehaviour
{
    public List<Sprite> actionIcons;
    public GameObject actionIcon;
    public GameObject actionTarget;
    public TextMeshProUGUI actionValue;
    public GameObject nextActionBackground;
    public GameObject eInfo2;
    public GameObject frontTarget;
    public GameObject middleTarget;
    public GameObject backTarget;
    public GameObject pointLeft;
    public GameObject pointLeftLight;
    public GameObject pointRight;
    public GameObject pointRightLight;

    private Vector3 leftPointOriginPosition;
    private Vector3 rightPointOriginPosition;


    void Start()
    {
        leftPointOriginPosition = pointLeft.transform.localPosition;
        rightPointOriginPosition = pointRight.transform.localPosition;
    }

    void OnDestroy()
    {
        transform.DOKill();
        eInfo2.transform.DOKill();
        pointLeft.transform.DOKill();
        pointRight.transform.DOKill();
    }

    public void StartBounce(int index)
    {
        NextActionIndicatorBounce(index);
    }

    public void NextActionIndicatorBounce(int index)
    {
        if(transform != null){
            transform.DOMove(transform.position + new Vector3(0f, 0.15f, 0f), 1f)
                .SetDelay((4 - index) * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// 행동 예고 수치 표시 여부 — false(기획 2026-08-31): 플레이어가 피해량을 알 수 없어야 하므로 아이콘·대상만 보여주고 숫자는 숨긴다.
    /// 몬스터 스크립트들이 넘기는 value 문자열은 그대로 두고(호출부 수정 없음) 여기서만 무시한다. 디버그로 다시 보려면 true.
    /// </summary>
    public const bool ShowActionValue = false;

    public void SetNextTargetAction(ActionType type, bool isTargetable, ActionTarget tar, string value)
    {
        //Action Type
        actionIcon.SetActive(true);
        nextActionBackground.SetActive(true);
        actionIcon.GetComponent<SpriteRenderer>().sprite = actionIcons[(int)type];
        actionValue.text = ShowActionValue ? value.ToString() : "";
        actionValue.gameObject.SetActive(ShowActionValue);
        if(isTargetable)
        {
            actionTarget.SetActive(true);
            backTarget.SetActive(tar == ActionTarget.FRONT || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.WHOLE || tar == ActionTarget.WHOLE_ALLY);
            middleTarget.SetActive(tar == ActionTarget.MIDDLE || tar == ActionTarget.MIDDLE_BACK || tar == ActionTarget.FRONT_MIDDLE || tar == ActionTarget.WHOLE || tar == ActionTarget.WHOLE_ALLY);
            frontTarget.SetActive(tar == ActionTarget.BACK || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.MIDDLE_BACK || tar == ActionTarget.WHOLE || tar == ActionTarget.WHOLE_ALLY);
        }
        else
            actionTarget.SetActive(false);
    }

    void OnMouseEnter()
    {
        NextActionIndicatorFocusOn();
    }

    void OnMouseExit()
    {
        NextActionIndicatorFocusOff();
    }

    public void NextActionIndicatorFocusOn()
    {
        pointLeft.transform.DOLocalMoveX(leftPointOriginPosition.x - 0.15f, 0.3f);
        pointRight.transform.DOLocalMoveX(rightPointOriginPosition.x + 0.15f, 0.3f);
        pointLeftLight.SetActive(true);
        pointRightLight.SetActive(true);
        eInfo2.SetActive(true);
        eInfo2.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void NextActionIndicatorFocusOff()
    {
        pointLeftLight.SetActive(false);
        pointRightLight.SetActive(false);
        pointLeft.transform.DOLocalMoveX(leftPointOriginPosition.x, 0.3f);
        pointRight.transform.DOLocalMoveX(rightPointOriginPosition.x, 0.3f);
        eInfo2.SetActive(false);
        eInfo2.transform.localScale = Vector3.one;
        eInfo2.transform.DOKill();
    }
}
