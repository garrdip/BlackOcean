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


    // 과잉 피해 표시 — 피해가 잔여 HP를 넘으면 HP 감소량 대신 실제 피해량을 띄운다.
    // 서버가 HP를 깎기 직전 overkillDamageDisplay SyncVar(HP보다 먼저 선언 — 같은 배치에서 훅보다 앞서 적용)를 설정하고,
    // 과잉이 아니면 0으로 리셋한다. HP 변화 훅이 이 값을 읽어 표시량을 정한다.
    // (주의: ClientRpc는 호스트에서 다음 프레임에 실행돼 훅보다 늦으므로 사용할 수 없다)

    // ----------------------------------------------           Damage 관련 함수        ---------------------------------------------------//
    // attribute: 공격 속성 — MAGIC이면 마법방어, 그 외는 방어력 스탯으로 경감 (RPG_CONVERSION_BATTLE 데미지 공식)
    // 반환: HP에 실제로 가해진 피해량. 실드 흡수/완전 경감은 0 — 호출부(GeneralAttack)는 피격 모션을 생략한다
    public int DamageToPlayer(int damage, AttackAttribute attribute = AttackAttribute.NONE)
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
        // RPG 스탯 방어 (TP 전투): 대열 보정(전열 피해 증가/후열 감소) → 실드 소모 → 남은 피해에만 방어력 감소 공식
        int shieldConsumed = 0; // TP 전투에서 실드가 흡수한 양 (유효 타격량 집계)
        if(M_TurnManager.instance.tpBattleActive && player != null)
        {
            // 보호 (게오르크 GS11) — WRAPWINGS 버프(값 = 분담 %, user = 보호자)가 있으면 그 비율만큼 보호자가 대신 맞는다 (보호자 쪽 대열/실드/방어력으로 계산)
            if(isServer) damage -= RedirectToGuardian(damage, attribute);
            if(damage <= 0) return 0;
            damage = damage * BattleActions.RowIncomingDamagePercent(player) / 100;
            // 맞을때 분노 — 방어력을 제외한 데미지 기준: (데미지/자신의 최대HP) x 변환제어 (RPG_CONVERSION_DESIGN 자원 규칙)
            if(isServer) BattleActions.GainRageByDamage(player, damage, playerMaxHP);
            if(damage <= 0) return 0;
            // 실드 소모 — 방어력 스탯의 영향을 받지 않는다: 대열 보정된 원 데미지 그대로 깎인다 (RPG_CONVERSION_BATTLE)
            if(defense >= damage){ defense -= damage; return 0; } // 실드 전량 흡수 — 실제 피격 아님 (피격 모션 없음)
            if(defense > 0){ shieldConsumed = defense; damage -= defense; defense = 0; }
            // 실드를 뚫고 남은 피해에만 방어력 감소 공식 적용 (MAGIC이면 마법방어, 그 외 방어력 — 장비 합산치)
            int defenseStat = (attribute == AttackAttribute.MAGIC) ? player.TotalMagicDefense
                : player.TotalDefense + Mathf.Max(0, GetBuffValue(BuffType.ICHI_DEFENSE)); // 방어력 상승 버프(빈틈없는 자세/찌르고 막기 — ICHI_DEFENSE 양수)는 물리 방어력에 가산 (음수는 GainDefense가 실드 획득량에서 차감)
            damage = BattleActions.ApplyDefenseFormula(damage, defenseStat);
            if(damage <= 0)
            {
                if(shieldConsumed == 0 && isServer) RpcDisplayZeroDamage(); // 완전 경감 — 숫자 "0"만 표시 (피격 모션/파티클/셰이크 없음)
                return 0; // 실드 일부 흡수 포함 — HP 피해 없음 (피격 모션 없음)
            }
        }
        if(damage <= 0) return 0;
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
            return 0; // 실드 전량 흡수 (카드 전투 경로) — 실제 피격 아님
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            int hpBefore = playerHP;
            if(isServer) overkillDamageDisplay = (remind > playerHP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
            playerHP -= remind;
            if(playerHP <= 0){
                playerHP = TryErisRevive() ? 1 : 0; // 에리스 — 한 전투에 한 번 HP 1로 생존 (광기 진입은 SetPlayerHP → UpdateErisMode)
            }
            player.HP = playerHP;
            // (피격 분노는 방어력 적용 전 데미지 기준으로 위에서 처리 — GainRageByDamage)
            if(isServer) CounterAttack(); // 반격 (게오르크 GS13 패시브) — HP 피해를 입었을 때만
        }
        return damage; // HP에 실제로 가해진 피해 (실드 관통분)
    }

    // 보호 분담 (게오르크 GS11) — 보호자에게 넘긴 피해량을 돌려준다. 보호자가 자신이거나 사망/부재면 0. 서버 전용
    int RedirectToGuardian(int damage, AttackAttribute attribute)
    {
        if(damage <= 0) return 0;
        Buff guard = buffs.Find(buff => buff.type == BuffType.WRAPWINGS && buff.value > 0);
        if(guard == null) return 0;
        TargetObject guardian = M_TurnManager.instance.spawnedPlayerList.Find(p => p != null && p.netId == guard.user);
        if(guardian == null || guardian == this || guardian.isDying || guardian.playerHP <= 0) return 0;
        int shared = damage * guard.value / 100;
        if(shared <= 0) return 0;
        int dealt = guardian.DamageToPlayer(shared, attribute);
        if(dealt > 0) M_TurnManager.instance.StartAnimation(guardian, 0, "Defense0", false); // 게오르크 피격 모션 (SpawnedMonster.GeneralAttack과 동일)
        return shared;
    }

    // 반격 (게오르크 GS13 패시브) — HP 피해를 입으면 지금 행동 중인 몬스터(M_TurnManager.tpActingUnit)에게 기본 공격력의 (10/20/30)% 반사. 서버 전용
    void CounterAttack()
    {
        if(M_TurnManager.instance == null || !M_TurnManager.instance.tpBattleActive) return;
        TargetObject attacker = M_TurnManager.instance.tpActingUnit;
        if(attacker == null || attacker == this || attacker.monster == null || attacker.isDying) return;
        int damage = SkillData.CounterDamage(this);
        if(damage <= 0) return;
        attacker.DamageToMonster(damage, this);
        M_TurnManager.instance.StartCoroutine(attacker.monster.OnHitAnimation());
    }

    // 완전 경감(최종 데미지 0) 표시 — 숫자 "0"만 띄운다 (DisPlayeDamage가 0일 때 피격 파티클/셰이크를 생략)
    [ClientRpc]
    void RpcDisplayZeroDamage()
    {
        M_EffectManager.instance.DisPlayeDamage(this, 0);
    }


    // 고정 체력 손실 — 방어·증폭(단말마/붕괴/개화) 전부 무시하고 체력만 감소 ("체력을 잃습니다" 계열 카드용, 예: E7 돌로레)
    // 사망/광기 변신 규칙과 템페스토소 누적은 일반 피해와 동일하게 적용
    public void LosePlayerHP(int value)
    {
        if(value <= 0) return;
        int hpBefore = playerHP;
        if(isServer) overkillDamageDisplay = (value > playerHP) ? value : 0; // 과잉 손실도 실제 수치 표시 (스테일 방지 겸)
        playerHP -= value; // SetPlayerHP가 0~최대치 클램프 및 GamePlayer.HP 동기화 처리
        if(playerHP <= 0 && TryErisRevive()) playerHP = 1; // 에리스 — 한 전투에 한 번 HP 1로 생존 (광기 진입은 SetPlayerHP → UpdateErisMode)
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
            if(isServer) overkillDamageDisplay = (remind > playerHP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
            playerHP -= remind;
            if(playerHP <= 0){
                playerHP = TryErisRevive() ? 1 : 0; // 에리스 — 한 전투에 한 번 HP 1로 생존 (광기 진입은 SetPlayerHP → UpdateErisMode)
            }
            player.HP = playerHP;
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
        // 방어력 적용
        if(defense >= damage)
        {
            defense -= damage;
        }
        else
        {
            int remind = damage - defense;
            defense = 0;
            if(isServer) monster.overkillDamageDisplay = (remind > monster.HP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
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
            if(isServer) monster.overkillDamageDisplay = (remind > monster.HP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
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
        if(isServer) UpdateErisMode(); // 에리스 변신 — HP 비율 변화(피해/회복/코스트)마다 상태 재판정 (스폰 전 초기 대입은 isServer가 false라 제외)
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
