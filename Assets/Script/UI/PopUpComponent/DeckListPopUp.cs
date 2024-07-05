using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectD;
using DG.Tweening;
using Mirror;
using TMPro;

public class DeckListPopUp : SingletonD<DeckListPopUp>, IPointerClickHandler
{
    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GameObject scrollViewLayout;
    public GridLayoutGroup deckListPopUpGrid;
    public TextMeshProUGUI textTitle;
    public bool isMouseOnCardOnDeck = false;


    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeDeckListPopUpShow += OnChangeDeckListPopUpShow;
        PopUpUIManager.instance.onChangeDeckListPopUpHide += OnChangeDeckListPopUpHide;
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isMouseOnCardOnDeck){
            PopUpUIManager.instance.HandleHideDeckListPopUp();
        }
    }

    // Deck정보 리스트 요소 추가
    private void AddDeckList(SyncList<Card> cards, GridLayoutGroup gridLayoutGroup)
    {
        ClearDeckList();
        foreach(Card card in cards){
            GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(gridLayoutGroup.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<CardOnDeck>().card = card.CardDeepCopy(false);
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
                textTitle.text = Const.PREFARE_DECK;
                M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
                // 로컬 플레이어의 PrefareDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.prefareDeck, deckListPopUpGrid);
                }
                break;

            case DeckListType.TRASH_DECK:
                textTitle.text = Const.TRASH_DECK;
                M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
                // 로컬 플레이어의 TrashDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.trashDeck, deckListPopUpGrid);
                }
                break;
            case DeckListType.FORGOTTEN_DECK:
                textTitle.text = Const.FORGOTTEN_DECK;
                M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
                // 로컬 플레이어의 ForgottenDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.forgottenDeck, deckListPopUpGrid);
                }
                break;
        }
    }

    // 댁 리스트 팝업 비활성화 콜백
    public void OnChangeDeckListPopUpHide()
    {
        // 버튼들의 랜더 순서 인덱스값 초기상태로 변경
        if(PopUpUIManager.instance.cardOnHandRemovePopUp.activeSelf){
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHandOverPopUp");
        }else{
            M_CardManager.instance.ChangeCardOnHandSortingLayerByName("CardOnHand");
        }
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            ClearDeckList();
            gameObject.SetActive(false);
        });
    }
}
