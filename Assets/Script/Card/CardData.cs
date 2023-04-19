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
            while(true)
            {
                string[] values = DB.ReadLine().Trim().Split(",");
                if(values[0] == "character"){
                    foreach(string item in values)
                    {
                        itemName.Add(item);
                    }
                    continue;
                }
                if(values[0] == "EndOfData") break;
                Card card = new Card();
                foreach(string item in values)
                {
                    // Enum Data Parsing
                    card.character = GetEnumData<Character>(values,"character");
                    card.grade = GetEnumData<CardGrade>(values,"grade");
                    card.effect = GetEnumData<CardEffect>(values,"effect");
                    card.enhancedEffect = GetEnumData<CardEffect>(values,"enhancedEffect");
                    card.characteristic = GetEnumData<CardCharacteristic>(values,"characteristic");
                    // Bool Data Parsing
                    card.isTargetable = GetBoolData(values,"isTargetable");
                    // String Data Parsing
                    card.name = values[GetIndex("name")];
                    // Int Data Parsing
                    card.cost = GetIntData(values,"cost");
                    card.hpCost = GetIntData(values,"hpCost");
                    card.effectValue = GetIntData(values,"effectValue");
                    card.enhancedEffectValue = GetIntData(values,"enhancedEffectValue");
                    // Geork Data
                    if(card.character == Character.GEORK)
                    {
                        card.tranformEffect = GetEnumData<CardEffect>(values,"tranformEffect");
                        card.tranformEffectValue = GetIntData(values,"tranformEffectValue");
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
                cards.Add(card);
            }
        }
    }

    static int GetIntData(string[] values, string name)
    {
        return int.Parse(values[GetIndex(name)]);
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
