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
    private void ErisAnimation(TargetObject tar, string animationName)    
	{
        // 변신 상태 분기: GetErisMode()가 광기(V)/분노(Ch) 프리픽스를 붙여 처리
        M_TurnManager.instance.StartAnimation(tar, 0, tar.GetErisMode() + animationName, false);
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
    
    // 부서지세요
    public IEnumerator E4(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.8f);
		GeneralSingleAttack(tar[0],tar[1],3);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[1].GainBuff(BuffType.BOONGGUI,1,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
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
    // 돌로레
    public IEnumerator E7(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.8f);
		tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(2);
        tar[0].LosePlayerHP(6); // 고정 체력 손실 — 방어·증폭 무시 (기획 확정 2026-07-10)
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
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


    // 권능 : 뒤틀리는 창조
    public IEnumerator E9(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        Card e10Card = new Card(CardData.instance.cards.Find(card => card.cardNumber == "E10"));
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.GenerateCardOnHand(e10Card, 1);
        gamePlayerDeck.deck.Add(card);
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
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
            tar[0].HealPlayer(tar[0].playerMaxHP - tar[0].playerHP); // 최대치까지는 회복하고 초과분만 피해로 전환
            foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
                GeneralSingleAttack(tar[0], enemy, hpDifference);
                StartCoroutine(enemy.monster.OnHitAnimation());
            }
        }else{
            tar[0].HealPlayer(hpRecoveryValue);
        }
        // 파괴의권능 미적용은 TargetObject.ApplyPowerOfDestruction에서 E10/E10_E 제외로 처리
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
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
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    // 이 카드가 버린덱 으로 가면 적 전체에게 피해 6을 줍니다.
    public void E13_CallBack(TargetObject playerTargetObject)
    {
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
            GeneralSingleAttack(playerTargetObject, enemy, 6);
            StartCoroutine(enemy.monster.OnHitAnimation());
        }
    }

    // 권능 : 찢어 가르기
    public IEnumerator E14(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.8f);
        GeneralSingleAttack(tar[0], tar[1], 20);
        StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    // 이 카드가 버린덱 으로 가면 무작위 적 한명에게 피해 10 를 줍니다.
    public void E14_CallBack(TargetObject playerTargetObject)
    {
        TargetObject randomTarget = M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0, M_TurnManager.instance.spawnedMonsterList.Count)];
        GeneralSingleAttack(playerTargetObject, randomTarget, 10);
        StartCoroutine(randomTarget.monster.OnHitAnimation());
    }

    // 권능 : 파괴
    public IEnumerator E15(Card card,List<TargetObject> tar)
    {
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        GeneralSingleAttack(tar[0], tar[1], 3 + gamePlayerDeck.numOfUsedAttackCardOnBattle * 2); // 이번 전투 동안 사용한 공격 카드 수만큼 2씩 증가
        StartCoroutine(tar[1].monster.OnHitAnimation());
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
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

    // 얼마나 버틸까요
    public IEnumerator E17(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        tar[1].defense = 0;
        M_EffectManager.instance.RpcEffectEnergyExplosion(tar[1].transform.position); // 방어 파괴 연출
        yield return new WaitForSeconds(0.25f);
        GeneralSingleAttack(tar[0], tar[1], 5);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 아니마토
    public IEnumerator E18(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        int deffence = tar[0].defense;
        tar[0].defense = 0;
        M_EffectManager.instance.RpcEffectEnergyExplosion(tar[0].transform.position); // 방어 제거(회복 전환) 연출
        yield return new WaitForSeconds(0.25f);
        tar[0].HealPlayer(deffence);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 물귀신
    public IEnumerator E19(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Buff1");
        yield return new WaitForSeconds(0.8f);
        int buffValue = 99;
        tar[0].GainBuff(BuffType.BOONGGUI, buffValue, false, false, false, false, tar[0], card); // 붕괴 부여 플래그는 E16 등 기존 관례와 동일 (전수 확인 완료)
        tar[1].GainBuff(BuffType.BOONGGUI, buffValue, false, false, false, false, tar[1], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 템페스토소
    public IEnumerator E20(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.TEMPESTOSO, 1, false, true, false, false, tar[0], card); // 전투 지속 — 발동은 TargetObject.Damage의 잃은 체력 누적에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 월식
    public IEnumerator E21(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.ECLIPSE, 1, false, false, true, false, tar[0], card); // 이번 턴(턴 경계 감쇠) — 발동은 DamageToMonster에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 기억파편
    public IEnumerator E22(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        yield return null;
    }

    // 삼색빛별
    public IEnumerator E23(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 4;
        // 증폭(힘의이치·붕괴·개화·파괴의권능) 반영된 실제 준 피해만큼 방어 획득 — cardDamageDealt 델타로 계산 (기획 확정 2026-07-10)
        int damageDealtBefore = tar[0].cardDamageDealt;
        GeneralSingleAttack(tar[0], tar[1], damage);
        GeneralGetDefense(tar[0], tar[0], tar[0].cardDamageDealt - damageDealtBefore, card);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[0].GainBuff(BuffType.BYEOLMURI, 1, false, false, false, false, tar[0], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 도돌이표
    public IEnumerator E24(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.REPEATMARK, 1, false, false, true, false, tar[0], card); // 이번 턴(턴 경계 감쇠) — 발동은 카드 큐 파이프라인에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
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
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 적 전체에게 피해 8 를 줍니다.
    public void E26_CallBack(TargetObject playerTargetObject)
    {
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
            GeneralSingleAttack(playerTargetObject, enemy, 8);
            StartCoroutine(enemy.monster.OnHitAnimation());
        }
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

    // 권능 : 관성
    public IEnumerator E28(Card card,List<TargetObject> tar)
    {
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.maxSelectableCardCount = gamePlayerDeck.cardOnHands.Count; // 패의 갯수 만큼 maxSelectableCardCount 설정
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 종말의 징조
    public IEnumerator E29(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.SIGNOFEND, 1, false, true, false, false, tar[0], card); // 전투 지속 — 발동은 뽑을덱→버린덱 콜백에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 창조의 권능
    public IEnumerator E30(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        for(int i = 0; i < 2; i++)
            tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Add(new Card(CardData.instance.cards.Find(c => c.cardNumber == "E10")));
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 E10 카드를 생성해 패에 1장 넣습니다.
    public void E30_CallBack(TargetObject playerTargetObject)
    {
        Card e10Card = new Card(CardData.instance.cards.Find(card => card.cardNumber == "E10"));
        GamePlayerDeck gamePlayerDeck = playerTargetObject.player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.GenerateCardOnHand(e10Card, 1);
    }

    // 티끌 모으기
    public IEnumerator E31(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].player.GetComponent<GamePlayerDeck>().currentIchi++;
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 이치 2 를 얻습니다.
    public void E31_CallBack(TargetObject playerTargetObject)
    {
        playerTargetObject.player.GetComponent<GamePlayerDeck>().currentIchi += 2;
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
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 무작위 적 한명에게 피해 20을 줍니다
    public void E32_CallBack(TargetObject playerTargetObject)
    {
        TargetObject randomTarget = M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0, M_TurnManager.instance.spawnedMonsterList.Count)];
        GeneralSingleAttack(playerTargetObject, randomTarget, 20);
        StartCoroutine(randomTarget.monster.OnHitAnimation());
    }

    // 뒤틀림의 끝
    public IEnumerator E33(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.ENDOFDISTORTION,1,false,true,false,false,tar[0],card); // 전투 지속 — 발동은 GeneralGetDefense에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 세포 수집
    public IEnumerator E34(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 2;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        Card e10Card = new Card(CardData.instance.cards.Find(card => card.cardNumber == "E10"));
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        gamePlayerDeck.GenerateCardOnHand(e10Card, 1);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 허물 강화
    public IEnumerator E35(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.ENHANCESKIN,1,false,false,true,false,tar[0],card); // 이번 턴(턴 경계 감쇠) — 발동은 HealPlayer에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 별자리 교환
    public IEnumerator E36(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        // 전열과 후열 스왑
        M_TurnManager.instance.SwapPlayerOrder(0, 2);

        // 전열과 후열에 별무리 1 부여
        uint frontPlayerNetId = M_TurnManager.instance.playerOrder[2];
        if(frontPlayerNetId != 0){
            GamePlayer frontPlayer = NetLookup.Server<GamePlayer>(frontPlayerNetId);
            TargetObject frontTargetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(frontPlayer);
            frontTargetObject.GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,frontTargetObject,card);
        }
        uint backPlayerNetId = M_TurnManager.instance.playerOrder[0];
        if(backPlayerNetId != 0){
            GamePlayer backPlayer = NetLookup.Server<GamePlayer>(backPlayerNetId);
            TargetObject backTargetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(backPlayer);
            backTargetObject.GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,backTargetObject,card);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 파멸의 메아리
    public IEnumerator E37(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.8f);
        for(int i=0; i<6; i++){
            int damage = 1;
            GeneralSingleAttack(tar[0], tar[1], damage);
            StartCoroutine(tar[1].monster.OnHitAnimation());
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 간다면 무작위 적 한명에게 피해 2를 7번 줍니다.
    public IEnumerator E37_CallBack(TargetObject playerTargetObject)
    {
        TargetObject randomTarget = M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0, M_TurnManager.instance.spawnedMonsterList.Count)];
        for(int i=0; i<7; i++){
            GeneralSingleAttack(playerTargetObject, randomTarget, 2);
            StartCoroutine(randomTarget.monster.OnHitAnimation());
            yield return new WaitForSeconds(0.15f);
        }
    }

    // 자비의 권능
    public IEnumerator E38(Card card,List<TargetObject> tar)
    {
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        
        string extractCardNumber = "E10";
        string extractCardNumberEnhance = "E10_E";

        // 패에 있는 E10 카드 모두 잊혀진 덱으로
        List<Card> cardsFromCardOnHand = new List<Card>();
        for(int i=gamePlayerDeck.cardOnHands.Count-1; i>=0; i--){
            Card c = gamePlayerDeck.cardOnHands[i].card;
            if(c.baseCard.cardNumber.Equals(extractCardNumber) || c.baseCard.cardNumber.Equals(extractCardNumberEnhance)){
                cardsFromCardOnHand.Add(c);
                gamePlayerDeck.forgottenDeck.Add(c);
                NetworkServer.Destroy(gamePlayerDeck.cardOnHands[i].gameObject);
                gamePlayerDeck.cardOnHands.Remove(gamePlayerDeck.cardOnHands[i]);
            }
        }
        gamePlayerDeck.TargetSendDeck(cardsFromCardOnHand, DeckListType.NONE, DeckListType.FORGOTTEN_DECK);
        
        // 뽑을 덱에 있는 E10 카드 모두 잊혀진 덱으로
        List<Card> cardsFromPrefarDeck = new List<Card>();
        for(int i=gamePlayerDeck.prefareDeck.Count-1; i>=0; i--){
            Card c = gamePlayerDeck.prefareDeck[i];
            if(c.baseCard.cardNumber.Equals(extractCardNumber) || c.baseCard.cardNumber.Equals(extractCardNumberEnhance)){
                cardsFromPrefarDeck.Add(c);
                gamePlayerDeck.forgottenDeck.Add(c);
                gamePlayerDeck.prefareDeck.Remove(c);
            }
        }
        gamePlayerDeck.TargetSendDeck(cardsFromPrefarDeck, DeckListType.PREFARE_DECK, DeckListType.FORGOTTEN_DECK);

        // 버린 덱에 있는 E10 카드 모두 잊혀진 덱으로
        List<Card> cardsFromTrashDeck = new List<Card>();
        for(int i=gamePlayerDeck.trashDeck.Count-1; i>=0; i--){
            Card c = gamePlayerDeck.trashDeck[i];
            if(c.baseCard.cardNumber.Equals(extractCardNumber) || c.baseCard.cardNumber.Equals(extractCardNumberEnhance)){
                cardsFromTrashDeck.Add(c);
                gamePlayerDeck.forgottenDeck.Add(c);
                gamePlayerDeck.trashDeck.Remove(c);
            }
        }
        gamePlayerDeck.TargetSendDeck(cardsFromTrashDeck, DeckListType.TRASH_DECK, DeckListType.FORGOTTEN_DECK);

        // 보낸 카드 1장당 '보낸 카드 개수'만큼 아군 전체 회복 — 총 회복량 = 개수² (기획 확정 2026-07-10: N = 잊혀진덱으로 보낸 카드 개수)
        List<Card> mergedList = new List<Card>(cardsFromCardOnHand);
        mergedList.AddRange(cardsFromPrefarDeck);
        mergedList.AddRange(cardsFromTrashDeck);
        int recoveryValue = mergedList.Count * mergedList.Count;
        foreach(TargetObject targetObject in M_TurnManager.instance.spawnedPlayerList){
            targetObject.HealPlayer(recoveryValue);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 알레그레토
    public IEnumerator E39(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        // 사용 시점에 이미 붕괴를 보유한 적에게만 추가 피해 — 부여 '전' 판정 (기획 확정 2026-07-10)
        bool hadBoonggui = tar[1].HasBuff(BuffType.BOONGGUI);
        tar[1].GainBuff(BuffType.BOONGGUI,1,true,false,true,false,tar[1],card);
        if(hadBoonggui){
            int damage = 10;
            GeneralSingleAttack(tar[0], tar[1], damage);
            StartCoroutine(tar[1].monster.OnHitAnimation());
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 공허 방패
    public IEnumerator E40(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        int value = 8;
        GeneralGetDefense(tar[0],tar[0],value,card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 아군 전체에게 방어 8을 부여합니다.
    public void E40_CallBack(TargetObject playerTargetObject, Card card)
    {
        foreach(TargetObject targetObject in M_TurnManager.instance.spawnedPlayerList){
            GeneralGetDefense(playerTargetObject,targetObject,8,card);
        }
    }

    // 혜성 조각
    public IEnumerator E41(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 5;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[1].GainBuff(BuffType.BOONGGUI,1,true,false,true,false,tar[1],card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 제가 대신하죠
    public IEnumerator E42(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        int value = 4;
        GeneralGetDefense(tar[0],tar[0],value,card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 부탁 드립니다
    public IEnumerator E43(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 6;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        yield return new WaitForSeconds(0.5f);
    }

    // 공허를 만지는 자
    public IEnumerator E44(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        yield return null;
    }

    // 쌍둥이 자리
    public IEnumerator E45(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        MovePosition(tar[0],tar[1]);
        tar[0].GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,tar[0],card);
        tar[1].GainBuff(BuffType.BYEOLMURI,1,false,false,false,false,tar[0],card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 헤일로
    public IEnumerator E46(Card card,List<TargetObject> tar)
    {
        GamePlayerDeck gamePlayerDeck = tar[0].player.GetComponent<GamePlayerDeck>();
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
        ErisAnimation(tar[0],"Attack2");
        yield return new WaitForSeconds(0.8f);
        int dmamage = 1 + gamePlayerDeck.e46DamageBonus; // 이 이름의 카드 누적 피해 증가 반영
        foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
            GeneralSingleAttack(tar[0], enemy, dmamage);
            StartCoroutine(enemy.monster.OnHitAnimation());
        }
        gamePlayerDeck.deck.Add(card);
        gamePlayerDeck.e46DamageBonus++; // 이 이름의 카드의 피해 1 증가 (전투 지속)
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 별무리의 가르침
    public IEnumerator E47(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 5;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1);
        if(tar[0].playerHP <= (tar[0].playerMaxHP / 2)){
            tar[0].GainBuff(BuffType.BYEOLMURI,1,true,false,true,false,tar[0],card);
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 그랜디오소
    public IEnumerator E48(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 8;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        // TODO :  이 카드가 뽑을덱 에서 버린덱 으로 가면 피해는 N배가 증가합니다.
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 봉제 인형
    public IEnumerator E49(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        int value = tar[0].playerMaxHP - tar[0].playerHP;
        foreach(TargetObject targetObjectPlayer in M_TurnManager.instance.spawnedPlayerList){
            if(targetObjectPlayer.netId != tar[0].netId){
                GeneralGetDefense(tar[0],targetObjectPlayer,value,card);  // 잃은 체력 만큼 자신을 제외한 아군 전체 방어 부여
            }
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 웃는 인형의 단말마
    public IEnumerator E50(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.DEATHTHROES, 1, false, true, false, false, tar[0], card); // 전투 지속 — 받는 피해 2배는 DamageToPlayer, 파괴의권능 +1배는 ApplyPowerOfDestruction에서
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 아프나요?
    public IEnumerator E51(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 2;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 8;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        // TODO : 이 카드는 파괴의 권능 효과를 N + 1배 더 받습니다.
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 중력파
    public IEnumerator E52(Card card,List<TargetObject> tar)
    {
        tar[0].player.GetComponent<GamePlayerDeck>().maxSelectableCardCount = 1;
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.8f);
        int damage = 1;
        for(int i=0; i<5; i++){
            GeneralSingleAttack(tar[0], tar[1], damage);
            StartCoroutine(tar[1].monster.OnHitAnimation());
            yield return new WaitForSeconds(0.15f);
        }
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 무작위 적 한명에게 피해 1을 9번 줍니다.
    public IEnumerator E52_CallBack(TargetObject playerTargetObject)
    {
        yield return new WaitForSeconds(0.5f);
        TargetObject randomTarget = M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0, M_TurnManager.instance.spawnedMonsterList.Count)];
        for(int i=0; i<9; i++){
            GeneralSingleAttack(playerTargetObject, randomTarget, 1);
            StartCoroutine(randomTarget.monster.OnHitAnimation());
            yield return new WaitForSeconds(0.15f);
        }
    }

    // 파멸에게 바치는 공물
    public IEnumerator E53(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        // 카드를 한장 뽑습니다.
        tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1, (spawnedCardOnHand) => {
            if(spawnedCardOnHand.baseCard.cardType == CardType.ATTACK){
                tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1); // 뽑은 카드가 공격 카드일 경우 한번 더 반복합니다.
            }
        });
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 산개 성단
    public IEnumerator E54(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 5;
        int repeatCount = tar[0].player.GetComponent<GamePlayerDeck>().numOfUsedAttackCardOnTurn; // 이번 턴에 쓴 공격 카드 수 (이 카드 자신 제외 — 카운터는 실행 후 증가)
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        for(int i = 0; i < repeatCount; i++)
        {
            if(tar[1].isDying) break;
            yield return new WaitForSeconds(0.2f);
            GeneralSingleAttack(tar[0], tar[1], damage);
            StartCoroutine(tar[1].monster.OnHitAnimation());
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 파편 분배
    public IEnumerator E55(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 10;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        int shieldValue = 2;
        GeneralGetDefense(tar[0],tar[0],shieldValue,card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 은하의 선율
    public IEnumerator E56(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        for(int i = 0; i < 2; i++)
        {
            GeneralSingleAttack(tar[0], tar[1], 3);
            StartCoroutine(tar[1].monster.OnHitAnimation());
            yield return new WaitForSeconds(0.2f);
        }
        int shieldValue = 3;
        GeneralGetDefense(tar[0],tar[0],shieldValue,card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
    // 이 카드가 뽑을덱 에서 버린덱 으로 가면 방어 15 를 얻습니다.
    public void E56_CallBack(TargetObject playerTargetObject, Card card)
    {
        GeneralGetDefense(playerTargetObject, playerTargetObject, 15, card);
    }

    // 거친 파도 처럼
    public IEnumerator E57(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.8f);
        int damage = 14;
        GeneralSingleAttack(tar[0], tar[1], damage);
        StartCoroutine(tar[1].monster.OnHitAnimation());
        int shieldValue = 5;
        GeneralGetDefense(tar[0],tar[0],shieldValue,card);
        // 은하수 카드당 비용 감소는 GamePlayerDeck.GetTotalCostOfCardOnHand에서 상시 적용
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }

    // 별로 빚어진 인형
    public IEnumerator E58(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        tar[0].GainBuff(BuffType.BYEOLMURI, 3, false, false, false, false, tar[0], card);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 이분법
    public IEnumerator E59(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		ErisAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.8f);
        int dichotomyIndex = tar[0].GainBuff(BuffType.DICHOTOMY, 1, false, true, false, false, tar[0], card);
        tar[0].buffTrunBeginEffect.Add(dichotomyIndex, E59_TurnBeginEffect);
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
    }

    // 이분법: 턴 시작 시 체력 2 소모 + 뒤틀리는 생명(E10) 1장을 패에 생성
    public IEnumerator E59_TurnBeginEffect(TargetObject target, int index, Card card)
    {
        target.StaticDamageToPlayer(2);
        target.player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(c => c.cardNumber == "E10")), 1);
        yield return null;
    }

    // 찢어줄게요
    public IEnumerator E60(Card card,List<TargetObject> tar)
    {
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		ErisAnimation(tar[0],"Attack1");
        int damage = 8;
        // 반복 조건: 변신(MAD) 3회 > 체력 절반 이하 2회 > 기본 1회
        // (종전에는 '절반 이하' 조건이 실플레이에서 설정되지 않는 ErisMode.ANGER에 묶여 있어 발동 불가였음)
        int repeatCount;
        if(tar[0].erisMode == ErisMode.MAD)
            repeatCount = 3;
        else if(tar[0].playerHP <= tar[0].playerMaxHP / 2)
            repeatCount = 2;
        else
            repeatCount = 1;
        for(int i=0; i<repeatCount; i++){
            yield return new WaitForSeconds(0.25f);
            GeneralSingleAttack(tar[0], tar[1], damage);
            StartCoroutine(tar[1].monster.OnHitAnimation());
        }
        yield return new WaitForSeconds(0.5f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
    }
}