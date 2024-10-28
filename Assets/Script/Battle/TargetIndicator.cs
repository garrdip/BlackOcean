using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TargetIndicator : MonoBehaviour
{
    public uint netId; // 타겟오브젝트의 netId
    public GameObject targetOn;
    public GameObject targetOnLight;
    public GameObject targetMove;
    public GameObject targetMoveLight;


    void Start()
    {
        gameObject.SetActive(false);
    }

    // 타겟 인디케이터 활성화 상태
    public void OnTargetEnable()
    {
        gameObject.SetActive(true);
        targetOn.SetActive(true);
        targetOn.GetComponent<SpriteRenderer>().color = new Color(
            targetOn.GetComponent<SpriteRenderer>().color.r,
            targetOn.GetComponent<SpriteRenderer>().color.g,
            targetOn.GetComponent<SpriteRenderer>().color.b,
            1f
        );
        targetOnLight.SetActive(true);
        targetMove.SetActive(true);
        targetMoveLight.SetActive(true);
        targetMove.transform.DOScale(1.5f, 0.5f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
        targetMoveLight.transform.DOScale(1.5f, 0.5f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // 타겟 인디케이터 비활성화 상태
    public void OnTargetDisable(bool isCandidate)
    {
        gameObject.SetActive(true);
        targetOn.SetActive(isCandidate);
        targetOn.GetComponent<SpriteRenderer>().color = new Color(
            targetOn.GetComponent<SpriteRenderer>().color.r,
            targetOn.GetComponent<SpriteRenderer>().color.g,
            targetOn.GetComponent<SpriteRenderer>().color.b,
            isCandidate ? 0.5f : 1f 
        );
        targetOnLight.SetActive(false);
        targetMove.SetActive(false);
        targetMoveLight.SetActive(false);
        targetMove.transform.localScale = Vector3.one;
        targetMove.transform.DOKill();
        targetMoveLight.transform.localScale = Vector3.one;
        targetMoveLight.transform.DOKill();
    }

    // 타겟 인디케이터 후보군 상태
    public void OnTargetCandidated()
    {
        gameObject.SetActive(true);
        targetOn.SetActive(true);
        targetOn.GetComponent<SpriteRenderer>().color = new Color(
            targetOn.GetComponent<SpriteRenderer>().color.r,
            targetOn.GetComponent<SpriteRenderer>().color.g,
            targetOn.GetComponent<SpriteRenderer>().color.b,
            0.5f
        );
        targetOnLight.SetActive(false);
        targetMove.SetActive(false);
        targetMoveLight.SetActive(false);
    }
}
