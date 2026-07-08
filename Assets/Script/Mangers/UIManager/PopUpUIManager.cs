using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using DG.Tweening;


public class PopUpUIManager : SingletonD<PopUpUIManager>
{
    // prefareDeck PopUp Delegate
    public delegate void OnChangePrefareDeckPopUpShow();
    public OnChangePrefareDeckPopUpShow onChangePrefareDeckPopUpShow;
    public delegate void OnChangePrefareDeckPopUpHide();
    public OnChangePrefareDeckPopUpHide onChangePrefareDeckPopUpHide;
    
    
    // trashDeck PopUp Delegate
    public delegate void OnChangeTrashDeckPopUpShow();
    public OnChangeTrashDeckPopUpShow onChangeTrashDeckPopUpShow;
    public delegate void OnChangeTrashDeckPopUpHide();
    public OnChangeTrashDeckPopUpHide onChangeTrashDeckPopUpHide;
    
    
    // forgottenDeck PopUp Delegate
    public delegate void OnChangeForgottenDeckPopUpShow();
    public OnChangeForgottenDeckPopUpShow onChangeForgottenDeckPopUpShow;
    public delegate void OnChangeForgottenDeckPopUpHide();
    public OnChangeForgottenDeckPopUpHide onChangeForgottenDeckPopUpHide;

    
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
    public OnCardRemovePopUpShow onCardRemovePopUpShow;
    public delegate void OnCardRemovePopUpHide();
    public OnCardRemovePopUpHide onCardRemovePopUpHide;


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

    [Header("CardOnDeck 오브젝트의 마우스 오버 상태값")]
    public bool isMouseOnCardOnDeck = false;

    [Header("팝업 활성화 상태값")]
    public bool isPrefareDeckListPopUpOpen = false;
    public bool isTrashDeckListPopUpOpen = false;
    public bool isForgottenDeckListPopUpOpen = false;
    public bool isCardOnHandRemovePopUpOpen = false;
    public bool isBattleResultPopUpOpen = false;
    public bool isGameoverPopUpOpen = false;
    public bool isDeckDrawPopUpOpen = false;
    public bool isCardEnhancePopUpOpen = false;
    public bool isCardRemovePopUpOpen = false;
    public bool isMercuriusPopUpOpen = false;
    public bool isCampPopUpOpen = false;
    public bool isItemShopPopUpOpen = false;
    public bool isDeckSelectPopUpOpen = false;
    public bool isDeckMultipleSelectPopUpOpen = false;

    [Header("뽑을 덱 정보 팝업")]
    public List<PrefareDeckListPopUp> prefarDeckPopUps = new List<PrefareDeckListPopUp>();
    public GameObject prefarDeckPopUpParent;
    public GameObject prefareDeckPopUpPrefab;

    [Header("버린린 덱 정보 팝업")]
    public List<TrashDeckListPopUp> trashDeckPopUps = new List<TrashDeckListPopUp>();
    public GameObject trashDeckPopUpParent;
    public GameObject trashDeckPopUpPrefab;

    [Header("잊혀진 덱 정보 팝업")]
    public List<ForgottenDeckListPopUp> forgottenDeckPopUps = new List<ForgottenDeckListPopUp>();
    public GameObject forgottenDeckPopUpParent;
    public GameObject forgottenDeckPopUpPrefab;

    [Header("팝업 UI 오브젝트")]
    public List<GameObject> popUpList = new List<GameObject>();
    public GameObject cardOnHandRemovePopUp; // 패 제거 팝업
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

    #region 뽑을 덱 정보 팝업
        // PrefareDeck 정보 팝업 생성
        public PrefareDeckListPopUp CreatePrefareDeckListPopUp(uint netId)
        {
            GameObject prefareDeckPopUpObject = Instantiate(PopUpUIManager.instance.prefareDeckPopUpPrefab, Vector3.zero, Quaternion.identity, PopUpUIManager.instance.prefarDeckPopUpParent.transform);
            prefareDeckPopUpObject.transform.localPosition = Vector3.zero;
            prefareDeckPopUpObject.transform.localScale = Vector3.one;
            PrefareDeckListPopUp prefareDeckListPopUp = prefareDeckPopUpObject.GetComponent<PrefareDeckListPopUp>();
            prefareDeckListPopUp.netId = netId;
            PopUpUIManager.instance.prefarDeckPopUps.Add(prefareDeckListPopUp);
            PopUpUIManager.instance.popUpList.Add(prefareDeckPopUpObject);
            return prefareDeckListPopUp;
        }

