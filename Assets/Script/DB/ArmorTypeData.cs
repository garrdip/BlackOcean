using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 방어구 종류 테이블 (Resources/DB/ArmorTypeDB.csv — 아이템 개편 Phase 4-3, 기획 「방어구」).
/// 중갑 = 타격·공명 약점 / 경갑 = 관통·심상 약점 / 로브 = 참격·이치 약점. 세트 규칙(미착용·혼합)은 GamePlayer.GetWeaknesses가 적용한다.
/// </summary>
public static class ArmorTypeData
{
    public class Def
    {
        public ArmorType armorType;
        public List<AttackAttribute> weaknesses = new List<AttackAttribute>();
        public string nameKey;
    }

    static Dictionary<ArmorType, Def> defs;

    static void EnsureLoaded()
    {
        if (defs != null) return;
        defs = new Dictionary<ArmorType, Def>();
        CsvTable table = CsvTable.LoadFromResources("DB/ArmorTypeDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var def = new Def
                {
                    armorType = (ArmorType)Enum.Parse(typeof(ArmorType), row.Get("ArmorType").Trim()),
                    nameKey = row.Get("NameKey").Trim(),
                };
                foreach (string weakness in row.Get("Weaknesses").Split('|'))
                    if (weakness.Trim().Length > 0) def.weaknesses.Add((AttackAttribute)Enum.Parse(typeof(AttackAttribute), weakness.Trim()));
                defs[def.armorType] = def;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ArmorTypeData] ArmorTypeDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static Def Get(ArmorType armorType)
    {
        EnsureLoaded();
        return defs.TryGetValue(armorType, out Def def) ? def : null;
    }

    /// <summary>해당 종류의 약점 속성 목록 (없으면 빈 목록)</summary>
    public static List<AttackAttribute> GetWeaknesses(ArmorType armorType)
    {
        Def def = Get(armorType);
        return def != null ? def.weaknesses : new List<AttackAttribute>();
    }

    /// <summary>전 속성 6종 (방어구 미착용·3종 혼합 — "모든 속성 약점")</summary>
    public static readonly AttackAttribute[] AllAttributes =
    {
        AttackAttribute.SLASH, AttackAttribute.STRIKE, AttackAttribute.PIERCE,
        AttackAttribute.ICHI, AttackAttribute.SIMSANG, AttackAttribute.RESONANCE,
    };
}
