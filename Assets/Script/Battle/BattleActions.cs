using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

/// <summary>
/// TP 턴제 전투의 공용 전투 계산/실행 유틸 (RPG 전환 — 아이템 개편 Phase 1에서 판정 파이프라인 통합, ToDo_RPG_ITEM 1-3).
/// 모든 타격은 HitContext를 만들어 ResolveHit을 거친다: 명중·회피 단일 판정 → 랜덤 보정 r → 약점(피해 120%) → 치명(약점 보정 포함) → 배율
/// → 대상 종류별 피격 함수(방어 측: 대열·보호 분담·실드·방어 공식·HP·분노) → 약점 TP + 치명 TP → 표시.
/// 플레이어 → 몬스터(AttackTarget)와 몬스터 → 플레이어(SpawnedMonster.GeneralAttack), 반격(TargetObject.CounterAttack)이 같은 함수·같은 공식을 쓴다.
/// 서버 전용 — M_TurnManager의 TP 루프와 SkillData 효과 코루틴이 사용한다.
/// </summary>
public static class BattleActions
{
    // ------------------------------------------------------------- 디버그 치트 (호스트 서버에서만 의미 — GamePlayer.DebugStats 토글) -------------------------------------------------------------//
    public static bool debugForcePlayerCrit;   // 플레이어 공격·회복 치명 100%
    public static bool debugForcePlayerEvade;  // 플레이어 회피 100% (몬스터 공격이 전부 MISS)
    public static bool debugNoRandom;          // 랜덤 보정 r = 0 고정

    // ------------------------------------------------------------- 피해량 계산 (공격 측 기본 피해) -------------------------------------------------------------//

