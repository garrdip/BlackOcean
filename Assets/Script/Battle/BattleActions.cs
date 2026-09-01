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
    /// <summary>스킬 데미지 = 계수 스탯(힘 또는 지능) x power% x 강화 레벨 보너스 x 시전자 대열 보정.
    /// PowerPerLevel이 정의된 스킬(게오르크 공격트리 등)은 레벨당 계수 자체가 오르고(0/3 — 최대 2강),
    /// 미정의 스킬은 구 방식(레벨당 +20%)을 유지한다</summary>
    public static int SkillDamage(TargetObject user, SkillData.SkillDef skill)
    {
        if (user == null || user.player == null) return 0;
        int stat = skill.scalesWithInt ? user.player.TotalIntelligence : user.player.TotalStrength; // 장비 합산치
        int skillLevel = user.player.GetSkillLevel(skill.skillNo); // 스킬트리 SKILL_LEVEL 노드 + 장비 스킬레벨 옵션
        int power = skill.power;
        if (skill.powerPerLevel > 0)
            power += skill.powerPerLevel * Mathf.Min(2, skillLevel); // 레벨별 계수 (Lv1~Lv3 — 초과분 클램프)
        int damage = Mathf.Max(1, stat * power / 100);
        if (skill.powerPerLevel == 0 && skillLevel > 0)
            damage = damage * (100 + skillLevel * BalanceData.Get("SKILL_LEVEL_BONUS_PERCENT", 20)) / 100;
        return ScaleByErisMode(user, ScaleByRow(user, damage));
    }

    /// <summary>에리스 변신 배율 — 1차 변신(ANGER) +20% / 광기(MAD) +50% (BalanceDB ERIS_*_ATTACK_PERCENT). 다른 캐릭터는 그대로 (RPG_CONVERSION_DESIGN 에리스 변신 매커니즘)</summary>
    public static int ScaleByErisMode(TargetObject user, int damage)
    {
        if (user == null) return damage;
        return damage * user.ErisAttackPercent() / 100;
    }

    /// <summary>기본 공격 데미지 = 캐릭터 공격 스탯 x BASIC_ATTACK_POWER% x 대열 보정</summary>
    public static int BasicAttackDamage(TargetObject user)
    {
        if (user == null || user.player == null) return 0;
        CharacterStatData.Entry stat = CharacterStatData.Get(user.player.character);
        int attackStat = (stat != null && stat.attackScalesWithInt) ? user.player.TotalIntelligence : user.player.TotalStrength; // 장비 합산치
        return ScaleByErisMode(user, ScaleByRow(user, Mathf.Max(1, attackStat * BalanceData.Get("BASIC_ATTACK_POWER", 100) / 100)));
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
            case 2: return BalanceData.Get("ROW_FRONT_ATTACK_PERCENT", 120); // 전열 — 몬스터와 가까운 쪽
            case 0: return BalanceData.Get("ROW_BACK_ATTACK_PERCENT", 80);  // 후열
            default: return 100;
        }
    }

    /// <summary>대상 대열에 따른 받는 피해 보정 — 전열 증가(방어 하락)/후열 감소(방어 증가). TargetObject.DamageToPlayer가 사용</summary>
    public static int RowIncomingDamagePercent(GamePlayer player)
    {
        int index = M_TurnManager.instance != null ? M_TurnManager.instance.playerOrder.IndexOf(player.netId) : -1;
        switch (index)
        {
            case 2: return BalanceData.Get("ROW_FRONT_TAKEN_PERCENT", 120); // 전열 — 몬스터와 가까운 쪽
            case 0: return BalanceData.Get("ROW_BACK_TAKEN_PERCENT", 80);  // 후열
            default: return 100;
        }
    }

    /// <summary>플레이어의 대열 인덱스 (playerOrder[2] = 전열(몬스터와 가까움) / [1] = 중열 / [0] = 후열, 미소속 -1).
    /// 몬스터 타겟 규칙(ActionTarget.FRONT → playerOrder[2])과 동일 기준</summary>
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
        // 피격 시점 지연 — 공격 모션 시작 후 첫 타격을 늦춘다 (에리스 0.5초, M_TurnManager.PlayPlayerActionAnimation이 설정). 한 모션당 한 번만 소비
        if (from.pendingHitDelay > 0f)
        {
            float hitDelay = from.pendingHitDelay;
            from.pendingHitDelay = 0f;
            yield return new WaitForSeconds(hitDelay);
            if (to == null || to.isDying) yield break; // 기다리는 사이 대상이 사라졌으면 중단
        }
        // 기사도 (게오르크 공격트리 패시브 GS7) — 자신을 노리는 적에게 주는 피해 증가 (레벨당 20/30/40%)
        int chivalryPercent = ChivalryBonusPercent(from, to);
        if (chivalryPercent > 0)
            damage = damage * (100 + chivalryPercent) / 100;
        if (to.monster != null && to.monster.monster != null && attribute != AttackAttribute.NONE
            && to.monster.monster.weaknesses.Contains(attribute))
        {
            damage = damage * BalanceData.Get("WEAKNESS_DAMAGE_PERCENT", 120) / 100; // 약점 피해 증폭
            M_TurnManager.instance.ApplyTpBreakTo(to);                               // 대상 TP 감소 (반복 시 효과 체감)
        }
        to.DamageToMonster(damage, from);
        if (from.player != null && to.monster != null)
            GainRageByDamage(from.player, damage, to.monster.MAXHP); // 때릴때 분노 — (데미지/몬스터 최대HP) x 변환제어
        if (to.monster != null) M_TurnManager.instance.StartCoroutine(to.monster.OnHitAnimation());
        yield return new WaitForSeconds(0.4f); // 타격 템포 (연출 정리 전 임시 간격)
    }

    /// <summary>기사도(GS7) 피해 증가 % — 시전자가 게오르크이고 기사도를 습득했으며, 대상 몬스터의 예고 행동이 시전자를 노릴 때만</summary>
    static int ChivalryBonusPercent(TargetObject from, TargetObject to)
    {
        if (from == null || from.player == null || to == null || to.monster == null) return 0;
        SkillData.SkillDef chivalry = SkillData.Get("GS7");
        if (chivalry == null || !from.player.KnowsSkill("GS7")) return 0;
        if (!IsMonsterTargeting(to.monster, from)) return 0;
        return chivalry.power + chivalry.powerPerLevel * Mathf.Min(2, from.player.GetSkillLevel("GS7"));
    }

    /// <summary>몬스터의 예고 행동(nextTarget)이 해당 플레이어를 노리는지 — 자기편 대상/무대상 행동은 false.
    /// 대상 매핑은 몬스터 공격이 실제로 사용하는 M_TurnManager.GetTargetObjectFromActionTarget과 동일 규칙</summary>
    static bool IsMonsterTargeting(SpawnedMonster monster, TargetObject player)
    {
        if (monster == null || player == null) return false;
        ActionTarget next = monster.nextTarget;
        if (next == ActionTarget.FIXEDPLAYER) return monster.nextTargetObject == player;
        if (next != ActionTarget.FRONT && next != ActionTarget.MIDDLE && next != ActionTarget.BACK
            && next != ActionTarget.FRONT_MIDDLE && next != ActionTarget.FRONT_BACK && next != ActionTarget.MIDDLE_BACK
            && next != ActionTarget.WHOLE)
            return false; // NONE/UNDEFINED/WHOLE_ALLY/ENEMY_SINGLE 등 — 플레이어 공격 아님
        foreach (TargetObject target in M_TurnManager.instance.GetTargetObjectFromActionTarget(next))
            if (target == player) return true;
        return false;
    }

    /// <summary>분노 충전 (고정량 — 고행길 등 스킬 효과용). 자원이 분노(게오르크)인 캐릭터만</summary>
    public static void GainRage(GamePlayer player, int amount)
    {
        if (player == null || amount <= 0) return;
        CharacterStatData.Entry stat = CharacterStatData.Get(player.character);
        if (stat == null || stat.resource != BattleResourceType.RAGE) return;
        player.currentResource = Mathf.Min(player.maxResource, player.currentResource + amount);
    }

    /// <summary>분노 생성 공식 (RPG_CONVERSION_BATTLE) — (데미지/기준 최대HP, 1 상한) x 변환제어(제어 + RAGE_CONTROL_OFFSET).
    /// 때릴때 기준 = 몬스터 최대HP(방어 포함 최종 데미지), 맞을때 기준 = 자신의 최대HP(방어력 제외 데미지). 최대치는 maxResource(100)로 클램프</summary>
    public static void GainRageByDamage(GamePlayer player, int damage, int referenceMaxHP)
    {
        if (player == null || damage <= 0 || referenceMaxHP <= 0) return;
        CharacterStatData.Entry stat = CharacterStatData.Get(player.character);
        if (stat == null || stat.resource != BattleResourceType.RAGE) return;
        float ratio = Mathf.Min(1f, (float)damage / referenceMaxHP);
        int convertedControl = player.control + BalanceData.Get("RAGE_CONTROL_OFFSET", 50); // 변환제어
        int amount = Mathf.RoundToInt(ratio * convertedControl);
        player.currentResource = Mathf.Min(player.maxResource, player.currentResource + amount);
    }

    /// <summary>방어력 감소 공식 (RPG_CONVERSION_BATTLE, 물리·마법 공용) —
    /// 절대감소 = 방어력/DMG_FLAT_REDUCE_DIVISOR (소수 버림), 최종 = (데미지 - 절대감소)/(1 + 방어력/DMG_RELATIVE_REDUCE_DIVISOR)</summary>
    public static int ApplyDefenseFormula(int damage, int defenseStat)
    {
        int flatReduce = defenseStat / Mathf.Max(1, BalanceData.Get("DMG_FLAT_REDUCE_DIVISOR", 10));
        float relativeDivisor = Mathf.Max(1, BalanceData.Get("DMG_RELATIVE_REDUCE_DIVISOR", 100));
        return Mathf.Max(0, Mathf.FloorToInt((damage - flatReduce) / (1f + defenseStat / relativeDivisor)));
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
