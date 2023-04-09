using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using TMPro;

public class DeckUI : SingletonD<DeckUI>
{
    public GameObject DeckListPanel;
    public GameObject GameCanvas;
    public GameObject CardPocket;
    public GameObject PrefareDeck;
    public GameObject TrashDeck;

    public Button buttonEndTurn;
    public Button buttonPrefareDeck;
    public Button buttonTrashDeck;
    public TextMeshProUGUI textPrefareDeckCount;
    public TextMeshProUGUI textTrashDeckCount;


    void Start()
    {
        buttonEndTurn.onClick.AddListener(HandleEndTurn);
        buttonPrefareDeck.onClick.AddListener(HandleShowPrefareDeck);
        buttonTrashDeck.onClick.AddListener(HandleShowTrashDeck);
    }

    // 턴 넘김
    public void HandleEndTurn()
    {
        if(M_TurnManager.instance.currentPlayer == NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>()){
            M_TurnManager.instance.SetNextTurn();
            M_TurnManager.instance.isMyTurn = false;
            RemoveAllCurrentPlayerDeck();
            RemoveAllCurrentPlayerArrow();
        }
    }

    // 내 턴 종료시 손에있는 모든 카드 제거
    private void RemoveAllCurrentPlayerDeck()
    {
        if(NetworkClient.connection != null){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    cardOnHand.CardOnHandAllThrowAwaySequence(cardOnHand);
                }
            }
        }
    }

    // 내 턴 종료시 카드 제어 화살표 제거
    private void RemoveAllCurrentPlayerArrow()
    {
         if(NetworkClient.connection != null){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                CardCtrlArrow[] cardCtrlArrows = FindObjectsOfType<CardCtrlArrow>();
                foreach(CardCtrlArrow cardCtrlArrow in cardCtrlArrows){
                    gamePlayerDeck.CmdDestroyArrowEmitter(cardCtrlArrow.gameObject);
                }
            }
        }
    }

    private void HandleShowPrefareDeck()
    {
        
    }

    private void HandleShowTrashDeck()
    {

    }
}