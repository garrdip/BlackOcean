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

    // ----------------------------------------------           Damage 관련 함수        ---------------------------------------------------//
    public void DamageToPlayer(int damage)
    {
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
