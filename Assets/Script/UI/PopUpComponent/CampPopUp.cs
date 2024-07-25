using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CampPopUp : SingletonD<CampPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GameObject frameLayout;
    public bool isMouseOnFrame = false;
    private TabLayout tabLayout;

    protected override void Awake()
    {
        tabLayout = GetComponent<TabLayout>();
        PopUpUIManager.instance.onCampPopUpShow += OnCampPopUpShow;
        PopUpUIManager.instance.onCampPopUpHide += OnCampPopUpHide;
        AddEventTriggers();   
    }

    void Start()
    {
        foreach(GameObject frame in tabLayout.tabFrames){
            Button buttonHealing = frame.transform.GetChild(0).GetComponent<Button>();
            buttonHealing.onClick.AddListener(() =>  HandleClickHealing());
            Button buttonGiveGold = frame.transform.GetChild(1).GetComponent<Button>();
            buttonGiveGold.onClick.AddListener(() =>  HandleClickGiveGold());
        }
    }

    private void HandleClickHealing()
    {
        GamePlayer gamePlayer = tabLayout.GetSelectedGamePlayerDeck().GetComponent<GamePlayer>();
        if(gamePlayer != null){
            gamePlayer.CmdHpRecovery();
        }
    }

    private void HandleClickGiveGold()
    {
        GamePlayer gamePlayer = tabLayout.GetSelectedGamePlayerDeck().GetComponent<GamePlayer>();
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
            PopUpUIManager.instance.HandleCampPopUp(false);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // 전초기지 팝업 활성화 콜백
    public void OnCampPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);

    }

    // 전초기지 팝업 비활성화 콜백
    public void OnCampPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
        });
    }
}
