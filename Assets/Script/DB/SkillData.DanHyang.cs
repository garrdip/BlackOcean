using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 홍단향 스킬 효과 구현 (MP 자원 — 자기 턴 시작 시 소량 회복).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다. 시그니처는 SkillCast 하나 (아이템 개편 Phase 1-4 — 수치는 cast.Scale, 타격은 AttackTarget(cast, …)).
// 기본 스킬 '철귀 이동'(HS0, 턴 소모 없음 — SkillDB FreeAction=1, 세 기본 스킬 중 유일): 철귀를 아군에게 붙이고, 턴 종료 효과(M_TurnManager.ApplyPlayerTurnEndEffects)가
// 그 아군에게 방어력·공격력 버프를 준다 (실드 → 방어력 버프로 변경, Q41 — 2026-10-19).
// 회복·실드는 TargetObject.Heal / GainShield — 시전자 치명률로 치명 회복·치명 실드 (Q29), 실드는 덮어쓰기·소진까지 지속 (Q41).
public static partial class SkillData
{
    // 철귀 이동 (기본 스킬, 턴 소모 없음) — 철귀를 아군 하나에게 보낸다 (자신 포함). 연출은 카드 전투의 IronDemonReturnProcess와 같은 텔레포트 순서.
    // 효과: 자기 턴 종료마다 철귀가 붙은 아군이 방어력 +HS0_DEFENSE(다음 자기 턴 시작까지) + 공격력 +HS0_ATTACK(다음 자기 턴 종료까지) — BalanceDB
    public static IEnumerator HS0(SkillCast cast)
    {
        TargetObject user = cast.user; List<TargetObject> targets = cast.targets;
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
    public static IEnumerator HS1(SkillCast cast)
    {
        if (cast.targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(cast, cast.targets[0], BattleActions.SkillDamage(cast.user, cast.def));
    }

    // 치유의 빛 — 아군 하나 회복 (지능 비례, 치명 회복 가능)
    public static IEnumerator HS2(SkillCast cast)
    {
        if (cast.targets.Count == 0) yield break;
        int heal = Mathf.Max(1, cast.user.GetAttackPower(cast.def.scalesWithInt, cast.def.attribute) * cast.def.power / 100); // 속성 NONE → 지능 + 무기 마법공격력 (Q46)
        cast.targets[0].Heal(cast.Scale(heal), cast.user);
        yield return new WaitForSeconds(0.3f);
    }

    // 마력 폭풍 — 전체 마법 피해 (대상마다 개별 판정)
    public static IEnumerator HS3(SkillCast cast)
    {
        int damage = BattleActions.SkillDamage(cast.user, cast.def);
        foreach (TargetObject target in cast.targets)
        {
            if (target == null || target.isDying) continue;
            yield return BattleActions.AttackTarget(cast, target, damage);
        }
    }

    // 수호 축복 — 아군 하나에게 지능 비례 실드 (기존 실드를 교체, 치명 실드 가능)
    public static IEnumerator HS4(SkillCast cast)
    {
        if (cast.targets.Count == 0) yield break;
        int shield = Mathf.Max(1, cast.user.GetAttackPower(cast.def.scalesWithInt, cast.def.attribute) * cast.def.power / 100);
        cast.targets[0].GainShield(cast.Scale(shield), cast.user);
        yield return new WaitForSeconds(0.3f);
    }

    // 필살기: 만개 — 아군 전원 대회복 + 절반만큼 실드(교체). 사용 후 자신의 TP 감소 (하이리스크)
    public static IEnumerator HU1(SkillCast cast)
    {
        TargetObject user = cast.user;
        int heal = cast.Scale(Mathf.Max(1, user.GetAttackPower(cast.def.scalesWithInt, cast.def.attribute) * cast.def.power / 100));
        foreach (TargetObject ally in cast.targets)
        {
            if (ally == null || ally.isDying) continue;
            ally.Heal(heal, user);
            ally.GainShield(heal / 2, user);
        }
        M_TurnManager.instance.AddTpTo(user, -BalanceData.Get("ULTIMATE_TP_PENALTY", 50)); // 페널티 — 배율 미적용
        yield return new WaitForSeconds(0.4f);
    }
}
