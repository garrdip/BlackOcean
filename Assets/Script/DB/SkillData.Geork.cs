using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// SkillData partial — 게오르크 스킬 효과 구현 (분노 자원 — 피해를 주고받으며 충전, BattleActions.GainRage).
// 메서드명 = SkillDB.csv의 SkillNo. 서버 코루틴에서만 실행된다.
// RPG_CONVERSION_SKILLS '게오르크 - 공격' 트리 (GS6~GS10, 2026-08-26) + '게오르크 - 투사' 트리 (GS17~GS21, 2026-09-02) + '게오르크 - 방어' 트리 (GS11~GS16, 2026-09-02).
// 구 테스트 스킬(강타 GS2 등)은 제거됨.
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

    // ------------------------------------------------------------- 공격 트리 (GS6~GS10) -------------------------------------------------------------//

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

    // 약점 찌르기 — 기본공격력의 (100/120/140)% 참격 피해 + 자신의 쇠락(공격력 저하) 부여.
    // 저하량은 투사 트리 '쇠락'(GS17)의 습득 레벨을 따른다 — 쇠락을 배우지 않았으면 Lv1 수치(15)
    public static IEnumerator GS8(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
        ApplySoirak(user, target);
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
    // '북방의 위대한 투사'(BOOKBANG 버프)로 상승한 공격력을 2배로 받는다 — 스택 수치를 계수 스탯에 한 번 더 더한다
    public static IEnumerator GS10(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        int damage = BattleActions.SkillDamage(user, skill, user.GetBuffValue(BuffType.BOOKBANG));
        yield return BattleActions.AttackTarget(user, targets[0], damage, skill.attribute);
    }

    // ------------------------------------------------------------- 투사 트리 (GS17~GS21) -------------------------------------------------------------//

    // 쇠락 — 적 하나의 공격력을 (15/20/25) 낮춘다 (3턴). 약점 찌르기(GS8)도 이 수치를 쓴다
    public static IEnumerator GS17(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        ApplySoirak(user, targets[0]);
        yield return new WaitForSeconds(0.4f); // 연출 템포 (피해 없는 스킬 — 모션이 보이도록 잠시 대기)
    }

    // 압도의 일격 — 기본공격력의 (90/100/110)% 참격 피해 + 대상 TP (10/20/30) 감소 (BalanceDB GEORK_OVERWHELM_TP_DAMAGE*)
    public static IEnumerator GS18(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
        if (target == null || target.isDying) yield break;
        int level = Mathf.Min(2, user.player.GetSkillLevel(skill.skillNo));
        int tpDamage = BalanceData.Get("GEORK_OVERWHELM_TP_DAMAGE", 10) + BalanceData.Get("GEORK_OVERWHELM_TP_DAMAGE_PER_LEVEL", 10) * level;
        M_TurnManager.instance.DamageTpTo(target, tpDamage);
    }

    // 북방의 위대한 투사 — 이번 전투 동안 자기 턴이 시작할 때마다 공격력이 (2/4/6)씩 누적 상승 (BOOKBANG 버프 값 = 누적 공격력).
    // 사용한 턴에도 1회 적용해 턴 소모가 헛되지 않게 한다. 이후 턴 시작 누적은 M_TurnManager.ApplyPlayerTurnStartEffects가 담당.
    // 절대자(GS21)를 알면 상승량 2배. 버프는 TargetObject가 전투마다 새로 스폰되므로 자연 리셋
    public static IEnumerator GS19(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        GainNorthWarriorStack(user);
        yield return new WaitForSeconds(0.4f);
    }

    // 기도 — 아군 하나의 공격력을 (10/15/20) 올린다 (3턴, 대상 아군의 턴 종료 기준 — ICHI_ATTACK 양수 지속 버프)
    public static IEnumerator GS20(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject ally = targets[0];
        if (ally == null || ally.isDying || ally.playerHP <= 0) yield break;
        M_TurnManager.instance.ApplyTimedDebuffTo(ally, BuffType.ICHI_ATTACK, SkillLevelValue(user, skill.skillNo),
            BalanceData.Get("GEORK_PRAYER_DURATION", 3), user);
        yield return new WaitForSeconds(0.4f);
    }

    // 절대자 (패시브) — 북방의 위대한 투사의 턴당 상승량 2배. GainNorthWarriorStack이 KnowsSkill로 상시 판정한다 (Passive=1 — 직접 실행되지 않음)
    public static IEnumerator GS21(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        yield break;
    }

    // ------------------------------------------------------------- 방어 트리 (GS11~GS16) -------------------------------------------------------------//

    // 보호 — 아군 하나가 받는 피해의 (20/40/60)%를 대신 받는다 (3턴, 보호받는 아군의 턴 종료 기준).
    // 아군에게 WRAPWINGS 버프(값 = 분담 %, user = 게오르크)를 걸고, TargetObject.DamageToPlayer → RedirectToGuardian이 피해를 나눠 게오르크에게 보낸다
    public static IEnumerator GS11(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject ally = targets[0];
        if (ally == null || ally.isDying || ally.playerHP <= 0 || ally == user) yield break; // 자신은 보호 대상이 될 수 없다
        M_TurnManager.instance.ApplyTimedDebuffTo(ally, BuffType.WRAPWINGS, SkillLevelValue(user, skill.skillNo),
            BalanceData.Get("GEORK_GUARD_DURATION", 3), user);
        yield return new WaitForSeconds(0.4f);
    }

    // 도발 — 적 하나의 예고된 공격 대상을 자신으로 고정한다 (1턴 = 그 적의 다음 행동. 행동 후 SetNextAction이 다시 정한다).
    // 예고 행동이 플레이어 공격이 아니면(방어/모으기 등) 효과가 없다 — SpawnedMonster.TauntBy
    public static IEnumerator GS12(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        TargetObject target = targets[0];
        if (target != null && !target.isDying && target.monster != null) target.monster.TauntBy(user);
        yield return new WaitForSeconds(0.4f);
    }

    // 반격 (패시브) — HP 피해를 입으면 자신을 공격한 적(현재 행동 중인 몬스터)에게 기본 공격력의 (10/20/30)% 반사.
    // TargetObject.DamageToPlayer → CounterAttack이 CounterDamage로 상시 판정한다 (Passive=1 — 직접 실행되지 않음)
    public static IEnumerator GS13(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        yield break;
    }

    // 빈틈없는 자세 — 자신의 방어력 +(10/20/30) 3턴 (ICHI_DEFENSE 양수 — 받는 피해 방어 공식과 방어 행동 실드량에 가산)
    public static IEnumerator GS14(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        M_TurnManager.instance.ApplyTimedDebuffTo(user, BuffType.ICHI_DEFENSE, SkillLevelValue(user, skill.skillNo),
            BalanceData.Get("GEORK_STANCE_DURATION", 3), user);
        yield return new WaitForSeconds(0.4f);
    }

    // 찌르고 막기 — 힘의 (80/100/120)% 관통 피해 + 자신의 방어력 +(15/20/25) 1턴
    public static IEnumerator GS15(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        if (targets.Count == 0) yield break;
        yield return BattleActions.AttackTarget(user, targets[0], BattleActions.SkillDamage(user, skill), skill.attribute);
        GainParryDefense(user, skill);
    }

    // 고통 수집 — 모든 적에게 힘의 (50/60/70)% 참격 피해 + 자신의 방어력 +(15/20/25) 1턴
    public static IEnumerator GS16(SkillDef skill, TargetObject user, List<TargetObject> targets)
    {
        foreach (TargetObject target in targets)
        {
            if (target == null || target.isDying) continue;
            yield return BattleActions.AttackTarget(user, target, BattleActions.SkillDamage(user, skill), skill.attribute);
        }
        GainParryDefense(user, skill);
    }

    // 찌르고 막기/고통 수집 공통 — 방어력 +(15/20/25) 1턴 (BalanceDB GEORK_PARRY_*). 자기 턴 중 건 버프라 이번 턴 종료 감소는 건너뛴다 (M_TurnManager skipFirstTick)
    static void GainParryDefense(TargetObject user, SkillDef skill)
    {
        if (user == null || user.player == null || user.isDying) return;
        int level = Mathf.Min(2, user.player.GetSkillLevel(skill.skillNo));
        int amount = BalanceData.Get("GEORK_PARRY_DEFENSE_UP", 15) + BalanceData.Get("GEORK_PARRY_DEFENSE_UP_PER_LEVEL", 5) * level;
        M_TurnManager.instance.ApplyTimedDebuffTo(user, BuffType.ICHI_DEFENSE, amount, BalanceData.Get("GEORK_PARRY_DURATION", 1), user);
    }

    /// <summary>반격(GS13) 반사 피해량 = 기본 공격력 x (10/20/30)%. 반격을 모르면 0 — TargetObject.CounterAttack이 호출</summary>
    public static int CounterDamage(TargetObject user)
    {
        if (user == null || user.player == null || !user.player.KnowsSkill("GS13")) return 0;
        return BattleActions.BasicAttackDamage(user) * SkillLevelValue(user, "GS13") / 100;
    }

    // ------------------------------------------------------------- 공용 헬퍼 -------------------------------------------------------------//

    /// <summary>북방의 위대한 투사 스택 1회 누적 — GS19 사용 시와 이후 자기 턴 시작마다(M_TurnManager) 호출. 절대자(GS21) 습득 시 2배</summary>
    public static void GainNorthWarriorStack(TargetObject user)
    {
        if (user == null || user.player == null) return;
        int gain = SkillLevelValue(user, "GS19");
        if (user.player.KnowsSkill("GS21")) gain *= 2; // 절대자
        if (gain > 0) user.GainBuff(BuffType.BOOKBANG, gain, false, false, false, false, user);
    }

    // 쇠락 부여 — 저하량 = 쇠락(GS17)의 Power + 레벨 보너스(15/20/25). 몬스터 대상만 (ICHI_ATTACK 음수, 대상 턴 종료 기준 지속)
    static void ApplySoirak(TargetObject user, TargetObject target)
    {
        if (target == null || target.isDying || target.monster == null) return;
        M_TurnManager.instance.ApplyTimedDebuffTo(target, BuffType.ICHI_ATTACK, -SkillLevelValue(user, "GS17"),
            BalanceData.Get("GEORK_SOIRAK_DURATION", 3), user);
    }

    /// <summary>스킬의 Power + PowerPerLevel x 강화 레벨(최대 2) — 피해 계수가 아닌 수치형 효과(저하량/상승량)에 사용.
    /// 해당 스킬을 습득하지 않았으면(약점 찌르기만 배운 상태의 쇠락 등) Lv1 수치</summary>
    static int SkillLevelValue(TargetObject user, string skillNo)
    {
        SkillDef def = Get(skillNo);
        if (def == null) return 0;
        bool knows = user != null && user.player != null && user.player.KnowsSkill(skillNo);
        int level = knows ? Mathf.Min(2, user.player.GetSkillLevel(skillNo)) : 0;
        return def.power + def.powerPerLevel * level;
    }
}
