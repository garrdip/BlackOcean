using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SliderExt : MonoBehaviour
{
    public GameObject HandleIconLight;
    public GameObject checkmarkBGLight;


    // -------- 이벤트 트리거에 할당되어있는 함수들 --------- //
    
    public void OnPointerEnterSliderHandle()
    {
        HandleIconLight.SetActive(true);
    }

    public void OnPointerExitSliderHandle()
    {
        HandleIconLight.SetActive(false);
    }

    public void OnPointerEnterToggle()
    {

        checkmarkBGLight.SetActive(true);
    }

    public void OnPointerExitToggle()
    {
       checkmarkBGLight.SetActive(false);
    }
    
    // ----------------------------------------------------- //
}
