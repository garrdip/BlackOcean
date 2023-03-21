using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class DeckUI : SingletonD<DeckUI>
{
    public GameObject DeckListPanel;
    public GameObject GameCanvas;

    public delegate void OnCardHoverForAction(int cardIndex);
    public event OnCardHoverForAction onCardHoverForAction;

    void Start()
    {
        // onCardHoverForAction += OnCardHovered;
        transform.localPosition = new Vector3(0f, -3.8f, 0f); // 부모 오브젝트 기준으로 Y축 -4.5위치
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