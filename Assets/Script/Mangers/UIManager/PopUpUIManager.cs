using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using DG.Tweening;


public class PopUpUIManager : SingletonD<PopUpUIManager>
{
    // DeckListPopUp Delegate
    public delegate void OnChangeDeckListPopUpShow(DeckListType type);
    public OnChangeDeckListPopUpShow onChangeDeckListPopUpShow;
    public delegate void OnChangeDeckListPopUpHide();
    public OnChangeDeckListPopUpHide onChangeDeckListPopUpHide;

    
    // CardOnHandRemovePopUp Delegate
    public delegate void OnChangeCardOnHandRemovePopUpShow();
    public OnChangeCardOnHandRemovePopUpShow onChangeCardOnHandRemovePopUpShow;
    public delegate void OnChangeCardOnHandRemovePopUpHide();
    public OnChangeCardOnHandRemovePopUpHide onChangeCardOnHandRemovePopUpHide;


    // BattleResultPopUp Delegate
    public delegate void OnChangeBattleResultPopUpShow();
    public OnChangeBattleResultPopUpShow onChangeBattleResultPopUpShow;
    public delegate void OnChangeBattleResultPopUpHide();
    public OnChangeBattleResultPopUpHide onChangeBattleResultPopUpHide;


    // DeckDrawPopUp Delegate
    public delegate void OnChangeDeckDrawPopUpShow();
    public OnChangeDeckDrawPopUpShow onChangeDeckDrawPopUpShow;
    public delegate void OnChangeDeckDrawPopUpHide();
    public OnChangeDeckDrawPopUpHide onChangeDeckDrawPopUpHide;


    // Mercurius PopUp Delegate
    public delegate void OnMercuriusPopUpShow();
    public OnMercuriusPopUpShow onMercuriusPopUpShow;
    public delegate void OnMercuriusPopUpHide();
    public OnMercuriusPopUpHide onMercuriusPopUpHide;

    
    // ItemShop PopUp Delegate
    public delegate void OnItemShopPopUpShow();
    public OnItemShopPopUpShow onItemShopPopUpShow;
    public delegate void OnItemShopPopUpHide();
    public OnItemShopPopUpHide onItemShopPopUpHide;

    
    // Camp PopUp Delegate
    public delegate void OnCampPopUpShow(CampAction campAction);
    public OnCampPopUpShow onCampPopUpShow;
    public delegate void OnCampPopUpHide();
    public OnCampPopUpHide onCampPopUpHide;


    // CardEnhance PopUp Delegate
    public delegate void OnCardEnhancePopUpShow();
    public OnCardEnhancePopUpShow onCardEnhancePopUpShow;
    public delegate void OnCardEnhancePopUpHide();
    public OnCardEnhancePopUpHide onCardEnhancePopUpHide;

    
    // CardRemove PopUp Delegate
    public delegate void OnCardRemovePopUpShow();
    public OnCardEnhancePopUpShow onCardRemovePopUpShow;
    public delegate void OnCardRemovePopUpHide();
    public OnCardEnhancePopUpHide onCardRemovePopUpHide;


    // DeckSelect PopUp Delegate
    public delegate void OnDeckSelectPopUpShow(DeckListType type, string cardNumber);
    public OnDeckSelectPopUpShow onDeckSelectPopUpShow;
    public delegate void OnDeckSelectPopUpHide();
    public OnDeckSelectPopUpHide onDeckSelectPopUpHide;

    // DeckMultipleSelect PopUp Delegate
    public delegate void OnDeckMultipleSelectPopUpShow();
    public OnDeckMultipleSelectPopUpShow onDeckMultipleSelectPopUpShow;
    public delegate void OnDeckMultipleSelectPopUpHide();
    public OnDeckMultipleSelectPopUpHide onDeckMultipleSelectPopUpHide;


    [Header("팝업 활성화 상태값")]
    public bool isDeckListPopUpOpen = false;
    public bool isDeckDrawPopUpOpen = false;
    public bool isCardOnHandRemovePopUpOpen = false;
    public bool isBattleResultPopUpOpen = false;
    public bool isMercuriusPopUpOpen = false;
    public bool isCampPopUpOpen = false;
    public bool isItemShopPopUpOpen = false;
    public bool isCardEnhancePopUpOpen = false;
    public bool isCardRemovePopUpOpen = false;
    public bool isGameoverPopUpOpen = false;
    public bool isDeckSelectPopUpOpen = false;
    public bool isDeckMultipleSelectPopUpOpen = false;

