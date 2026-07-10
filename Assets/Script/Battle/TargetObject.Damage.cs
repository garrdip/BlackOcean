using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Mirror;
using ProjectD;
using DG.Tweening;
using TMPro;
using Spine.Unity;
using Spine.Unity.Examples;
using System.Linq;

// TargetObject partial — 플레이어/몬스터 피해 처리 및 사망 프로세스
public partial class TargetObject
{

    private int tempestosoHpLost; // 템페스토소 — 이번 전투에서 잃은 체력 누적(10마다 드로우)
    public int cardDamageDealt; // 별무리 — 현재 실행 중인 카드가 넣은 피해 누적 (파이프라인이 카드 실행 전 리셋)

    // ----------------------------------------------           Damage 관련 함수        ---------------------------------------------------//
    public void DamageToPlayer(int damage)
    {
        // 웃는 인형의 단말마: 받는 (일반)피해 +1배. 고정피해(StaticDamage)는 증폭하지 않는다.
        if(HasBuff(BuffType.DEATHTHROES))
        {
            damage *= 2;
        }
        if(GetBuffValue(BuffType.BOONGGUI, null) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            damage -= GetBuffValue(BuffType.FLOWER,target);
        if(damage <= 0) return;
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            int hpBefore = playerHP;
            playerHP -= remind;
            if(playerHP <= 0){
                if(player.character == Character.ERIS && erisMode != ErisMode.MAD)
                {
                    playerHP = 1;
                    StartCoroutine(ErisTransform());
                }
                else
                    playerHP = 0;
            }
            player.HP = playerHP;
            AccumulateTempestosoHpLost(hpBefore - playerHP);
        }
    }


    public void StaticDamageToPlayer(int damage)
    {
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            int hpBefore = playerHP;
            playerHP -= remind;
            if(playerHP <= 0){
                if(player.character == Character.ERIS && erisMode != ErisMode.MAD)
                {
                    playerHP = 1;
                    StartCoroutine(ErisTransform());
                }
                else
                    playerHP = 0;
            }
            player.HP = playerHP;
            AccumulateTempestosoHpLost(hpBefore - playerHP);
        }
    }


    // 템페스토소: 이번 전투에서 체력을 10 잃을 때마다 카드 1장 드로우
    private void AccumulateTempestosoHpLost(int lost)
    {
        if(!isServer || lost <= 0 || !HasBuff(BuffType.TEMPESTOSO)) return;
        tempestosoHpLost += lost;
        while(tempestosoHpLost >= 10)
        {
            tempestosoHpLost -= 10;
            player.GetComponent<GamePlayerDeck>().ServerSpawnCardOnHand(1);
        }
    }


    // 체력 회복 공통 경로 — 허물 강화(ENHANCESKIN) 보유 시 이번 턴 회복이 방어로 전환된다
    public void HealPlayer(int value)
    {
        if(value <= 0) return;
        if(HasBuff(BuffType.ENHANCESKIN))
        {
            defense += value;
        }
        else
        {
            playerHP = Mathf.Min(playerHP + value, playerMaxHP);
            player.HP = playerHP;
        }
    }


    public void DamageToMonster(int damage, TargetObject from)
    {
        // 붕괴 적용
        if(GetBuffValue(BuffType.BOONGGUI,null) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            damage += GetBuffValue(BuffType.FLOWER,target);
        // 월식: 시전 플레이어가 이번 턴에 넣은 피해만큼 방어 획득
        if(from != null && from != this && from.objectType == ObjectType.PLAYER)
        {
            if(from.HasBuff(BuffType.ECLIPSE))
                from.defense += damage;
            from.cardDamageDealt += damage; // 별무리(공격 카드 강화)용 피해 누적
        }
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            if(isServer && monster.HP <= remind){
                isDying = true;
                RpcMonsterDissolve();
            }
            monster.HP -= remind;
        }
    }


    public void StaticDamageToMonster(int damage)
    {
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            if(isServer && monster.HP <= remind){
                isDying = true;
                RpcMonsterDissolve();
            }
            monster.HP -= remind;
        }
    }


    // --------------------------------------------------------- Server Method -----------------------------------------------------------//

    [Server]
    IEnumerator PlayerDeathProcess()
    {
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
        {
            if(target.player.character == Character.HONGDANHYANG)
            {
                if(target.ironDemonLocation == this)
                {
                    yield return M_TurnManager.instance.IronDemonReturnProcess(target);
                }
            }
        }
        foreach(CardOnHand cardOnHand in player.GetComponent<GamePlayerDeck>().cardOnHands)
            NetworkServer.Destroy(cardOnHand.gameObject);
        player.GetComponent<GamePlayerDeck>().cardOnHands.Clear();
        M_TurnManager.instance.spawnedPlayerList.Remove(this);
        NetworkServer.Destroy(this.gameObject);
    }


    [Server]
    public void ServerProcessMonsterDeath()
    {
        M_TurnManager.instance.monsterDeathOperating = true;
        M_TurnManager.instance.ProcessMonsterDeath(this);
    }


    // 플레이어 타겟오브젝트 Hp값 변경(음수값 방지, 최대체력 초과 방지)
    [Server]
    private void SetPlayerHP(int newHp)
    {
        _playerHP = Mathf.Clamp(newHp, 0, playerMaxHP); // 플레이어 Hp값을 최소 0, 최대 MaxHp 사이로 값 제한
        player.HP = _playerHP; // 타겟오브젝트의 체력 값과 GamePlayer의 체력 값 동기화
    }


    [ClientRpc]
    public void RpcMonsterDissolve()
    {
        if(gameObject.activeSelf){
            monster.StartDissolveEffect(() => {
                if(isServer){
                    ServerProcessMonsterDeath();
                }
            });
        }
    }
}
