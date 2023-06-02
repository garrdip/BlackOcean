using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;

public static class CardData
{
    public static List<CardBase> cards = new List<CardBase>();
    public static List<(string,ExecuteCard)> CardMethods = new List<(string, ExecuteCard)>();
    public static void LoadCardDataFromDB()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/CardDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {          
            while(true)
            {
                string value = DB.ReadLine();
                if( value == null ) break; // 마지막 데이터의 경우 null을 반환
                CardBase card = new CardBase();
                
                string[] values = value.Trim().Split(",");
                if(values[0] == "Character") continue; // 첫줄 데이터 스킵   
                card.character = (Character)Enum.Parse<Character>(values[0]);
                card.name = values[1];
                card.isTargetable = (values[3] == "Y") ? true : false;
                ExecuteCard temp = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard),typeof(CardMethod),values[2]);
                cards.Add(card);
                CardMethods.Add((card.name,temp));
            }
        }
    }

    public static void RunCard(Card card,TargetObject[] targets)
    {
        CardMethods.Find(data => data.Item1 == card.baseCard.name).Item2(card,targets);
    }
}

public class CardMethod
{
    // Card Method List
    public static void SingleAttack(Card card,TargetObject[] tar)
    {
        Debug.Log("Single Attack!");
        if(!(tar[0].monster == null))
        {
            Debug.Log("Single Attack!2");
            tar[0].monster.HP -= 10;
        }
        if(!(tar[0].player == null))
        {
            tar[0].player.HP -= 5;
        }
    }

    public static void FullAttack(Card card,TargetObject[] tar)
    {
        if(tar == null) return;
        Debug.Log("Full Scale Attack!");
        foreach(TargetObject obj in tar)
        {
            if(obj.monster != null)
                obj.monster.HP -= 20;
        }
    }
}
