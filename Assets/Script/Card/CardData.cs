using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;
using Spine.Unity;

public partial class CardData : SingletonD<CardData>
{
    public List<CardBase> cards = new List<CardBase>();
    public List<(string,ExecuteCard)> CardMethods = new List<(string, ExecuteCard)>();

    public bool isCardOperatingTEST;
    public bool isCardOperating{get{
        return isCardOperatingTEST;
    }
    set{
        isCardOperatingTEST = value;
    }}

    WaitForSeconds tempWait = new WaitForSeconds(1f);

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
                ExecuteCard temp = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard),this,values[0]); // valuse[0] : 메소드 이름
                cards.Add(card);
                CardMethods.Add((card.cardNumber,temp)); // cardNumber
            }
        }
    }

    public void RunCard(Card card,List<TargetObject> targets)
    {
        StartCoroutine(CardMethods.Find(data => data.Item1 == card.baseCard.cardNumber).Item2(card,targets));
    }

    public bool CheckCardCharacteristic(Card card, CardCharacteristic character)
    {
        return (card.cardCharacteristics.Exists(cha => cha == character) || card.baseCard.cardCharacteristics.Exists(cha => cha == character));
    }

    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */

    public void GeneralSingleAttack(TargetObject from, TargetObject tar, int damage)
    {
        // 이곳에 최소 딜레이 넣어야함
        if(from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK) == null)
            tar.monster.HP -= damage;
        else
            tar.monster.HP -= ( damage + from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK).value + tar.buffs.Find(buff => buff.type == BuffType.FLOWER).value);
    }

    private void GeneralSingleDamage(TargetObject tar, int damage)
    {
        tar.monster.HP -= damage;
    }

    public void GeneralAddBuff(TargetObject tar, BuffType type, int value)
    {
        if(tar.buffs.Find(buff => buff.type == type) == null)
        {
            Buff newBuff = new Buff(type,value,false);
            tar.buffs.Add(newBuff);
        }
        else
        {
            tar.buffs.Find(buff => buff.type == type).value += value;
        }
    }
    
    public void GeneralAddBuff(TargetObject tar, BuffType type, int value, TargetObject user) //고유 디버프 사용시
    {
        if(tar.buffs.Find(buff => buff.type == type && buff.user == user) == null)
        {
            Buff newBuff = new Buff(type,value,true,user);
            tar.buffs.Add(newBuff);
        }
        else
        {
            tar.buffs.Find(buff => buff.type == type && buff.user == user).value += value;
        }
    }

    public void GeneralAddBuff(TargetObject tar, BuffType type, int value, bool isDebuff)
    {
        if(tar.buffs.Find(buff => buff.type == type) == null)
        {
            Buff newBuff = new Buff(type,value,isDebuff);
            tar.buffs.Add(newBuff);
        }
        else
        {
            tar.buffs.Find(buff => buff.type == type).value += value;
        }
    }

    public void GeneralGetDefense(TargetObject from, TargetObject tar, int value, Card card)
    {
        if(from.isCloneData)return;
        if(from.player.character == Character.ERIS && from == tar) // 에리스의 경우 피가 닳아있을경우 체력을 채움
        {
            int remind = from.playerMaxHP - from.playerHP;
            if(remind >= value)
                from.playerHP += value;
            else
            {
                from.playerHP = from.playerMaxHP;
                from.defense += value - remind;
            }
        }
        else
        {
            int defenseValue = value;
            if(CheckCardCharacteristic(card,CardCharacteristic.GOOWON)&& tar != from) // 카드 또는 카드 베이스
                defenseValue *= 2;
            if(from.buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE) == null)
                tar.defense += defenseValue;
            else
                tar.defense += ( defenseValue + from.buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value );
        }
    }

    public bool IsGISADO(List<TargetObject> tar)
    {
        return ((int)tar[1].monster.nextTarget == tar[0].player.selectOrder)? true : false;
    }
  
}