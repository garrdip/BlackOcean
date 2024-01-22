using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using System.Linq;

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
        
    }
    public IEnumerator HA_E(Card card,List<TargetObject> tar)
    {
        yield return HA(card,tar);
    }

    public IEnumerator HWAHAP(TargetObject tar)
    {
        if(tar.player.character == Character.HONGDANHYANG)
        {
            if(tar.ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
            {
                M_TurnManager.instance.AnimIronDemon("Buff0",tar);
                tar.ironDemonLocation.defense += tar.sizeOfIronDemon;
                yield return new WaitForSeconds(1.33f);
            }
            else // 몬스터의 경우 데미지
            {
                if(UnityEngine.Random.Range(0,2) == 0)M_TurnManager.instance.AnimIronDemon("Attack0",tar);
                else M_TurnManager.instance.AnimIronDemon("Attack1",tar);
                yield return new WaitForSeconds(0.4f);
                StartCoroutine(tar.ironDemonLocation.monster.OnHitAnimation()); // 실제 피격 애니메이션
                tar.ironDemonLocation.DamageToMonster(tar.sizeOfIronDemon, tar);
                yield return new WaitForSeconds(0.6f);
            }
            M_TurnManager.instance.AnimIronDemon("Idle",tar);
            yield return new WaitForSeconds(0.1f);
        }
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
        tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth ++;
        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        yield return GeneralIronDemonAttack(tar[0], tar[1], 6); // 철귀 공격
        
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
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
        tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth ++;
        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        yield return GeneralIronDemonAttack(tar[0], tar[1], 6); // 철귀 공격

        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));

        tar[0].GainBuff(BuffType.BOONGGUI,1,true,false,true,null,card);
        
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
        tar[0].GainBuff(BuffType.SOIRAK,1,true,false,true,null,card);
        yield return new WaitForSeconds(1.33f);

        yield return MoveIronDemonCoroutine(tar[0],preLocation);

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); 
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedPlayerList);
        
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
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        tar[0].player.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand(2); // 카드 사용한 유저의 드로우 카드 Synclist에 카드 2개 추가
        tar[0].player.GetComponent<GamePlayerDeck>().currentIchi ++;
        yield return new WaitForSeconds(1f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
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
        
    }
    public IEnumerator H8_E(Card card,List<TargetObject> tar)
    {
        yield return H8(card,tar);
    }

    //너희와 함께하리
    public IEnumerator H9(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedPlayerList);
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

        foreach(TargetObject player in M_TurnManager.instance.spawnedPlayerList)
            if(player != tar[0])GeneralGetDefense(tar[0],player,30,card);
        tar[0].GainBuff(BuffType.BOONGGUI,3,true,false,true,null,card);
        yield return new WaitForSeconds(1.33f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedPlayerList);
        
    }
    public IEnumerator H9_E(Card card,List<TargetObject> tar)
    {
        yield return H9(card,tar);
    }

    // 철의 꽃밭
    public IEnumerator H10(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            target.player.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand(2);
            target.player.GetComponent<GamePlayerDeck>().currentIchi ++;
        }
        
    }
    public IEnumerator H10_E(Card card,List<TargetObject> tar)
    {
        yield return H10(card,tar);
    }

    // 비료
    public IEnumerator H11(Card card,List<TargetObject> tar)
    {
        yield return tempWait;
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.CmdSpawnCardOnHand((int)(gamePlayerDeck.trashDeck.Count/7));
        
    }
    public IEnumerator H11_E(Card card,List<TargetObject> tar)
    {
       yield return H11(card,tar);
    }


    // 양손 베기
    public IEnumerator H12(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 

        yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
        StartCoroutine(tar[1].monster.OnHitAnimation()); // 실제 피격 애니메이션
        GeneralSingleAttack(tar[0],tar[1],22); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.1f); // 공격모션 끝남
        GeneralSingleAttack(tar[0],tar[1],22); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.1f); // 공격모션 끝남
        GeneralSingleAttack(tar[0],tar[1],22); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.1f); // 공격모션 끝남

        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H12_E(Card card, List<TargetObject> tar)
    {
        yield return H12(card,tar);
    }

    // 차오르는 복수심
    public IEnumerator H13(Card card, List<TargetObject> tar)
    {
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.2f); // 공격모션 끝남
        foreach(TargetObject monster in M_TurnManager.instance.spawnedMonsterList)
        {
            StartCoroutine(monster.monster.OnHitAnimation());
            GeneralSingleAttack(tar[0],monster,20);
        }
        tar[0].GainBuff(BuffType.BOONGGUI,3,true,false,true,tar[0],card);
        yield return new WaitForSeconds(0.2f); // 공격모션 끝남
        M_DimmingManager.instance.StopDimming(targets);
        

    }
    public IEnumerator H13_E(Card card, List<TargetObject> tar)
    {
        yield return H13(card,tar);
    }

    // 피해라 
	public IEnumerator H14(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        GeneralGetDefense(tar[0],tar[0],7,card);
        yield return new WaitForSeconds(1.33f);
        MovePosition(true,tar[0]);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H14_E(Card card, List<TargetObject> tar)
    {
        yield return H14(card,tar);
    }

    // 부탁하마
    public IEnumerator H15(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        GeneralSingleAttack(tar[0],tar[1],8);
        yield return new WaitForSeconds(1.33f);
        MovePosition(false,tar[0]);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H15_E(Card card, List<TargetObject> tar)
    {
        yield return H15(card,tar);
    }

    // 그을린 꽃의 기적
    public IEnumerator H16(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedPlayerList);
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        foreach(TargetObject player in M_TurnManager.instance.spawnedPlayerList)
        {
            bool hasDebuff = false;
            if(player.buffs.FindIndex(x => x.type == BuffType.BOONGGUI) != -1)
            {
                hasDebuff = true;
                player.buffs.Remove(player.buffs.Find(buff => buff.type == BuffType.BOONGGUI));
            }
            if(player.buffs.FindIndex(x => x.type == BuffType.SOIRAK) != -1)
            {
                hasDebuff = true;
                player.buffs.Remove(player.buffs.Find(buff => buff.type == BuffType.SOIRAK));
            }
            if(hasDebuff)
            {
                player.GainBuff(BuffType.ICHI_DEFENSE,2,false,true,false,tar[0],card);
            }
        } 
        yield return new WaitForSeconds(1.33f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedPlayerList);
        
    }
    public IEnumerator H16_E(Card card, List<TargetObject> tar)
    {
        yield return H16(card,tar);
    }

    // 기적의 무게
    public IEnumerator H17(Card card, List<TargetObject> tar)
    {
        cardSelectCallBack = H17_CallBack;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1.33f);
        tar[0].player.GetComponent<GamePlayerDeck>().AddDrawCard(2); // 카드 사용한 유저의 드로우 카드 Synclist에 카드 2개 추가
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H17_E(Card card, List<TargetObject> tar)
    {
        yield return H17(card,tar);
    }

    public void H17_CallBack(List<Card> cards)
    {
        cards[0].cardCharacteristics.Add(CardCharacteristic.JOONGREUK);
    }

    // 주조 (강화 이펙트 추가)
    public IEnumerator H18(Card card, List<TargetObject> tar)
    {
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션
        foreach(TargetObject player in M_TurnManager.instance.spawnedPlayerList)
        {
            int count = 0;
            foreach(CardOnHand cardOnHand in player.player.GetComponent<GamePlayerDeck>().cardOnHands)
            {
                if(cardOnHand.card == card)continue;
                cardOnHand.card.cardCharacteristics.Add(CardCharacteristic.SOOKREON);
                cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);
                count ++;
                if(count == 3)break;
            }
        }
        yield return new WaitForSeconds(1.33f);
        
    }
    public IEnumerator H18_E(Card card, List<TargetObject> tar)
    {
        yield return H18(card,tar);
    }

    // 맡기거라
    public IEnumerator H19(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        GeneralGetDefense(tar[0],tar[0],10,card);
        yield return new WaitForSeconds(1.33f);
        MovePosition(tar[0],tar[1]);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H19_E(Card card, List<TargetObject> tar)
    {
        yield return H19(card,tar);
    }

    // 철로 만든 괴물
    public IEnumerator H20(Card card, List<TargetObject> tar)
    {
        int totalCount = 0;

        foreach(Card pCard in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
        {
            if(pCard.baseCard.cardNumber == "H0" || pCard.baseCard.cardNumber == "H1")
                totalCount++;
        }
        foreach(Card pCard in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
        {
            if(pCard.baseCard.cardNumber == "H0" || pCard.baseCard.cardNumber == "H1")
                totalCount++;
        }
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;

        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        M_TurnManager.instance.AnimIronDemon("Attack0",tar[0]); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
        StartCoroutine(tar[1].monster.OnHitAnimation()); // 실제 피격 애니메이션
        for(int i = 0 ; i < totalCount+1 ; i ++)
        {
            GeneralSingleDamage(tar[1],5); // 실제 데미지 적용시점
            yield return new WaitForSeconds(0.2f);
        }
        
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
        
    }
    public IEnumerator H20_E(Card card, List<TargetObject> tar)
    {
        yield return H20(card,tar);
    }

    // 철의 분노
    public IEnumerator H21(Card card, List<TargetObject> tar)
    {
        foreach(Card pCard in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
        {
            if(pCard.baseCard.cardNumber == "H0" || pCard.baseCard.cardNumber == "H1")
                pCard.tempEnhanced = true;
        }
        foreach(Card pCard in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
        {
            if(pCard.baseCard.cardNumber == "H0" || pCard.baseCard.cardNumber == "H1")
                pCard.tempEnhanced = true;
        }
        foreach(CardOnHand pCard in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
        {
            if(pCard.card.baseCard.cardNumber == "H0" || pCard.card.baseCard.cardNumber == "H1")
            {
                pCard.card.tempEnhanced = true;
                pCard.OnChangeCardData(pCard.card,pCard.card);
            }
        }
        yield return new WaitForSeconds(0.2f);
        
    }
    public IEnumerator H21_E(Card card, List<TargetObject> tar)
    {
        yield return H21(card,tar);
    }

    //손톱 다듬기 // ToDo : 생성카드는 생성 느낌 줘야함
    public IEnumerator H22(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        GeneralGetDefense(tar[0],tar[0],7,card);
        yield return new WaitForSeconds(1f);
        tar[0].player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "H0")),3);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H22_E(Card card, List<TargetObject> tar)
    {
        yield return H22(card,tar);
    }

    //아버지의 화신
    public IEnumerator H23(Card card, List<TargetObject> tar)
    {
        List<TargetObject> allEnemy = new List<TargetObject>();
        allEnemy.Add(tar[0]);
        allEnemy.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(allEnemy);
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        for(int i = 0 ;i < tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth + 1 ; i ++)
        {
            int randomIndex = UnityEngine.Random.Range(0,M_TurnManager.instance.spawnedMonsterList.Count);
            GeneralSingleAttack(tar[0],M_TurnManager.instance.spawnedMonsterList[randomIndex],4);
            StartCoroutine(M_TurnManager.instance.spawnedMonsterList[randomIndex].monster.OnHitAnimation());
            yield return new WaitForSeconds(0.2f);
        }
        M_DimmingManager.instance.StopDimming(allEnemy);
        
    }
    public IEnumerator H23_E(Card card, List<TargetObject> tar)
    {
        yield return H23(card,tar);
    }

    // 칼날 손질
    public IEnumerator H24(Card card, List<TargetObject> tar)
    {
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션
        tar[0].GainBuff(BuffType.BLADETRIMMING,1, false, true, false, tar[0],card);
        
    }
    public IEnumerator H24_E(Card card, List<TargetObject> tar)
    {
        yield return H24(card,tar);
    }

    // 입맛 다시기
    public IEnumerator H25(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        tar[0].player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "H1")),M_TurnManager.instance.spawnedMonsterList.Count);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H25_E(Card card, List<TargetObject> tar)
    {
        yield return H25(card,tar);
    }

    TargetObject cardGenUser;

    //재조물 (카드 버리기)
    public IEnumerator H26(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        cardGenUser = tar[0];
        yield return new WaitForSeconds(1f);
        tar[0].player.GetComponent<GamePlayerDeck>().TargetCardOnHandRemovePopUpShow();  // 패 카드 제거 팝업 호출
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    public void H26_CallBack(int count)
    {
        for( int i = 0 ;i  < count ; i++)   
            if(UnityEngine.Random.Range(0,2) == 0)
                cardGenUser.player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "H0")),1);
            else
                cardGenUser.player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "H1")),1);
    }

    public IEnumerator H26_E(Card card, List<TargetObject> tar)
    {
        yield return H26(card,tar);
    }

    // 화가 나는구나
    public IEnumerator H27(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        tar[0].GainBuff(BuffType.IMANGRY,1,false,true,true,tar[0],card);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H27_E(Card card, List<TargetObject> tar)
    {
        yield return H27(card,tar);
    }

    // 성장기
    public IEnumerator H28(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        tar[0].GainBuff(BuffType.GROWTHSPURT, 1, false, true, false, tar[0],card);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H28_E(Card card, List<TargetObject> tar)
    {
        yield return H28(card,tar);
    }

    // 용철 폭발
    public IEnumerator H29(Card card, List<TargetObject> tar)
    {
        int totalDamage = tar[0].sizeOfIronDemon - 1;
        tar[0].sizeOfIronDemon = 1;
        M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
        {
            GeneralSingleDamage(enemy,totalDamage*8);
            StartCoroutine(enemy.monster.OnHitAnimation());
        }
        yield return new WaitForSeconds(0.5f);
        
    }
    public IEnumerator H29_E(Card card, List<TargetObject> tar)
    {
        yield return H29(card,tar);
    }

    // 아버지의 자비
    public IEnumerator H30(Card card, List<TargetObject> tar)
    {
        tar[0].sizeOfIronDemon = (tar[0].sizeOfIronDemon - 5) <= 0 ? 1 : tar[0].sizeOfIronDemon - 5;
        
        M_TurnManager.instance.AnimIronDemon("Buff0",tar[0]); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            GeneralGetDefense(tar[0],target,30,card);
        }
        yield return new WaitForSeconds(0.5f);
        
    }
    public IEnumerator H30_E(Card card, List<TargetObject> tar)
    {
        yield return H30(card,tar);
    }

    // 무게감 (H0랑 같음)
    public IEnumerator H31(Card card, List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;
        tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth ++;
        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        M_TurnManager.instance.AnimIronDemon("Attack0",tar[0]); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
        StartCoroutine(tar[1].monster.OnHitAnimation()); // 실제 피격 애니메이션
        GeneralSingleDamage(tar[1],tar[0].sizeOfIronDemon); // 실제 데미지 적용시점
        yield return new WaitForSeconds(0.6f); // 공격모션 끝남
        
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H31_E(Card card, List<TargetObject> tar)
    {
        yield return H31(card,tar);
    }

    // 포식자
    public IEnumerator H32(Card card, List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;
        tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth ++;
        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        M_TurnManager.instance.AnimIronDemon("Attack1",tar[0]); // 철귀 공격 모션 시작
        yield return new WaitForSeconds(0.4f); // 타격지점까지 시간
        StartCoroutine(tar[1].monster.OnHitAnimation()); // 실제 피격 애니메이션
        GeneralSingleAttack(tar[0],tar[1],80); // 실제 데미지 적용시점
        if(tar[1].monster.HP <= 0)tar[0].player.GetComponent<GamePlayerDeck>().AdditionalSizeOfIromDemon ++;
        tar[0].sizeOfIronDemon ++;
        yield return new WaitForSeconds(0.6f); // 공격모션 끝남
        
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H32_E(Card card, List<TargetObject> tar)
    {
        yield return null;
    }

    // 갑옷 약탈
    public IEnumerator H33(Card card, List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;
        tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth ++;
        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        yield return GeneralIronDemonAttack(tar[0], tar[1], 10); // 철귀 공격
        tar[0].sizeOfIronDemon += 2;
        
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H33_E(Card card, List<TargetObject> tar)
    {
        yield return H33(card,tar);
    }

    // 꽃과 철의 분노
    public IEnumerator H34(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        tar[0].GainBuff(BuffType.FURYOFFLOWER, 1, false, true, false, tar[0],card);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H34_E(Card card, List<TargetObject> tar)
    {
        yield return H34(card,tar);
    }

    // 휩쓸기
    public IEnumerator H35(Card card, List<TargetObject> tar)
    {
        List<TargetObject> allEnemy = new List<TargetObject>();
        allEnemy.Add(tar[0]);
        allEnemy.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        M_DimmingManager.instance.StartDimming(allEnemy);
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
        {
            GeneralSingleAttack(tar[0],enemy,7);
            StartCoroutine(enemy.monster.OnHitAnimation());
        }
        yield return new WaitForSeconds(0.5f);
        if(tar[0].ironDemonLocation.objectType == ObjectType.ENEMY)
            yield return HWAHAP(tar[0]);
        M_DimmingManager.instance.StopDimming(allEnemy);
        
    }
    public IEnumerator H35_E(Card card, List<TargetObject> tar)
    {
        yield return H35(card,tar);
    }

    // 어머니를 지켜라
    public IEnumerator H36(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return MoveIronDemonCoroutine(tar[0],tar[0]); // 철귀 자신의 위치로
        yield return HWAHAP(tar[0]);
        yield return HWAHAP(tar[0]);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H36_E(Card card, List<TargetObject> tar)
    {
        yield return H36(card,tar);
    }

    // 식후 명령
    public IEnumerator H37(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        tar[0].sizeOfIronDemon ++;
        yield return HWAHAP(tar[0]);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        
    }
    public IEnumerator H37_E(Card card, List<TargetObject> tar)
    {
        yield return H37(card,tar);
    }

    //철의 장막
    public IEnumerator H38(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedPlayerList);
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
        {
            if(target != tar[0])
                GeneralGetDefense(tar[0],target,8,card);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedPlayerList);
    }
    public IEnumerator H38_E(Card card, List<TargetObject> tar)
    {
        yield return H38(card,tar);
    }

    // 추적자
    public IEnumerator H39(Card card, List<TargetObject> tar)
    {
        TargetObject preLocation;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        yield return tempWait; // 임시 딜레이
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        preLocation = tar[0].ironDemonLocation;
        tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth ++;
        yield return MoveIronDemonCoroutine(tar[0],tar[1]); // 철귀 적으로 이동

        for(int i = 0 ;i < 2 ; i ++)
        {
            yield return GeneralIronDemonAttack(tar[0], tar[1], 2); // 철귀 공격
            GeneralGetDefense(tar[0],tar[0],2,card);
        }
        yield return MoveIronDemonCoroutine(tar[0],preLocation); // 철귀 복귀

        M_TurnManager.instance.AnimIronDemon("Idle",tar[0]); // 아이들 모션 
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
        
    }
    public IEnumerator H39_E(Card card, List<TargetObject> tar)
    {
        yield return H39(card,tar);
    }

    // 신중한 명령
    public IEnumerator H40(Card card, List<TargetObject> tar)
    {
        for(int i = 0 ; i < 3 ; i ++)
            yield return HWAHAP(tar[0]);
        
    }
    public IEnumerator H40_E(Card card, List<TargetObject> tar)
    {
        yield return H40(card,tar);
    }

    //집중 포화
    public IEnumerator H41(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        if(tar[1].objectType == ObjectType.PLAYER)tar[1].GainBuff(BuffType.FLOWERPOWDER,5,false,false,true,tar[0],card);
        else tar[1].GainBuff(BuffType.FLOWERPOWDER,5,true,false,true,tar[0],card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator H41_E(Card card, List<TargetObject> tar)
    {
        yield return H41(card,tar);
    }
    public IEnumerator H42(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        GeneralSingleAttack(tar[0],tar[1],3);
        tar[1].GainBuff(BuffType.FLOWERPOWDER,2,true,false,true,tar[0],card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator H42_E(Card card, List<TargetObject> tar)
    {
        yield return H42(card,tar);
    }

    // 축포
    public IEnumerator H43(Card card, List<TargetObject> tar)
    {
        List<TargetObject> allEnemy = new List<TargetObject>();
        if(tar[1].objectType == ObjectType.PLAYER)
        {
            allEnemy.AddRange(M_TurnManager.instance.spawnedPlayerList);
            M_DimmingManager.instance.StartDimming(allEnemy);
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
            yield return new WaitForSeconds(0.5f);
            foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            {
                target.GainBuff(BuffType.FLOWERPOWDER,3,false,false,true,tar[0],card);
            }
        }
        else
        {
            allEnemy.Add(tar[0]);
            allEnemy.AddRange(M_TurnManager.instance.spawnedMonsterList);
            M_DimmingManager.instance.StartDimming(allEnemy);
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
            yield return new WaitForSeconds(0.5f);
            foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
            {
                target.GainBuff(BuffType.FLOWERPOWDER,3,true,false,true,tar[0],card);
            }
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(allEnemy);
    }
    public IEnumerator H43_E(Card card, List<TargetObject> tar)
    {
        yield return H43(card,tar);
    }

    //이른 봉우리
    public IEnumerator H44(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            target.defense += target.GetBuffValue(BuffType.FLOWERPOWDER)*2;
            target.buffs.Remove(target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
        }
        foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
        {
            GeneralSingleDamage(target,target.GetBuffValue(BuffType.FLOWERPOWDER)*2);
            target.buffs.Remove(target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
    }
    public IEnumerator H44_E(Card card, List<TargetObject> tar)
    {
        yield return H44(card,tar);
    }

    // 뿌리내리기
    public IEnumerator H45(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        if(tar[1].GetBuffValue(BuffType.FLOWERPOWDER) >= 1)GeneralSingleAttack(tar[0],tar[1],14);
        GeneralSingleAttack(tar[0],tar[1],7);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator H45_E(Card card, List<TargetObject> tar)
    {
        yield return H45(card,tar);
    }

    //꽃가루 폭발
    public IEnumerator H46(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        tar[1].GainBuff(BuffType.FLOWERPOWDER,tar[1].GetBuffValue(BuffType.FLOWERPOWDER),false,false,true,tar[0],card);
        
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator H46_E(Card card, List<TargetObject> tar)
    {
        yield return H46(card,tar);
    }

    //환절기
    public IEnumerator H47(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            target.GainBuff(BuffType.FLOWERPOWDER,target.GetBuffValue(BuffType.FLOWERPOWDER),false,false,true,tar[0],card);
        }
        foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
        {
            target.GainBuff(BuffType.FLOWERPOWDER,target.GetBuffValue(BuffType.FLOWERPOWDER),true,false,true,tar[0],card);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
    }
    public IEnumerator H47_E(Card card, List<TargetObject> tar)
    {
        yield return H47(card,tar);
    }

    // 향기로운 꽃
    public IEnumerator H48(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            target.GainBuff(BuffType.FLOWERPOWDER,2,false,false,true,tar[0],card);
        }
        foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
        {
            target.GainBuff(BuffType.FLOWERPOWDER,2,true,false,true,tar[0],card);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
    }
    public IEnumerator H48_E(Card card, List<TargetObject> tar)
    {
        yield return H48(card,tar);
    }

    // 꽃사슬갑옷
    public IEnumerator H49(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        GeneralGetDefense(tar[0],tar[0],8,card);
        tar[0].GainBuff(BuffType.FLOWERPOWDER,2,false,false,true,tar[0],card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H49_E(Card card, List<TargetObject> tar)
    {
        yield return H49(card,tar);
    }

    //기다림의 미학
    public IEnumerator H50(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
        if(tar[1].objectType == ObjectType.PLAYER)
        {
            M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
            yield return new WaitForSeconds(0.5f);
            tar[0].GainBuff(BuffType.FLOWERPOWDER,3,false,false,true,tar[0],card);
        }
        else
        {
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
            yield return new WaitForSeconds(0.5f);
            tar[0].GainBuff(BuffType.FLOWERPOWDER,3,true,false,true,tar[0],card);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator H50_E(Card card, List<TargetObject> tar)
    {
        yield return H50(card,tar);
    }

    // 꽃 줍기
    public IEnumerator H51(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        tar[1].player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "H53")),3);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H51_E(Card card, List<TargetObject> tar)
    {
        yield return H51(card,tar);
    }

    // 꽃 잎
    public IEnumerator H52(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        GeneralSingleDamage(tar[1],tar[1].GetBuffValue(BuffType.FLOWERPOWDER));
        tar[0].player.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand(1);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H52_E(Card card, List<TargetObject> tar)
    {
        yield return H52(card,tar);
    }

    // 독기에 죽어라
    public IEnumerator H53(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        GeneralSingleDamage(tar[1],tar[1].GetBuffValue(BuffType.FLOWERPOWDER)*3);
        yield return new WaitForSeconds(0.3f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H53_E(Card card, List<TargetObject> tar)
    {
        yield return H53(card,tar);
    }

    //개화
    public IEnumerator H54(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(1f);
        foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
        {
            target.GainBuff(BuffType.FLOWER,target.GetBuffValue(BuffType.FLOWERPOWDER),true,false,true,tar[0],card);
            target.buffs.Remove(target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
        }
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            target.GainBuff(BuffType.FLOWER,target.GetBuffValue(BuffType.FLOWERPOWDER),false,false,true,tar[0],card);
            target.buffs.Remove(target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(M_TurnManager.instance.spawnedMonsterList.Concat<TargetObject>(M_TurnManager.instance.spawnedPlayerList).ToList<TargetObject>());
    }
    public IEnumerator H54_E(Card card, List<TargetObject> tar)
    {
        yield return H54(card,tar);
    }

    //동방의 신비
    public IEnumerator H55(Card card, List<TargetObject> tar)
    {
        List<TargetObject> allEnemy = new List<TargetObject>();
        if(tar[1].objectType == ObjectType.PLAYER)
        {
            allEnemy.AddRange(M_TurnManager.instance.spawnedPlayerList);
            M_DimmingManager.instance.StartDimming(allEnemy);
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
            yield return new WaitForSeconds(0.5f);
            foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            {
                target.GainBuff(BuffType.FLOWER,target.GetBuffValue(BuffType.FLOWERPOWDER),false,false,true,tar[0],card);
                target.buffs.Remove(target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
            }
        }
        else
        {
            allEnemy.Add(tar[0]);
            allEnemy.AddRange(M_TurnManager.instance.spawnedMonsterList);
            M_DimmingManager.instance.StartDimming(allEnemy);
            M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
            yield return new WaitForSeconds(0.5f);
            foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
            {
                target.GainBuff(BuffType.FLOWER,target.GetBuffValue(BuffType.FLOWERPOWDER),true,false,true,tar[0],card);
                target.buffs.Remove(target.buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
            }
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(allEnemy);
    }
    public IEnumerator H55_E(Card card, List<TargetObject> tar)
    {
        yield return H55(card,tar);
    }

    // 꽃의 향
    public IEnumerator H56(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        tar[1].GainBuff(BuffType.FLOWER,tar[1].GetBuffValue(BuffType.FLOWERPOWDER),false,false,true,tar[0],card);
        tar[1].buffs.Remove(tar[1].buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H56_E(Card card, List<TargetObject> tar)
    {
        yield return H56(card,tar);
    }

    // 철에 피는 꽃
    public IEnumerator H57(Card card, List<TargetObject> tar)
    {
        List<TargetObject> allTarget= new List<TargetObject>();
        allTarget.Add(tar[0]);
        allTarget.Add(tar[0].ironDemonLocation);
        M_DimmingManager.instance.StartDimming(allTarget);
        M_TurnManager.instance.StartAnimation(tar[0],0,"Attack1",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        tar[0].ironDemonLocation.GainBuff(BuffType.FLOWERPOWDER,3,false,false,true,tar[0],card);
        tar[0].ironDemonLocation.GainBuff(BuffType.FLOWER,tar[1].GetBuffValue(BuffType.FLOWERPOWDER),false,false,true,tar[0],card);
        tar[0].ironDemonLocation.buffs.Remove(tar[1].buffs.Find(buff => buff.type == BuffType.FLOWERPOWDER));
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(allTarget);
    }
    public IEnumerator H57_E(Card card, List<TargetObject> tar)
    {
        yield return H57(card,tar);
    }

    //어버이의 후예들 
    public IEnumerator H58(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        List<TargetObject> targetObjects = new List<TargetObject>();
        targetObjects.Add(tar[0]);
        int number = 0;
        yield return new WaitForSeconds(0.5f);

        List<Card> removableDeck = new List<Card>();

        foreach(Card targetCard in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
        {
            if(targetCard.baseCard.cardNumber.Contains("H0") || targetCard.baseCard.cardNumber.Contains("H1") )
            {
                removableDeck.Add(targetCard);
                targetObjects.Add(M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0,M_TurnManager.instance.spawnedMonsterList.Count)]);
                yield return ExecuteCardCoroutine(targetCard,targetObjects);
                targetObjects.RemoveAt(1);
            }
        }
        foreach(Card removalbleCard in removableDeck)
        {
            tar[0].player.GetComponent<GamePlayerDeck>().trashDeck.Remove(removalbleCard);
            tar[0].player.GetComponent<GamePlayerDeck>().forgottenDeck.Add(removalbleCard);
        }
        Debug.Log(tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Count);
        foreach(Card targetCard in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
        {
            Debug.Log(number++);
            if(targetCard.baseCard.cardNumber.Contains("H0") || targetCard.baseCard.cardNumber.Contains("H1") )
            {
                Debug.Log("Find!");
                removableDeck.Add(targetCard);
                targetObjects.Add(M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0,M_TurnManager.instance.spawnedMonsterList.Count)]);
                yield return ExecuteCardCoroutine(targetCard,targetObjects);
                targetObjects.RemoveAt(1);
            }
        }
        foreach(Card removalbleCard in removableDeck)
        {
            tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Remove(removalbleCard);
            tar[0].player.GetComponent<GamePlayerDeck>().forgottenDeck.Add(removalbleCard);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H58_E(Card card, List<TargetObject> tar)
    {
        yield return H58(card,tar);
    }

    // 제국의 최전선
    public IEnumerator H59(Card card, List<TargetObject> tar)
    {
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.3f);
        // 철귀가 자신에게 없을 경우 자신으로 이동
        if(tar[0].ironDemonLocation != tar[0])
        {
            yield return MoveIronDemonCoroutine(tar[0],tar[0]); // 철귀 적으로 이동
        }
        ChangePosition(tar[0],2); // 전방으로 이동

        foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
        {
            GeneralSingleDamage(target,tar[0].sizeOfIronDemon);
            StartCoroutine(target.monster.OnHitAnimation());
        }
        M_DimmingManager.instance.StopDimming(targets);
    }
    public IEnumerator H59_E(Card card, List<TargetObject> tar)
    {
        yield return H59(card,tar);
    }

    // 홍씨 가문의 명예
    public IEnumerator H60(Card card, List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        tar[0].player.GetComponent<GamePlayerDeck>().currentIchi++;
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator H60_E(Card card, List<TargetObject> tar)
    {
        yield return H60(card,tar);
    }
    

}