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
      // Card Method List 
    // HONG DAN HYANG
    public IEnumerator H0(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData)
        {
            yield return tempWait;
            yield return new WaitForSeconds(0.5f);
            M_TurnManager.instance.StartAnimation(tar[0],1,"01Attack",false);
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(tar[1].monster.OnHitAnimation());
        }
        GeneralSingleAttack(tar[0],tar[1],6);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H0_E(Card card,List<TargetObject> tar)
    { 
        if(!tar[0].isCloneData)
        {
            yield return tempWait;
            M_TurnManager.instance.ChangePlayerOrder(tar[0].player,MoveDirection.FORWARD);
            yield return new WaitForSeconds(0.5f);
            M_TurnManager.instance.StartAnimation(tar[0],1,"01Attack",false);
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(tar[1].monster.OnHitAnimation());
            
        }
        GeneralSingleAttack(tar[0],tar[1],10);
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    public IEnumerator H1(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralSingleAttack(tar[0],tar[1],10);
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    public IEnumerator H1_E(Card card,List<TargetObject> tar)
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
    public IEnumerator H2_E(Card card,List<TargetObject> tar)
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
    public IEnumerator H3_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[1],5,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H4(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[1].player.GetComponentInChildren<GamePlayerDeck>().maxIchi < tar[1].player.GetComponentInChildren<GamePlayerDeck>().limitiChi)
            tar[1].player.GetComponentInChildren<GamePlayerDeck>().maxIchi += 1;
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H4_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        if(tar[1].player.GetComponentInChildren<GamePlayerDeck>().maxIchi < tar[1].player.GetComponentInChildren<GamePlayerDeck>().limitiChi)
            tar[1].player.GetComponentInChildren<GamePlayerDeck>().maxIchi += 1;
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H5(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //GeneralAddBuff(tar[0],BuffType.MOMISPOWERFUL,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H5_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //GeneralAddBuff(tar[0],BuffType.MOMISPOWERFUL,1);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H6(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        GeneralGetDefense(tar[0],tar[0],7,card);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H6_E(Card card,List<TargetObject> tar)
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
    public IEnumerator H7_E(Card card,List<TargetObject> tar)
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
    public IEnumerator H8_E(Card card,List<TargetObject> tar)
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
    public IEnumerator H9_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //식후 명령
        //철구 매커니즘 설계후 적용
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H10(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //GeneralAddBuff(tar[1],BuffType.FLOWERPOWDER, 5,tar[0]);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H10_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        //GeneralAddBuff(tar[1],BuffType.FLOWERPOWDER, 5,tar[0]);
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
    public IEnumerator H11_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData) yield return tempWait;
        foreach(TargetObject target in tar)
        {
            target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER).type = BuffType.FLOWER;
        }
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    // 임시 강화 카드
}