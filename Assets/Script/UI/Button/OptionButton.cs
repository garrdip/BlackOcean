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

    void Start()
    {
        OptionUIManager.instance.onChangeOptionPopUpShow += OnChangeOptionPopUpShow; // 옵션 팝업 활성화 상태 변경 이벤트 수신
    }

    private void OnChangeOptionPopUpShow(bool isActive)
    {
        if(isActive){
            optionIconRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 90f), 0.3f);
            optionIconLightRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 90f), 0.3f);
        }else{
            optionIconRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 0f), 0.3f);
            optionIconLightRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 0f), 0.3f);
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        OptionUIManager.instance.HandShowOptionPopUp(!OptionUIManager.instance.isOptionPopUpActive);
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
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
