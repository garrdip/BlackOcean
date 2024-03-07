using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropdownItem : MonoBehaviour
{
    public GameObject dropdownItemLight;
    public GameObject dropdownItemLineLight;


    // -------- 이벤트 트리거에 할당되어있는 함수들 --------- //
    
    public void OnPointerEnterDropdownItem()
    {
        dropdownItemLight.SetActive(true);
        dropdownItemLineLight.SetActive(true);
    }

    public void OnPointerExitDropdownItem()
    {
        dropdownItemLight.SetActive(false);
        dropdownItemLineLight.SetActive(false);
    }
    
    // ----------------------------------------------------- //
}
