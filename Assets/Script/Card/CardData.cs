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
    public static List<CardEffect> cardEffects = new List<CardEffect>();

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
                CardEffect cardEffect = new CardEffect();
                string[] values = value.Trim().Split(",");
                if(values[0] == "Character") continue; // 첫줄 데이터 스킵   
                card.character = (Character)Enum.Parse<Character>(values[0]);
                card.name = values[1];
                card.isTargetable = (values[3] == "Y") ? true : false;
                MethodInfo methodInfo = typeof(CardMethods).GetMethod(values[2]);
                cardEffect.ProcessCard = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard), null, methodInfo, true);
                cards.Add(card);
                cardEffects.Add(cardEffect);
            }
        }
    }


}

public class CardMethods
{
    // Card Method List
    public static void SingleAttack(TargetObject[] tar)
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

    public static void FullAttack(TargetObject[] tar)
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
