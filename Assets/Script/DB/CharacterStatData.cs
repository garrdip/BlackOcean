using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 캐릭터별 기본 스탯/성장치/보조 스탯/전투 자원 테이블 (Resources/DB/CharacterStatDB.csv) — 모든 캐릭터 스탯의 단일 관리 지점.
/// 스탯 6종 + HP: 힘(물리 공격력)/민첩(TP 속도)/지능(마법 공격력)/방어력/마법방어/제어(MP 회복·분노 생성) + 최대 HP(BaseHP = 1레벨 최대 HP).
/// (구 '체력(Vit)' 스탯은 최대 HP 외 용도가 없어 폐기 — 2026-08-31. HP가 직접 성장/스킬트리 대상이 된다)
/// 레벨업(GamePlayer.AddExp): GrowX = 1레벨 → 최대 레벨(LevelDB)까지의 "총" 성장치 (GrowHP = 최대 HP 총 성장치). 레벨별 상승량은 LevelGrowthTable이 시드 기반으로 랜덤 분배하고
/// 만렙에서의 합은 항상 GrowX (편차 폭은 BalanceDB LEVEL_GROWTH_VARIANCE_PERCENT).
/// (스킬 포인트는 BalanceDB SKILL_POINTS_PER_LEVEL 고정 — 구 SkillPointPerLevel 컬럼은 폐기됨)
/// BalanceData와 같은 정적 로더 패턴.
/// </summary>
public static class CharacterStatData
{
    public class Entry
    {
        public Character character;
        public int baseHP = 50;                                                     // 1레벨 최대 HP
        public int baseStr, baseAgi, baseInt, baseDef, baseMdef, baseCtrl;          // 1레벨 기본치
        public int growHP;                                                          // 1레벨 → 최대 레벨까지 최대 HP 총 성장치
        public int growStr, growAgi, growInt, growDef, growMdef, growCtrl;          // 1레벨 → 최대 레벨까지 총 성장치 (레벨별 분배는 LevelGrowthTable)
        // (캐릭터 고유 약점·내성(Weakness/Resist)과 기본 공격 계수·속성(AttackStat/BasicAttackAttribute) 컬럼은 아이템 개편 Phase 4에서 삭제 — 약점은 방어구 세트(GamePlayer.GetWeaknesses), 평타는 장착 무기가 정한다 (Q17·Q25))
        public BattleResourceType resource; // 전투 자원 종류 (분노/MP/HP)
        public int baseResource;            // 자원 최대치 (HP형은 0 — 자신의 HP를 소모)
        // 보조 스탯 (아이템 개편 Phase 1-1, 기획 「보조 스탯」 — 레벨 성장 없음). 컬럼이 없으면 기본값 5 / 95 / 5
        public int baseEvade = 5;           // 회피율 %
        public int baseAccuracy = 95;       // 명중률 %
        public int baseCrit = 5;            // 치명타율 %
    }

    static Dictionary<Character, Entry> entries;

    static void EnsureLoaded()
    {
        if (entries != null) return;
        entries = new Dictionary<Character, Entry>();
        CsvTable table = CsvTable.LoadFromResources("DB/CharacterStatDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var entry = new Entry
                {
                    character = (Character)Enum.Parse(typeof(Character), row.Get("Character").Trim()),
                    baseHP = row.GetInt("BaseHP"),
                    baseStr = row.GetInt("BaseStr"), baseAgi = row.GetInt("BaseAgi"),
                    baseInt = row.GetInt("BaseInt"), baseDef = row.GetInt("BaseDef"), baseMdef = row.GetInt("BaseMdef"),
                    baseCtrl = row.GetInt("BaseCtrl"),
                    growHP = row.GetInt("GrowHP"),
                    growStr = row.GetInt("GrowStr"), growAgi = row.GetInt("GrowAgi"),
                    growInt = row.GetInt("GrowInt"), growDef = row.GetInt("GrowDef"), growMdef = row.GetInt("GrowMdef"),
                    growCtrl = row.GetInt("GrowCtrl"),
                    resource = (BattleResourceType)Enum.Parse(typeof(BattleResourceType), row.Get("Resource").Trim()),
                    baseResource = row.GetInt("BaseResource"),
                };
                if (table.HasColumn("BaseEvade")) entry.baseEvade = row.GetInt("BaseEvade");
                if (table.HasColumn("BaseAccuracy")) entry.baseAccuracy = row.GetInt("BaseAccuracy");
                if (table.HasColumn("BaseCrit")) entry.baseCrit = row.GetInt("BaseCrit");
                entries[entry.character] = entry;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CharacterStatData] CharacterStatDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    /// <summary>캐릭터 스탯 엔트리 조회. 없으면 에러 로그 후 null (호출부는 기존 BalanceDB 초기값으로 폴백)</summary>
    public static Entry Get(Character character)
    {
        EnsureLoaded();
        if (entries.TryGetValue(character, out Entry entry)) return entry;
        Debug.LogError($"[CharacterStatData] CharacterStatDB에 없는 캐릭터: {character}");
        return null;
    }
}
