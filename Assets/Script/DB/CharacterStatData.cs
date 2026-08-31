using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 캐릭터별 기본 스탯/성장치/약점·내성/전투 자원 테이블 (Resources/DB/CharacterStatDB.csv) — 모든 캐릭터 스탯의 단일 관리 지점.
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
        public AttackAttribute weakness; // 피격 시 약점 속성
        public AttackAttribute resist;   // 피격 시 내성 속성
        public BattleResourceType resource; // 전투 자원 종류 (분노/MP/HP)
        public int baseResource;            // 자원 최대치 (HP형은 0 — 자신의 HP를 소모)
        public bool attackScalesWithInt;    // 공격 계수 스탯 — true면 지능, false면 힘 (CSV AttackStat: INT/STR)
        public AttackAttribute basicAttack; // 기본 공격(무기) 속성 — 검(참격)/지팡이(마법)/크리스털 코어(공명)
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
                    weakness = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("Weakness").Trim()),
                    resist = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("Resist").Trim()),
                    resource = (BattleResourceType)Enum.Parse(typeof(BattleResourceType), row.Get("Resource").Trim()),
                    baseResource = row.GetInt("BaseResource"),
                    attackScalesWithInt = row.Get("AttackStat").Trim() == "INT",
                    basicAttack = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("BasicAttackAttribute").Trim()),
                };
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