    /// <summary>스킬 데미지 = 계수 스탯(힘 또는 지능 — 장비 합산 + 공격력 버프) x power% x 강화 레벨 보너스 x 시전자 대열 보정 x 변신 배율.
    /// PowerPerLevel이 정의된 스킬(게오르크 공격트리 등)은 레벨당 계수 자체가 오르고(0/3 — 최대 2강),
    /// 미정의 스킬은 구 방식(레벨당 +20%)을 유지한다. 판정(명중·치명·r)은 여기가 아니라 ResolveHit이 한다</summary>
    public static int SkillDamage(TargetObject user, SkillData.SkillDef skill, int extraFlatAttack = 0)
    {
        if (user == null || user.player == null) return 0;
        int stat = skill.scalesWithInt ? user.GetMagicAttack() : user.GetPhysicalAttack(); // 장비 합산치 + 공격력 버프 (철귀 +3, 쇠락, 북방의 위대한 투사 스택, 공격력 물약)
        stat = Mathf.Max(0, stat + extraFlatAttack); // extraFlatAttack = 범접할 수 없는 힘의 스택 2배분
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

    /// <summary>기본 공격 데미지 = 캐릭터 공격 스탯(+ 공격력 버프) x BASIC_ATTACK_POWER% x 대열 보정 x 변신 배율.
    /// 계수 스탯은 Phase 3(Q46)까지 CharacterStatDB AttackStat, 이후 장착 무기의 ScaleStat·Attribute가 정한다</summary>
    public static int BasicAttackDamage(TargetObject user)
    {
        if (user == null || user.player == null) return 0;
        CharacterStatData.Entry stat = CharacterStatData.Get(user.player.character);
        int attackStat = (stat != null && stat.attackScalesWithInt) ? user.GetMagicAttack() : user.GetPhysicalAttack();
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

    // ------------------------------------------------------------- 속성 · 약점 -------------------------------------------------------------//

    /// <summary>마법 속성인지 — 마법방어로 경감된다. Phase 4(4-2)에서 ICHI·SIMSANG·RESONANCE로 바뀐다 (지금은 MAGIC·RESONANCE)</summary>
    public static bool IsMagic(AttackAttribute attribute)
    {
        return attribute == AttackAttribute.MAGIC || attribute == AttackAttribute.RESONANCE;
    }

    /// <summary>대상의 약점 속성 목록 — 몬스터 MonsterStatDB Weakness. 플레이어 약점은 Phase 4(방어구 세트, GamePlayer.GetWeaknesses)에서 붙는다 (캐릭터 고유 약점은 폐기 — Q17)</summary>
    public static bool IsWeakTo(TargetObject target, AttackAttribute attribute)
    {
        if (target == null || attribute == AttackAttribute.NONE) return false;
        if (target.monster != null && target.monster.monster != null) return target.monster.monster.weaknesses.Contains(attribute);
        return false;
    }

    // ------------------------------------------------------------- 판정 -------------------------------------------------------------//

    /// <summary>percent% 확률 굴림 (서버)</summary>
    public static bool RollChance(int percent)
    {
        if (percent <= 0) return false;
        if (percent >= 100) return true;
        return Random.Range(0, 100) < percent;
    }

    /// <summary>치명 판정 — 시전자 치명률 + 보정 %p. 시전자가 없으면(고정 효과) 치명 없음. 플레이어 치명 100% 치트 반영</summary>
    public static bool RollCritical(TargetObject user, int bonusPercent)
    {
        if (user == null) return false;
        if (debugForcePlayerCrit && user.player != null) return true;
        return RollChance(user.GetCritRate() + bonusPercent);
    }

    /// <summary>
    /// 타격 판정·적용의 단일 진입점 (서버). 공격 측 계산 후 대상 종류별 피격 함수를 부르고, 약점 TP·치명 TP까지 처리한다.
    /// 반환 = HP에 실제로 들어간 피해 (ctx.dealt). MISS면 0이며 ctx.miss = true — 호출부는 그 타격에 딸린 부가 효과(디버프·TP 감소)를 생략해야 한다 (Q27).
    /// 광역 공격은 대상마다 따로 호출한다 (대상별 개별 판정). 피격 모션은 호출부가 dealt > 0일 때만 재생한다.
    /// </summary>
    public static int ResolveHit(HitContext ctx) // 서버 전용 (호출부가 보장)
    {
        if (ctx == null || ctx.attacker == null || ctx.target == null || ctx.target.isDying) return 0;
        TargetObject from = ctx.attacker;
        TargetObject to = ctx.target;

        // 1. 명중·회피 — 단일 판정 (Q26): MISS 확률 = max(0, 대상 회피 + (100 − 공격자 명중)). 기본(95/5) = 10%
        int missChance = Mathf.Max(0, to.GetEvade() + (100 - from.GetAccuracy()));
        if (debugForcePlayerEvade && to.player != null) missChance = 100;
        if (RollChance(missChance))
        {
            ctx.miss = true;
            to.RpcDisplayMiss();
            Debug.Log($"[Hit] MISS  {UnitName(from)} → {UnitName(to)}  (MISS {missChance}%)");
            return 0;
        }

        // 2. 랜덤 보정 r (Q11) — 타격마다 −R ~ +R 정수
        int range = BalanceData.Get("DAMAGE_RANDOM_PERCENT", 10);
        ctx.randomPercent = debugNoRandom ? 0 : Random.Range(-range, range + 1);

        // 3. 약점 (피해 120%) — 플레이어·몬스터 같은 공식·같은 키 (Q18)
        ctx.weakness = IsWeakTo(to, ctx.attribute);
        // 4. 치명 — 치명률 + 약점 보정 (몬스터 기본 0 + 약점 10%는 의도, Q28)
        ctx.critical = RollCritical(from, ctx.weakness ? BalanceData.Get("WEAKNESS_CRIT_BONUS", 10) : 0);

        // 5. 피해 조립 — 기본 피해 × 추가% × 콤보 배율 × (100 + r)% × 약점 × 치명 (정수 절삭은 단계마다 — 기존 공식 관례)
        int damage = ctx.baseDamage;
        if (ctx.bonusPercent != 0) damage = damage * (100 + ctx.bonusPercent) / 100;
        if (!Mathf.Approximately(ctx.powerMultiplier, 1f)) damage = Mathf.RoundToInt(damage * ctx.powerMultiplier);
        damage = damage * (100 + ctx.randomPercent) / 100;
        if (ctx.weakness) damage = damage * BalanceData.Get("WEAKNESS_DAMAGE_PERCENT", 120) / 100;
        if (ctx.critical) damage = damage * from.GetCritDamage() / 100;
        ctx.finalDamage = Mathf.Max(1, damage);

        // 6. 대상 종류별 피격 함수 — 방어 측(대열·보호 분담·실드·방어 공식·HP·분노)은 여기서
        if (to.player != null)
            ctx.dealt = to.DamageToPlayer(ctx.finalDamage, ctx.attribute, ctx.critical);
        else
            ctx.dealt = to.DamageToMonster(ctx.finalDamage, from, ctx.attribute, ctx.critical);

        // 7. 약점 TP + 치명 TP (약점 TP는 반복 체감, 치명 TP는 체감 없음 — Q12·Q13)
        if (M_TurnManager.instance != null)
        {
            if (ctx.weakness) M_TurnManager.instance.ApplyWeaknessTpDamage(to, ctx.randomPercent);
            if (ctx.critical) M_TurnManager.instance.DamageTpTo(to, BalanceData.Get("CRIT_TP_DAMAGE", 5));
        }

        // 8. 때릴 때 분노 (게오르크) — HP에 들어간 최종 피해 기준 (Q32), 기준 최대 HP = 대상 몬스터
        if (from.player != null && to.monster != null && ctx.dealt > 0)
            GainRageByDamage(from.player, ctx.dealt, to.monster.MAXHP);

        Debug.Log($"[Hit] {UnitName(from)} → {UnitName(to)}  기본 {ctx.baseDamage}  r {ctx.randomPercent:+0;-0;0}%{(ctx.weakness ? " 약점" : "")}{(ctx.critical ? " 치명" : "")}  → {ctx.finalDamage}  HP 피해 {ctx.dealt}  (MISS {missChance}% / 치명 {from.GetCritRate() + (ctx.weakness ? BalanceData.Get("WEAKNESS_CRIT_BONUS", 10) : 0)}%)");
        return ctx.dealt;
    }

    static string UnitName(TargetObject unit)
    {
        if (unit == null) return "?";
        if (unit.player != null) return unit.player.character.ToString();
        if (unit.monster != null) return unit.monster.monsterName;
        return unit.name;
    }

    // ------------------------------------------------------------- 플레이어 → 몬스터 공격 (코루틴 — 피격 지연·타격 템포 포함) -------------------------------------------------------------//

    /// <summary>스킬 공격 — SkillCast의 콤보 배율을 HitContext에 싣는다 (isSkill = true)</summary>
    public static IEnumerator AttackTarget(SkillData.SkillCast cast, TargetObject to, int damage)
    {
        return AttackTarget(cast.user, to, damage, cast.def.attribute, true, cast.powerMultiplier);
    }

    /// <summary>
    /// 공격 실행 공통 경로 (플레이어 → 몬스터): 피격 지연 → 기사도 가산 → ResolveHit → 피격 모션 → 타격 템포.
    /// 다단 공격은 타수만큼 호출한다 (타마다 r·치명이 따로 굴려진다). 호출 뒤 대상의 부가 효과를 걸려면 LastHitLanded로 MISS 여부를 확인한다
    /// </summary>
    public static IEnumerator AttackTarget(TargetObject from, TargetObject to, int damage, AttackAttribute attribute, bool isSkill = false, float powerMultiplier = 1f)
    {
        lastHit = null;
        if (from == null || to == null || to.isDying) yield break;
        // 피격 시점 지연 — 공격 모션 시작 후 첫 타격을 늦춘다 (에리스 0.5초, M_TurnManager.PlayPlayerActionAnimation이 설정). 한 모션당 한 번만 소비
        if (from.pendingHitDelay > 0f)
        {
            float hitDelay = from.pendingHitDelay;
            from.pendingHitDelay = 0f;
            yield return new WaitForSeconds(hitDelay);
            if (to == null || to.isDying) yield break; // 기다리는 사이 대상이 사라졌으면 중단
        }
        HitContext ctx = new HitContext
        {
            attacker = from, target = to, baseDamage = damage, attribute = attribute,
            isSkill = isSkill, powerMultiplier = powerMultiplier,
            bonusPercent = ChivalryBonusPercent(from, to), // 기사도 (게오르크 공격트리 패시브 GS7) — 자신을 노리는 적에게 주는 피해 증가 (레벨당 20/30/40%)
        };
        ResolveHit(ctx);
        lastHit = ctx;
        if (ctx.dealt > 0 && to.monster != null) M_TurnManager.instance.StartCoroutine(to.monster.OnHitAnimation());
        yield return new WaitForSeconds(0.4f); // 타격 템포 (연출 정리 전 임시 간격)
    }

    /// <summary>직전 AttackTarget의 판정 결과 (서버 코루틴 순차 실행 전제) — 스킬이 "피해 후 디버프" 같은 부가 효과를 MISS 때 생략하는 데 쓴다 (Q27)</summary>
    public static HitContext lastHit;
    public static bool LastHitLanded => lastHit != null && lastHit.Landed;

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

    // ------------------------------------------------------------- 자원 -------------------------------------------------------------//

    /// <summary>분노 충전 (고정량 — 고행길 등 스킬 효과용). 자원이 분노(게오르크)인 캐릭터만</summary>
    public static void GainRage(GamePlayer player, int amount)
    {
        if (player == null || amount <= 0) return;
        CharacterStatData.Entry stat = CharacterStatData.Get(player.character);
        if (stat == null || stat.resource != BattleResourceType.RAGE) return;
        player.currentResource = Mathf.Min(player.maxResource, player.currentResource + amount);
    }

    /// <summary>분노 생성 공식 (RPG_CONVERSION_BATTLE) — (데미지/기준 최대HP, 1 상한) x 변환제어(제어 + RAGE_CONTROL_OFFSET).
    /// 때릴 때 기준 = 몬스터 최대HP, 맞을 때 기준 = 자신의 최대HP. 데미지는 모든 계산이 끝나 HP에 들어간 최종 피해 (실드 흡수분 제외 — Q32). 최대치는 maxResource(100)로 클램프</summary>
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

    // ------------------------------------------------------------- 방어 공식 -------------------------------------------------------------//

    /// <summary>방어력 감소 공식 (RPG_CONVERSION_BATTLE, 물리·마법 공용) —
    /// 절대감소 = 방어력/DMG_FLAT_REDUCE_DIVISOR (소수 버림), 최종 = (데미지 - 절대감소)/(1 + 방어력/DMG_RELATIVE_REDUCE_DIVISOR)</summary>
    public static int ApplyDefenseFormula(int damage, int defenseStat)
    {
        int flatReduce = defenseStat / Mathf.Max(1, BalanceData.Get("DMG_FLAT_REDUCE_DIVISOR", 10));
        float relativeDivisor = Mathf.Max(1, BalanceData.Get("DMG_RELATIVE_REDUCE_DIVISOR", 100));
        return Mathf.Max(0, Mathf.FloorToInt((damage - flatReduce) / (1f + defenseStat / relativeDivisor)));
    }

    /// <summary>치명 여부를 반영한 방어 공식 — 치명이면 대상 방어력을 CRIT_DEFENSE_PERCENT(50)%만 적용하고 CRIT_DEFENSE_FLAT_REDUCE(10)를 더 뺀다 (0 하한). 플레이어·몬스터 공용 (Q14)</summary>
    public static int ApplyDefenseFormula(int damage, int defenseStat, bool isCritical)
    {
        return ApplyDefenseFormula(damage, EffectiveDefense(defenseStat, isCritical));
    }

    /// <summary>치명 시 유효 방어력 = max(0, 방어력 × CRIT_DEFENSE_PERCENT/100 − CRIT_DEFENSE_FLAT_REDUCE)</summary>
    public static int EffectiveDefense(int defenseStat, bool isCritical)
    {
        if (!isCritical) return Mathf.Max(0, defenseStat);
        return Mathf.Max(0, defenseStat * BalanceData.Get("CRIT_DEFENSE_PERCENT", 50) / 100 - BalanceData.Get("CRIT_DEFENSE_FLAT_REDUCE", 10));
    }

    /// <summary>속성 한글 표기 (임시 UI/로그용 — 정식 로컬라이제이션은 Phase 8)</summary>
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
