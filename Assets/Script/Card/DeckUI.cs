using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class DeckUI : SingletonD<DeckUI>
{
    public GameObject DeckListPanel;

    public delegate void OnCardHoverForAction(int cardIndex);
    public event OnCardHoverForAction onCardHoverForAction;

    void Start()
    {
        onCardHoverForAction += OnCardHovered;
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

}
