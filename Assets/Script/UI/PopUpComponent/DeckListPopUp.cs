using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using DG.Tweening;
using Mirror;

public class DeckListPopUp : SingletonD<DeckListPopUp>
{
    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GridLayoutGroup deckListPopUpGrid;

    [Header("UI 컴포넌트 요소의 정렬 순서값")]
    private int originSiblingIndex = 0;


    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeDeckListPopUpShow += OnChangeDeckListPopUpShow;
        PopUpUIManager.instance.onChangeDeckListPopUpHide += OnChangeDeckListPopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // Deck정보 리스트 요소 추가
    private void AddDeckList(SyncList<Card> cards, GridLayoutGroup gridLayoutGroup)
    {
        ClearDeckList();
        foreach(Card card in cards){
            GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;
            deckList.Add(cardOnDeck);
        }
    }

    // Deck정보 리스트 요소 제거
    private void ClearDeckList()
    {
        for(int i=deckList.Count-1; i >=0; i--){
            Destroy(deckList[i]);
            deckList.RemoveAt(i);
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //
    
    // 댁 리스트 팝업 활성화 콜백. 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
    public void OnChangeDeckListPopUpShow(DeckListType type)
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        switch(type){
            case DeckListType.PREFARE_DECK:
                GameUIManager.instance.buttonPrefareDeck.transform.SetParent(transform);
                GameUIManager.instance.buttonPrefareDeck.transform.SetAsLastSibling();
                M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
                originSiblingIndex = GameUIManager.instance.buttonPrefareDeck.transform.GetSiblingIndex();
                // 로컬 플레이어의 PrefareDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.prefareDeck, deckListPopUpGrid);
                }
                break;

            case DeckListType.TRASH_DECK:
                GameUIManager.instance.buttonTrashDeck.transform.SetParent(transform);
                GameUIManager.instance.buttonTrashDeck.transform.SetAsLastSibling();
                M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
                originSiblingIndex = GameUIManager.instance.buttonTrashDeck.transform.GetSiblingIndex();
                // 로컬 플레이어의 TrashDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.trashDeck, deckListPopUpGrid);
                }
                break;
        }
    }

    // 댁 리스트 팝업 비활성화 콜백
    public void OnChangeDeckListPopUpHide(DeckListType type)
    {
        // 버튼들의 랜더 순서 인덱스값 초기상태로 변경
        if(PopUpUIManager.instance.cardOnHandRemovePopUp.activeSelf){
            GameUIManager.instance.buttonPrefareDeck.transform.SetParent(PopUpUIManager.instance.cardOnHandRemovePopUp.transform);
            GameUIManager.instance.buttonTrashDeck.transform.SetParent(PopUpUIManager.instance.cardOnHandRemovePopUp.transform);
            GameUIManager.instance.buttonPrefareDeck.transform.SetSiblingIndex(originSiblingIndex);
            GameUIManager.instance.buttonTrashDeck.transform.SetSiblingIndex(originSiblingIndex);
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            GameUIManager.instance.buttonPrefareDeck.transform.SetParent(GameUIManager.instance.PrefareDeck.transform);
            GameUIManager.instance.buttonTrashDeck.transform.SetParent(GameUIManager.instance.TrashDeck.transform);
            GameUIManager.instance.buttonPrefareDeck.transform.SetSiblingIndex(originSiblingIndex);
            GameUIManager.instance.buttonTrashDeck.transform.SetSiblingIndex(originSiblingIndex);
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        }
        PopUpUIManager.instance.isOpenPrefareDeckPopUp = false;
        PopUpUIManager.instance.isOpenTrashDeckPopUp = false;
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            ClearDeckList();
            gameObject.SetActive(false);
        });
    }
}
