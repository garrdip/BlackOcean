using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using ProjectD;

public static class CardData
{
    public static List<Card> cards = new List<Card>();

    public static void LoadCardDataFromDB()
    {
        TextAsset DBtext = Resources.Load<TextAsset>("DBs/CardDB");
        using (StringReader DB = new StringReader(DBtext.text))
        {
            while(true)
            {
                string[] values = DB.ReadLine().Trim().Split(",");
                if(values[0] == "Character") continue;
                if(values[0] == "EndOfData") break;
                Card card = new Card();
                card.character = (Character)Enum.Parse(typeof(Character),values[0]);
                card.name = values[1];
                card.grade = (CardGrade)Enum.Parse(typeof(CardGrade),values[2]);
                card.cost = int.Parse(values[3]);
                card.hpCost = int.Parse(values[4]);
                card.isTargetable = (values[5] == "Y") ? true : false;
                //values[6] 카드 설명 Text Globalization
                card.effect = (CardEffect)Enum.Parse(typeof(CardEffect),values[7]);
                card.effectValue = int.Parse(values[8]);
                //values[9] 카드 설명 Text Globalization
                card.enhancedEffect = (CardEffect)Enum.Parse(typeof(CardEffect),values[10]);
                card.enhancedEffectValue = int.Parse(values[11]);
                //values[12] 카드 설명 Text Globalization
                if(values[13] != "") //게오르크 전용 카드
                {
                    card.ultimateEffect = (CardEffect)Enum.Parse(typeof(CardEffect),values[13]);
                    card.ultimateEffectValue = int.Parse(values[14]);
                }
                if(values[15] != "") //에리스 전용 카드
                {
                    card.attribute = (CardAttribute)Enum.Parse(typeof(CardAttribute),values[15]);
                    card.attributeValue = int.Parse(values[16]);
                }
                if(values[17] != "") // 게오르크 전용 카드효과
                    card.Belief = int.Parse(values[17]);
                    card.characteristic = CardCharacteristic.NONE; // 소멸 등 추후 추가해야함
                cards.Add(card);
            }
        }
    }
}
