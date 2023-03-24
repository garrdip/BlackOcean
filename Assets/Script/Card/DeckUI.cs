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
        // onCardHoverForAction += OnCardHovered;
        transform.localPosition = new Vector3(0f, -4.5f, 0f);
        buttonEndTurn.onClick.AddListener(HandleEndTurn);
    }

    // 카드 Hover Delegate 송신
    public void EmitCardHoverAction(int cardIndex)
    {
        if(onCardHoverForAction != null){
            onCardHoverForAction.Invoke(cardIndex);
        }
    }

    // 카드 Hover Delegate 수신
    public void OnCardHovered(int cardIndex)
    {
        Debug.Log(cardIndex + "번째 카드위에 마우스 올려짐");
    }

    // 턴 넘김
    public void HandleEndTurn()
    {
        if(M_TurnManager.instance.currentPlayer == NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>()){
            M_TurnManager.instance.SetNextTurn();
            M_TurnManager.instance.isMyTurn = false;
        }
    }
}