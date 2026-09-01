using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 에리스 스킬 효과 구현 (HP 자원 — 코스트는 실행 전에 M_TurnManager.PayCost가 HP 1까지만 지불).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
// RPG_CONVERSION_SKILLS '에리스 - 공격' 트리 (ES5~ES10). 구 임시 스킬(흡혈 베기/피의 가속/별무리 파편/은하수 일격/초신성)과 구 트리(은하수/별무리)는 제거됨 (2026-09-01).
// 변신 배율(1차 +20% / 광기 +50%)은 BattleActions.SkillDamage가 상시 적용한다.
public static partial class SkillData
{
    // 권능 : 찌르기 — 힘의 (130/150/170)% 공명 피해
    public static IEnumerator ES5(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
    }

    // 부서지세요 — 힘의 (90/100/110)% 공명 피해 + 방어력 (10/15/20) 저하 3턴
    public static IEnumerator ES6(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
        int level = Mathf.Min(2, user.player.GetSkillLevel(skill.skillNo));
        int defenseDown = BalanceData.Get("ERIS_BREAK_DEF_DOWN", 10) + BalanceData.Get("ERIS_BREAK_DEF_DOWN_PER_LEVEL", 5) * level;
        ReduceMonsterDefense(target, defenseDown, BalanceData.Get("ERIS_BREAK_DURATION", 3), user);
    }

    // 권능 : 파괴 — 힘의 (50/60/70)% 공명 피해. 전투 중 사용할 때마다 계수 +30%p (TargetObject.erisDestroyUses — 전투마다 새 스폰이라 자연 리셋)
    public static IEnumerator ES7(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        int ramp = 100 + user.erisDestroyUses * BalanceData.Get("ERIS_DESTROY_RAMP_PERCENT", 30);
        user.erisDestroyUses++;
        int damage = BattleActions.SkillDamage(user, skill) * ramp / 100;
        yield return BattleActions.AttackTarget(user, targets[0], damage, skill.attribute);
    }

    // 얼마나 버틸까요 — 적의 방어력을 (50/100/150) 낮춘 뒤 힘의 100% 공명 피해. 저하는 1턴
    public static IEnumerator ES8(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        int level = Mathf.Min(2, user.player.GetSkillLevel(skill.skillNo));
        int defenseDown = BalanceData.Get("ERIS_ENDURE_DEF_DOWN", 50) + BalanceData.Get("ERIS_ENDURE_DEF_DOWN_PER_LEVEL", 50) * level;
        ReduceMonsterDefense(target, defenseDown, BalanceData.Get("ERIS_ENDURE_DURATION", 1), user); // 피해보다 먼저 — 실드를 벗겨낸 뒤 때린다
        yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
    }

    // 압력 분출 — 모든 적에게 힘의 (50/60/70)% 공명 피해. 이 공격으로 적이 사망하면 반복 (안전 상한 10회)
    public static IEnumerator ES9(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        for (int wave = 0; wave < 10; wave++)
        {
            bool killed = false;
            foreach (TargetObject target in targets)
            {
                if (target == null || target.isDying) continue;
                yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
                if (target != null && target.isDying) killed = true;
            }
            if (!killed) yield break;
            if (targets.FindIndex(target => target != null && !target.isDying) < 0) yield break; // 전멸 — 반복할 대상 없음
        }
    }

    // 찢어 줄게요 — 힘의 (150/200/250)% 공명 피해. 1차 변신 상태에서 2회, 광기 상태에서 3회 반복
    public static IEnumerator ES10(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        int repeat = user.erisMode == ErisMode.MAD ? 3 : user.erisMode == ErisMode.ANGER ? 2 : 1;
        for (int hit = 0; hit < repeat; hit++)
        {
            if (targets[0] == null || targets[0].isDying) yield break; // 사망하면 종료
            yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
        }
    }

    // 몬스터 방어력 저하 — 현재 실드(defense)를 즉시 깎고, 지속 턴 동안 방어 획득량을 낮추는 디버프(ICHI_DEFENSE 음수, TargetObject.GainDefense가 반영)를 건다.
    // 몬스터의 '방어력'은 TP 전투에서 방어 행동으로 얻는 실드뿐이라 두 축을 함께 낮춰야 체감이 된다
    static void ReduceMonsterDefense(TargetObject target, int amount, int turns, TargetObject from)
    {
        if (target == null || target.isDying || amount <= 0) return;
        target.defense = Mathf.Max(0, target.defense - amount);
        M_TurnManager.instance.ApplyTimedDebuffTo(target, BuffType.ICHI_DEFENSE, -amount, turns, from);
    }
}
