using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class DeckUI : SingletonD<DeckUI>
{
    public GameObject DeckListPanel;
    public GameObject GameCanvas;
    public Button buttonEndTurn;

    public delegate void OnCardHoverForAction(int cardIndex);
    public event OnCardHoverForAction onCardHoverForAction;

    void Start()
    {
        buttonEndTurn.onClick.AddListener(HandleEndTurn);
    }

    // 턴 넘김
    public void HandleEndTurn()
    {
        if(M_TurnManager.instance.currentPlayer == NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>()){
            M_TurnManager.instance.SetNextTurn();
            M_TurnManager.instance.isMyTurn = false;
            RemoveAllCurrentPlayerDeck();
        }
    }

    // 내 턴 종료시 손에있는 모든 카드 제거
    private void RemoveAllCurrentPlayerDeck()
    {
        if(NetworkClient.connection != null){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdDestroyAllCardOnHand();
            }
        }
    }
}