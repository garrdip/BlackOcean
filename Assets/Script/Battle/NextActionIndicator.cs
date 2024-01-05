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

    private Sequence sequence;
    private Vector3 leftPointOriginPosition;
    private Vector3 rightPointOriginPosition;


    void Start()
    {
        leftPointOriginPosition = pointLeft.transform.localPosition;
        rightPointOriginPosition = pointRight.transform.localPosition;
    }

    void OnDestroy()
    {
        pointLeft.transform.DOKill();
        pointRight.transform.DOKill();
        sequence.Kill();
    }

    public void SetNextTargetAction(ActionType type, bool isTargetable, ActionTarget tar, string value)
    {
        //Action Type
        actionIcon.SetActive(true);
        nextActionBackground.SetActive(true);
        actionIcon.GetComponent<SpriteRenderer>().sprite = actionIcons[(int)type];
        actionValue.text = value.ToString();
        if(isTargetable)
        {
            actionTarget.SetActive(true);
            backTarget.SetActive(tar == ActionTarget.FRONT || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.WHOLE);
            middleTarget.SetActive(tar == ActionTarget.MIDDLE || tar == ActionTarget.MIDDLE_BACK || tar == ActionTarget.FRONT_MIDDLE || tar == ActionTarget.WHOLE);
            frontTarget.SetActive(tar == ActionTarget.BACK || tar == ActionTarget.FRONT_BACK || tar == ActionTarget.MIDDLE_BACK || tar == ActionTarget.WHOLE);
        }
        else
            actionTarget.SetActive(false);
    }

    void OnMouseEnter()
    {
        pointLeft.transform.DOLocalMoveX(leftPointOriginPosition.x - 0.15f, 0.3f);
        pointRight.transform.DOLocalMoveX(rightPointOriginPosition.x + 0.15f, 0.3f);
        pointLeftLight.SetActive(true);
        pointRightLight.SetActive(true);
        eInfo2.SetActive(true);
        sequence = DOTween.Sequence()
            .Append(eInfo2.transform.DOScale(0.8f, 0.5f))
            .Append(eInfo2.transform.DOScale(1f, 0.5f))
            .SetEase(Ease.Linear)
            .SetLoops(-1);
    }

    void OnMouseExit()
    {
        pointLeftLight.SetActive(false);
        pointRightLight.SetActive(false);
        pointLeft.transform.DOLocalMoveX(leftPointOriginPosition.x, 0.3f);
        pointRight.transform.DOLocalMoveX(rightPointOriginPosition.x, 0.3f);
        eInfo2.SetActive(false);
        eInfo2.transform.localScale = Vector3.one;
        sequence.Kill();
    }
}
