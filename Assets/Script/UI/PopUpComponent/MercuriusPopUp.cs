using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;

public class MercuriusPopUp : SingletonD<MercuriusPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GameObject frameLayout;
    public TextMeshProUGUI textCardEnhancePrice;
    public TextMeshProUGUI textCardRemovePrice;
    public bool isMouseOnFrame = false;
    public List<GridLayoutGroup> grids = new List<GridLayoutGroup>();
    public List<Button> tabButtons = new List<Button>();
    public List<GameObject> tabFrames = new List<GameObject>();


    protected override void Awake()
    {
        PopUpUIManager.instance.onMercuriusPopUpShow += OnMercuriusPopUpShow;
        PopUpUIManager.instance.onMercuriusPopUpHide += OnMercuriusPopUpHide;
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.onClientDisconnected += OnClientDisconnected;
        AddEventTriggers();   
    }

    void Start()
    {
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i; // C# 에서 람다식 클로저
            tabButtons[i].onClick.AddListener(() => ShowTab(buttonIndex));
        }
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // mercuriusPop의 PointerClick 이벤트
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isMouseOnFrame){
            PopUpUIManager.instance.HandleMercuriusPopUp(false);
        }
    }

    // mercuriusPop의 FrameLayout 오브젝트 PointerEnter 이벤트
    public void OnPointerEnterFramLayout(PointerEventData eventData)
    {
        isMouseOnFrame = true;
    }

    // mercuriusPop의 FrameLayout 오브젝트 PointerExit 이벤트
    public void OnPointerExitFramLayout(PointerEventData eventData)
    {
        isMouseOnFrame = false;
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

    // 클라이언트 연결 해제 이벤트 수신
    private void OnClientDisconnected(GamePlayer gamePlayer)
    {
        SetTabButtonByOwnedPlayersCount();
    }

    // 제어할 플레이어 오브젝트 숫자에 따라 활성화 시킬 탭버튼 갯수 설정
    private void SetTabButtonByOwnedPlayersCount()
    {
        HideAllTabButtons();
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        if(playerInterface.ownedPlayers.Count > 1){
            for(int i=0; i<playerInterface.ownedPlayers.Count; i++){
                GamePlayer gamePlayer = playerInterface.ownedPlayers[i];
                tabButtons[i].gameObject.SetActive(true); // 제어할 플레이어가 2명 이상이면 플레이어 수만큼 탭버튼 활성화
                tabButtons[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gamePlayer.character.ToString();
            }
        }   
    }

    // 선택한 탭 활성화
    public void ShowTab(int index)
    {
        tabFrames[index].SetActive(true);
        tabButtons[index].image.color = new Color32(255, 255, 255, 255);
        HideOtherTabs(index);
    }

    // 선택한 탭을 제외한 다른 탭 비활성화
    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].image.color = new Color32(255, 255, 255, 70);
                tabFrames[i].SetActive(false);
            }
        }
    }

    // 모든 탭버튼 비활성화
    public void HideAllTabButtons()
    {
        foreach(Button tabButton in tabButtons){
            tabButton.gameObject.SetActive(false);
        }
    }


    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // MercuriusPopUp 활성화 콜백
    public void OnMercuriusPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        GameUIManager.instance.GameUI.gameObject.SetActive(false);
        SetTabButtonByOwnedPlayersCount();
    }

    // MercuriusPopUp 비활성화 콜백
    public void OnMercuriusPopUpHide()
    {
        GameUIManager.instance.GameUI.gameObject.SetActive(true);
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
        });
    }
}
