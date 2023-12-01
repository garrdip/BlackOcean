using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MapTurnInfo : SingletonD<MapTurnInfo>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Vector3 originPosition;
    private bool isMouseExitComplete = false;


    void Start()
    {
        originPosition = GetComponent<RectTransform>().localPosition;
    }

    void OnDestroy()
    {
        GetComponent<RectTransform>().DOKill();
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if(isMouseExitComplete){
            GetComponent<RectTransform>().DOLocalMove(originPosition + new Vector3(-50f, 20f, 0f), 0.1f);
            GetComponent<RectTransform>().DOScale(new Vector3(1.4f, 1.4f, 1.4f), 0.1f);
        }
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        GetComponent<RectTransform>().DOLocalMove(originPosition, 0.1f);
        GetComponent<RectTransform>().DOScale(new Vector3(1f, 1f, 1f), 0.1f);
        StartCoroutine(MouseExitDelay());
    }

    IEnumerator MouseExitDelay()
    {
        isMouseExitComplete = false;
        yield return new WaitForSeconds(0.1f);
        isMouseExitComplete = true;
    }
}
