using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 에리스 스킬 효과 구현 (HP 자원 — 코스트는 실행 전에 M_TurnManager.PayCost가 HP 1까지만 지불).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다. 시그니처는 SkillCast 하나 (아이템 개편 Phase 1-4 — 수치는 cast.Scale, 타격은 AttackTarget(cast, …)).
// RPG_CONVERSION_SKILLS '에리스 - 공격' 트리 (ES5~ES10). 구 임시 스킬(흡혈 베기/피의 가속/별무리 파편/은하수 일격/초신성)과 구 트리(은하수/별무리)는 제거됨 (2026-09-01).
// 변신 배율(1차 +20% / 광기 +50%)은 BattleActions.SkillDamage가 상시 적용한다.
public static partial class SkillData
{
    // 피의 질주 (기본 스킬 '자해' — 턴을 소모한다, 2026-09-14) — 최대 HP의 ERIS_SELF_HARM_PERCENT(10)%만큼 HP를 잃고 TP를 ES1_TP_GAIN(50) 회복한다.
    // HP 1 미만 불가(HP 1이면 HP 소모 없이 TP만). 변신(40%/10%) 진입과 턴 가속을 겸한다. 이름은 SkillDB에서 변경 가능
    public static IEnumerator ES1(SkillCast cast)
    {
        TargetObject user = cast.user;
        int amount = Mathf.Max(1, user.playerMaxHP * BalanceData.Get("ERIS_SELF_HARM_PERCENT", 10) / 100); // HP 코스트 — 배율 미적용
        amount = Mathf.Min(amount, user.playerHP - 1);
        if (amount > 0) user.LosePlayerHP(amount); // SetPlayerHP → UpdateErisMode가 변신 판정
        M_TurnManager.instance.AddTpTo(user, cast.Scale(BalanceData.Get("ES1_TP_GAIN", 50)));
        yield break;
    }

    // 권능 : 찌르기 — 힘의 (130/150/170)% 공명 피해
    public static IEnumerator ES5(SkillCast cast)
    {
        if (cast.targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(cast, cast.targets[0], BattleActions.SkillDamage(cast.user, cast.def));
    }

    // 부서지세요 — 힘의 (90/100/110)% 공명 피해 + 방어력 (10/15/20) 저하 3턴. MISS면 저하도 없음 (Q27)
    public static IEnumerator ES6(SkillCast cast)
    {
        SkillDef skill = cast.def; TargetObject user = cast.user; List<TargetObject> targets = cast.targets;
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        yield return BattleActions.AttackTarget(cast, target, BattleActions.SkillDamage(user, skill));
        if (!BattleActions.LastHitLanded) yield break;
        int level = Mathf.Min(2, user.player.GetSkillLevel(skill.skillNo));
        int defenseDown = BalanceData.Get("ERIS_BREAK_DEF_DOWN", 10) + BalanceData.Get("ERIS_BREAK_DEF_DOWN_PER_LEVEL", 5) * level;
        ReduceMonsterDefense(target, cast.Scale(defenseDown), BalanceData.Get("ERIS_BREAK_DURATION", 3), user, skill.skillNo);
    }

    // 권능 : 파괴 — 힘의 (50/60/70)% 공명 피해. 전투 중 사용할 때마다 계수 +30%p (TargetObject.erisDestroyUses — 전투마다 새 스폰이라 자연 리셋)
    public static IEnumerator ES7(SkillCast cast)
    {
        SkillDef skill = cast.def; TargetObject user = cast.user; List<TargetObject> targets = cast.targets;
        if (targets.Count == 0) yield break;
        int ramp = 100 + user.erisDestroyUses * BalanceData.Get("ERIS_DESTROY_RAMP_PERCENT", 30);
        user.erisDestroyUses++;
        int damage = BattleActions.SkillDamage(user, skill) * ramp / 100;
        yield return BattleActions.AttackTarget(cast, targets[0], damage);
    }

    // 얼마나 버틸까요 — 적의 방어력을 (50/100/150) 낮춘 뒤 힘의 100% 공명 피해. 저하는 1턴 (피해보다 먼저 거는 선행 효과라 MISS와 무관)
    public static IEnumerator ES8(SkillCast cast)
    {
        SkillDef skill = cast.def; TargetObject user = cast.user; List<TargetObject> targets = cast.targets;
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        int level = Mathf.Min(2, user.player.GetSkillLevel(skill.skillNo));
        int defenseDown = BalanceData.Get("ERIS_ENDURE_DEF_DOWN", 50) + BalanceData.Get("ERIS_ENDURE_DEF_DOWN_PER_LEVEL", 50) * level;
        ReduceMonsterDefense(target, cast.Scale(defenseDown), BalanceData.Get("ERIS_ENDURE_DURATION", 1), user, skill.skillNo); // 피해보다 먼저 — 방어력을 낮춘 뒤 때린다
        yield return BattleActions.AttackTarget(cast, target, BattleActions.SkillDamage(user, skill));
    }

    // 압력 분출 — 모든 적에게 힘의 (50/60/70)% 공명 피해. 이 공격으로 적이 사망하면 반복 (안전 상한 10회)
    public static IEnumerator ES9(SkillCast cast)
    {
        SkillDef skill = cast.def; TargetObject user = cast.user; List<TargetObject> targets = cast.targets;
        for (int wave = 0; wave < 10; wave++)
        {
            bool killed = false;
            foreach (TargetObject target in targets)
            {
                if (target == null || target.isDying) continue;
                yield return BattleActions.AttackTarget(cast, target, BattleActions.SkillDamage(user, skill));
                if (target != null && target.isDying) killed = true;
            }
            if (!killed) yield break;
            if (targets.FindIndex(target => target != null && !target.isDying) < 0) yield break; // 전멸 — 반복할 대상 없음
        }
    }

    // 찢어 줄게요 — 힘의 (150/200/250)% 공명 피해. 1차 변신 상태에서 2회, 광기 상태에서 3회 반복
    public static IEnumerator ES10(SkillCast cast)
    {
        SkillDef skill = cast.def; TargetObject user = cast.user; List<TargetObject> targets = cast.targets;
        if (targets.Count == 0) yield break;
        int repeat = user.erisMode == ErisMode.MAD ? 3 : user.erisMode == ErisMode.ANGER ? 2 : 1;
        for (int hit = 0; hit < repeat; hit++)
        {
            if (targets[0] == null || targets[0].isDying) yield break; // 사망하면 종료
            yield return BattleActions.AttackTarget(cast, targets[0], BattleActions.SkillDamage(user, skill));
        }
    }

    // 몬스터 방어력 저하 — 방어력 스탯 디버프(STAT_DEFENSE 음수, 대상 턴 종료 차감. 아이템 개편 Phase 2-4·Q31). TargetObject.GetDefense가 0 하한으로 반영한다.
    // 출처 = 스킬 번호 — 부서지세요와 얼마나 버틸까요가 겹치면 합산, 같은 스킬을 다시 쓰면 갱신 (Q45)
    static void ReduceMonsterDefense(TargetObject target, int amount, int turns, TargetObject from, string sourceId)
    {
        if (target == null || target.isDying || amount <= 0) return;
        M_TurnManager.instance.ApplyTimedStatBuff(target, BuffType.STAT_DEFENSE, -amount, turns, from, sourceId);
    }
}
