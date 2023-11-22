using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReadyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject readyBaseL1;
    public GameObject readyBaseL2;
    public GameObject readyBaseL3;
    public GameObject readySBase;
    public GameObject readyS1;
    public GameObject readyS2;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        readyBaseL1.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        readyBaseL1.SetActive(false);
    }
}
