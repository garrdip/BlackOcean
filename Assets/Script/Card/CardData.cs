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
    public bool isCardOperating = false;
    public int count = 0;

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
                ExecuteCard temp = (ExecuteCard)Delegate.CreateDelegate(typeof(ExecuteCard),this,"H3"); // valuse[0] : 메소드 이름
                cards.Add(card);
                CardMethods.Add((card.cardNumber,temp)); // cardNumber
            }
        }
    }

    public void RunCard(Card card,List<TargetObject> targets)
    {
        CardMethods.Find(data => data.Item1 == card.baseCard.cardNumber).Item2(card,targets);
    }

    public IEnumerator EffectProcess()
    {
        while(true)
        {
            if(count == 0)isCardOperating = false;
            else{
                count --;
                isCardOperating = true;
            }
            yield return new WaitForSeconds(0.01f);
        }
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
        if(from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK) == null)
            tar.monster.HP -= damage;
        else
            tar.monster.HP -= ( damage + from.buffs.Find(buff => buff.type == BuffType.ICHI_ATTACK).value + tar.buffs.Find(buff => buff.type == BuffType.FLOWER).value);
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
        int defenseValue = value;
        if((card.cardCharacteristics.Exists(character => character == CardCharacteristic.GOOWON) || 
            card.baseCard.cardCharacteristics.Exists(character => character == CardCharacteristic.GOOWON) )&& tar != from) // 카드 또는 카드 베이스
            defenseValue *= 2;
        if(from.buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE) == null)
            tar.defense += defenseValue;
        else
            tar.defense += ( defenseValue + from.buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value );
    }
    // Card Method List 
    // HONG DAN HYANG
    public void H0(Card card,List<TargetObject> tar)
    {
        M_TurnManager.instance.StartAnimation(tar[0],1,"01Attack",false);
        GeneralSingleAttack(tar[0],tar[1],6);
    }

    public void H1(Card card,List<TargetObject> tar)
    {
        GeneralSingleAttack(tar[0],tar[1],10);
    }

    public void H2(Card card,List<TargetObject> tar)
    {
        GeneralGetDefense(tar[0],tar[0],5,card);
    }
    public void H3(Card card,List<TargetObject> tar)
    {
        GeneralGetDefense(tar[0],tar[1],5,card);
    }
    public void H4(Card card,List<TargetObject> tar)
    {
        if(tar[1].maxIchi < tar[1].limitiChi)
            tar[1].maxIchi += 1;
    }
    public void H5(Card card,List<TargetObject> tar)
    {
        GeneralAddBuff(tar[0],BuffType.MOMISPOWERFUL,1);
    }
    public void H6(Card card,List<TargetObject> tar)
    {
        GeneralGetDefense(tar[0],tar[0],7,card);
    }
    public void H7(Card card,List<TargetObject> tar)
    {
        GeneralSingleAttack(tar[0],tar[1],8);
    }
    public void H8(Card card,List<TargetObject> tar)
    {
        //갑옷 약탈 
        //철구 매커니즘 설계후 적용
    }
    public void H9(Card card,List<TargetObject> tar)
    {
        //식후 명령
        //철구 매커니즘 설계후 적용
    }
    public void H10(Card card,List<TargetObject> tar)
    {
        GeneralAddBuff(tar[1],BuffType.FLOWERPOWDER, 5);
    }
    public void H11(Card card,List<TargetObject> tar)
    {
        foreach(TargetObject target in tar)
        {
            target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER).type = BuffType.FLOWER;
        }
    }
}
