using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MapInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject mapInfoBaseOverBase;
    public GameObject mapDangerLayout;
    public GameObject mapTurnLayout;
    public GameObject textArea;

    void OnDestroy()
    {
        mapInfoBaseOverBase.GetComponent<Image>().DOKill();
        mapDangerLayout.GetComponent<CanvasGroup>().DOKill();
        mapTurnLayout.GetComponent<CanvasGroup>().DOKill();
    }


    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        mapInfoBaseOverBase.SetActive(true);
        mapInfoBaseOverBase.GetComponent<Image>().DOFade(1.0f, 0.5f);
        mapDangerLayout.SetActive(true);
        mapDangerLayout.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        mapTurnLayout.SetActive(true);
        mapTurnLayout.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        textArea.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        mapInfoBaseOverBase.GetComponent<Image>().DOFade(0f, 0.5f);
        mapInfoBaseOverBase.SetActive(false);
        mapDangerLayout.GetComponent<CanvasGroup>().DOFade(0f, 0.5f);
        mapDangerLayout.SetActive(false);
        mapTurnLayout.GetComponent<CanvasGroup>().DOFade(0f, 0.5f);
        mapTurnLayout.SetActive(false);
        textArea.SetActive(false);
    }
}
