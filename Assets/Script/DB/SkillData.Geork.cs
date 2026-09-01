using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 게오르크 스킬 효과 구현 (분노 자원 — 피해를 주고받으며 충전, BattleActions.GainRage).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
// RPG_CONVERSION_SKILLS '게오르크 - 공격' 트리 (GS6~GS10). 구 테스트 스킬(강타 GS2 등)은 제거됨 (2026-08-26).
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

    // 연속 베기 — 기본공격력의 (70/80/90)% 참격으로 같은 대상을 두 번 공격
    public static IEnumerator GS6(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        for (int hit = 0; hit < 2; hit++)
        {
            if (targets[0] == null || targets[0].isDying) yield break; // 첫 타로 사망하면 종료
            if (hit > 0) M_TurnManager.instance.PlayPlayerActionAnimation(user, "Attack1"); // 2타 — 공격 모션 다시 재생 (1타 모션은 스킬 시작 시 M_TurnManager가 건다)
            yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
        }
    }

    // 기사도 (패시브) — 자신을 노리는 적에게 주는 피해 (20/30/40)% 증가.
    // 효과는 BattleActions.AttackTarget의 ChivalryBonusPercent가 상시 적용한다 — 이 메서드는 직접 실행되지 않는다 (Passive=1)
    public static IEnumerator GS7(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        yield break;
    }

    // 약점 찌르기 — 기본공격력의 (100/120/140)% 참격 피해 + 쇠락(공격력 저하) 부여.
    // 쇠락 수치는 투사 트리 '쇠락' 스킬 레벨1 기준 (BalanceDB) — TODO(투사 트리): 습득한 쇠락 레벨(15/20/25) 연동
    public static IEnumerator GS8(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
        if (target != null && !target.isDying && target.monster != null)
            M_TurnManager.instance.ApplyTimedDebuffTo(target, BuffType.ICHI_ATTACK,
                -BalanceData.Get("GEORK_SOIRAK_ATTACK_DOWN", 15), BalanceData.Get("GEORK_SOIRAK_DURATION", 3), user);
    }

    // 풍차 베기 — 기본공격력의 (40/50/60)% 참격으로 적 전체를 두 번 공격
    public static IEnumerator GS9(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        for (int wave = 0; wave < 2; wave++)
        {
            if (wave > 0) M_TurnManager.instance.PlayPlayerActionAnimation(user, "Attack1"); // 2회째 회전 — 공격 모션 다시 재생 (1회째는 스킬 시작 시 M_TurnManager가 건다)
            foreach (TargetObject target in targets)
            {
                if (target == null || target.isDying) continue;
                yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
            }
        }
    }

    // 범접할 수 없는 힘 — 기본공격력의 (120/140/160)% 참격 피해.
    // TODO(투사 트리): '북방의 위대한 투사' 스택으로 상승한 공격력의 효과를 2배로 적용
    public static IEnumerator GS10(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
    }
}
