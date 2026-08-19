using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 게오르크 스킬 효과 구현 (분노 자원 — 피해를 주고받으며 충전, BattleActions.GainRage).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
public static partial class SkillData
{
    // 고행길 (기본 스킬) — 자신의 HP를 잃고 분노 획득
    public static IEnumerator GS1(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int hpCost = BalanceData.Get("GS1_HP_COST", 8);
        if (user.playerHP <= hpCost) yield break; // 빈사까지는 사용 불가
        user.LosePlayerHP(hpCost);
        BattleActions.GainRage(user.player, BalanceData.Get("GS1_RAGE_GAIN", 40));
    }

    // 강타 — 단일 참격 피해
    public static IEnumerator GS2(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
    }

    // 방패치기 — 단일 타격 피해 (검 캐릭터의 타격 커버리지)
    public static IEnumerator GS3(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
    }

    // 수호의 맹세 — 아군 전원에게 자신의 방어력 비례 실드
    public static IEnumerator GS4(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int shield = Mathf.Max(1, user.player.TotalDefense * skill.power / 100);
        foreach (TargetObject ally in targets)
        {
            if (ally == null || ally.isDying) continue;
            ally.GainDefense(shield);
        }
        yield return new WaitForSeconds(0.3f);
    }

    // 대지 가르기 — 전체 참격 피해
    public static IEnumerator GS5(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int damage = BattleActions.SkillDamage(user, skill);
        foreach (TargetObject target in targets)
        {
            if (target == null || target.isDying) continue;
            yield return BattleActions.AttackTarget(user, target, damage, skill.attribute);
        }
    }

    // 필살기: 대장군의 일격 — 단일 초고위력 참격. 사용 후 자신의 TP 감소 (하이리스크)
    public static IEnumerator GU1(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
        M_TurnManager.instance.AddTpTo(user, -BalanceData.Get("ULTIMATE_TP_PENALTY", 50));
    }
}
