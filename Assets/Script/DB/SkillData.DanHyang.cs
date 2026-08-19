using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 홍단향 스킬 효과 구현 (MP 자원 — 자기 턴 시작 시 소량 회복, 물약은 Phase 4).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
// 기본 스킬 '철귀 이동'(턴 종료 시 공·방 버프)은 철귀 시스템 재설계와 함께 후속 작업.
public static partial class SkillData
{
    // 화염구 — 단일 마법 피해
    public static IEnumerator HS1(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
    }

    // 치유의 빛 — 아군 하나 회복
    public static IEnumerator HS2(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        int heal = Mathf.Max(1, user.player.TotalIntelligence * skill.power / 100);
        targets[0].HealPlayer(heal);
        yield return new WaitForSeconds(0.3f);
    }

    // 마력 폭풍 — 전체 마법 피해
    public static IEnumerator HS3(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int damage = BattleActions.SkillDamage(user, skill);
        foreach (TargetObject target in targets)
        {
            if (target == null || target.isDying) continue;
            yield return BattleActions.AttackTarget(user, target, damage, skill.attribute);
        }
    }

    // 수호 축복 — 아군 하나에게 지능 비례 실드
    public static IEnumerator HS4(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        int shield = Mathf.Max(1, user.player.TotalIntelligence * skill.power / 100);
        targets[0].GainDefense(shield);
        yield return new WaitForSeconds(0.3f);
    }

    // 필살기: 만개 — 아군 전원 대회복 + 절반만큼 실드. 사용 후 자신의 TP 감소 (하이리스크)
    public static IEnumerator HU1(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int heal = Mathf.Max(1, user.player.TotalIntelligence * skill.power / 100);
        foreach (TargetObject ally in targets)
        {
            if (ally == null || ally.isDying) continue;
            ally.HealPlayer(heal);
            ally.GainDefense(heal / 2);
        }
        M_TurnManager.instance.AddTpTo(user, -BalanceData.Get("ULTIMATE_TP_PENALTY", 50));
        yield return new WaitForSeconds(0.4f);
    }
}
