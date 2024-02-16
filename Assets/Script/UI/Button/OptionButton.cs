using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;


public class OptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GameObject optionButtonBaseLight;
    public GameObject optionIcon;
    public GameObject optionIconLight;
    public RectTransform optionIconRect;
    public RectTransform optionIconLightRect;
    public bool isButtonClick = false;


    public void OnPointerClick(PointerEventData pointerEventData)
    {
        isButtonClick = !isButtonClick;
        if(isButtonClick){
            optionIconLight.gameObject.SetActive(true);
            optionIconRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 90f), 0.3f);
            optionIconLightRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 90f), 0.3f);
        }else{
            optionIconLight.gameObject.SetActive(false);
            optionIconRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 0f), 0.3f);
            optionIconLightRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 0f), 0.3f);
        }
        OptionUIManager.instance.optionPopUp.gameObject.SetActive(isButtonClick);
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        optionButtonBaseLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        optionButtonBaseLight.SetActive(false);
    }
}