    [Header("팝업 UI 오브젝트")]
    public List<GameObject> popUpList = new List<GameObject>();
    public GameObject cardOnHandRemovePopUp; // 패 제거 팝업
    public GameObject deckListPopUp; // 덱 목록 팝업
    public GameObject battleResultPopUp; // 전투 보상 팝업
    public GameObject gameOverPopUp; // 게임 오버 팝업
    public GameObject deckDrawPopUp; // 덱 드로우 팝업
    public GameObject cardEnhancePopUp; // 카드 강화 팝업
    public GameObject cardRemovePopUp; // 카드 제거 팝업
    public GameObject mercuriusPopUp; // 카드 상점 팝업
    public GameObject campPopUp; // 전초 기지 팝업
    public GameObject itemShopPopUp; // 아이템 상점 팝업
    public GameObject deckSelectPopUp; // 뽑을덱, 버린덱, 잊혀진덱의 카드 선택용 팝업
    public GameObject deckMultipleSelectPopUp;


    [Header("패 제거 팝업 카드 위치설정용 슬롯 프리팹")]
    public GameObject RemoveCardSlotPrefab;

    [Header("추가 드로우 팝업 카드 위치설정용 슬롯 프리팹")]
    public GameObject AddtionDrawCardSlotPrefab;

    [Header("댁 리스트 팝업에 사용되는 카드 프리팹")]
    public GameObject CardOnDeckPrefab;

    [Header("보상 목록 아이템 프리팹")]
    public GameObject RewardListItemPrefab;

    [Header("선택한 보상카드 애니매이션에 사용되는 카드 프리팹")]
    public GameObject CardOnHandChoosedPrefab;

    public GameObject CardShopSlot;
    public GameObject CardShopPrice;


