using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;


public class OptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject optionButtonBaseLight;
    public GameObject optionIcon;
    public GameObject optionIconLight;
    public RectTransform optionIconRect;
    public RectTransform optionIconLightRect;
    public bool isButtonClick = false;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        optionButtonBaseLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        optionButtonBaseLight.SetActive(false);
    }
}
