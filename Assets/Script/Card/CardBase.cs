using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class CardBase
{
    public Character character;
    public string name;
    public bool isTargetable = true;
    public CardType cardType;
    public int cost;
}