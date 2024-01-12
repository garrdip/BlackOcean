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
    void MoveIronDemonLocation(TargetObject owner, TargetObject target)
    {
        owner.ironDemonLocation = target;
        //owner.SetIronDemonParent(target.transform);
    }

    private IEnumerator MoveIronDemonCoroutine(TargetObject owner, TargetObject tar)
    {
        if(tar != owner.ironDemonLocation)
        {
            M_TurnManager.instance.AnimIronDemon("TeleportGo",owner); // 철귀 사라짐
            yield return new WaitForSeconds(0.34f); // 철귀 완전히 사라지는 시간
            M_TurnManager.instance.MoveIronDemon(owner, tar); // 철귀 적으로 이동
            M_TurnManager.instance.AnimIronDemon("TeleportBack",owner); // 철귀 나타나기 시작
            yield return new WaitForSeconds(0.333f); // 적당히 나타날때까지 기다림
        }
        if(tar != null)MoveIronDemonLocation(owner,tar); // 철귀 적으로 이동
    } 

    private IEnumerator GeneralIronDemonAttack(TargetObject spawner, TargetObject Target, int Damage)
    {
        M_TurnManager.instance.AnimIronDemon("Attack0",spawner); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
        StartCoroutine(Target.monster.OnHitAnimation()); // 실제 피격 애니메이션
        GeneralSingleAttack(spawner,Target,Damage); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.6f); // 공격모션 끝남
    }

    public IEnumerator HA(Card card,List<TargetObject> tar)
    {
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return MoveIronDemonCoroutine(tar[0],tar[1]);
        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션
        isCardOperating = false;
    }
    public IEnumerator HA_E(Card card,List<TargetObject> tar)
    {
        yield return HA(card,tar);
    }

    // Card Method List 
    // HONG DAN HYANG


    // 철의 손톱 Complete 
    public IEnumerator H0(Card card,List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;

        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        yield return GeneralIronDemonAttack(tar[0], tar[1], 6); // 철귀 공격
        
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        isCardOperating = false;
    }
    public IEnumerator H0_E(Card card,List<TargetObject> tar)
    { 
        yield return H0(card,tar);
    }


    
    //철의 이빨 Complete
    public IEnumerator H1(Card card,List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;

        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        yield return GeneralIronDemonAttack(tar[0], tar[1], 6); // 철귀 공격

        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));

        tar[0].GainBuff(BuffType.BOONGGUI,1,true,false,true,null);
        isCardOperating = false;
    }

    public IEnumerator H1_E(Card card,List<TargetObject> tar)
    {
        yield return H1(card,tar);
    }


    // 막아라 Complete
    public IEnumerator H2(Card card,List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        preLocation = tar[0].ironDemonLocation;
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

        yield return tempWait;

        yield return MoveIronDemonCoroutine(tar[0],tar[0]); // 철귀 단향이로 이동

        M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]);
        GeneralGetDefense(tar[0],tar[0],5,card);
        yield return new WaitForSeconds(1.33f);

        yield return MoveIronDemonCoroutine(tar[0],preLocation);

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        isCardOperating = false;
        
    }
    public IEnumerator H2_E(Card card,List<TargetObject> tar)
    {
        yield return H2(card,tar);
    }

    // 방패가 되어라
    public IEnumerator H3(Card card,List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        preLocation = tar[0].ironDemonLocation;
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

        yield return tempWait;

        yield return MoveIronDemonCoroutine(tar[0],tar[0]); // 철귀 단향이로 이동

        M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]);
        GeneralGetDefense(tar[0],tar[0],5,card);
        yield return new WaitForSeconds(1.33f);

        yield return MoveIronDemonCoroutine(tar[0],preLocation);

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        isCardOperating = false;
    }

    public IEnumerator H3_E(Card card,List<TargetObject> tar)
    {
        yield return H3(card,tar);
    }

    // 철의 방패
    public IEnumerator H4(Card card,List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedPlayerList);
        preLocation = tar[0].ironDemonLocation;
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

        yield return tempWait;

        yield return MoveIronDemonCoroutine(tar[0],tar[0]); // 철귀 단향이로 이동

        M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]);
        foreach(TargetObject player in M_TurnManager.instance.spawnedPlayerList)
            GeneralGetDefense(tar[0],player,4,card);
        tar[0].GainBuff(BuffType.SOIRAK,1,true,false,true,null);
        yield return new WaitForSeconds(1.33f);

        yield return MoveIronDemonCoroutine(tar[0],preLocation);

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); 
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedPlayerList);
        isCardOperating = false;
    }
    public IEnumerator H4_E(Card card,List<TargetObject> tar)
    {
        yield return H4(card,tar);
    }

    // 새싹 Testing
    public IEnumerator H5(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        tar[1].maxIchi ++;
        isCardOperating = false;
    }
    public IEnumerator H5_E(Card card,List<TargetObject> tar)
    {
        yield return H5(card,tar);
    }

    //따뜻함
    public IEnumerator H6(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);

        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            target.maxIchi ++;
        
        isCardOperating = false;
    }
    public IEnumerator H6_E(Card card,List<TargetObject> tar)
    {
        yield return H6(card,tar);
    }

    //어버이의 축복 : 
    // 1. 카드 10장 이상 뽑히지 않아야함
    // 2. 덱이 비었을때 더이상 드로우 하면 안됨
    // 3. 그밖에 수정사항 넣어야함 
    // 4. ASAP
    public IEnumerator H7(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        tar[0].player.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand(2);
        tar[0].currentIchi ++;
        isCardOperating = false;
    }
    public IEnumerator H7_E(Card card,List<TargetObject> tar)
    {
        yield return H7(card,tar);
    }

    // 씹어먹기
    public IEnumerator H8(Card card,List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;

        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        M_TurnManager.instance.AnimIronDemon("Attack0",tar[0]); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
        StartCoroutine(tar[1].monster.OnHitAnimation()); // 실제 피격 애니메이션
        GeneralSingleAttack(tar[0],tar[1],22); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.1f); // 공격모션 끝남
        GeneralSingleAttack(tar[0],tar[1],22); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.1f); // 공격모션 끝남
        GeneralSingleAttack(tar[0],tar[1],22); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.1f); // 공격모션 끝남

        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        isCardOperating = false;
    }
    public IEnumerator H8_E(Card card,List<TargetObject> tar)
    {
        yield return H8(card,tar);
    }
    public IEnumerator H9(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        tar[0].sizeOfIronDemon += 1;

        if(tar[0].ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
        {
            M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
            M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]);
            tar[0].ironDemonLocation.defense += tar[0].sizeOfIronDemon;
            yield return new WaitForSeconds(1.33f);
            M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        }
        else // 몬스터의 경우 데미지
        {
            M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));

                if(UnityEngine.Random.Range(0,2) == 0)M_TurnManager.instance.AnimIronDemon("Attack0",tar[0]);
                else M_TurnManager.instance.AnimIronDemon("Attack1",tar[0]);
                yield return new WaitForSeconds(0.4f);

            tar[0].ironDemonLocation.DamageToMonster(tar[0].sizeOfIronDemon,tar[0]);
            yield return new WaitForSeconds(0.6f);
            M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        }
        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]);
        isCardOperating = false;
    }
    public IEnumerator H9_E(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        tar[0].sizeOfIronDemon += 2;

        if(tar[0].ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
        {
            M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
            M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]);
            tar[0].ironDemonLocation.defense += tar[0].sizeOfIronDemon;
            yield return new WaitForSeconds(1.33f);
            M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        }
        else // 몬스터의 경우 데미지
        {
            M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));

                if(UnityEngine.Random.Range(0,2) == 0)M_TurnManager.instance.AnimIronDemon("Attack0",tar[0]);
                else M_TurnManager.instance.AnimIronDemon("Attack1",tar[0]);
                yield return new WaitForSeconds(0.4f);

            tar[0].ironDemonLocation.DamageToMonster(tar[0].sizeOfIronDemon,tar[0]);
            yield return new WaitForSeconds(0.6f);
            M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        }
        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]);
        isCardOperating = false;
    }
    public IEnumerator H10(Card card,List<TargetObject> tar)
    {

            yield return tempWait;
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack0",false);
            yield return new WaitForSeconds(1f);

        tar[1].GainBuff(BuffType.FLOWERPOWDER,5,false,false,true,tar[0]);
        isCardOperating = false;
    }
    public IEnumerator H10_E(Card card,List<TargetObject> tar)
    {

            yield return tempWait;
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack0",false);
            yield return new WaitForSeconds(1f);

        tar[1].GainBuff(BuffType.FLOWERPOWDER,8,false,false,true,tar[0]);
        isCardOperating = false;
    }
    public IEnumerator H11(Card card,List<TargetObject> tar)
    {
         yield return tempWait;
        // 개화 UI 구현 후 적용
        isCardOperating = false;
    }
    public IEnumerator H11_E(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        // 개화
        isCardOperating = false;
    }

    public IEnumerator H12(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H12_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H13(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H13_E(Card card, List<TargetObject> tar){yield return null;}
	public IEnumerator H14(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H14_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H15(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H15_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H16(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H16_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H17(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H17_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H18(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H18_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H19(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H19_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H20(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H20_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H21(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H21_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H22(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H22_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H23(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H23_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H24(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H24_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H25(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H25_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H26(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H26_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H27(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H27_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H28(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H28_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H29(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H29_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H30(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H30_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H31(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H31_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H32(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H32_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H33(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H33_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H34(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H34_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H35(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H35_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H36(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H36_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H37(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H37_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H38(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H38_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H39(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H39_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H40(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H40_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H41(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H41_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H42(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H42_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H43(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H43_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H44(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H44_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H45(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H45_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H46(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H46_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H47(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H47_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H48(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H48_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H49(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H49_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H50(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H50_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H51(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H51_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H52(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H52_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H53(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H53_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H54(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H54_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H55(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H55_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H56(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H56_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H57(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H57_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H58(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H58_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H59(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H59_E(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H60(Card card, List<TargetObject> tar){yield return null;}
    public IEnumerator H60_E(Card card, List<TargetObject> tar){yield return null;}
    
    

    // 임시 강화 카드
}