        // PrefareDeck 정보 팝업 활성화
        public void HandleShowPrefareDeckListPopUp(uint netId)
        {
            isPrefareDeckListPopUpOpen = true;
            foreach(PrefareDeckListPopUp prefareDeckListPopUp in prefarDeckPopUps){
                prefareDeckListPopUp.gameObject.SetActive(prefareDeckListPopUp.netId == netId);
            }
            onChangePrefareDeckPopUpShow?.Invoke();
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_1"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }

        // PrefareDeck 정보 팝업 비활성화
        public void HandleHidePrefareDeckListPopUp()
        {
            isPrefareDeckListPopUpOpen = false;
            onChangePrefareDeckPopUpHide?.Invoke();
        }
    #endregion

    #region 버린 덱 정보 팝업
        // TrashDeck 정보 팝업 생성
        public TrashDeckListPopUp CreateTrashDeckListPopUp(uint netId)
        {
            GameObject trashDeckPopUpObject = Instantiate(PopUpUIManager.instance.trashDeckPopUpPrefab, Vector3.zero, Quaternion.identity, PopUpUIManager.instance.trashDeckPopUpParent.transform);
            trashDeckPopUpObject.transform.localPosition = Vector3.zero;
            trashDeckPopUpObject.transform.localScale = Vector3.one;
            TrashDeckListPopUp trashDeckListPopUp = trashDeckPopUpObject.GetComponent<TrashDeckListPopUp>();
            trashDeckListPopUp.netId = netId;
            PopUpUIManager.instance.trashDeckPopUps.Add(trashDeckListPopUp);
            PopUpUIManager.instance.popUpList.Add(trashDeckPopUpObject);
            return trashDeckListPopUp;
        }

        // TrashDeck 정보 팝업 활성화
        public void HandleShowTrashDeckListPopUp(uint netId)
        {
            isTrashDeckListPopUpOpen = true;
            foreach(TrashDeckListPopUp trashDeckListPopUp in trashDeckPopUps){
                trashDeckListPopUp.gameObject.SetActive(trashDeckListPopUp.netId == netId);
            }
            onChangeTrashDeckPopUpShow?.Invoke();
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_3"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }

        // TrashDeck 정보 팝업 비활성화
        public void HandleHideTrashDeckListPopUp()
        {
            isTrashDeckListPopUpOpen = false;
            onChangeTrashDeckPopUpHide?.Invoke();
        }
    #endregion

    #region 잊혀진 덱 정보 팝업
        // ForgottenDeck 정보 팝업 생성
        public ForgottenDeckListPopUp CreateForgottenDeckListPopUp(uint netId)
        {
            GameObject forgottenDeckPopUpObject = Instantiate(PopUpUIManager.instance.forgottenDeckPopUpPrefab, Vector3.zero, Quaternion.identity, PopUpUIManager.instance.forgottenDeckPopUpParent.transform);
            forgottenDeckPopUpObject.transform.localPosition = Vector3.zero;
            forgottenDeckPopUpObject.transform.localScale = Vector3.one;
            ForgottenDeckListPopUp forgottenDeckListPopUp = forgottenDeckPopUpObject.GetComponent<ForgottenDeckListPopUp>();
            forgottenDeckListPopUp.netId = netId;
            PopUpUIManager.instance.forgottenDeckPopUps.Add(forgottenDeckListPopUp);
            PopUpUIManager.instance.popUpList.Add(forgottenDeckPopUpObject);
            return forgottenDeckListPopUp;
        }

        // ForgottenDeck 정보 팝업 활성화
        public void HandShowForgottenDeckListPopUp(uint netId)
        {
            isForgottenDeckListPopUpOpen = true;
            foreach(ForgottenDeckListPopUp forgottenDeckListPopUp in forgottenDeckPopUps){
                forgottenDeckListPopUp.gameObject.SetActive(forgottenDeckListPopUp.netId == netId);
            }
            onChangeForgottenDeckPopUpShow?.Invoke();
            AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_2"));
            M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        }

        // ForgottenDeck 정보 팝업 비활성화
        public void HandleHideForgottenDeckListPopUp()
        {
            isForgottenDeckListPopUpOpen = false;
            onChangeForgottenDeckPopUpHide?.Invoke();
        }
    #endregion

    #region CardOnHand 제거 팝업 
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
    #endregion

    #region 전투보상 팝업
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
    #endregion

    #region 덱 드로우 팝업
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
    #endregion

    #region 카드 상점 팝업
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
    #endregion

    #region 카드 상점 카드 강화 팝업
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
    #endregion

    #region 카드 상점 카드 제거 팝업
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
    #endregion

    #region 전초기지지 팝업
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
    #endregion

    #region 아이템 상점 팝업
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
    #endregion

    #region 덱 선택 팝업
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
    #endregion

    #region 덱 멀티 선택 팝업
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
    #endregion

    #region 게임오버 팝업
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
    #endregion
}
