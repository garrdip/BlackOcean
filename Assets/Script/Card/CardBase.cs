using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class CardBase
{
    public Character character;
    public string name;
    public string description;
    public string cardNumber;
    public bool isTargetable = true;
    public CardType cardType;
    public int cost;
    public List<CardCharacteristic> cardCharacteristics;

    public CardBase()
    {
        cardCharacteristics = new List<CardCharacteristic>();
    }
}
