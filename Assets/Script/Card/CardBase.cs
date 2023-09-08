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
    public int maxExperience;
    public string cardImage;
    public ValidTarget validTarget;
    public List<Infomation> info = new List<Infomation>();
    public List<CardCharacteristic> cardCharacteristics;

    public CardBase()
    {
        cardCharacteristics = new List<CardCharacteristic>();
    }
}

[System.Serializable]
public class Infomation
{
    string info = "";
    int colorCode = 0;

    public Infomation(){}

    public Infomation(string str, int num)
    {
        info = str;
        colorCode = num;
    }
}