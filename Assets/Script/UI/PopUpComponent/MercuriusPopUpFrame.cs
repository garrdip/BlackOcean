using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MercuriusPopUpFrame : MonoBehaviour,  IPointerEnterHandler, IPointerExitHandler
{
    public MercuriusPopUp mercuriusPop;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // mercuriusPop의 Frame오브젝트에 마우스 진입 시 isMouseOnFrame = true 로 변경하여 팝업 비활성화 방지
        mercuriusPop.isMouseOnFrame = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // mercuriusPop의 Frame오브젝트에 마우스 나갈 시 isMouseOnFrame = false 로 변경하여 팝업 비활성화 가능하도록 변경
        mercuriusPop.isMouseOnFrame = false;
    }
}
