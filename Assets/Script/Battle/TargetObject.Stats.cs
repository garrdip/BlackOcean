using UnityEngine;
using Mirror;
using ProjectD;

// TargetObject partial — 플레이어·몬스터 공용 스탯 조회 + 회복/실드 부여 (아이템 개편 Phase 1, ToDo_RPG_ITEM 1-2·1-10).
// 전투 계산(BattleActions.ResolveHit, 피격 함수)은 반드시 이 조회 함수를 거친다 — 플레이어는 GamePlayer.Total*(기본 + 장비) + STAT_* 버프,
// 몬스터는 MonsterStatDB(+ 위험도 보정 — Phase 2) + STAT_* 버프. 보조 스탯(회피·명중·치명)은 레벨 성장이 없다 (기획 「보조 스탯」).
public partial class TargetObject
{
    // ------------------------------------------------------------- 스탯 조회 (서버·클라 공용 — 몬스터 데이터는 서버에만 있다) -------------------------------------------------------------//

    /// <summary>회피율 % — 플레이어 기본 5(에리스 10) + 장비 옵션 + STAT_EVADE 버프 / 몬스터 MonsterStatDB Evade(기본 5) + 버프</summary>
    public int GetEvade()
    {
        int baseValue = player != null ? player.TotalEvade : (monster != null && monster.monster != null ? monster.monster.evade : 5);
        return baseValue + GetBuffValue(BuffType.STAT_EVADE);
    }

    /// <summary>명중률 % — 플레이어 기본 95 + 장비 옵션 + STAT_ACCURACY 버프 / 몬스터 Accuracy(기본 95) + 버프</summary>
    public int GetAccuracy()
    {
        int baseValue = player != null ? player.TotalAccuracy : (monster != null && monster.monster != null ? monster.monster.accuracy : 95);
        return baseValue + GetBuffValue(BuffType.STAT_ACCURACY);
    }

    /// <summary>치명타율 % — 플레이어 기본 5(에리스 10) + 장비 옵션 + STAT_CRIT_RATE 버프 / 몬스터 CritRate(기본 0) + 버프. 약점 보정은 ResolveHit이 더한다</summary>
    public int GetCritRate()
    {
        int baseValue = player != null ? player.TotalCritRate : (monster != null && monster.monster != null ? monster.monster.critRate : 0);
        return baseValue + GetBuffValue(BuffType.STAT_CRIT_RATE);
    }

    /// <summary>치명 배수 % — CRIT_DAMAGE_PERCENT(150) + 플레이어 치명타 데미지 옵션. 피해·회복·실드 부여 공용 (Q29)</summary>
    public int GetCritDamage()
    {
        return player != null ? player.TotalCritDamage : BalanceData.Get("CRIT_DAMAGE_PERCENT", 150);
    }

    /// <summary>물리 방어력 — 플레이어 TotalDefense + 방어력 상승 버프(ICHI_DEFENSE 양수 — 빈틈없는 자세 등, 버프 개편 전 호환) + STAT_DEFENSE 버프(방어 행동·철귀).
    /// 몬스터는 Phase 2 전까지 스탯 0 + STAT_DEFENSE 버프 (실드 defense는 별도)</summary>
    public int GetDefense()
    {
        int baseValue = player != null ? player.TotalDefense + Mathf.Max(0, GetBuffValue(BuffType.ICHI_DEFENSE)) : 0;
        return baseValue + GetBuffValue(BuffType.STAT_DEFENSE);
    }

    /// <summary>마법 방어력 — 플레이어 TotalMagicDefense + STAT_MAGIC_DEFENSE 버프. 몬스터는 Phase 2 전까지 0 + 버프</summary>
    public int GetMagicDefense()
    {
        int baseValue = player != null ? player.TotalMagicDefense : 0;
        return baseValue + GetBuffValue(BuffType.STAT_MAGIC_DEFENSE);
    }

