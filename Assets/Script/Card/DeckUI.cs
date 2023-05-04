using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using TMPro;
using Steamworks;

public class DeckUI : SingletonD<DeckUI>
{
    [Header("게임 오브젝트")]
    public GameObject DeckListPanel;
    public GameObject GameUI;
    public GameObject CardPocket;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;
    public GameObject DeckListPopUp;
    public GameObject CardOnDeckPrefab;
    public GameObject GameBackGround;

    [Header("UI 요소")]
    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public Button buttonReturnGame;
    public GridLayoutGroup gridLayoutGroup;
    public TextMeshProUGUI textPrefareDeckCount;
    public TextMeshProUGUI textTrashDeckCount;

    [Header("댁 리스트")]
    public List<GameObject> deckList;

    [Header("플레이어 리스트(플레이어 정보 및 턴 정보)")]
    public List<GameObject> playerOrderList;

    private int originSiblingIndex = 0;
    private bool isOpenPrefareDeckPopUp = false;
    private bool isOpenTrashDeckPopUp = false;

    void Start()
    {
        DeckListPopUp.gameObject.SetActive(false);
        buttonEndTurn.onClick.AddListener(HandleEndTurn);
        buttonPrefareDeck.onClick.AddListener(HandleShowPrefareDeck);
        buttonTrashDeck.onClick.AddListener(HandleShowTrashDeck);
        buttonReturnGame.onClick.AddListener(HandleReturnGame);
    }

    // 턴 넘김
    public void HandleEndTurn()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            if(M_TurnManager.instance.currentPlayer == NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>()){
                M_TurnManager.instance.SetNextTurn();
                M_TurnManager.instance.isMyTurn = false;
                RemoveAllCurrentPlayerDeck();
                RemoveAllCurrentPlayerArrow();
            }
        }
    }

    // 내 턴 종료시 손에있는 모든 카드 제거
    private void RemoveAllCurrentPlayerDeck()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    M_CardManager.instance.CardOnHandAllThrowAwaySequence(cardOnHand);
                }
            }
        }
    }

    // 내 턴 종료시 카드 제어 화살표 제거
    private void RemoveAllCurrentPlayerArrow()
    {
         if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                CardCtrlArrow[] cardCtrlArrows = FindObjectsOfType<CardCtrlArrow>();
                foreach(CardCtrlArrow cardCtrlArrow in cardCtrlArrows){
                    gamePlayerDeck.CmdDestroyArrowEmitter(cardCtrlArrow.gameObject);
                }
            }
        }
    }

    // PrefareDeck 정보 팝업
    private void HandleShowPrefareDeck()
    {
        isOpenPrefareDeckPopUp = !isOpenPrefareDeckPopUp; // 버튼 클릭으로도 팝업 열기 및 닫기
        originSiblingIndex = buttonPrefareDeck.transform.GetSiblingIndex(); // 원래의 오브젝트 스택 순서 인덱스값
        DeckListPopUp.gameObject.SetActive(isOpenPrefareDeckPopUp);
        if(isOpenPrefareDeckPopUp){
            // 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
            buttonPrefareDeck.transform.SetParent(DeckListPopUp.transform);
            buttonPrefareDeck.transform.SetAsLastSibling();
        }else{
            HandleReturnGame();
        }
        // 현재 턴인 플레이어의 PrefareDeck 데이터를 가지고 댁 목록 팝업창 세팅
        GamePlayerDeck gamePlayerDeck = M_TurnManager.instance.currentPlayer.GetComponent<GamePlayerDeck>();
        if(gamePlayerDeck != null){
            AddDeckList(gamePlayerDeck.prefareDeck);
        }
    }

    // TrashDeck 정보 팝업
    private void HandleShowTrashDeck()
    {
        isOpenTrashDeckPopUp = !isOpenTrashDeckPopUp; // 버튼 클릭으로도 팝업 열기 및 닫기
        originSiblingIndex = buttonTrashDeck.transform.GetSiblingIndex(); // 원래의 오브젝트 스택 순서 인덱스값
        DeckListPopUp.gameObject.SetActive(isOpenTrashDeckPopUp);
        if(isOpenTrashDeckPopUp){
            // 팝업창 열리면 버튼 오브젝트의 부모를 팝업으로 바꾼뒤 가장 마지막 순서로 추가하여 팝업창 위에 버튼 그려지도록 변경
            buttonTrashDeck.transform.SetParent(DeckListPopUp.transform);
            buttonTrashDeck.transform.SetAsLastSibling();
        }else{
            HandleReturnGame();
        }
        // 현재 턴인 플레이어의 TrashDeck 데이터를 가지고 댁 목록 팝업창 세팅
        GamePlayerDeck gamePlayerDeck = M_TurnManager.instance.currentPlayer.GetComponent<GamePlayerDeck>();
        if(gamePlayerDeck != null){
            AddDeckList(gamePlayerDeck.trashDeck);
        }
    }

    // 팝업 닫고 게임으로 돌아가기
    private void HandleReturnGame()
    {
        isOpenPrefareDeckPopUp = false;
        isOpenTrashDeckPopUp = false;
        buttonPrefareDeck.transform.SetSiblingIndex(originSiblingIndex);
        buttonTrashDeck.transform.SetSiblingIndex(originSiblingIndex);
        buttonPrefareDeck.transform.SetParent(PrefareDeck.transform);
        buttonTrashDeck.transform.SetParent(TrashDeck.transform);
        DeckListPopUp.gameObject.SetActive(false);
        ClearDeckList();
    }

    // Deck정보 리스트 요소 추가
    private void AddDeckList(SyncList<Card> cards)
    {
        ClearDeckList();
        foreach(Card card in cards){
            GameObject cardOnDeck = Instantiate(CardOnDeckPrefab);
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
}