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
    public IEnumerator HA(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData)
        {
            yield return tempWait; // 임시 딜레이

            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

            if(tar[1] != tar[0].ironDemonLocation)
            {
                M_TurnManager.instance.AnimIronDemon("TeleportGo",tar[0]); // 철귀 사라짐
                yield return new WaitForSeconds(0.333f); // 철귀 완전히 사라지는 시간
                M_TurnManager.instance.MoveIronDemon(tar[1],tar[0]); // 철귀 적으로 이동
                M_TurnManager.instance.AnimIronDemon("TeleportBack",tar[0]); // 철귀 나타나기 시작
                yield return new WaitForSeconds(0.2f); // 적당히 나타날때까지 기다림
            }
            tar[0].ironDemonLocation = tar[1]; // 철귀 적으로 이동
            M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션
        }
        else 
            tar[0].ironDemonLocation = tar[1]; // 철귀 적으로 이동
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator HA_E(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData)
        {
            yield return tempWait; // 임시 딜레이

            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

            if(tar[1] != tar[0].ironDemonLocation)
            {
                M_TurnManager.instance.AnimIronDemon("TeleportGo",tar[0]); // 철귀 사라짐
                yield return new WaitForSeconds(0.333f); // 철귀 완전히 사라지는 시간
                M_TurnManager.instance.MoveIronDemon(tar[1],tar[0]); // 철귀 적으로 이동
                M_TurnManager.instance.AnimIronDemon("TeleportBack",tar[0]); // 철귀 나타나기 시작
                yield return new WaitForSeconds(0.2f); // 적당히 나타날때까지 기다림
            }
            tar[0].ironDemonLocation = tar[1];
            M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션
        }
        else
            tar[0].ironDemonLocation = tar[1]; // 철귀 적으로 이동
        if(!tar[0].isCloneData) isCardOperating = false;
    }

    // Card Method List 
    // HONG DAN HYANG
    public IEnumerator H0(Card card,List<TargetObject> tar)
    {
        if(!tar[0].isCloneData)
        {
            yield return tempWait; // 임시 딜레이

            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

            if(tar[1] != tar[0].ironDemonLocation)
            {
                M_TurnManager.instance.AnimIronDemon("TeleportGo",tar[0]); // 철귀 사라짐
                yield return new WaitForSeconds(0.333f); // 철귀 완전히 사라지는 시간
                M_TurnManager.instance.MoveIronDemon(tar[1],tar[0]); // 철귀 적으로 이동
                M_TurnManager.instance.AnimIronDemon("TeleportBack",tar[0]); // 철귀 나타나기 시작
                yield return new WaitForSeconds(0.2f); // 적당히 나타날때까지 기다림
            }

            M_TurnManager.instance.AnimIronDemon("Attack0",tar[0]); // 철귀 공격 모션 시작
            yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
            StartCoroutine(tar[1].monster.OnHitAnimation()); // 실제 피격 애니메이션
            GeneralSingleAttack(tar[0],tar[1],6); // 실제 데미지 적용시점
            yield return new WaitForSeconds(0.6f); // 공격모션 끝남

            if(tar[1] != tar[0].ironDemonLocation)
            {
                M_TurnManager.instance.AnimIronDemon("TeleportGo",tar[0]); // 다시 사라짐
                yield return new WaitForSeconds(0.33f);// 완전히 사라지는 시간
                M_TurnManager.instance.MoveIronDemon(tar[0].ironDemonLocation,tar[0]); // 플레이어에게 다시 이동
                M_TurnManager.instance.AnimIronDemon("TeleportBack",tar[0]); // 다시 나타남
                yield return new WaitForSeconds(0.33f); // 완전히 나타날때까지 기다림
            }

            M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션
        }
        else
            GeneralSingleAttack(tar[0],tar[1],6);
        if(!tar[0].isCloneData) isCardOperating = false;
    }
    public IEnumerator H0_E(Card card,List<TargetObject> tar)
    { 
        if(!tar[0].isCloneData)
        {
            yield return tempWait;
            yield return new WaitForSeconds(0.5f);
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack0",false);
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