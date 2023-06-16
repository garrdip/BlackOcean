using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BattleResultPopUp : MonoBehaviour
{
    [Header("랜덤으로 추출한 카드 리스트")]
    public List<Card> extractCards = new List<Card>();

    [Header("랜덤으로 추출한 카드 오브젝트 리스트")]
    public List<GameObject> extractCardObjects = new List<GameObject>();

 
    void OnEnable()
    {
        List<Card> randomCards = M_CardManager.instance.ExtractRandomCards(3);
        foreach(Card card in randomCards){
            extractCards.Add(card);
            GameObject cardOnDeck = Instantiate(PopUpUI.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(PopUpUI.instance.SelectableCardLIst.transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 500);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;
            extractCardObjects.Add(cardOnDeck);
        }
    }

    void OnDisable()
    {
        for(int i=extractCardObjects.Count-1; i >=0; i--){
            Destroy(extractCardObjects[i]);
        }
        extractCards.Clear();
        extractCardObjects.Clear();
    }
}
