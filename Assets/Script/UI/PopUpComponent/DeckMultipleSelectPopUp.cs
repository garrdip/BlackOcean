using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using ProjectD;


// 뽑을덱, 버린덱, 잊혀진덱 중 2개의 덱을 보여주는 팝업 (현재는 E44 카드를 위한 버린덱, 잊혀진덱의 데이터만 세팅됨)
public class DeckMultipleSelectPopUp : SingletonD<DeckMultipleSelectPopUp>
{
    [Header("잊혀진 덱 데이터")]
    public List<GameObject> forgottenDecks = new List<GameObject>();
    public List<Card> selectCardsFromForgottenDeck = new List<Card>();
    public CardOnDeck selectCardFromForgottenDeck;

    [Header("버린 덱 데이터")]
    public List<GameObject> trashDecks = new List<GameObject>();
    public List<Card> selectCardsFromTrashDeck = new List<Card>();
    public CardOnDeck selectCardFromTrashDeck;

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GameObject forgottenDeckScrollViewLayout;
    public GridLayoutGroup forgottenDeckGrid;
    public GameObject trashDeckScrollViewLayout;
    public GridLayoutGroup trashDeckGrid;
    public Button buttonSelectSubmit;

    protected override void Awake()
    {
        PopUpUIManager.instance.onDeckMultipleSelectPopUpShow += OnDeckMultipleSelectPopUpShow;
        PopUpUIManager.instance.onDeckMultipleSelectPopUpHide += OnDeckMultipleSelectPopUpHide;
    }

    void Start()
    {
        buttonSelectSubmit.onClick.AddListener(OnClickDeckSelectSubmit);
    }

    public void OnClickDeckSelectSubmit()
    {
        selectCardsFromForgottenDeck.Clear();
        selectCardsFromTrashDeck.Clear();
        selectCardsFromForgottenDeck.Add(selectCardFromForgottenDeck.card);
        selectCardsFromTrashDeck.Add(selectCardFromTrashDeck.card);
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.CmdSpawnCardOnHandExtractFromDeck(selectCardsFromForgottenDeck, DeckListType.FORGOTTEN_DECK); // 잊혀진 덱에서 선택하여 패로 생성
        gamePlayerDeck.CmdSpawnCardOnHandExtractFromDeck(selectCardsFromTrashDeck, DeckListType.TRASH_DECK); // 버린 덱에서 선택하여 패로 생성
        PopUpUIManager.instance.HandleHideDeckMultipleSelectPopUp();
    }

    public void ChangeCardOnDeckSelectState(GameObject selectCardObject)
    {
        CardOnDeck selectCardOnDeck = selectCardObject.GetComponent<CardOnDeck>();
        switch(selectCardOnDeck.deckListType){
            case DeckListType.PREFARE_DECK:
                
                break;
            case DeckListType.TRASH_DECK:
                foreach(GameObject cardObject in trashDecks){
                    CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
                    if(cardOnDeck.card.guid.Equals(selectCardOnDeck.card.guid)){
                        cardOnDeck.cardBackground.color = Color.red;
                    }else{
                        cardOnDeck.cardBackground.color = Color.white;
                    }
                }
                selectCardFromTrashDeck = selectCardOnDeck;
                break;
            case DeckListType.FORGOTTEN_DECK:
                foreach(GameObject cardObject in forgottenDecks){
                    CardOnDeck cardOnDeck = cardObject.GetComponent<CardOnDeck>();
                    if(cardOnDeck.card.guid.Equals(selectCardOnDeck.card.guid)){
                        cardOnDeck.cardBackground.color = Color.red;
                    }else{
                        cardOnDeck.cardBackground.color = Color.white;
                    }
                }
                selectCardFromForgottenDeck = selectCardOnDeck;
                break;
        }
    }
    
    private void AddDeckList(SyncList<Card> deckSynclist, List<GameObject> deckList, Transform grid, DeckListType deckListType)
    {
        foreach(Card card in deckSynclist){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardObject.transform.SetParent(grid);
            cardObject.transform.localScale = Vector3.one;
            CardOnDeck selectCardOnDeck = cardObject.GetComponent<CardOnDeck>();
            selectCardOnDeck.card = card;
            selectCardOnDeck.deckListType = deckListType;
            deckList.Add(cardObject);
        }
    }

    private void ClearDeckList()
    {
        for(int i=forgottenDecks.Count-1; i >=0; i--){
            Destroy(forgottenDecks[i]);
            forgottenDecks.RemoveAt(i);
        }
        selectCardsFromForgottenDeck.Clear();
        for(int i=trashDecks.Count-1; i >=0; i--){
            Destroy(trashDecks[i]);
            trashDecks.RemoveAt(i);
        }
        selectCardsFromTrashDeck.Clear();
    }

    public void OnDeckMultipleSelectPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        ClearDeckList();
        AddDeckList(gamePlayerDeck.forgottenDeck, forgottenDecks, forgottenDeckGrid.transform, DeckListType.FORGOTTEN_DECK);
        AddDeckList(gamePlayerDeck.trashDeck, trashDecks, trashDeckGrid.transform, DeckListType.TRASH_DECK);
    }

    public void OnDeckMultipleSelectPopUpHide()
    {
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.CmdResetMaxCardSelectableCount(); // 카드 선택 가능 갯수 초기화
        ClearDeckList();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}
