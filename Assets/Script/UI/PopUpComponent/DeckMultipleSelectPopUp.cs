using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;



// 뽑을덱, 버린덱, 잊혀진덱 
public class DeckMultipleSelectPopUp : SingletonD<DeckMultipleSelectPopUp>
{
    public List<GameObject> forgottenDecks = new List<GameObject>();
    public List<GameObject> trashDecks = new List<GameObject>();
    public List<GameObject> selectCardObjectList = new List<GameObject>();
    public List<Card> selectCards = new List<Card>();


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
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        PopUpUIManager.instance.HandleHideDeckMultipleSelectPopUp();
    }

    public void ChangeCardOnDeckSelectState(GameObject selectCardObject, bool isSelect)
    {
       // TODO : 카드 선택된 상태로 보이는 뷰 처리
    }
    
    private void AddDeckList(SyncList<Card> deck, Transform grid)
    {
        foreach(Card card in deck){
            GameObject cardObject = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardObject.transform.SetParent(grid);
            cardObject.transform.localScale = Vector3.one;
            CardOnDeck cardOnDeck =cardObject.GetComponent<CardOnDeck>();
            cardOnDeck.card = card;
            forgottenDecks.Add(cardObject);
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
        GamePlayerDeck gamePlayerDeck = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.GetComponent<GamePlayerDeck>();
        ClearDeckList();
        AddDeckList(gamePlayerDeck.forgottenDeck, forgottenDeckGrid.transform);
        AddDeckList(gamePlayerDeck.trashDeck, trashDeckGrid.transform);
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
