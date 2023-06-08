using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;
using Spine.Unity;

public class CardData : InstanceD<CardData>
{
    public List<CardBase> cards = new List<CardBase>();
    public List<(string,ExecuteCard)> CardMethods = new List<(string, ExecuteCard)>();

    public void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    //Version 3
    public void LoadCardDataFromDB()
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
                ExecuteCard temp = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard),this,"H0"); // valuse[0] : 메소드 이름
                cards.Add(card);
                CardMethods.Add((card.cardNumber,temp)); // cardNumber
            }
        }
    }

    public void RunCard(Card card,List<TargetObject> targets)
    {
        CardMethods.Find(data => data.Item1 == card.baseCard.cardNumber).Item2(card,targets);
    }

    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */

    public void GeneralSingleAttack(TargetObject tar, int damage)
    {
        tar.monster.HP -= damage;

    }
    // Card Method List
    public void H0(Card card,List<TargetObject> tar)
    {
        M_TurnManager.instance.StartAnimation(tar[0],1,"01Attack",false);
        GeneralSingleAttack(tar[1],6);
    }

    public void H1(Card card,List<TargetObject> tar)
    {
        GeneralSingleAttack(tar[1],10);
    }

}
