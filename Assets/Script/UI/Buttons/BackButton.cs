using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject backButtonArrow;
    public GameObject backButtonArrowLight;
    public GameObject backButtonTail;
    public GameObject backButtonTailLight;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        backButtonArrowLight.SetActive(true);
        backButtonTailLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        backButtonArrowLight.SetActive(false);
        backButtonTailLight.SetActive(false);
    }
}
