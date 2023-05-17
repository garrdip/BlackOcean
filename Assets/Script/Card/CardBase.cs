using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class CardBase
{
    public Character character;
    public string name;
    public int cost;
    public bool isTargetable = true;
    public List<CardCharacteristic> characteristic = new List<CardCharacteristic>();
}

public class CardEffect
{
    public ExecuteCard ProcessCard;
}