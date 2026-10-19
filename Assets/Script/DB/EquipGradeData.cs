using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 장비 등급 테이블 (Resources/DB/EquipGradeDB.csv — 아이템 개편 Phase 3, ToDo_RPG_ITEM 0-1·3-3, Q3).
/// 등급별 옵션 개수(일반/악세사리/전설)·드랍 가중치(+ 엘리트/보스 보너스)·위험도 하한·판매율·표시 색. BalanceData와 같은 정적 로더 패턴.
/// </summary>
public static class EquipGradeData
{
    public class Def
    {
        public EquipGrade grade;
        public int optionCountMin, optionCountMax;          // 일반 옵션 개수 (무기·방어구)
        public int legendOptionCount;                       // 전설 옵션 개수 (LEGEND만 1)
        public int accessoryOptionCountMin, accessoryOptionCountMax; // 악세사리 일반 옵션 개수 (노멀도 1개)
        public int dropWeight, eliteBonusWeight, bossBonusWeight;    // 등급 굴림 가중치
        public int minHazard;                               // 이번 전투 유효 위험도가 이 값 미만이면 이 등급은 나오지 않는다
        public int sellPercent;                             // 플레이어 판매가 = EquipDB.Price x 이 값 %
        public string colorHex;                             // 이름 색
        public string nameKey;                              // 로컬라이즈 키 (equip.grade.<enum>)

        public Color Color => ColorUtility.TryParseHtmlString(colorHex, out Color color) ? color : Color.white;
    }

    static Dictionary<EquipGrade, Def> grades;

    static void EnsureLoaded()
    {
        if (grades != null) return;
        grades = new Dictionary<EquipGrade, Def>();
        CsvTable table = CsvTable.LoadFromResources("DB/EquipGradeDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var def = new Def
                {
                    grade = (EquipGrade)Enum.Parse(typeof(EquipGrade), row.Get("Grade").Trim()),
                    optionCountMin = row.GetInt("OptionCountMin"), optionCountMax = row.GetInt("OptionCountMax"),
                    legendOptionCount = row.GetInt("LegendOptionCount"),
                    accessoryOptionCountMin = row.GetInt("AccessoryOptionCountMin"), accessoryOptionCountMax = row.GetInt("AccessoryOptionCountMax"),
                    dropWeight = row.GetInt("DropWeight"), eliteBonusWeight = row.GetInt("EliteBonusWeight"), bossBonusWeight = row.GetInt("BossBonusWeight"),
                    minHazard = row.GetInt("MinHazard"), sellPercent = row.GetInt("SellPercent"),
                    colorHex = row.Get("ColorHex").Trim(), nameKey = row.Get("NameKey").Trim(),
                };
                grades[def.grade] = def;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipGradeData] EquipGradeDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static Def Get(EquipGrade grade)
    {
        EnsureLoaded();
        if (grades.TryGetValue(grade, out Def def)) return def;
        Debug.LogError($"[EquipGradeData] EquipGradeDB에 없는 등급: {grade}");
        return null;
    }

    /// <summary>등급 굴림 — DropWeight + (엘리트 처치면 EliteBonusWeight, 보스면 BossBonusWeight) 가중 추출. 유효 위험도 < MinHazard인 등급은 제외. 후보가 없으면 NORMAL</summary>
    public static EquipGrade RollGrade(MonsterGrade killedGrade, int hazard)
    {
        EnsureLoaded();
        int total = 0;
        var weights = new List<(EquipGrade grade, int weight)>();
        foreach (Def def in grades.Values)
        {
            if (hazard < def.minHazard) continue;
            int weight = def.dropWeight + (killedGrade == MonsterGrade.ELITE ? def.eliteBonusWeight : killedGrade == MonsterGrade.BOSS ? def.bossBonusWeight : 0);
            if (weight <= 0) continue;
            weights.Add((def.grade, weight));
            total += weight;
        }
        if (total <= 0) return EquipGrade.NORMAL;
        int roll = UnityEngine.Random.Range(0, total);
        foreach ((EquipGrade grade, int weight) in weights)
        {
            roll -= weight;
            if (roll < 0) return grade;
        }
        return EquipGrade.NORMAL;
    }

    /// <summary>등급 한글 표기 (임시 — 정식 로컬라이즈는 Phase 8 nameKey)</summary>
    public static string DisplayName(EquipGrade grade)
    {
        switch (grade)
        {
            case EquipGrade.ADVANCED: return "고급";
            case EquipGrade.RARE: return "희귀";
            case EquipGrade.LEGEND: return "전설";
            default: return "일반";
        }
    }
}
