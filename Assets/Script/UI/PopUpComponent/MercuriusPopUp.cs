using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using ProjectD;

public class MercuriusPopUp : SingletonD<MercuriusPopUp>, IPointerClickHandler
{
    public CanvasGroup canvasGroup;
    public GameObject frameLayout;
    public bool isMouseOnFrame = false;
    public List<GridLayoutGroup> grids = new List<GridLayoutGroup>();
    public List<Button> tabButtons = new List<Button>();
    public List<Button> tabCardEnhanceButtons = new List<Button>();
    public List<Vector2> tabCardEnhanceButtonPositions = new List<Vector2>();
    public List<Button> tabCardRemoveButtons = new List<Button>();
    public List<Vector2> tabCardRemoveButtonPositions = new List<Vector2>();
    public List<GameObject> tabFrames = new List<GameObject>();
    public Button buttonClose;
    public GameObject buttonCloseLight;
    public int currentIndex = 0;
    public CanvasGroup cardInfoCanvasGroup;
    public CardOnDeck hoveredCardOnDeck;
    public Sprite georkIcon;
    public Sprite danhyangIcon;
    public Sprite erisIcon;



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
        buttonClose.onClick.AddListener(() => PopUpUIManager.instance.HandleMercuriusPopUp(false));
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i; // C# 에서 람다식 클로저
            tabButtons[i].onClick.AddListener(() => ShowTab(buttonIndex));
        }
        foreach(Button enhanceButton in tabCardEnhanceButtons){
            tabCardEnhanceButtonPositions.Add(enhanceButton.transform.GetChild(0).transform.localPosition);
        }
        foreach(Button removeButton in tabCardRemoveButtons){
            tabCardRemoveButtonPositions.Add(removeButton.transform.GetChild(0).transform.localPosition);
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
                switch(gamePlayer.character)
                {
                    case Character.GEORK:
                        tabButtons[i].transform.GetChild(2).GetComponent<Image>().sprite = georkIcon;
                        break;
                    case Character.HONGDANHYANG:
                        tabButtons[i].transform.GetChild(2).GetComponent<Image>().sprite = danhyangIcon;
                        break;
                    case Character.ERIS:
                        tabButtons[i].transform.GetChild(2).GetComponent<Image>().sprite = erisIcon;
                        break;
                }
            }
        }   
    }

    // 선택한 탭 활성화
    public void ShowTab(int index)
    {
        currentIndex = index;
        tabFrames[index].SetActive(true);
        tabButtons[index].GetComponent<CanvasGroup>().alpha = 1f;
        HideOtherTabs(index);
    }

    // 선택한 탭을 제외한 다른 탭 비활성화
    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].GetComponent<CanvasGroup>().alpha = 0.5f;
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

    // 마우스 오버된 상점카드 정보 활성화
    public void ShowHoverdCardInfo(Card card)
    {
        Card hoverdCard = new Card(card.baseCard);
        hoveredCardOnDeck.card = hoverdCard;
        hoveredCardOnDeck.initCardData(hoverdCard);
        hoveredCardOnDeck.InitCardTemplateByCharacter(hoverdCard);
        cardInfoCanvasGroup.DOFade(1f, 0.3f);  
    }

    // 마우스 오버된 상점카드 정보 비활성화
    public void HideHoverdCardInfo()
    {
        hoveredCardOnDeck.card = null;
        cardInfoCanvasGroup.DOFade(0f, 0.3f);  
    }


    // -------------------------------------------------------------------  이벤트 트리거 함수 -------------------------------------------------------------------------- //

    public void OnPointerEnterCardEnhanceButton()
    {
        tabCardEnhanceButtons[currentIndex].transform.GetChild(0).DOLocalMoveX(
            tabCardEnhanceButtonPositions[currentIndex].x + 25f, 0.3f
        );
        tabCardEnhanceButtons[currentIndex].transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
    }

    public void OnPointerExitCardEnhanceButton()
    {
        tabCardEnhanceButtons[currentIndex].transform.GetChild(0).DOLocalMoveX(
            tabCardEnhanceButtonPositions[currentIndex].x, 0.3f
        );
        tabCardEnhanceButtons[currentIndex].transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
    }

    public void OnPointerEnterCardRemoveButton()
    {
        tabCardRemoveButtons[currentIndex].transform.GetChild(0).DOLocalMoveX(
            tabCardRemoveButtonPositions[currentIndex].x - 25f, 0.3f
        );
        tabCardRemoveButtons[currentIndex].transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
    }

    public void OnPointerExitCardRemoveButton()
    {
        tabCardRemoveButtons[currentIndex].transform.GetChild(0).DOLocalMoveX(
            tabCardRemoveButtonPositions[currentIndex].x, 0.3f
        );
        tabCardRemoveButtons[currentIndex].transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
    }

    public void OnPointerEnterCloseButton()
    {
        buttonCloseLight.SetActive(true);
    }

    public void OnPointerExitCloseButton()
    {
        buttonCloseLight.SetActive(false);
    }


    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // MercuriusPopUp 활성화 콜백
    public void OnMercuriusPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        SetTabButtonByOwnedPlayersCount();
    }

    // MercuriusPopUp 비활성화 콜백
    public void OnMercuriusPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
            isMouseOnFrame = false;
        });
    }
}
