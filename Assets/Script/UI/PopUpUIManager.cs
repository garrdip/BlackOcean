using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;


public class PopUpUIManager : SingletonD<PopUpUIManager>
{
    // DeckListPopUp Delegate
    public delegate void OnChangeDeckListPopUpShow(DeckListType type);
    public OnChangeDeckListPopUpShow onChangeDeckListPopUpShow;
    public delegate void OnChangeDeckListPopUpHide(DeckListType type);
    public OnChangeDeckListPopUpHide onChangeDeckListPopUpHide;

    
    // CardOnHandRemovePopUp Delegate
    public delegate void OnChangeCardOnHandRemovePopUpShow();
    public OnChangeCardOnHandRemovePopUpShow onChangeCardOnHandRemovePopUpShow;
    public delegate void OnChangeCardOnHandRemovePopUpHide();
    public OnChangeCardOnHandRemovePopUpHide onChangeCardOnHandRemovePopUpHide;

    
    // BattleResultPopUp Delegate
    public delegate void OnChangeBattleResultPopUpShow(List<Card> rewardCards);
    public OnChangeBattleResultPopUpShow onChangeBattleResultPopUpShow;
    public delegate void OnChangeBattleResultPopUpHide();
    public OnChangeBattleResultPopUpHide onChangeBattleResultPopUpHide;

    
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
    public GameObject deckListPopUp; // 덱 목록 팝업
    public GameObject deckRemovePopUp; // 덱 제거 팝업
    public GameObject cardOnHandRemovePopUp; // 패 제거 팝업
    public GameObject battleResultPopUp; // 전투 종료 후 카드 선택 팝업
    public GameObject layoutCardOnHandForRemove; // 카드 제거 팝업의 선택된 카드가 움직일 위치의 레이아웃
    public GameObject selectableCardList; // 전투 종료 보상 카드 목록 레이아웃 
    public GameObject mercuriusPopUp;


    [Header("댁 리스트 팝업에 사용되는 카드 프리팹")]
    public GameObject CardOnDeckPrefab;

    [Header("선택한 보상카드 애니매이션에 사용되는 카드 프리팹")]
    public GameObject CardOnHandChoosedPrefab;

    public GameObject CardShopSlot;
    public GameObject CardShopPrice;


    [Header("팝업 제어 변수")]
    public bool isOpenPrefareDeckPopUp = false;
    public bool isOpenTrashDeckPopUp = false;



    // PrefareDeck 정보 팝업 활성화, 비활성화
    public void HandleShowPrefareDeckListPopUp()
    {
        isOpenPrefareDeckPopUp = !isOpenPrefareDeckPopUp;
        if(isOpenPrefareDeckPopUp){
            deckListPopUp.gameObject.SetActive(true);
            if(onChangeDeckListPopUpShow != null){
                onChangeDeckListPopUpShow.Invoke(DeckListType.PREFARE_DECK);
            }
        }else{
            if(onChangeDeckListPopUpHide != null){
                onChangeDeckListPopUpHide.Invoke(DeckListType.PREFARE_DECK);
            }
        }
    }

    // TrashDeck 정보 팝업 활성화, 비활성화
    public void HandleShowTrashDeckListPopUp()
    {
        isOpenTrashDeckPopUp = !isOpenTrashDeckPopUp;
        if(isOpenTrashDeckPopUp){
            deckListPopUp.gameObject.SetActive(true);
            if(onChangeDeckListPopUpShow != null){
                onChangeDeckListPopUpShow.Invoke(DeckListType.TRASH_DECK);
            }
        }else{
            if(onChangeDeckListPopUpHide != null){
                onChangeDeckListPopUpHide.Invoke(DeckListType.TRASH_DECK);
            }
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
    }

    // 전투보상 카드선택 팝업창 활성화
    public void HandleShowBattleResultPopUp(List<Card> rewardCards)
    {
        battleResultPopUp.SetActive(true);
        if(onChangeBattleResultPopUpShow != null){
            onChangeBattleResultPopUpShow.Invoke(rewardCards);
        }
    }

    // 전투보상 카드선택 팝업창 비활성화
    public void HandleHideBattleResultPopUp()
    {
        if(onChangeBattleResultPopUpHide != null){
            onChangeBattleResultPopUpHide.Invoke();
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
}
