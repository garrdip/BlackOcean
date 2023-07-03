using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;
using Spine.Unity;

public class CardData : SingletonD<CardData>
{
    public List<CardBase> cards = new List<CardBase>();
    public List<(string,ExecuteCard)> CardMethods = new List<(string, ExecuteCard)>();

    public bool isCardOperatingTEST;
    public bool isCardOperating{get{
        return isCardOperatingTEST;
    }
    set{
        isCardOperatingTEST = value;
        TEST();
    }}
    public void TEST()
    {
        Debug.Log("False로 바뀜");
    }
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
            int remind = from.player.MaxHP - from.player.HP;
            if(remind >= value)
                from.player.HP += value;
            else
            {
                from.player.HP = from.player.MaxHP;
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
    // Card Method List 
    // HONG DAN HYANG
    public IEnumerator H0(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        M_TurnManager.instance.StartAnimation(tar[0],1,"01Attack",false);
        GeneralSingleAttack(tar[0],tar[1],15);
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    public IEnumerator H1(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],10);
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    public IEnumerator H2(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],5,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H3(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[1],5,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H4(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[1].maxIchi < tar[1].limitiChi)
            tar[1].maxIchi += 1;
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H5(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralAddBuff(tar[0],BuffType.MOMISPOWERFUL,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H6(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],7,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H7(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],8);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H8(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //갑옷 약탈 
        //철구 매커니즘 설계후 적용
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H9(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //식후 명령
        //철구 매커니즘 설계후 적용
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H10(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralAddBuff(tar[1],BuffType.FLOWERPOWDER, 5,tar[0]);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H11(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        foreach(TargetObject target in tar)
        {
            target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER).type = BuffType.FLOWER;
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    // 게오르크
    private void GeneralApDo(TargetObject user,TargetObject tar, int value)
    {
        if(user.buffs.Find(buff => buff.type == BuffType.THEREISNOJABI) != null) // 자비는 없다 보유시 압도스택 => 데미지(힘의이치영향X)
        {
            GeneralSingleDamage(tar,3+tar.buffs.Find(buff => buff.type == BuffType.APDO && buff.user == user).value);
            tar.buffs.Remove(tar.buffs.Find(buff => buff.type == BuffType.APDO && buff.user == user));
        }
        else
        {
            GeneralAddBuff(tar,BuffType.APDO,1,user);
        }
    }

    public IEnumerator G0(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].isTransformed) // 변신 후 효과
        {
            GeneralSingleAttack(tar[0],tar[1],9);
            // 기사도 효과
            if(tar[1].monster.nextTarget == tar[0])
            {
                GeneralSingleAttack(tar[0],tar[1],9);
                GeneralSingleAttack(tar[0],tar[1],9);
            }
        }
        else // 변신 전 효과
        {
            tar[1].defense = 0;
            GeneralSingleAttack(tar[0],tar[1],30);
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G1(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].isTransformed) // 변신 후 효과
        {
            foreach(Buff buff in tar[0].buffs) // 디버프 스택 1씩 감소
            {
                if(buff.isDebuff)
                    buff.value -= 1;
                if(buff.value <= 0)
                    tar[0].buffs.Remove(buff);
            }
            GeneralGetDefense(tar[0],tar[0],20,card);
        }
        else // 변신 전 효과
        {
            GeneralGetDefense(tar[0],tar[0],15,card);
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G2(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].isTransformed) // 변신 후 효과
        {
            foreach(CardOnHand cardOnHand in tar[0].player.gameObject.GetComponent<GamePlayerDeck>().cardOnHands)
            {
                cardOnHand.card.costAddition = -1;
            }
        }
        else // 변신 전 효과
        {
            GeneralAddBuff(tar[0],BuffType.CARDCOSTONE,1);
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G3(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],7);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G4(Card card,List<TargetObject> tar)
    {
        GeneralSingleAttack(tar[0],tar[1],9);
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralAddBuff(tar[1],BuffType.SOIRAK,1,true);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G5(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],6,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G6(Card card,List<TargetObject> tar) 
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],4,card);
        GeneralGetDefense(tar[0],tar[1],4,card);//내부에 구원 구현되어 있음
        if(!tar[0].isCloneData) isCardOperating = false;

    }
    public IEnumerator G7(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],9);
        if(tar[1].monster.nextTarget == tar[0])
            GeneralSingleAttack(tar[0],tar[1],9);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G8(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],9);
        GeneralApDo(tar[0],tar[1],1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G9(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],7,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G10(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],5);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G11(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralAddBuff(tar[0],BuffType.THEREISNOJABI,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G12(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        // 무작위 기사도 카드 3장 생성
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    //에리스
    public IEnumerator E0(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],5);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E1(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],8);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E2(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],4,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E3(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[1],3,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E4(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],3);
        GeneralAddBuff(tar[1],BuffType.BOONGGUI,1,true);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E5(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],2);
        GeneralAddBuff(tar[0],BuffType.BYEOLMURI,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E6(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        // 카드생성 
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E7(Card card,List<TargetObject> tar)
    {
        //ToDo : 힘의 이치, 방어의 이치 적용 여부, 
        if(!tar[0].isCloneData) yield return tempWait;
        int remind = tar[0].player.MaxHP - tar[0].player.HP;
        if(remind >= 3 + tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value)
        {
            tar[0].player.HP += 3 + tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value; // 방어의 이치 적용할지 판단
        }
        else
        {
            tar[0].player.HP = tar[0].player.MaxHP;
            foreach(TargetObject target in tar)
            {
                if(target.objectType != ObjectType.PLAYER) // 힘의 이치를 적용할지 판단해야함
                    GeneralSingleAttack(tar[0],target,3+tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value - remind);
            }
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E8(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].player.HP != 1) tar[0].player.HP /= 2;
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E9(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],12);
        // 뽑을덱에서 한장 선택후 버린덱으로 보내기 
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E10(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],4,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E11(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],6);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E12(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],8);
        // 뽑을덱에서 한장 선택후 버린덱으로 보내기 
        if(!tar[0].isCloneData) isCardOperating = false;
    }

}