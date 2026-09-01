using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 홍단향 스킬 효과 구현 (MP 자원 — 자기 턴 시작 시 소량 회복, 물약은 Phase 4).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
// 기본 스킬 '철귀 이동'(HS0, 턴 소모 없음): 철귀를 아군에게 붙이고, 턴 종료 효과(M_TurnManager.ApplyPlayerTurnEndEffects)가 그 아군에게 실드·공격 버프를 준다 (2026-09-01).
public static partial class SkillData
{
    // 철귀 이동 (기본 스킬, 턴 소모 없음) — 철귀를 아군 하나에게 보낸다 (자신 포함). 연출은 카드 전투의 IronDemonReturnProcess와 같은 텔레포트 순서.
    // 효과: 자기 턴 종료마다 철귀가 붙은 아군이 실드 HS0_DEFENSE + 공격력 HS0_ATTACK(다음 자기 턴 종료까지) — BalanceDB
    public static IEnumerator HS0(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        if (target == null || target.isDying || target == user.ironDemonLocation) yield break; // 이미 그곳에 있으면 이동 없음
        M_TurnManager.instance.AnimIronDemon("TeleportGo", user);
        yield return new WaitForSeconds(0.333f); // 철귀가 완전히 사라지는 시간
        user.ironDemonLocation = target;
        M_TurnManager.instance.MoveIronDemon(user, target);
        M_TurnManager.instance.AnimIronDemon("TeleportBack", user);
        yield return new WaitForSeconds(0.2f);
        M_TurnManager.instance.AnimIronDemon("Idle", user);
    }

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
