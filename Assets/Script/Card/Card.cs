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

}
