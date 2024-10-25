using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;


/*
    에리스 카드중 팝업을 통해 카드를 선택하는 카드들은, 특정 덱에서 N장의 카드를 고를 수 있도록 maxSelectableCardCount 값을 설정하고, 팝업은 해당 카드 수행 후 Rpc를 통해 호출됨.
*/
public partial class CardData : SingletonD<CardData>
{
    private void ErisAnimation(TargetObject tar, string normal)    
	{
        // TODO : 변신상태, 반피상태에 따른 분기처리
        M_TurnManager.instance.StartAnimation(tar, 0, normal, false);
	}

    // 권능 : 찌르기
    public IEnumerator E0(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],5);
        StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E0_E(Card card,List<TargetObject> tar)
    {
        yield return E0(card,tar);
    }

    // 권능 : 깊게 찌르기
    public IEnumerator E1(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],8);
        StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E1_E(Card card,List<TargetObject> tar)
    {
        yield return E1(card,tar);
    }
    // 변형된 팔
    public IEnumerator E2(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],4,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E2_E(Card card,List<TargetObject> tar)
    {
        yield return E2(card,tar);
    }
    // 구원의 팔
    public IEnumerator E3(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[1],3,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E3_E(Card card,List<TargetObject> tar)
    {
        yield return E3(card,tar);
    }
    
    // 부서지세요
    public IEnumerator E4(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],5);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[1].GainBuff(BuffType.BOONGGUI,1,true,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E4_E(Card card,List<TargetObject> tar)
    {
        yield return E4(card,tar);
    }

    // 별 따기
    public IEnumerator E5(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],2);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[0].GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E5_E(Card card,List<TargetObject> tar)
    {
        yield return E5(card,tar);
    }

    // 별의 축복
    public IEnumerator E6(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
		GeneralGetDefense(tar[0],tar[1],5,card);
        tar[1].GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E6_E(Card card,List<TargetObject> tar)
    {
        yield return E6(card,tar);
    }
    // 돌로레
    public IEnumerator E7(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
		tar[0].player.GetComponent<GamePlayerDeck>().CmdSpawnCardOnHand(2);
        tar[0].DamageToPlayer(6);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E7_E(Card card,List<TargetObject> tar)
    {
        yield return E7(card,tar);
    }
    
    // 한번 볼까요
    public IEnumerator E8(Card card,List<TargetObject> tar)
    {
        cardSelectCallBack = E8_CallBack;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
        tar[0].player.GetComponent<GamePlayerDeck>().AddDrawCard(3);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        yield return null;
    }

    public void E8_CallBack(GamePlayerDeck gpd, List<CardOnHand> cards)
    {
        gpd.cardOnHands.Remove(cards[2]);
        gpd.cardOnHands.Remove(cards[1]);
        gpd.forgottenDeck.Add(cards[2].card);
        gpd.forgottenDeck.Add(cards[1].card);
        gpd.TargetCardOnHandRemoveToForgotenDeck(cards[2]);
        gpd.TargetCardOnHandRemoveToForgotenDeck(cards[1]);
    }

    public IEnumerator E8_E(Card card,List<TargetObject> tar)
    {
        yield return E8(card, tar);
    }

    // 권능 : 뒤틀리는 창조
    public IEnumerator E9(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        Card e10Card = new Card(CardData.instance.cards.Find(card => card.cardNumber == "E10"));
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.GenerateCardOnHand(e10Card, 1);
        gamePlayerDeck.deck.Add(e10Card);
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E9_E(Card card,List<TargetObject> tar)
    {
        yield return E9(card, tar);
    }

    // 뒤틀리는 생명
    public IEnumerator E10(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        int hpRecoveryValue = 3;
        int newHP = tar[0].playerHP + hpRecoveryValue;
        if (newHP > tar[0].playerMaxHP){
            int hpDifference = newHP - tar[0].playerMaxHP;
            tar[0].playerHP = tar[0].playerMaxHP;
            foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
                GeneralSingleAttack(tar[0], enemy, hpDifference);
                StartCoroutine(enemy.monster.OnHitAnimation());
            }
        }else{
            tar[0].playerHP = newHP;
        }
        // TODO : 파괴의 권능 효과를 받지 않습니다.
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E10_E(Card card,List<TargetObject> tar)
    {
        yield return E10(card, tar);
    }

    // 별무리꾼
    public IEnumerator E11(Card card,List<TargetObject> tar)
    {
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedPlayerList);
        M_DimmingManager.instance.StartDimming(targets);
        ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
        foreach(TargetObject targetObject in M_TurnManager.instance.spawnedPlayerList){
            targetObject.GainBuff(BuffType.BYEOLMURI, 1, false, false, false, false, targetObject, card);
        }
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(targets);	
    }
    public IEnumerator E11_E(Card card,List<TargetObject> tar)
    {
        yield return E11(card, tar);
    }

    // 권능 : 상실
    public IEnumerator E12(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].playerHP = (int)tar[0].playerHP / 2;
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E12_E(Card card,List<TargetObject> tar)
    {
        yield return E12(card, tar);;
    }

    // 압력 분출
    public IEnumerator E13(Card card,List<TargetObject> tar)
    {
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
        ErisAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.8f);
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
            GeneralSingleAttack(tar[0], enemy, 3);
            StartCoroutine(enemy.monster.OnHitAnimation());
        }
        // TODO : 이 카드가 버린덱 으로 가면 적 전체에게 피해 6을 줍니다.
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E13_E(Card card,List<TargetObject> tar)
    {
        yield return E13(card, tar);
    }

    // 권능 : 찢어 가르기
    public IEnumerator E14(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.8f);
        GeneralSingleAttack(tar[0], tar[1], 20);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        // TODO : 이 카드가 버린덱 으로 가면 무작위 적 한명에게 피해 10 를 줍니다.
		yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E14_E(Card card,List<TargetObject> tar)
    {
        yield return E14(card, tar);
    }

    // 권능 : 파괴
    public IEnumerator E15(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        GeneralSingleAttack(tar[0], tar[1], 3);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        // TODO : 이번 게임동안 사용한 공격 카드의 개수 만큼 2씩 증가합니다.
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E15_E(Card card,List<TargetObject> tar)
    {
        yield return E15(card, tar);
    }

    // 권능 : 붕괴
    public IEnumerator E16(Card card,List<TargetObject> tar)
    {
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
        ErisAnimation(tar[0],"Buff1");
        yield return new WaitForSeconds(0.8f);
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
            enemy.GainBuff(BuffType.BOONGGUI, 2, false, false, false, false, enemy, card);
        }
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E16_E(Card card,List<TargetObject> tar)
    {
        yield return E16(card, tar);
    }

    // 얼마나 버틸까요
    public IEnumerator E17(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        tar[1].defense = 0;
        // TODO : 방어 제거 이펙트
        yield return new WaitForSeconds(0.25f);
        GeneralSingleAttack(tar[0], tar[1], 5);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E17_E(Card card,List<TargetObject> tar)
    {
        yield return E17(card, tar);
    }

    // 아니마토
    public IEnumerator E18(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        int deffence = tar[0].defense;
        tar[0].defense = 0;
        // TODO : 방어 제거 이펙트
        yield return new WaitForSeconds(0.25f);
        tar[0].playerHP += deffence;
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E18_E(Card card,List<TargetObject> tar)
    {
        yield return E18(card, tar);
    }

    // 물귀신
    public IEnumerator E19(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Buff1");
        yield return new WaitForSeconds(0.8f);
        int buffValue = 99;
        tar[0].GainBuff(BuffType.BOONGGUI, buffValue, false, false, false, false, tar[0], card); // TODO : 어떤값인지 몰라서 3 ~ 6번 파라미터 bool값들 임시로 모두 false 처리함.
        tar[1].GainBuff(BuffType.BOONGGUI, buffValue, false, false, false, false, tar[1], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E19_E(Card card,List<TargetObject> tar)
    {
        yield return E19(card, tar);
    }

    // 템페스토소
    public IEnumerator E20(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.TEMPESTOSO, 0, false, false, false, false, tar[0], card);  // TODO : 템페스토소 버프 처리
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E20_E(Card card,List<TargetObject> tar)
    {
        yield return E20(card, tar);
    }

    // 월식
    public IEnumerator E21(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.ECLIPSE, 0, false, false, false, false, tar[0], card); // TODO : 월식 버프 처리
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E21_E(Card card,List<TargetObject> tar)
    {
        yield return E21(card, tar);
    }

    // 기억파편
    public IEnumerator E22(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        yield return null;
    }
    public IEnumerator E22_E(Card card,List<TargetObject> tar)
    {
        yield return E22(card, tar);
    }

    // 삼색빛별
    public IEnumerator E23(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 4;
        GeneralSingleAttack(tar[0], tar[1], damage);
        GeneralGetDefense(tar[0], tar[0], damage, card);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[0].GainBuff(BuffType.BYEOLMURI, 1, false, false, false, false, tar[0], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E23_E(Card card,List<TargetObject> tar)
    {
        yield return E23(card, tar);
    }

    // 도돌이표
    public IEnumerator E24(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.REPEATMARK, 1, false, false, false, false, tar[0], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E24_E(Card card,List<TargetObject> tar)
    {
        yield return E24(card, tar);
    }

    // 닿을 수 없던 꿈
    public IEnumerator E25(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 3;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E25_E(Card card,List<TargetObject> tar)
    {
        yield return E25(card, tar);
    }

    // 델리카토
    public IEnumerator E26(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 12;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        // TODO :  이 카드가 뽑을덱 에서 버린덱 으로 가면 적 전체에게 피해 !8 를 줍니다.
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E26_E(Card card,List<TargetObject> tar)
    {
        yield return E26(card, tar);
    }

    // 공허 속 갈무리
    public IEnumerator E27(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        int damage = gamePlayerDeck.trashDeck.Count; // 데미지는 버린덱의 카드 수 만큼
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E27_E(Card card,List<TargetObject> tar)
    {
        yield return E27(card, tar);
    }

    // 권능 : 관성
    public IEnumerator E28(Card card,List<TargetObject> tar)
    {
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.maxSelectableCardCount = gamePlayerDeck.cardOnHands.Count; // 패의 갯수 만큼 maxSelectableCardCount 설정
        yield return null;
    }
    public IEnumerator E28_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 종말의 징조
    public IEnumerator E29(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.SIGNOFEND, 1, false, false, false, false, tar[0], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E29_E(Card card,List<TargetObject> tar)
    {
        yield return E29(card, tar);
    }

    // 창조의 권능
    public IEnumerator E30(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "E10")), 2);
        // TODO : 이 카드가 뽑을덱 에서 버린덱 으로 가면 E10 카드를 생성해 패에 1장 넣습니다. 
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E30_E(Card card,List<TargetObject> tar)
    {
        yield return E30(card, tar);
    }

    // 티끌 모으기
    public IEnumerator E31(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].player.GetComponent<GamePlayerDeck>().currentIchi++;
        // TODO : 이 카드가 뽑을덱 에서 버린덱 으로 가면 이치 2 를 얻습니다.
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    public IEnumerator E31_E(Card card,List<TargetObject> tar)
    {
        yield return E31(card, tar);
    }

    // 빙산의 일각
    public IEnumerator E32(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 8;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        // TODO :  이 카드가 뽑을덱 에서 버린덱 으로 가면 무작위 적 한명에게 피해 20을 줍니다
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    public IEnumerator E32_E(Card card,List<TargetObject> tar)
    {
        yield return E32(card, tar);
    }

    // 뒤틀림의 끝
    public IEnumerator E33(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E33_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 세포 수집
    public IEnumerator E34(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E34_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 허물 강화
    public IEnumerator E35(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E35_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 별자리 교환
    public IEnumerator E36(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E36_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 파멸의 메아리
    public IEnumerator E37(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        yield return null;
    }
    public IEnumerator E37_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 자비의 권능
    public IEnumerator E38(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E38_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 알레그레토
    public IEnumerator E39(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E39_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 공허 방패
    public IEnumerator E40(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        yield return null;
    }
    public IEnumerator E40_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 혜성 조각
    public IEnumerator E41(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E41_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 제가 대신하죠
    public IEnumerator E42(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E42_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 부탁 드립니다
    public IEnumerator E43(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E43_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 공허를 만지는 자
    public IEnumerator E44(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        yield return null;
    }
    public IEnumerator E44_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 쌍둥이 자리
    public IEnumerator E45(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E45_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 헤일로
    public IEnumerator E46(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E46_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 별무리의 가르침
    public IEnumerator E47(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E47_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 그랜디오소
    public IEnumerator E48(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        yield return null;
    }
    public IEnumerator E48_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 봉제 인형
    public IEnumerator E49(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E49_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 웃는 인형의 단말마
    public IEnumerator E50(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E50_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 아프나요?
    public IEnumerator E51(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E51_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 중력파
    public IEnumerator E52(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        yield return null;
    }
    public IEnumerator E52_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 파멸에게 바치는 공물
    public IEnumerator E53(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E53_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 산개 성단
    public IEnumerator E54(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E54_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 파편 분배
    public IEnumerator E55(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E55_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 은하의 선율
    public IEnumerator E56(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E56_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 거친 파도 처럼
    public IEnumerator E57(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E57_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 별로 빚어진 인형
    public IEnumerator E58(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E58_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 이분법
    public IEnumerator E59(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E59_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }

    // 찢어줄게요
    public IEnumerator E60(Card card,List<TargetObject> tar)
    {
        yield return null;
    }
    public IEnumerator E60_E(Card card,List<TargetObject> tar)
    {
        yield return null;
    }       
}