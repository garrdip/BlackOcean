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

// TargetObject partial — 플레이어/몬스터 피해 처리 및 사망 프로세스.
// 아이템 개편 Phase 1 (ToDo_RPG_ITEM 1-3): 공격 측 판정(명중·r·약점·치명·배율)은 BattleActions.ResolveHit이 끝내고 오며,
// 여기의 피격 함수는 **방어 측**만 담당한다 — 대열 보정 → 보호 분담 → 실드(방어 무시) → 방어 공식(치명이면 50% − 10) → HP → 분노(최종 피해).
// 두 함수 모두 HP에 실제로 들어간 피해(int)를 반환한다 (분노·생명력 흡수·반사의 기준값).
public partial class TargetObject
{


    // 과잉 피해 표시 — 피해가 잔여 HP를 넘으면 HP 감소량 대신 실제 피해량을 띄운다.
    // 서버가 HP를 깎기 직전 overkillDamageDisplay SyncVar(HP보다 먼저 선언 — 같은 배치에서 훅보다 앞서 적용)를 설정하고,
    // 과잉이 아니면 0으로 리셋한다. HP 변화 훅이 이 값을 읽어 표시량을 정한다. critDamageDisplay(치명 표시)도 같은 방식.
    // (주의: ClientRpc는 호스트에서 다음 프레임에 실행돼 훅보다 늦으므로 사용할 수 없다)

    // ----------------------------------------------           Damage 관련 함수        ---------------------------------------------------//
    // attribute: 공격 속성 — 마법 속성(BattleActions.IsMagic)이면 마법방어, 그 외는 방어력 스탯으로 경감 (RPG_CONVERSION_BATTLE 데미지 공식)
    // isCritical: 치명이면 방어 공식의 방어력을 CRIT_DEFENSE_PERCENT%만 적용하고 CRIT_DEFENSE_FLAT_REDUCE를 더 뺀다 (ResolveHit이 판정)
    // 반환: HP에 실제로 가해진 피해량. 실드 흡수/완전 경감은 0 — 호출부(GeneralAttack)는 피격 모션을 생략한다
    public int DamageToPlayer(int damage, AttackAttribute attribute = AttackAttribute.NONE, bool isCritical = false)
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
        // RPG 스탯 방어 (TP 전투): 대열 보정(전열 피해 증가/후열 감소) → 보호 분담 → 실드 소모 → 남은 피해에만 방어력 감소 공식
        int shieldConsumed = 0; // TP 전투에서 실드가 흡수한 양 (유효 타격량 집계)
        if(M_TurnManager.instance.tpBattleActive && player != null)
        {
            damage = damage * BattleActions.RowIncomingDamagePercent(player) / 100;
            if(damage <= 0) return 0;
            // 보호 (게오르크 GS11) — WRAPWINGS 버프(값 = 분담 %, user = 보호자)가 있으면 그 비율만큼 보호자가 대신 맞는다.
            // 원 대상에서 판정이 끝난 피해를 나누기만 한다 — 보호자에게 약점·치명을 다시 판정하지 않고, 보호자 쪽 대열/실드/방어력으로 계산
            if(isServer) damage -= RedirectToGuardian(damage, attribute, isCritical);
            if(damage <= 0) return 0;
            // 실드 소모 — 방어력 스탯의 영향을 받지 않는다: 대열 보정된 원 데미지 그대로 깎인다 (Q29). 실드는 소진될 때까지 지속 (Q37)
            if(defense >= damage){ defense -= damage; return 0; } // 실드 전량 흡수 — 실제 피격 아님 (피격 모션 없음, 분노 없음 — Q32)
            if(defense > 0){ shieldConsumed = defense; damage -= defense; defense = 0; }
            // 실드를 뚫고 남은 피해에만 방어력 감소 공식 적용 (마법 속성이면 마법방어, 그 외 방어력 — 장비 합산 + 스탯 버프, TargetObject.Stats)
            int defenseStat = BattleActions.IsMagic(attribute) ? GetMagicDefense() : GetDefense();
            damage = BattleActions.ApplyDefenseFormula(damage, defenseStat, isCritical);
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
            if(isServer)
            {
                overkillDamageDisplay = (remind > playerHP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
                critDamageDisplay = isCritical;                           // 치명 표시 (HP 대입 전에 설정)
            }
            playerHP -= remind;
            if(playerHP <= 0){
                playerHP = TryErisRevive() ? 1 : 0; // 에리스 — 한 전투에 한 번 HP 1로 생존 (광기 진입은 SetPlayerHP → UpdateErisMode)
            }
            player.HP = playerHP;
            // 맞을 때 분노 — 모든 계산이 끝나 HP에 들어간 최종 피해 기준 (실드 흡수분 제외, Q32): (데미지/자신의 최대HP) x 변환제어
            if(isServer && M_TurnManager.instance.tpBattleActive) BattleActions.GainRageByDamage(player, remind, playerMaxHP);
            if(isServer) CounterAttack(); // 반격 (게오르크 GS13 패시브) — HP 피해를 입었을 때만
            return remind; // HP에 실제로 가해진 피해 (실드 관통분)
        }
    }

    // 보호 분담 (게오르크 GS11) — 보호자에게 넘긴 피해량을 돌려준다. 보호자가 자신이거나 사망/부재면 0. 서버 전용
    int RedirectToGuardian(int damage, AttackAttribute attribute, bool isCritical)
    {
        if(damage <= 0) return 0;
        Buff guard = buffs.Find(buff => buff.type == BuffType.WRAPWINGS && buff.value > 0);
        if(guard == null) return 0;
        TargetObject guardian = M_TurnManager.instance.spawnedPlayerList.Find(p => p != null && p.netId == guard.user);
        if(guardian == null || guardian == this || guardian.isDying || guardian.playerHP <= 0) return 0;
        int shared = damage * guard.value / 100;
        if(shared <= 0) return 0;
        int dealt = guardian.DamageToPlayer(shared, attribute, isCritical);
        if(dealt > 0) M_TurnManager.instance.StartAnimation(guardian, 0, "Defense0", false); // 게오르크 피격 모션 (SpawnedMonster.GeneralAttack과 동일)
        return shared;
    }

