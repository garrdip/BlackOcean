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
    public List<CardEffect> effect = new List<CardEffect>();
    public List<int> effectValue = new List<int>();
    public List<int> effectValue2 = new List<int>();
    public List<CardEffect> enhancedEffect = new List<CardEffect>();
    public List<int> enhancedEffectValue = new List<int>();
    public List<int> enhancedEffectValue2 = new List<int>();
    public List<CardEffect> tranformEffect = new List<CardEffect>();
    public List<int> tranformEffectValue = new List<int>();
    public List<int> tranformEffectValue2 = new List<int>();
    public CardAttribute attribute;
    public int attributeValue;
    public int belief;
    public List<CardCharacteristic> characteristic = new List<CardCharacteristic>();
}
