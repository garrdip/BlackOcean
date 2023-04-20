using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using ProjectD;

public static class CardData
{
    public static List<Card> cards = new List<Card>();
    public static List<string> itemName = new List<string>();

    public static void LoadCardDataFromDB()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/CardDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {
            Card card = new Card();
            
            while(true)
            {
                string[] values = DB.ReadLine().Trim().Split(",");
                if(values[0] == "character"){ // 데이터 시작 및 타이틀 저장
                    foreach(string item in values)
                    {
                        itemName.Add(item);
                    }
                    continue;
                }
                if(values[0] == "EndOfData") break; // 데이터 종료
                if(values[0] == "#") // 카드 다중 효과
                {
                    if(values[GetIndex("effect")]!= "") card.effect.Add(GetEnumData<CardEffect>(values,"effect"));
                    if(values[GetIndex("enhancedEffect")]!= "") card.enhancedEffect.Add(GetEnumData<CardEffect>(values,"enhancedEffect"));
                    if(values[GetIndex("effectValue")]!= "") card.effectValue.Add(GetIntData(values,"effectValue"));
                    if(IsAdditionalIntData(values,"effectValue"))card.effectValue2.Add(GetAdditionalIntData(values,"effectValue"));
                    if(values[GetIndex("enhancedEffectValue")]!= "") card.enhancedEffectValue.Add(GetIntData(values,"enhancedEffectValue"));
                    if(IsAdditionalIntData(values,"enhancedEffectValue"))card.enhancedEffectValue2.Add(GetAdditionalIntData(values,"enhancedEffectValue"));
                    if(card.character == Character.GEORK)
                    {
                        if(values[GetIndex("tranformEffect")]!= "") card.tranformEffect.Add(GetEnumData<CardEffect>(values,"tranformEffect"));
                        if(values[GetIndex("tranformEffectValue")]!= "") card.tranformEffectValue.Add(GetIntData(values,"tranformEffectValue"));
                        if(IsAdditionalIntData(values,"tranformEffectValue"))card.tranformEffectValue2.Add(GetAdditionalIntData(values,"tranformEffectValue"));
                    }
                    if(values[GetIndex("characteristic")]!= "") card.characteristic.Add(GetEnumData<CardCharacteristic>(values,"characteristic"));
                    continue;
                }
                else
                {
                    cards.Add(card);
                }
                card = new Card();
                foreach(string item in values)
                {
                    // Enum Data Parsing
                    card.character = GetEnumData<Character>(values,"character");
                    card.grade = GetEnumData<CardGrade>(values,"grade");
                    card.effect.Add(GetEnumData<CardEffect>(values,"effect"));
                    card.enhancedEffect.Add(GetEnumData<CardEffect>(values,"enhancedEffect"));
                    card.characteristic.Add(GetEnumData<CardCharacteristic>(values,"characteristic"));
                    // Bool Data Parsing
                    card.isTargetable = GetBoolData(values,"isTargetable");
                    // String Data Parsing
                    card.name = values[GetIndex("name")];
                    // Int Data Parsing
                    card.cost = GetIntData(values,"cost");
                    card.hpCost = GetIntData(values,"hpCost");
                    card.effectValue.Add(GetIntData(values,"effectValue"));
                    if(IsAdditionalIntData(values,"effectValue"))card.effectValue2.Add(GetAdditionalIntData(values,"effectValue"));
                    card.enhancedEffectValue.Add(GetIntData(values,"enhancedEffectValue"));
                    if(IsAdditionalIntData(values,"enhancedEffectValue"))card.enhancedEffectValue2.Add(GetAdditionalIntData(values,"enhancedEffectValue"));
                    // Geork Data
                    if(card.character == Character.GEORK)
                    {
                        card.tranformEffect.Add(GetEnumData<CardEffect>(values,"tranformEffect"));
                        card.tranformEffectValue.Add(GetIntData(values,"tranformEffectValue"));
                        if(IsAdditionalIntData(values,"tranformEffectValue"))card.tranformEffectValue2.Add(GetAdditionalIntData(values,"tranformEffectValue"));
                        card.belief = GetIntData(values,"belief");
                    }
                    // Eris Data
                    if(card.character == Character.ERIS)
                    {
                        card.attribute = GetEnumData<CardAttribute>(values,"attribute");
                        card.attributeValue = GetIntData(values,"attributeValue");
                    }
                    // DanHyang Data
                    if(card.character == Character.HONGDANHYANG)
                    {

                    }
                }
                
            }
        }
    }

    static int GetIntData(string[] values, string name)
    {
        string[] splitData = values[GetIndex(name)].Split("#");
        return int.Parse(splitData[0]);
    }

    static int GetAdditionalIntData(string[] values, string name)
    {
        string[] splitData = values[GetIndex(name)].Split("#");
        return int.Parse(splitData[1]);
    }

    static bool IsAdditionalIntData(string[] values,string name)
    {
        string[] splitData = values[GetIndex(name)].Split("#");
        return (splitData.Length == 1) ? false : true;
    }
    static T GetEnumData<T>(string[] values, string name)
    {
        return (T)Enum.Parse(typeof(T),values[GetIndex(name)]);
    }

    static bool GetBoolData(string[] values, string name)
    {
        return values[GetIndex(name)] == "Y" ? true : false;
    }

    static int GetIndex(string name)
    {
        int retVal = -1; //인덱스를 벗어나면 Error가 발생하도록 유도
        for(int i = 0 ;i < itemName.Count ;i++)
        {
            if(itemName[i] == name) return i;
        }
        return retVal;
    }
}
