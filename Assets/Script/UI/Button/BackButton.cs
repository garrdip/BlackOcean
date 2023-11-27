using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class BackButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject backButtonArrow;
    public GameObject backButtonArrowLight;
    public GameObject backButtonTail;
    public GameObject backButtonTailLight;

    private float moveTime;
    private float movePositionX;

    void Awake()
    {
        moveTime = 0.3f;
        movePositionX = -29f;
    }

    void OnDestroy()
    {
        backButtonTail.GetComponent<RectTransform>().DOKill();
        backButtonTailLight.GetComponent<RectTransform>().DOKill();
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        backButtonArrowLight.SetActive(true);
        backButtonTailLight.SetActive(true);
        backButtonTail.GetComponent<RectTransform>().DOLocalMoveX(movePositionX, moveTime);
        backButtonTailLight.GetComponent<RectTransform>().DOLocalMoveX(movePositionX, moveTime);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        backButtonArrowLight.SetActive(false);
        backButtonTailLight.SetActive(false);
        backButtonTail.GetComponent<RectTransform>().DOLocalMoveX(0f, moveTime);
        backButtonTailLight.GetComponent<RectTransform>().DOLocalMoveX(0f, moveTime);
    }
}
