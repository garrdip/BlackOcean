using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 캐릭터별 기본 스탯/성장치/스킬 포인트/약점·내성/전투 자원 테이블 (Resources/DB/CharacterStatDB.csv).
/// 스탯 6종: 힘(물리 공격력)/민첩(TP 속도)/체력(최대 HP)/지능(마법 공격력)/방어력/마법방어.
/// 레벨업(GamePlayer.AddExp): 성장치(GrowX)만큼 스탯 상승 + SkillPointPerLevel 포인트, BonusSkillPointEvery 레벨마다 보너스 포인트 1.
/// BalanceData와 같은 정적 로더 패턴.
/// </summary>
public static class CharacterStatData
{
    public class Entry
    {
        public Character character;
        public int baseStr, baseAgi, baseVit, baseInt, baseDef, baseMdef; // 1레벨 기본치
        public int growStr, growAgi, growVit, growInt, growDef, growMdef; // 레벨업당 성장치(가중치)
        public int skillPointPerLevel = 1;   // 레벨업당 스킬 포인트
        public int bonusSkillPointEvery = 0; // N레벨마다 보너스 스킬 포인트 +1 (0 = 없음)
        public AttackAttribute weakness; // 피격 시 약점 속성
        public AttackAttribute resist;   // 피격 시 내성 속성
        public BattleResourceType resource; // 전투 자원 종류 (분노/MP/HP)
        public int baseResource;            // 자원 최대치 (HP형은 0 — 자신의 HP를 소모)
        public bool attackScalesWithInt;    // 공격 계수 스탯 — true면 지능, false면 힘 (CSV AttackStat: INT/STR)
        public AttackAttribute basicAttack; // 기본 공격(무기) 속성 — 검(참격)/지팡이(마법)/크리스털 코어(공명)

        /// <summary>해당 레벨에 도달했을 때 지급할 스킬 포인트 (기본 + N레벨 보너스)</summary>
        public int GetSkillPointsForLevel(int reachedLevel)
        {
            int points = skillPointPerLevel;
            if (bonusSkillPointEvery > 0 && reachedLevel % bonusSkillPointEvery == 0) points += 1;
            return points;
        }
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
                    baseStr = row.GetInt("BaseStr"), baseAgi = row.GetInt("BaseAgi"), baseVit = row.GetInt("BaseVit"),
                    baseInt = row.GetInt("BaseInt"), baseDef = row.GetInt("BaseDef"), baseMdef = row.GetInt("BaseMdef"),
                    growStr = row.GetInt("GrowStr"), growAgi = row.GetInt("GrowAgi"), growVit = row.GetInt("GrowVit"),
                    growInt = row.GetInt("GrowInt"), growDef = row.GetInt("GrowDef"), growMdef = row.GetInt("GrowMdef"),
                    weakness = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("Weakness").Trim()),
                    resist = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("Resist").Trim()),
                    resource = (BattleResourceType)Enum.Parse(typeof(BattleResourceType), row.Get("Resource").Trim()),
                    baseResource = row.GetInt("BaseResource"),
                    attackScalesWithInt = row.Get("AttackStat").Trim() == "INT",
                    basicAttack = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), row.Get("BasicAttackAttribute").Trim()),
                };
                if (table.HasColumn("SkillPointPerLevel")) entry.skillPointPerLevel = row.GetInt("SkillPointPerLevel");
                if (table.HasColumn("BonusSkillPointEvery")) entry.bonusSkillPointEvery = row.GetInt("BonusSkillPointEvery");
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
