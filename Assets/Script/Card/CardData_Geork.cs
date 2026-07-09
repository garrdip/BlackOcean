using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;
using System;
using ProjectD;
using Mirror;
using Spine.Unity;
using Steamworks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEditor;
using TMPro;

public partial class CardData : SingletonD<CardData>
{
	public delegate void CardCallBack();
	// 게오르크
	private int GetAdditionalValueOfGisadoAction(List<TargetObject> tar)
	{
		return tar[0].buffs.FindIndex(buff => buff.type == BuffType.UGLYKNIGHT) == -1 ? 0 : 1;
	}

	private void GeorkAnimation(TargetObject tar, string normal)    
	{
		M_TurnManager.instance.StartAnimation(tar,0,tar.isTransformed? "H" + normal : normal, false);
	}

	public bool IsGISADO(List<TargetObject> tar)
    {
		int currentIndex = M_TurnManager.instance.playerOrder.FindIndex(x => x == tar[0].player.netId);
		foreach(TargetObject target in M_TurnManager.instance.GetTargetObjectFromActionTarget(tar[1].monster.nextTarget))
		{
			if(target == tar[0])return true;
		}
        return false;
    }

	public IEnumerator G0(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		for(int i = 0 ;i < 3 + GetAdditionalValueOfGisadoAction(tar) ; i++)
		{
			GeorkAnimation(tar[0],"Attack0");
			yield return new WaitForSeconds(0.5f);
			GeneralSingleAttack(tar[0],tar[1],9);
			StartCoroutine(tar[1].monster.OnHitAnimation());
			yield return new WaitForSeconds(0.5f);
			if(!IsGISADO(tar))break;
		}
		if(IsGISADO(tar) && tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	public IEnumerator G0_Effect(TargetObject tar)
	{
		if(tar.defense >= 10)
			tar.defense -= 10;
		else
			tar.defense = 0;
		yield return null;
	}
	public IEnumerator G0_H(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		tar[1].defense = 0;
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],30);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}
	public IEnumerator G1(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],15,card);
		yield return new WaitForSeconds(0.5f);
		tar[0].buffs.Remove(tar[0].buffs.Find(buff => buff.type == BuffType.GOHANG2_DEBUFF));
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	public IEnumerator G1_Effect(TargetObject tar)
	{
		tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,tar,null);
		yield return null;
	}
	public IEnumerator G1_H(Card card,List<TargetObject> tar) 
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		for(int i = tar[0].buffs.Count - 1 ; i >= 0 ; i--)
		{
			if(tar[0].buffs[i].isDebuff)
				tar[0].buffs[i].value -= 1;
			if(tar[0].buffs[i].value == 0)tar[0].buffs.RemoveAt(i);
		}
		GeneralGetDefense(tar[0],tar[0],20,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}
	public IEnumerator G2(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.GOHANG3,1,false,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		tar[0].buffs.Remove(tar[0].buffs.Find(buff => buff.type == BuffType.GOHANG3_DEBUFF));
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
		yield return null;
	}
	public IEnumerator G2_Effect(TargetObject tar)
	{
		yield return null;
		//tar.GainBuff(BuffType.GOHANG3_DEBUFF,1,true,false,false,false,tar,null);
	}
	public IEnumerator G2_H(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		foreach(CardOnHand cardOnHand in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
			cardOnHand.card.costAddition -= 1;
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}
	public IEnumerator G3(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],4);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}
	public IEnumerator G3_Effect(TargetObject tar)
	{
		tar.StaticDamageToPlayer(3);
		yield return null;
	}
	public IEnumerator G3_H(Card card,List<TargetObject> tar)
	{
		int totalDamage = 6;
		foreach(Card deckcard in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
			if(deckcard.baseCard.cardType == CardType.HERO)
				totalDamage += 2;
		foreach(Card deckcard in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
			if(deckcard.baseCard.cardType == CardType.HERO)
				totalDamage += 2;
		foreach(CardOnHand deckcard in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
			if(deckcard.card.baseCard.cardType == CardType.HERO)
				totalDamage += 2;
				
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],totalDamage);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}
	public IEnumerator G4(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],6,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	public IEnumerator G4_Effect(TargetObject tar)
	{
		tar.GainBuff(BuffType.ICHI_ATTACK,-1,false,false,false,false,tar,null);
		yield return null;
	}
	public IEnumerator G4_H(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff1");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.ICHI_DEFENSE,2,false,false,false,false,tar[0],card);
		GeneralGetDefense(tar[0],tar[0],18,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}
	public IEnumerator G5(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[1].GainBuff(BuffType.SOIRAK,1,true,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}
	public IEnumerator G5_Effect(TargetObject tar)
	{
		tar.GainBuff(BuffType.BOONGGUI,1,true,false,true,false,tar,null);
		yield return null;
	}

	public IEnumerator G5_H(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Buff1");
		yield return new WaitForSeconds(0.5f);
		tar[1].GainBuff(BuffType.SOIRAK,2,true,true,true,false,tar[0],card);
		tar[1].GainBuff(BuffType.APDO,3,true,true,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 흔들리는 신념 ( 이 카드는 존재하지 않습니다. )
	public IEnumerator G6(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6_Effect(TargetObject tar)
	{
		yield return null;
	}
	public IEnumerator G6_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6_H(Card card,List<TargetObject> tar)
	{
		yield return null;
	}
	public IEnumerator G6_H_E(Card card,List<TargetObject> tar)
	{
		yield return null;
	}

	// 굳건한 신념
	public IEnumerator G7(Card card,List<TargetObject> tar)
	{
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false);
        yield return new WaitForSeconds(0.5f);
        tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1);
        yield return new WaitForSeconds(1f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	public IEnumerator G7_Effect(TargetObject tar)
	{
		tar.StaticDamageToPlayer(2);
		yield return null;
	}
	public IEnumerator G7_H(Card card,List<TargetObject> tar)
	{
        M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        M_TurnManager.instance.StartAnimation(tar[0],0,"Buff0",false); // 단향이 공격 모션 
        yield return new WaitForSeconds(0.5f);
        tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(3);
        tar[0].player.GetComponent<GamePlayerDeck>().currentIchi += 2;
        yield return new WaitForSeconds(1f);
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 가르기
	public IEnumerator G8(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],7);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		M_EffectManager.instance.RpcEffectSwordSlash(tar[1].transform.position, false);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
		yield return null;
	}

	// 찍기
	public IEnumerator G9(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],9);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		M_EffectManager.instance.RpcEffectSwordSting(tar[1].transform.position);
		tar[1].GainBuff(BuffType.SOIRAK,1,true,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
		yield return null;
	}

	// 막기
	public IEnumerator G10(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],6,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
		yield return null;
	}

	// 막겠다
	public IEnumerator G11(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff1");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],4,card);
		GeneralGetDefense(tar[0],tar[1],4,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 덤벼라
	public IEnumerator G12(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		for(int i = 0 ;i < 2 + GetAdditionalValueOfGisadoAction(tar) ; i ++)
		{
			if(IsGISADO(tar))GeorkAnimation(tar[0],"Attack2");
				else GeorkAnimation(tar[0],"Attack0");
			yield return new WaitForSeconds(0.5f);
			GeneralSingleAttack(tar[0],tar[1],8);
			StartCoroutine(tar[1].monster.OnHitAnimation());
			yield return new WaitForSeconds(0.5f);
			if(!IsGISADO(tar)) break;
		}
		if(IsGISADO(tar) && tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 뭉개주마
	public IEnumerator G13(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],8);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		tar[1].GainBuff(BuffType.APDO,1,true,false,false,true,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 회전 베기
	public IEnumerator G14(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
		M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
			GeneralSingleAttack(tar[0],enemy,6);
			StartCoroutine(enemy.monster.OnHitAnimation());
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 절단
	public IEnumerator G15(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		tar[1].GainBuff(BuffType.SOIRAK,2,false,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 성흔의 고통 : 성흔(G3)
	public IEnumerator G16(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Add(new Card(CardData.instance.cards.Find(card => card.cardNumber == "G3")));
		tar[0].player.GetComponent<GamePlayerDeck>().GenerateCardOnHand(new Card(CardData.instance.cards.Find(card => card.cardNumber == "G3")), 1);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}


	// 북방의 위대한 투사
	public IEnumerator G17(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.5f);
        int index = tar[0].GainBuff(BuffType.BOOKBANG,1, false, true, false, false, tar[0],card);
        tar[0].buffTrunBeginEffect.Add(index,G17_Buff_Effect);
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	public IEnumerator G17_Buff_Effect(TargetObject tar, int index,Card card)
	{
		tar.GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar,null);
		yield return null;
	}


	// 연격 준비
	public IEnumerator G18(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],8);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1); 
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 고통의 메아리 ( 1. 중첩됨, 2. 버프 등록시 효과)
	public IEnumerator G19(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
        yield return new WaitForSeconds(0.5f);
        int index = tar[0].GainBuff(BuffType.GOTONG,0, false, true, false, false, tar[0],card);
        tar[0].buffCardUseEffect.Add(index,G19_Buff_Effect);
        yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}
	public IEnumerator G19_Buff_Effect(TargetObject tar, int index,Card card)
	{
		if(card.baseCard.cardType == CardType.CURSE)
			foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
				enemy.StaticDamageToMonster(3);
		yield return null;
	}

	//선의 투사
	public IEnumerator G20(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],10);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
		{
			for(int i = 0 ;i < 1 + GetAdditionalValueOfGisadoAction(tar);i++)
			foreach(TargetObject friend in M_TurnManager.instance.spawnedPlayerList)
				GeneralGetDefense(tar[0],friend,6,card);
			if(tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 범접할 수 없는 힘
	public IEnumerator G21(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],11,3);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 찌르고 막기
	public IEnumerator G22(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],5);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
		{
			for(int i = 0 ;i < 1 + GetAdditionalValueOfGisadoAction(tar);i++)
				GeneralGetDefense(tar[0],tar[0],15,card);
			if(tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 황금빛 갑주
	public IEnumerator G23(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],15,card);
		tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Add(new Card(CardData.instance.cards.Find(card => card.cardNumber == "G4")));
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//차오르는 투지
	public IEnumerator G24(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],5);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
		{
			for(int i = 0 ;i < 1 + GetAdditionalValueOfGisadoAction(tar);i++)
				tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
			if(tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//저추조차 찬란하다
	public IEnumerator G25(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
        GeorkAnimation(tar[0],"Buff1");
        yield return new WaitForSeconds(0.5f);
        tar[0].GainBuff(BuffType.BRILLIANTCURSE,1,false,true,false,false,tar[0],card);
        foreach(Card pCard in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
        {
            if(pCard.baseCard.cardType == CardType.CURSE)
                pCard.tempEnhanced = true;
        }
        foreach(Card pCard in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
        {
            if(pCard.baseCard.cardType == CardType.CURSE)
                pCard.tempEnhanced = true;
        }
        foreach(CardOnHand pCard in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
        {
            if(pCard.card.baseCard.cardType == CardType.CURSE)
            {
                pCard.card.tempEnhanced = true;
                pCard.OnChangeCardData(pCard.card,pCard.card);
            }
        }
        M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
        yield return new WaitForSeconds(0.2f);
	}


	// 쐐기박기
	public IEnumerator G26(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		int addDamage = tar[1].GetBuffValue(BuffType.APDO) * 4;
		tar[1].buffs.Remove(tar[1].buffs.Find(buff => buff.type == BuffType.APDO));
		GeneralSingleAttack(tar[0],tar[1],4 + addDamage);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 한번에 처리하마
	public IEnumerator G27(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
		M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
		{
			GeneralSingleAttack(tar[0],target,6);
			M_EffectManager.instance.RpcEffectSwordSlash(target.transform.position, true);
			StartCoroutine(target.monster.OnHitAnimation());
			target.GainBuff(BuffType.APDO,1,true,false,false,true,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	//저주자의 울부짖음
	public IEnumerator G28(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
		M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		// 소유중인 저주카드 카운트
		int addDamage = 0;
		foreach(Card item in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
			if(item.baseCard.cardType == CardType.CURSE)addDamage += 3;
		foreach(Card item in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
			if(item.baseCard.cardType == CardType.CURSE)addDamage += 3;
		foreach(CardOnHand item in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
			if(item.card.baseCard.cardType == CardType.CURSE)addDamage += 3;

		foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
		{
			GeneralSingleAttack(tar[0],target,6 + addDamage);
			StartCoroutine(target.monster.OnHitAnimation());
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	//두려운가?
	public IEnumerator G29(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],10);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
		{
			for(int i = 0 ;i < 1 + GetAdditionalValueOfGisadoAction(tar);i++)
				tar[1].GainBuff(BuffType.APDO,2,true,false,false,true,tar[0],card);
			if(tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//추하기에 기사답게
	public IEnumerator G30(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.UGLYKNIGHT,0,false,true,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 고통 전가
	public IEnumerator G31(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		// 패에 있는 저주카드만 카운트 (설명: "내 패에 있는 @이치의저주 카드 당")
		int addDamage = 0;
		foreach(CardOnHand item in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
			if(item.card.baseCard.cardType == CardType.CURSE)addDamage += 8;

		GeneralSingleAttack(tar[0],tar[1],addDamage);
		StartCoroutine(tar[1].monster.OnHitAnimation());

		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 약점 찌르기
	public IEnumerator G32(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],8);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
		{
			for(int i = 0 ;i < 1 + GetAdditionalValueOfGisadoAction(tar);i++)
				tar[1].GainBuff(BuffType.SOIRAK,2,true,false,false,true,tar[0],card);
			if(tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 대신하지
	public IEnumerator G33(Card card,List<TargetObject> tar)
	{
		ChangePosition(tar[0],M_TurnManager.instance.playerOrder.FindIndex(x => x == tar[1].player.netId));
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],8,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 내가 가마
	public IEnumerator G34(Card card,List<TargetObject> tar)
	{
		MovePosition(true,tar[0]);
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],7,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 맡기지
	public IEnumerator G35(Card card,List<TargetObject> tar)
	{
		MovePosition(false,tar[0]);
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],5);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 내 눈을 봐라
	public IEnumerator G36(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff1");
		yield return new WaitForSeconds(0.5f);
		tar[1].GainBuff(BuffType.APDO,2,true,false,false,true,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 비켜라
	public IEnumerator G37(Card card,List<TargetObject> tar)
	{
		ChangePosition(tar[0],M_TurnManager.instance.playerOrder.FindIndex(x => x == tar[1].player.netId));
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.ICHI_ATTACK,2,false,false,false,false,tar[0],card);		
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 절대 강자
	public IEnumerator G38(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.ABSOLUTEDOMINATOR,0,false,true,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 남아있는 한손
	public IEnumerator G39(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack1");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],4);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
		{
			for(int i = 0 ;i < 1 + GetAdditionalValueOfGisadoAction(tar);i++)
				tar[0].player.GetComponent<GamePlayerDeck>().currentIchi++;
			if(tar[0].HasBuff(BuffType.ABSOLUTEDOMINATOR))tar[0].GainBuff(BuffType.ICHI_ATTACK,1,false,false,false,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//신들린 연격
	public IEnumerator G40(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		for(int i = 0 ; i < 5 ; i ++)
		{
			GeneralSingleAttack(tar[0],tar[1],2);
			StartCoroutine(tar[1].monster.OnHitAnimation());
			M_EffectManager.instance.RpcEffectSwordSlash(tar[1].transform.position, true);
			yield return new WaitForSeconds(0.2f);
		}
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//풍차 베기
	public IEnumerator G41(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
		M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		for(int i = 0 ; i < 2 ; i ++)
		{
			foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
				GeneralSingleAttack(tar[0],enemy,3);
				M_EffectManager.instance.RpcEffectSwordSlash(enemy.transform.position, true);
				StartCoroutine(enemy.monster.OnHitAnimation());
			}
			yield return new WaitForSeconds(0.2f);
		}
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 눈먼 공격
	public IEnumerator G42(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
		M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		for(int i = 0 ; i < 3 ; i ++)
		{
			TargetObject randomTarget = M_TurnManager.instance.spawnedMonsterList[UnityEngine.Random.Range(0,M_TurnManager.instance.spawnedMonsterList.Count)];
			M_EffectManager.instance.RpcEffectSwordSlash(randomTarget.transform.position, true);
			GeneralSingleAttack(tar[0], randomTarget, 4);
			StartCoroutine(randomTarget.monster.OnHitAnimation());
			yield return new WaitForSeconds(0.2f);
		}
		M_DimmingManager.instance.StopDimming(targets);
	}

	// 난도질
	public IEnumerator G43(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
		M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
		{
			GeneralSingleAttack(tar[0],enemy,4);
			StartCoroutine(enemy.monster.OnHitAnimation());
			enemy.GainBuff(BuffType.SOIRAK,1,true,false,true,false,tar[0],card);
		}
		yield return new WaitForSeconds(0.2f);
		M_DimmingManager.instance.StopDimming(targets);
	}

	//자비란 없다
	public IEnumerator G44(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.THEREISNOJABI,3,false,false,false,true,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}


	// 준비 자세
	public IEnumerator G45(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		GeneralGetDefense(tar[0],tar[0],7,card);
		tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	// 날개 감싸기
	public IEnumerator G46(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.WRAPWINGS,1,false,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	// 기도
	public IEnumerator G47(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[1].GainBuff(BuffType.ICHI_ATTACK,2,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));	
	}

	//본보기
	public IEnumerator G48(Card card,List<TargetObject> tar)
	{
        List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],4);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],4);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList)
			if(enemy != tar[1])enemy.GainBuff(BuffType.SOIRAK,1,true,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(targets);	
	}

	// 빈틈없는 자세
	public IEnumerator G49(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.CLOSEPOSE,1,false,false,true,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	// 게오르크라는 이름
	public IEnumerator G50(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],15);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Add(new Card(CardData.instance.cards.Find(card => card.cardNumber == "G5")));
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	// 노병의 지혜
	public IEnumerator G51(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].GainBuff(BuffType.WISDOMOFOLDSOLDIER,5,false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	// 맺어지는 열매
	public IEnumerator G52(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		int addDamage = 6;
		foreach(Card item in tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck)
			if(item.baseCard.cardType == CardType.CURSE || item.baseCard.cardType == CardType.HERO)addDamage += 3;
		foreach(Card item in tar[0].player.GetComponent<GamePlayerDeck>().trashDeck)
			if(item.baseCard.cardType == CardType.CURSE || item.baseCard.cardType == CardType.HERO)addDamage += 3;
		foreach(CardOnHand item in tar[0].player.GetComponent<GamePlayerDeck>().cardOnHands)
			if(item.card.baseCard.cardType == CardType.CURSE || item.card.baseCard.cardType == CardType.HERO)addDamage += 3;

		GeneralGetDefense(tar[0],tar[0],addDamage,card);

		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	//전략 수정 // 수정 필요 !!! TODO !!
	public IEnumerator G53(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(2); // 패 2개 생성
		tar[0].player.GetComponent<GamePlayerDeck>().maxRemoveCardCount = 2; // 제거용 카드 슬롯 2개로 설정
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));	
	}

	//고통 수집
	public IEnumerator G54(Card card,List<TargetObject> tar)
	{
		List<TargetObject> targets = new List<TargetObject>();
        targets.Add(tar[0]);
        targets.AddRange(M_TurnManager.instance.spawnedMonsterList);
        M_DimmingManager.instance.StartDimming(targets);
		GeorkAnimation(tar[0],"Attack2");
		yield return new WaitForSeconds(0.5f);
		int numberOfenemy = M_TurnManager.instance.spawnedMonsterList.Count;
		foreach(TargetObject enemy in M_TurnManager.instance.spawnedMonsterList){
			GeneralSingleAttack(tar[0],enemy,14);
			StartCoroutine(enemy.monster.OnHitAnimation());
		}
		GeneralGetDefense(tar[0],tar[0],numberOfenemy*5,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(targets);	
	}

	//지치지 않는 자
	public IEnumerator G55(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		for(int i = 0 ;i < tar[0].GetBuffValue(BuffType.ICHI_ATTACK); i++)
		{
			GeneralSingleAttack(tar[0],tar[1],2);
			StartCoroutine(tar[1].monster.OnHitAnimation());
			yield return new WaitForSeconds(0.2f);
		}
		if(IsGISADO(tar)) card.isReturnable =true;
		else card.isReturnable = false;
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));	
	}

	//전리품 수집 //엘리트 처치시 스택 카운트 증가 구현 필요 // TODO
	public IEnumerator G56(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],15 + card.stackCount * 5);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//십자가
	public IEnumerator G57(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		tar[0].player.GetComponent<GamePlayerDeck>().currentIchi ++;
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	//절대자
	public IEnumerator G58(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		if(tar[0].buffs.FindIndex(x => x.type == BuffType.ICHI_ATTACK) != -1) // 힘의 이치가 있을경우에만 발동
			tar[0].GainBuff(BuffType.ICHI_ATTACK,tar[0].GetBuffValue(BuffType.ICHI_ATTACK),false,false,false,false,tar[0],card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	// 시체 걸기
	public IEnumerator G59(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],3);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(tar[1].isDying)
		{
			foreach(TargetObject target in M_TurnManager.instance.spawnedMonsterList)
			{
				if(target != tar[1])
				{
					target.GainBuff(BuffType.APDO,3,true,false,false,true,tar[0],card);
				}
			}
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//저주 벼린 검
	public IEnumerator G60(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		GeneralSingleAttack(tar[0],tar[1],10 + tar[0].player.GetComponent<GamePlayerDeck>().gainCurseCardCount);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	//신의 뜻
	public IEnumerator G61(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);
		if(tar[0].isTransformed){
			GeneralSingleAttack(tar[0],tar[1],60);
			StartCoroutine(tar[1].monster.OnHitAnimation());
		}else{
			GeneralSingleAttack(tar[0],tar[1],2);
			StartCoroutine(tar[1].monster.OnHitAnimation());
		}
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	

	//구세주
	public IEnumerator G62(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);
		if(tar[0].isTransformed)
		{
			foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
				GeneralGetDefense(tar[0],target,25,card);
		}
		else
			GeneralGetDefense(tar[0],tar[0],4,card);
		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	//선봉대
	public IEnumerator G63(Card card,List<TargetObject> tar)
	{
		if(M_TurnManager.instance.playerOrder.FindIndex(x => x == tar[0].player.netId) == 2)
		{
			M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
			GeorkAnimation(tar[0],"Buff0");
			GeneralGetDefense(tar[0],tar[0],15,card);
			yield return new WaitForSeconds(0.5f);
			M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
		}
		else
		{
			ChangePosition(tar[0],2);
			yield return new WaitForSeconds(0.5f); // 위치 스왑 완료 후
			List<TargetObject> dimmingList = new List<TargetObject>();
			dimmingList.Add(tar[0]);
			foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
			{
				if(target.player.netId == M_TurnManager.instance.playerOrder[0])
					dimmingList.Add(target);
			}
			M_DimmingManager.instance.StartDimming(dimmingList);
			GeorkAnimation(tar[0],"Buff0");
			GeneralGetDefense(tar[0],tar[0],7,card);
			yield return new WaitForSeconds(0.5f);
			M_DimmingManager.instance.StopDimming(dimmingList);
		}
	}

	//노련함
	public IEnumerator G64(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);

		GeneralSingleAttack(tar[0],tar[1],5);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
			tar[0].player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1);

		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 한번더
	public IEnumerator G65(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,2));
		GeorkAnimation(tar[0],"Attack0");
		yield return new WaitForSeconds(0.5f);

		GeneralSingleAttack(tar[0],tar[1],12);
		StartCoroutine(tar[1].monster.OnHitAnimation());
		if(IsGISADO(tar))
			tar[0].player.GetComponent<GamePlayerDeck>().prefareDeck.Add(new Card(CardData.instance.cards.Find(card => card.cardNumber == "G65")));

		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,2));
	}

	// 영웅의 선율 ( 2번째 카드 사용시 1장만 드로우 되야함 TODO !)
	public IEnumerator G66(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);

		int index = tar[0].GainBuff(BuffType.MELODYOFHERO,1,false,false,false,false,tar[0],card);
		if(!tar[0].buffCardUseEffect.ContainsKey(index))
			tar[0].buffCardUseEffect.Add(index,G66_Buff_Effect);

		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

	public IEnumerator G66_Buff_Effect(TargetObject tar, int index,Card card)
	{
		if(card.baseCard.cardType == CardType.BLESS)
			tar.player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(tar.GetBuffValue(BuffType.MELODYOFHERO));
		yield return null;
	}

	//단련된 몸
	public IEnumerator G67(Card card,List<TargetObject> tar)
	{
		M_DimmingManager.instance.StartDimming(tar.GetRange(0,1));
		GeorkAnimation(tar[0],"Buff0");
		yield return new WaitForSeconds(0.5f);

		GeneralGetDefense(tar[0],tar[0],3 + tar[0].GetBuffValue(BuffType.ICHI_ATTACK),card);

		yield return new WaitForSeconds(0.5f);
		M_DimmingManager.instance.StopDimming(tar.GetRange(0,1));
	}

}