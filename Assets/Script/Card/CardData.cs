using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;

public static class CardData
{
    public static List<Card> cards = new List<Card>();
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
                Card card = new Card();
                CardEffect cardEffect = new CardEffect();
                string[] values = value.Trim().Split(",");
                if(values[0] == "Character") continue; // 첫줄 데이터 스킵   
                card.character = (Character)Enum.Parse<Character>(values[0]);
                card.name = values[1];

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
    public static void TEST(TargetObject[] tar)
    {
        Debug.Log("TEST 메소드 실행");
    }
}
