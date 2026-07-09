using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using ProjectD;
using TMPro;


// 뽑을덱, 버린덱, 잊혀진덱 중 2개의 덱을 보여주는 팝업 (현재는 E44 카드를 위한 버린덱, 잊혀진덱의 데이터만 세팅됨)
public class DeckMultipleSelectPopUp : SingletonD<DeckMultipleSelectPopUp>
{
    [Header("잊혀진 덱 데이터")]
    public List<GameObject> forgottenDecks = new List<GameObject>();
    public CardOnDeck selectCardFromForgottenDeck;

    [Header("버린 덱 데이터")]
    public List<GameObject> trashDecks = new List<GameObject>();
    public CardOnDeck selectCardFromTrashDeck;

    [Header("UI 컴포넌트")]
    public CanvasGroup canvasGroup;
    public GridLayoutGroup forgottenDeckGrid;
    public GridLayoutGroup trashDeckGrid;
    public Button buttonSelectSubmit;
    public TextMeshProUGUI textExplanation;


    protected override void Awake()
    {
        PopUpUIManager.instance.onDeckMultipleSelectPopUpShow += OnDeckMultipleSelectPopUpShow;
        PopUpUIManager.instance.onDeckMultipleSelectPopUpHide += OnDeckMultipleSelectPopUpHide;
    }

    void Start()
    {
        textExplanation.text = $"잊혀진 덱에서 카드 <color=green>1</color> 장, 버린 덱에서 카드 <color=red>1</color> 장을 선택하세요.";
        buttonSelectSubmit.onClick.AddListener(OnClickDeckSelectSubmit);
    }

    public void OnClickDeckSelectSubmit()
    {
        GamePlayerDeck gamePlayerDeck = PlayerRegistry.Local.currentGamePlayer.GetComponent<GamePlayerDeck>();
        if(selectCardFromForgottenDeck != null){
            gamePlayerDeck.CmdCheckRequestCard(selectCardFromForgottenDeck.card, DeckListType.FORGOTTEN_DECK);
        }
        if(selectCardFromTrashDeck != null){
            gamePlayerDeck.CmdCheckRequestCard(selectCardFromTrashDeck.card, DeckListType.TRASH_DECK);
        }
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
        for(int i=trashDecks.Count-1; i >=0; i--){
            Destroy(trashDecks[i]);
            trashDecks.RemoveAt(i);
        }
    }

    public void OnDeckMultipleSelectPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        GamePlayerDeck gamePlayerDeck = PlayerRegistry.Local.currentGamePlayer.GetComponent<GamePlayerDeck>();
        ClearDeckList();
        AddDeckList(gamePlayerDeck.forgottenDeck, forgottenDecks, forgottenDeckGrid.transform, DeckListType.FORGOTTEN_DECK);
        AddDeckList(gamePlayerDeck.trashDeck, trashDecks, trashDeckGrid.transform, DeckListType.TRASH_DECK);
    }

    public void OnDeckMultipleSelectPopUpHide()
    {
        GamePlayerDeck gamePlayerDeck = PlayerRegistry.Local.currentGamePlayer.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.CmdResetMaxCardSelectableCount(); // 카드 선택 가능 갯수 초기화
        ClearDeckList();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}
