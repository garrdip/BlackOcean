using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class Card
{
    public CardBase baseCard;
    public bool isEnhanced;
    public int costAddition;
    public List<CardCharacteristic> cardCharacteristics = new List<CardCharacteristic>();
    public int experience;

    public Card(CardBase basecard)
    {
        baseCard = basecard;
    }

    public Card(){} // For Mirror Library default constructor

    
    // 카드 클래스 깊은복사
    public Card CardDeepCopy()
    {
        Card card = new Card();
        card.baseCard = baseCard;
        card.isEnhanced = isEnhanced;
        card.costAddition = costAddition;
        card.experience = experience;
        foreach(CardCharacteristic cardChar in cardCharacteristics)
            card.cardCharacteristics.Add(cardChar);
        return card;
    }
}


