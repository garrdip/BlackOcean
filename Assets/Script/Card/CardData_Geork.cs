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
            //GeneralAddBuff(tar,BuffType.APDO,1,user);
        }
    }

    public IEnumerator G0(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].isTransformed) // 변신 후 효과
        {
            GeneralSingleAttack(tar[0],tar[1],9);
            // 기사도 효과
            if(IsGISADO(tar))
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
            //GeneralAddBuff(tar[0],BuffType.CARDCOSTONE,1);
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
        //GeneralAddBuff(tar[1],BuffType.SOIRAK,1,true);
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
        if(IsGISADO(tar))
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
        //GeneralAddBuff(tar[0],BuffType.THEREISNOJABI,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G12(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        // 무작위 기사도 카드 3장 생성
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    // 임시 강화 카드
     public IEnumerator G0_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[0].isTransformed) // 변신 후 효과
        {
            GeneralSingleAttack(tar[0],tar[1],9);
            // 기사도 효과
            if(IsGISADO(tar))
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
    public IEnumerator G1_E(Card card,List<TargetObject> tar)
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
    public IEnumerator G2_E(Card card,List<TargetObject> tar)
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
            //GeneralAddBuff(tar[0],BuffType.CARDCOSTONE,1);
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G3_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],7);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G4_E(Card card,List<TargetObject> tar)
    {
        GeneralSingleAttack(tar[0],tar[1],9);
        if(!tar[0].isCloneData) yield return tempWait;
        //GeneralAddBuff(tar[1],BuffType.SOIRAK,1,true);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G5_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],6,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G6_E(Card card,List<TargetObject> tar) 
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],4,card);
        GeneralGetDefense(tar[0],tar[1],4,card);//내부에 구원 구현되어 있음
        if(!tar[0].isCloneData) isCardOperating = false;

    }
    public IEnumerator G7_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],9);
        if(IsGISADO(tar))
            GeneralSingleAttack(tar[0],tar[1],9);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G8_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],9);
        GeneralApDo(tar[0],tar[1],1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G9_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],7,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G10_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],5);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G11_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //GeneralAddBuff(tar[0],BuffType.THEREISNOJABI,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator G12_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        // 무작위 기사도 카드 3장 생성
        if(!tar[0].isCloneData) isCardOperating = false;
    }
}