using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectD;
using DG.Tweening;
using Mirror;
using TMPro;


public class DeckSelectPopUp : SingletonD<DeckSelectPopUp>
{
    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GameObject scrollViewLayout;
    public GridLayoutGroup deckListPopUpGrid;
    public TextMeshProUGUI textTitle;

    
    protected override void Awake()
    {
        PopUpUIManager.instance.onDeckSelectPopUpShow += OnDeckSelectPopUpShow;
        PopUpUIManager.instance.onDeckSelectPopUpHide += OnDeckSelectPopUpHide;
    }

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

    private void ClearDeckList()
    {
        for(int i=deckList.Count-1; i >=0; i--){
            Destroy(deckList[i]);
            deckList.RemoveAt(i);
        }
    }
    
    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    public void OnDeckSelectPopUpShow(DeckListType type)
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        switch(type){
            case DeckListType.PREFARE_DECK:
                textTitle.text = Const.PREFARE_DECK;
                // 로컬 플레이어의 PrefareDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.prefareDeck, deckListPopUpGrid);
                }
                break;

            case DeckListType.TRASH_DECK:
                textTitle.text = Const.TRASH_DECK;
                // 로컬 플레이어의 TrashDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.trashDeck, deckListPopUpGrid);
                }
                break;
            case DeckListType.FORGOTTEN_DECK:
                textTitle.text = Const.FORGOTTEN_DECK;
                // 로컬 플레이어의 ForgottenDeck 조회
                if(NetworkClient.connection != null && NetworkClient.active){
                    GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
                    AddDeckList(gamePlayerDeck.forgottenDeck, deckListPopUpGrid);
                }
                break;
        }
    }

    // 댁 리스트 팝업 비활성화 콜백
    public void OnDeckSelectPopUpHide()
    {
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            ClearDeckList();
            gameObject.SetActive(false);
        });
    }
}
