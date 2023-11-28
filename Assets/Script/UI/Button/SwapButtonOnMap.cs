using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwapButtonOnMap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public GameObject t_Base;
    public GameObject t_Base_Light;
    public GameObject t_M_Icon;
    public GameObject t_M_Icon_Light;
    public GameObject t_Chan_Icon;
    public GameObject t_Chan_Icon_Light;
    public GameObject t_Ready_Icon;
    public GameObject t_Ready_Icon_Light;

    void Start()
    {
        
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        t_Base_Light.SetActive(t_Base.activeSelf);
        t_M_Icon_Light.SetActive(t_M_Icon.activeSelf);
        t_Chan_Icon_Light.SetActive(t_Chan_Icon.activeSelf);
        t_Ready_Icon_Light.SetActive(t_Ready_Icon.activeSelf);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        t_Base_Light.SetActive(false);
        t_M_Icon_Light.SetActive(false);
        t_Chan_Icon_Light.SetActive(false);
        t_Ready_Icon_Light.SetActive(false);
    }
}
