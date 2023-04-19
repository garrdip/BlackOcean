using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class Card
{
    public Character character;
    public string name;
    public int index;
    public CardGrade grade;
    public int cost;
    public int hpCost;
    public bool isTargetable;
    public CardEffect effect;
    public int effectValue;
    public CardEffect enhancedEffect;
    public int enhancedEffectValue;
    public CardEffect tranformEffect;
    public int tranformEffectValue;
    public CardAttribute attribute;
    public int attributeValue;
    public int belief;
    public CardCharacteristic characteristic;

}
