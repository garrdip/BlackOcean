using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card
{
    public CardBase baseCard;

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
        return card;
    }
}