    /// <summary>공격력 버프 합 (플레이어·몬스터 공용) — 기존 ICHI_ATTACK(철귀·쇠락·감시자 등) + BOOKBANG(북방의 위대한 투사 스택) + STAT_ATTACK(공격력 물약 등). 계수 스탯에 플랫 가감</summary>
    public int GetAttackBuff()
    {
        return GetBuffValue(BuffType.ICHI_ATTACK) + GetBuffValue(BuffType.BOOKBANG) + GetBuffValue(BuffType.STAT_ATTACK);
    }

    /// <summary>물리 공격력 (플레이어) — 힘(장비 합산) + 공격력 버프. Phase 3(Q46)에서 무기 공격력이 더해진다</summary>
    public int GetPhysicalAttack()
    {
        if (player == null) return 0;
        return Mathf.Max(0, player.TotalStrength + GetAttackBuff());
    }

    /// <summary>마법 공격력 (플레이어) — 지능(장비 합산) + 공격력 버프. Phase 3(Q46)에서 무기 공격력이 더해진다</summary>
    public int GetMagicAttack()
    {
        if (player == null) return 0;
        return Mathf.Max(0, player.TotalIntelligence + GetAttackBuff());
    }

    // ------------------------------------------------------------- 회복 · 실드 부여 (서버) -------------------------------------------------------------//

    /// <summary>회복 — 시전자(healer) 치명률로 치명 판정, 치명이면 CRIT_DAMAGE_PERCENT 배수 (판정 순서 "회복 : 치명", Q29).
    /// 플레이어·몬스터 공통. healer가 null이면 치명 없음. 반환 = 실제 적용한 회복량 (최대치 클램프 전)</summary>
    [Server]
    public int Heal(int value, TargetObject healer)
    {
        if (value <= 0 || isDying) return 0;
        bool crit = BattleActions.RollCritical(healer, 0);
        if (crit) value = value * (healer != null ? healer.GetCritDamage() : BalanceData.Get("CRIT_DAMAGE_PERCENT", 150)) / 100;
        value = Mathf.Max(1, value);
        if (player != null)
        {
            critDamageDisplay = crit; // HP 훅이 읽는 표시 플래그 (HP 대입 전에 설정)
            overkillDamageDisplay = 0;
            playerHP = Mathf.Min(playerHP + value, playerMaxHP);
        }
        else if (monster != null)
        {
            monster.critDamageDisplay = crit;
            monster.overkillDamageDisplay = 0;
            monster.HP = Mathf.Min(monster.HP + value, monster.MAXHP);
        }
        if (crit) Debug.Log($"[Heal] 치명 회복! {name} +{value}");
        return value;
    }

    /// <summary>실드 부여 — 기존 실드를 지우고 새 값으로 **교체**한다 (합산 아님: 실드 100에 10을 부여하면 10 — Q41).
    /// 시전자(giver) 치명률로 치명 판정, 치명이면 CRIT_DAMAGE_PERCENT 배수 (Q29). 실드는 소진될 때까지 지속한다 (Q37 — 턴 시작 리셋 없음).
    /// 수호 축복(HS4)·만개(HU1)·보호막 옵션(Phase 5)이 이 경로. 몬스터 실드 행동은 Phase 2에서 방어력 버프로 바뀐다. 반환 = 적용한 실드량</summary>
    [Server]
    public int GainShield(int value, TargetObject giver)
    {
        if (value <= 0 || isDying) return 0;
        bool crit = BattleActions.RollCritical(giver, 0);
        if (crit) value = value * (giver != null ? giver.GetCritDamage() : BalanceData.Get("CRIT_DAMAGE_PERCENT", 150)) / 100;
        value = Mathf.Max(1, value);
        defense = value; // 교체 (같은 값이면 SyncVar 변화가 없어 표시만 생략된다)
        if (crit) Debug.Log($"[Shield] 치명 실드! {name} = {value}");
        return value;
    }

    // ------------------------------------------------------------- 표시 RPC -------------------------------------------------------------//

    /// <summary>MISS 표시 — 피격 모션·셰이크·숫자 없이 "MISS" 텍스트만 (ResolveHit이 호출). RpcDisplayZeroDamage와 같은 패턴</summary>
    [ClientRpc]
    public void RpcDisplayMiss()
    {
        M_EffectManager.instance.DisplayMiss(this);
    }
}
