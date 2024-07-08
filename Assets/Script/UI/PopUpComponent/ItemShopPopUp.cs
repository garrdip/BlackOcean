using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;


public class ItemShopPopUp : SingletonD<ItemShopPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GameObject frameLayout;
    public bool isMouseOnFrame = false;

    protected override void Awake()
    {
        PopUpUIManager.instance.onItemShopPopUpShow += OnItemShopPopUpOpen;
        PopUpUIManager.instance.onItemShopPopUpHide += OnItemShopPopUpHide;
        AddEventTriggers();  
    }
    
    private void AddEventTriggers()
    {
        // 각 프레임들의 부모오브젝트인 frameLayout에 이벤트 트리거 컴포넌트 추가
        EventTrigger eventTrigger = frameLayout.AddComponent<EventTrigger>();
        
        // PointerEnter 이벤트 추가
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnPointerEnterFramLayout((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerEnterEntry);

        // PointerExit 이벤트 추가
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnPointerExitFramLayout((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerExitEntry); 
    }

    public void OnPointerEnterFramLayout(PointerEventData eventData)
    {
        isMouseOnFrame = true;
    }

    public void OnPointerExitFramLayout(PointerEventData eventData)
    {
        isMouseOnFrame = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isMouseOnFrame){
            PopUpUIManager.instance.HandleItemShopPopUp(false);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // 아이템 상점 팝업 활성화 콜백
    public void OnItemShopPopUpOpen()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
    }

    // 아이템 상점 팝업 비활성화 콜백
    public void OnItemShopPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
        });
    }
}