    private void Start()
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentComponents.Add(gameObject.name, gameObject); // DDOL 관리 컴포넌트에 등록
    }

    // PrefareDeck 정보 팝업 활성화(씬 버튼에 이벤트 등록)
    public void HandleShowPrefareDeckListPopUp()
    {
        isDeckListPopUpOpen = true;
        deckListPopUp.gameObject.SetActive(true);
        onChangeDeckListPopUpShow?.Invoke(DeckListType.PREFARE_DECK);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_1"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // TrashDeck 정보 팝업 활성화(씬 버튼에 이벤트 등록)
    public void HandleShowTrashDeckListPopUp()
    {
        isDeckListPopUpOpen = true;
        deckListPopUp.gameObject.SetActive(true);
        onChangeDeckListPopUpShow?.Invoke(DeckListType.TRASH_DECK);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_3"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // ForgottenDeck 정보 팝업 활성화(씬 버튼에 이벤트 등록)
    public void HandShowForgottenDeckListPopUp()
    {
        isDeckListPopUpOpen = true;
        deckListPopUp.gameObject.SetActive(true);
        onChangeDeckListPopUpShow?.Invoke(DeckListType.FORGOTTEN_DECK);
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_2"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 덱 정보 팝업 비활성화
    public void HandleHideDeckListPopUp()
    {
        isDeckListPopUpOpen = false;
        onChangeDeckListPopUpHide?.Invoke();
    }

    // CardOnHand 제거 팝업 활성화
    public void HandleShowCardOnHandRemovePopUp()
    {
        isCardOnHandRemovePopUpOpen = true;
        cardOnHandRemovePopUp.gameObject.SetActive(true);
        onChangeCardOnHandRemovePopUpShow?.Invoke();
    }

    // CardOnHand 제거 팝업 비활성화
    public void HandleHideCardOnHandRemovePopUp()
    {
        isCardOnHandRemovePopUpOpen = false;
        onChangeCardOnHandRemovePopUpHide?.Invoke();
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 전투보상 카드선택 팝업창 활성화
    public void HandleShowBattleResultPopUp()
    {
        isBattleResultPopUpOpen = true;
        battleResultPopUp.SetActive(true);
        onChangeBattleResultPopUpShow?.Invoke();
    }

    // 전투보상 카드선택 팝업창 비활성화
    public void HandleHideBattleResultPopUp()
    {
        isBattleResultPopUpOpen = false;
        onChangeBattleResultPopUpHide?.Invoke(); 
    }

    // 덱 드로우 팝업창 활성화
    public void HandleShowDeckDrawPopUp()
    {
        isDeckDrawPopUpOpen = true;
        deckDrawPopUp.SetActive(true);
        onChangeDeckDrawPopUpShow?.Invoke();
    }

    // 덱 드로우 팝업창 비활성화
    public void HandleHideDeckDrawPopUp()
    {
        isDeckDrawPopUpOpen = false;
        onChangeDeckDrawPopUpHide?.Invoke();
    }

    // 카드 상점 팝업 활성화/비활성화
    public void HandleMercuriusPopUp(bool isPopUp)
    {
        isMercuriusPopUpOpen = isPopUp;
        if(isPopUp){
            mercuriusPopUp.SetActive(true);
            onMercuriusPopUpShow?.Invoke();  
        }else{
            onMercuriusPopUpHide?.Invoke();
        }
    }

    // 전초기지 팝업 활성화
    public void HandleCampPopUpShow(CampAction campAction)
    {
        isCampPopUpOpen = true;
        campPopUp.SetActive(true);
        onCampPopUpShow?.Invoke(campAction);   
    }

    // 전초기지 팝업 비활성화
    public void HandleCampPopUpHide()
    {
        isCampPopUpOpen = false;
        onCampPopUpHide?.Invoke();
    }

    // 아이템 상점 팝업 활성화/비활성화
    public void HandleItemShopPopUp(bool isPopUp)
    {
        isItemShopPopUpOpen = isPopUp;
        if(isPopUp){
            itemShopPopUp.SetActive(true);
            onItemShopPopUpShow?.Invoke();  
        }else{
            onItemShopPopUpHide?.Invoke();
        }
    }

    // 카드 상점 카드 강화 팝업 활성화/비활성화
    public void HandleCardEnhancePopUp(bool isOpen)
    {
        isCardEnhancePopUpOpen = isOpen;
        if(isOpen){
            cardEnhancePopUp.SetActive(true);
            onCardEnhancePopUpShow?.Invoke();     
        }else{
            onCardEnhancePopUpHide?.Invoke(); 
        }
    }

    // 카드 상점 카드 제거 팝업 활성화/비활성화
    public void HandleCardRemovePopUp(bool isOpen)
    {
        isCardRemovePopUpOpen = isOpen;
        if(isOpen){
            cardRemovePopUp.SetActive(true);
            onCardRemovePopUpShow?.Invoke();     
        }else{
            onCardRemovePopUpHide?.Invoke(); 
        }
    }

    // 뽑을덱, 버린덱, 잊혀진덱의 카드 선택 팝업 활성화/비활성화
    public void HandleShowDeckSelectPopUp(DeckListType deckListType, string cardNumber)
    {
        isDeckSelectPopUpOpen = true;
        deckSelectPopUp.SetActive(true);
        onDeckSelectPopUpShow?.Invoke(deckListType, cardNumber);   
    }

    public void HandleHideDeckSelectPopUp()
    {
        isDeckSelectPopUpOpen = false;
        onDeckSelectPopUpHide?.Invoke(); 
    }

    // 한 화면에서 2개의 덱에서 카드 선택해야하는 팝업 활성화/비활성화 (ex. E44 - 공허를 만지는 자)
    public void HandleShowDeckMultipleSelectPopUp()
    {
        isDeckMultipleSelectPopUpOpen = true;
        deckMultipleSelectPopUp.SetActive(true);
        onDeckMultipleSelectPopUpShow?.Invoke();   
    }

    public void HandleHideDeckMultipleSelectPopUp()
    {
        isDeckMultipleSelectPopUpOpen = false;
        onDeckMultipleSelectPopUpHide?.Invoke();
    }

    // 게임오버 팝업 활성화
    public void HandleShowGameOverPopUp()
    {
        isGameoverPopUpOpen = true;
        gameOverPopUp.SetActive(true);
        gameOverPopUp.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
    }

    // 게임오버 팝업 비활성화
    public void HandleHideGameOverPopUp()
    {
        isGameoverPopUpOpen = false;
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        gameOverPopUp.GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f).OnComplete(() => {
            gameOverPopUp.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
            networkRoomManager.StopClient();
            M_SteamManager.LeaveLobby();
        });
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }
}
