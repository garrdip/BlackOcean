using System.Collections;
using UnityEngine;
using Mirror;
using ProjectD;

/// <summary>
/// TP 턴제 전투의 공용 전투 계산/실행 유틸 (RPG 전환).
/// 데미지 공식: 스탯(힘/지능) x 계수% x 대열 보정 → 약점 판정(피해 증폭 + TP 브레이크) → TargetObject 피해 경로.
/// 서버 전용 — M_TurnManager의 TP 루프와 SkillData 효과 코루틴이 사용한다.
/// </summary>
public static class BattleActions
{
    /// <summary>스킬 데미지 = 계수 스탯(힘 또는 지능) x power% x 강화 레벨 보너스 x 시전자 대열 보정</summary>
    public static int SkillDamage(TargetObject user, SkillData.SkillDef skill)
    {
        if (user == null || user.player == null) return 0;
        int stat = skill.scalesWithInt ? user.player.TotalIntelligence : user.player.TotalStrength; // 장비 합산치
        int damage = Mathf.Max(1, stat * skill.power / 100);
        int skillLevel = user.player.GetSkillLevel(skill.skillNo); // 스킬트리 SKILL_LEVEL 노드 — 레벨당 +20%
        if (skillLevel > 0)
            damage = damage * (100 + skillLevel * BalanceData.Get("SKILL_LEVEL_BONUS_PERCENT", 20)) / 100;
        return ScaleByRow(user, damage);
    }

    /// <summary>기본 공격 데미지 = 캐릭터 공격 스탯 x BASIC_ATTACK_POWER% x 대열 보정</summary>
    public static int BasicAttackDamage(TargetObject user)
    {
        if (user == null || user.player == null) return 0;
        CharacterStatData.Entry stat = CharacterStatData.Get(user.player.character);
        int attackStat = (stat != null && stat.attackScalesWithInt) ? user.player.TotalIntelligence : user.player.TotalStrength; // 장비 합산치
        return ScaleByRow(user, Mathf.Max(1, attackStat * BalanceData.Get("BASIC_ATTACK_POWER", 100) / 100));
    }

    /// <summary>시전자 대열에 따른 가하는 피해 보정 — 전열 증가/후열 감소 (RPG_CONVERSION_DESIGN 포지션 규칙)</summary>
    public static int ScaleByRow(TargetObject user, int damage)
    {
        return damage * RowAttackPercent(user) / 100;
    }

    static int RowAttackPercent(TargetObject user)
    {
        switch (GetRowIndex(user))
        {
            case 0: return BalanceData.Get("ROW_FRONT_ATTACK_PERCENT", 120);
            case 2: return BalanceData.Get("ROW_BACK_ATTACK_PERCENT", 80);
            default: return 100;
        }
    }

    /// <summary>대상 대열에 따른 받는 피해 보정 — 전열 증가(방어 하락)/후열 감소(방어 증가). TargetObject.DamageToPlayer가 사용</summary>
    public static int RowIncomingDamagePercent(GamePlayer player)
    {
        int index = M_TurnManager.instance != null ? M_TurnManager.instance.playerOrder.IndexOf(player.netId) : -1;
        switch (index)
        {
            case 0: return BalanceData.Get("ROW_FRONT_TAKEN_PERCENT", 120);
            case 2: return BalanceData.Get("ROW_BACK_TAKEN_PERCENT", 80);
            default: return 100;
        }
    }

    /// <summary>플레이어의 대열 인덱스 (0 전열 / 1 중열 / 2 후열, 미소속 -1)</summary>
    public static int GetRowIndex(TargetObject user)
    {
        if (user == null || user.player == null || M_TurnManager.instance == null) return -1;
        return M_TurnManager.instance.playerOrder.IndexOf(user.player.netId);
    }

    /// <summary>
    /// 공격 실행 공통 경로: 약점 판정(피해 증폭 + 대상 TP 브레이크) 후 피해 적용.
    /// 몬스터 대상 전용 (플레이어 피격은 몬스터 AI의 기존 DamageToPlayer 경로 사용).
    /// </summary>
    public static IEnumerator AttackTarget(TargetObject from, TargetObject to, int damage, AttackAttribute attribute)
    {
        if (from == null || to == null || to.isDying) yield break;
        if (to.monster != null && to.monster.monster != null && attribute != AttackAttribute.NONE
            && to.monster.monster.weaknesses.Contains(attribute))
        {
            damage = damage * BalanceData.Get("WEAKNESS_DAMAGE_PERCENT", 120) / 100; // 약점 피해 증폭
            M_TurnManager.instance.ApplyTpBreakTo(to);                               // 대상 TP 감소 (반복 시 효과 체감)
        }
        to.DamageToMonster(damage, from);
        if (from.player != null)
            GainRage(from.player, BalanceData.Get("RAGE_GAIN_ON_DEAL", 5)); // 게오르크 — 피해를 주면 분노 충전
        if (to.monster != null) M_TurnManager.instance.StartCoroutine(to.monster.OnHitAnimation());
        yield return new WaitForSeconds(0.4f); // 타격 템포 (연출 정리 전 임시 간격)
    }

    /// <summary>분노 충전 — 자원이 분노(게오르크)인 캐릭터만. 피해를 주거나(AttackTarget) 받을 때(DamageToPlayer) 호출된다</summary>
    public static void GainRage(GamePlayer player, int amount)
    {
        if (player == null || amount <= 0) return;
        CharacterStatData.Entry stat = CharacterStatData.Get(player.character);
        if (stat == null || stat.resource != BattleResourceType.RAGE) return;
        player.currentResource = Mathf.Min(player.maxResource, player.currentResource + amount);
    }

    /// <summary>속성 한글 표기 (임시 UI/로그용 — 정식 로컬라이제이션은 Phase 7)</summary>
    public static string AttributeName(AttackAttribute attribute)
    {
        switch (attribute)
        {
            case AttackAttribute.SLASH: return "참격";
            case AttackAttribute.STRIKE: return "타격";
            case AttackAttribute.PIERCE: return "관통";
            case AttackAttribute.MAGIC: return "마법";
            case AttackAttribute.RESONANCE: return "공명";
            default: return "무속성";
        }
    }
}
