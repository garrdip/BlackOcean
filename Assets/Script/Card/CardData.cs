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

    //Version 3
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
                if(values[0] == "CardNo") continue; // 첫줄 데이터 스킵   


                card.cardNumber = values[0]; //카드 번호 사실상 메소드이름
                card.character = (Character)Enum.Parse<Character>(values[1]);//케릭터
                card.name = values[2];//카드이름
                card.isTargetable = (values[5] == "Y") ? true : false;
                card.cardType = (CardType)Enum.Parse<CardType>(values[3]);
                card.cost = int.Parse(values[4]);
                for(int i = 6 ;i < values.Length ; i++)
                {
                    if(values[i] == "")break;
                    card.cardCharacteristics.Add((CardCharacteristic)Enum.Parse<CardCharacteristic>(values[i]));
                }
                ExecuteCard temp = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard),typeof(CardMethod),"H1"); // valuse[0] : 메소드 이름
                cards.Add(card);
                CardMethods.Add((card.cardNumber,temp)); // cardNumber
            }
        }
    }

    public static void RunCard(Card card,TargetObject[] targets)
    {
        CardMethods.Find(data => data.Item1 == card.baseCard.cardNumber).Item2(card,targets);
    }
}

public class CardMethod
{
    // Card Method List
    public static void H0(Card card,TargetObject[] tar)
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

    public static void H1(Card card,TargetObject[] tar)
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
