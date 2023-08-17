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
        //GeneralAddBuff(tar[1],BuffType.BOONGGUI,1,true);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E5(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],2);
        //GeneralAddBuff(tar[0],BuffType.BYEOLMURI,1);
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
        int remind = tar[0].playerMaxHP - tar[0].playerHP;
        if(remind >= 3 + tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value)
        {
            tar[0].playerHP += 3 + tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value; // 방어의 이치 적용할지 판단
        }
        else
        {
            tar[0].playerHP = tar[0].playerMaxHP;
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
        if(tar[0].playerHP != 1) tar[0].playerHP /= 2;
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
    // 임시 강화 카드

    public IEnumerator E0_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],5);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E1_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],8);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E2_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],4,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E3_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[1],3,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E4_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],3);
        //GeneralAddBuff(tar[1],BuffType.BOONGGUI,1,true);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E5_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],2);
        //GeneralAddBuff(tar[0],BuffType.BYEOLMURI,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E6_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        // 카드생성 
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E7_E(Card card,List<TargetObject> tar)
    {
        //ToDo : 힘의 이치, 방어의 이치 적용 여부, 
        if(!tar[0].isCloneData) yield return tempWait;
        int remind = tar[0].playerMaxHP - tar[0].playerHP;
        if(remind >= 3 + tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value)
        {
            tar[0].playerHP += 3 + tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value; // 방어의 이치 적용할지 판단
        }
        else
        {
            tar[0].playerHP = tar[0].playerMaxHP;
            foreach(TargetObject target in tar)
            {
                if(target.objectType != ObjectType.PLAYER) // 힘의 이치를 적용할지 판단해야함
                    GeneralSingleAttack(tar[0],target,3+tar[0].buffs.Find(buff => buff.type == BuffType.ICHI_DEFENSE).value - remind);
            }
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E8_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].playerHP != 1) tar[0].playerHP /= 2;
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E9_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],12);
        // 뽑을덱에서 한장 선택후 버린덱으로 보내기 
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E10_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],4,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E11_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],6);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator E12_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],8);
        // 뽑을덱에서 한장 선택후 버린덱으로 보내기 
        if(!tar[0].isCloneData) isCardOperating = false;
    }
}