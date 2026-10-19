using UnityEngine;
using Mirror;
using ProjectD;

// GamePlayer partial — 보조 스탯(회피·명중·치명)과 비전투 회복 경로 (아이템 개편 Phase 1, ToDo_RPG_ITEM 1-1·1-10).
// 전투 중 수치는 TargetObject.Get*(여기 Total* + STAT_* 버프)가 쓰고, 비전투(거점·미로)에서는 TargetObject가 없으므로 GamePlayer가 직접 처리한다 (R1·Q38).
public partial class GamePlayer
{
    // ------------------------------------------------------------- 보조 스탯 (기본치 + 장비 옵션 — 레벨 성장 없음) -------------------------------------------------------------//
    // 장비 옵션(EVADE/ACCURACY/CRIT_RATE/CRIT_DAMAGE) 가산은 Phase 5에서 EquipOptionSum으로 붙는다

    /// <summary>회피율 % — CharacterStatDB BaseEvade (게오르크·홍단향 5 / 에리스 10)</summary>
    public int TotalEvade => CharacterStatData.Get(character)?.baseEvade ?? 5;

    /// <summary>명중률 % — CharacterStatDB BaseAccuracy (전원 95)</summary>
    public int TotalAccuracy => CharacterStatData.Get(character)?.baseAccuracy ?? 95;

    /// <summary>치명타율 % — CharacterStatDB BaseCrit (게오르크·홍단향 5 / 에리스 10)</summary>
    public int TotalCritRate => CharacterStatData.Get(character)?.baseCrit ?? 5;

    /// <summary>치명 배수 % — BalanceDB CRIT_DAMAGE_PERCENT(150). 피해·회복·실드 부여 공용 (Q29)</summary>
    public int TotalCritDamage => BalanceData.Get("CRIT_DAMAGE_PERCENT", 150);

    // ------------------------------------------------------------- 회복 (전투·비전투 공용 진입점) -------------------------------------------------------------//

    /// <summary>HP 회복 — 전투 중(아바타 있음)이면 TargetObject.Heal(치명 판정·표시 포함)로 위임, 비전투면 GamePlayer HP를 직접 올리되 같은 치명 규칙을 적용한다.
    /// healer = 회복을 거는 쪽(물약은 자신). 받는 회복량 옵션(Phase 5-7)은 양쪽에 적용. 반환 = 적용한 회복량</summary>
    [Server]
    public int ServerHeal(int value, GamePlayer healer)
    {
        if (value <= 0) return 0;
        TargetObject self = M_TurnManager.instance != null ? M_TurnManager.instance.GetCurrentPlayerTargetObject(this) : null;
        if (self != null && self.player == this)
        {
            TargetObject healerTarget = healer != null ? (healer == this ? self : M_TurnManager.instance.GetCurrentPlayerTargetObject(healer)) : null;
            return self.Heal(value, healerTarget);
        }
        // 비전투 — TargetObject 없음. 치명률은 healer의 Total* 기준
        bool crit = healer != null && BattleActions.RollChance(healer.TotalCritRate);
        if (crit) value = value * (healer != null ? healer.TotalCritDamage : BalanceData.Get("CRIT_DAMAGE_PERCENT", 150)) / 100;
        value = Mathf.Max(1, value);
        HP = Mathf.Min(TotalMaxHP, HP + value);
        if (crit) Debug.Log($"[Heal] 치명 회복(비전투)! {character} +{value}");
        return value;
    }
}
