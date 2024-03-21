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

    
    // DeckRemovePopUp Delegate
    public delegate void OnChangeDeckRemovePopUpShow();
    public OnChangeDeckRemovePopUpShow onChangeDeckRemovePopUpShow;
    public delegate void OnChangeDeckRemovePopUpHide();
    public OnChangeDeckRemovePopUpHide onChangeDeckRemovePopUpHide;

    // Mercurius PopUp Delegate
    public delegate void OnMercuriusPopUpShow();
    public OnMercuriusPopUpShow onMercuriusPopUpShow;
    public delegate void OnMercuriusPopUpHide();
    public OnMercuriusPopUpHide onMercuriusPopUpHide;


    [Header("팝업 UI 오브젝트")]
    public List<GameObject> popUpList = new List<GameObject>();
    public GameObject deckListPopUp; // 덱 목록 팝업
    public GameObject deckDrawPopUp; // 덱 드로우 팝업
    public GameObject deckRemovePopUp; // 덱 제거 팝업
    public GameObject cardOnHandRemovePopUp; // 패 제거 팝업
    public GameObject battleResultPopUp; // 전투 종료 후 카드 선택 팝업
    public GameObject layoutCardOnHandForRemove; // 카드 제거 팝업의 선택된 카드가 움직일 위치의 레이아웃
    public GameObject selectableCardList; // 전투 종료 보상 카드 목록 레이아웃 
    public GameObject mercuriusPopUp;
    public GameObject gameOverPopUp;

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


    // PrefareDeck 정보 팝업 활성화
    public void HandleShowPrefareDeckListPopUp()
    {
        deckListPopUp.gameObject.SetActive(true);
        if(onChangeDeckListPopUpShow != null){
            onChangeDeckListPopUpShow.Invoke(DeckListType.PREFARE_DECK);
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_1"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // TrashDeck 정보 팝업 활성화
    public void HandleShowTrashDeckListPopUp()
    {
        deckListPopUp.gameObject.SetActive(true);
        if(onChangeDeckListPopUpShow != null){
            onChangeDeckListPopUpShow.Invoke(DeckListType.TRASH_DECK);
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_3"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    public void HandShowForgottenDeckListPopUp()
    {
        deckListPopUp.gameObject.SetActive(true);
        if(onChangeDeckListPopUpShow != null){
            onChangeDeckListPopUpShow.Invoke(DeckListType.FORGOTTEN_DECK);
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("combat_card_deckbook_2"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 덱 정보 팝업 비활성화
    public void HandleHideDeckListPopUp()
    {
        if(onChangeDeckListPopUpHide != null){
            onChangeDeckListPopUpHide.Invoke();
        }
    }

    // CardOnHand 제거 팝업 활성화
    public void HandleShowCardOnHandRemovePopUp()
    {
        cardOnHandRemovePopUp.gameObject.SetActive(true);
        if(onChangeCardOnHandRemovePopUpShow != null){
            onChangeCardOnHandRemovePopUpShow.Invoke();
        }
    }

    // CardOnHand 제거 팝업 비활성화
    public void HandleHideCardOnHandRemovePopUp()
    {
        if(onChangeCardOnHandRemovePopUpHide != null){
            onChangeCardOnHandRemovePopUpHide.Invoke();
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 전투보상 카드선택 팝업창 활성화
    public void HandleShowBattleResultPopUp()
    {
        battleResultPopUp.SetActive(true);
        if(onChangeBattleResultPopUpShow != null){
            onChangeBattleResultPopUpShow.Invoke();
        }
    }

    // 전투보상 카드선택 팝업창 비활성화
    public void HandleHideBattleResultPopUp()
    {
        if(onChangeBattleResultPopUpHide != null){
            onChangeBattleResultPopUpHide.Invoke();
        } 
    }

    // 덱 드로우 팝업창 활성화
    public void HandleShowDeckDrawPopUp()
    {
        deckDrawPopUp.SetActive(true);
        if(onChangeDeckDrawPopUpShow != null){
            onChangeDeckDrawPopUpShow.Invoke();
        }
    }

    // 덱 드로우 팝업창 비활성화
    public void HandleHideDeckDrawPopUp()
    {
        if(onChangeDeckDrawPopUpHide != null){
            onChangeDeckDrawPopUpHide.Invoke();
        }
    }

    // 덱 제거 팝업창 활성화
    public void HandleShowDeckRemovePopUp(SyncList<Card> deck)
    {
        deckRemovePopUp.SetActive(true);
        if(onChangeDeckRemovePopUpShow != null){
            onChangeDeckRemovePopUpShow.Invoke();
        }
    }

     // 덱 제거 팝업창 비활성화
    public void HandleHideDeckRemovePopUp(SyncList<Card> deck)
    {
        if(onChangeDeckRemovePopUpHide != null){
            onChangeDeckRemovePopUpHide.Invoke();
        }
    }

    // Mercurius NPC 팝업
    public void HandleMercuriusPopUp(bool isPopUp)
    {
        if(isPopUp)
        {
            mercuriusPopUp.SetActive(true);
            if(onMercuriusPopUpShow != null)
                onMercuriusPopUpShow.Invoke();   
        }
        else
        {
            if(onMercuriusPopUpHide != null)
                onMercuriusPopUpHide.Invoke();
        }
    }

    // 게임오버 팝업 활성화
    public void HandleShowGameOverPopUp()
    {
        gameOverPopUp.SetActive(true);
        gameOverPopUp.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
    }

    // 게임오버 팝업 비활성화
    public void HandleHideGameOverPopUp()
    {
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
