using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 에리스 스킬 효과 구현 (RPG 전투 수직 슬라이스).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
// 코스트(HP 소모)는 실행 전에 M_TurnManager가 지불 처리하므로 여기서는 효과만 구현한다.
public static partial class SkillData
{
    // 흡혈 베기 — 단일 공명 피해 + 가한 피해의 50% HP 회복
    public static IEnumerator ES1(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        int hpBefore = (target.monster != null) ? target.monster.HP : 0;
        yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
        int dealt = hpBefore - ((target != null && target.monster != null) ? target.monster.HP : 0);
        if (dealt > 0) user.HealPlayer(dealt / 2);
    }

    // 피의 가속 — 자신의 TP 즉시 증가
    public static IEnumerator ES2(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        M_TurnManager.instance.AddTpTo(user, BalanceData.Get("ES2_TP_GAIN", 30));
        yield break;
    }

    // 별무리 파편 — 전체 공명 피해
    public static IEnumerator ES3(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int damage = BattleActions.SkillDamage(user, skill);
        foreach (TargetObject target in targets)
        {
            if (target == null || target.isDying) continue;
            yield return BattleActions.AttackTarget(user, target, damage, skill.attribute);
        }
    }

    // 은하수 일격 — 단일 고위력 공명 피해
    public static IEnumerator ES4(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
    }

    // 필살기: 초신성 — 전체 고위력 공명 피해. 사용 후 자신의 TP 감소 (하이리스크)
    public static IEnumerator EU1(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        int damage = BattleActions.SkillDamage(user, skill);
        foreach (TargetObject target in targets)
        {
            if (target == null || target.isDying) continue;
            yield return BattleActions.AttackTarget(user, target, damage, skill.attribute);
        }
        M_TurnManager.instance.AddTpTo(user, -BalanceData.Get("ULTIMATE_TP_PENALTY", 50));
    }
}
