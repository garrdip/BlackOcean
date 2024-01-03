using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SwapButtonOnRoom : MonoBehaviour,  IPointerEnterHandler, IPointerExitHandler
{
    public GameObject topBase;
    public GameObject topBaseLight;
    public GameObject topMy;
    public GameObject topMyLight;
    public GameObject topC;
    public GameObject topCLight;
    public GameObject topR;
    public GameObject topRLight;


    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        topBaseLight.SetActive(topBase.activeSelf);
        topMyLight.SetActive(topMy.activeSelf);
        topCLight.SetActive(topC.activeSelf);
        topRLight.SetActive(topR.activeSelf);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        topBaseLight.SetActive(false);
        topMyLight.SetActive(false);
        topCLight.SetActive(false);
        topRLight.SetActive(false);
    }
}