    // 반격 (게오르크 GS13 패시브) — HP 피해를 입으면 지금 행동 중인 몬스터(M_TurnManager.tpActingUnit)에게 기본 공격력의 (10/20/30)% 반사.
    // 별도의 HitContext로 2차 판정(명중·치명·r)을 거친다 (Q27). isSecondary — 반격의 반격은 없다. 서버 전용
    void CounterAttack()
    {
        if(M_TurnManager.instance == null || !M_TurnManager.instance.tpBattleActive) return;
        TargetObject attacker = M_TurnManager.instance.tpActingUnit;
        if(attacker == null || attacker == this || attacker.monster == null || attacker.isDying) return;
        int damage = SkillData.CounterDamage(this);
        if(damage <= 0) return;
        CharacterStatData.Entry stat = CharacterStatData.Get(player.character);
        HitContext counter = new HitContext
        {
            attacker = this, target = attacker, baseDamage = damage,
            attribute = stat != null ? stat.basicAttack : AttackAttribute.NONE, // 기본 공격 속성 (Phase 3부터 무기 속성)
            isSecondary = true,
        };
        BattleActions.ResolveHit(counter);
        if(counter.dealt > 0) M_TurnManager.instance.StartCoroutine(attacker.monster.OnHitAnimation());
    }

    // 완전 경감(최종 데미지 0) 표시 — 숫자 "0"만 띄운다 (DisPlayeDamage가 0일 때 피격 파티클/셰이크를 생략)
    [ClientRpc]
    void RpcDisplayZeroDamage()
    {
        M_EffectManager.instance.DisPlayeDamage(this, 0);
    }


    // 고정 체력 손실 — 방어·증폭(단말마/붕괴/개화) 전부 무시하고 체력만 감소 (스킬 HP 코스트·고행길·피의 질주)
    // 사망/광기 변신 규칙은 일반 피해와 동일하게 적용
    public void LosePlayerHP(int value)
    {
        if(value <= 0) return;
        int hpBefore = playerHP;
        if(isServer)
        {
            overkillDamageDisplay = (value > playerHP) ? value : 0; // 과잉 손실도 실제 수치 표시 (스테일 방지 겸)
            critDamageDisplay = false;
        }
        playerHP -= value; // SetPlayerHP가 0~최대치 클램프 및 GamePlayer.HP 동기화 처리
        if(playerHP <= 0 && TryErisRevive()) playerHP = 1; // 에리스 — 한 전투에 한 번 HP 1로 생존 (광기 진입은 SetPlayerHP → UpdateErisMode)
    }


    // 고정 피해 (개화꽃 등) — 판정·랜덤·방어 없음, 실드만 거친다 (Q27)
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
            if(isServer)
            {
                overkillDamageDisplay = (remind > playerHP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
                critDamageDisplay = false;
            }
            playerHP -= remind;
            if(playerHP <= 0){
                playerHP = TryErisRevive() ? 1 : 0; // 에리스 — 한 전투에 한 번 HP 1로 생존 (광기 진입은 SetPlayerHP → UpdateErisMode)
            }
            player.HP = playerHP;
        }
    }


    // 몬스터 피격 (방어 측) — 방어 공식(몬스터 방어력·마법방어 스탯 + 위험도 보정 + STAT_DEFENSE 버프, 치명이면 50% − 10) → HP. 몬스터에게 실드는 없다 (Phase 2, Q14).
    // 반환: HP에 실제로 들어간 피해 (완전 경감은 0 — 숫자 "0" 표시)
    public int DamageToMonster(int damage, TargetObject from, AttackAttribute attribute = AttackAttribute.NONE, bool isCritical = false)
    {
        // 붕괴 적용
        if(GetBuffValue(BuffType.BOONGGUI,null) > 0)
        {
            damage = (int)(damage * 1.5);
        }
        // 개화꽃 적용
        foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
            damage += GetBuffValue(BuffType.FLOWER,target);
        // 방어 공식 — 마법 속성이면 마법방어, 그 외 방어력 (TargetObject.Stats)
        int defenseStat = BattleActions.IsMagic(attribute) ? GetMagicDefense() : GetDefense();
        int remind = BattleActions.ApplyDefenseFormula(damage, defenseStat, isCritical);
        if(remind <= 0)
        {
            if(isServer) RpcDisplayZeroDamage();
            return 0;
        }
        if(isServer)
        {
            monster.overkillDamageDisplay = (remind > monster.HP) ? remind : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
            monster.critDamageDisplay = isCritical;
        }
        if(isServer && monster.HP <= remind){
            isDying = true;
            RpcMonsterDissolve();
        }
        monster.HP -= remind;
        return remind;
    }


    // 고정 피해 (자비란 없다 등) — 판정·랜덤·방어 없음 (방어 무시, Q27)
    public void StaticDamageToMonster(int damage)
    {
        if(damage <= 0) return;
        if(isServer)
        {
            monster.overkillDamageDisplay = (damage > monster.HP) ? damage : 0; // 과잉 피해 — 실제 피해량 표시 (HP 대입 전에 설정)
            monster.critDamageDisplay = false;
        }
        if(isServer && monster.HP <= damage){
            isDying = true;
            RpcMonsterDissolve();
        }
        monster.HP -= damage;
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
