using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatBoxVisibilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject buttonIconLight;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        buttonIconLight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        buttonIconLight.SetActive(false);
    }
}
