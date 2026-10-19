using System.Collections;
using UnityEngine;
using ProjectD;

/// <summary>
/// 소모품 효과 구현 (아이템 개편 Phase 6-2, Q5·Q6·Q38·R1). 메서드명 = ConsumableDB.csv의 Effect. 서버에서만 실행된다.
/// 인자: user/target = GamePlayer(거점·미로에도 있다), targetUnit = 전투 중 대상 TargetObject(비전투 null). 효과는 대상이 플레이어인지 몬스터인지 가정하지 않고
/// 공용 함수(GamePlayer.ServerHeal / TargetObject.Heal, M_TurnManager.ApplyTimedStatBuff, BattleActions.GainRage …)만 쓴다 — 추후 몬스터 대상은 target null + targetUnit 몬스터.
/// 버프 물약은 전투 전용(BattleOnly)이며 지속은 사용 턴을 제외한 Duration턴 (Q33 — 자기 턴 중 부여 = skipFirstTick).
/// </summary>
public static class ConsumableEffects
{
    // HP 회복 — ServerHeal 경유: 치명 회복(시전자 치명률)·받는 회복량 옵션이 전투·비전투 모두 적용 (Q38)
    public static IEnumerator HEAL_HP(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    {
        if (target != null) target.ServerHeal(def.value, user);
        else if (targetUnit != null) targetUnit.Heal(def.value, UnitOf(user));
        if (targetUnit != null) yield return new WaitForSeconds(0.3f);
    }

    // MP 회복 — 자원이 MP인 캐릭터만 (ConsumableDB UsableResource=MP가 검증)
    public static IEnumerator RESTORE_MP(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    {
        if (target != null) target.currentResource = Mathf.Min(target.TotalMaxResource, target.currentResource + def.value);
        if (targetUnit != null) yield return new WaitForSeconds(0.3f);
    }

    // 분노 충전 — 자원이 분노인 캐릭터만 (UsableResource=RAGE). 전투 전용
    public static IEnumerator RESTORE_RAGE(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    {
        if (target != null) BattleActions.GainRage(target, def.value);
        if (targetUnit != null) yield return new WaitForSeconds(0.3f);
    }

    // 공격력 물약 — STAT_ATTACK +Value, Duration턴 (출처 = 물약 번호 — 다른 출처 버프와 합산, Q45)
    public static IEnumerator BUFF_ATTACK(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    {
        return ApplyStatBuff(def, user, targetUnit, BuffType.STAT_ATTACK);
    }

    // 치명타 물약 — STAT_CRIT_RATE +Value %p
    public static IEnumerator BUFF_CRIT_RATE(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    {
        return ApplyStatBuff(def, user, targetUnit, BuffType.STAT_CRIT_RATE);
    }

    // 회피율 물약 — STAT_EVADE +Value %p
    public static IEnumerator BUFF_EVADE(ConsumableData.Def def, GamePlayer user, GamePlayer target, TargetObject targetUnit)
    {
        return ApplyStatBuff(def, user, targetUnit, BuffType.STAT_EVADE);
    }

    // ------------------------------------------------------------- 공용 -------------------------------------------------------------//

    static IEnumerator ApplyStatBuff(ConsumableData.Def def, GamePlayer user, TargetObject targetUnit, BuffType type)
    {
        if (targetUnit == null || M_TurnManager.instance == null) yield break; // 전투 전용 — BattleOnly가 막지만 방어적으로
        M_TurnManager.instance.ApplyTimedStatBuff(targetUnit, type, def.value, Mathf.Max(1, def.duration), UnitOf(user) ?? targetUnit, def.potionNo);
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>GamePlayer의 전투 유닛 (전투 중이 아니면 null)</summary>
    static TargetObject UnitOf(GamePlayer gamePlayer)
    {
        if (gamePlayer == null || M_TurnManager.instance == null || !M_TurnManager.instance.tpBattleActive) return null;
        return M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
    }
}
