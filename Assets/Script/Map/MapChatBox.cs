using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapChatBox : MonoBehaviour,  IPointerEnterHandler, IPointerExitHandler
{
    // 마우스 포인터가 채팅메시지 박스위에 진입 또는 벗어날때 MapUI 클래스의 isMouseOnChatBox 상태 변수값 변경

    public void OnPointerEnter(PointerEventData eventData)
    {
        MapUI.instance.isMouseOnChatBox = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MapUI.instance.isMouseOnChatBox = false;
    }
}
