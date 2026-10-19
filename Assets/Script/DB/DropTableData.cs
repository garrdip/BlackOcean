using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 몬스터별 드랍 테이블 (Resources/DB/DropTableDB.csv — 아이템 개편 Phase 7-2, ToDo_RPG_ITEM 0-12, Q24·Q34·Q39).
/// 행 = 테이블 ID + 종류(EQUIP/POTION) + 항목(특정 EquipNo/PotionNo 또는 풀 지정자 ANY_EQUIP/ANY_WEAPON/ANY_ARMOR/ANY_ACCESSORY/ANY_POTION — DropWeight 가중 추출, 레벨 필터 없음 F3)
/// + Chance %(100 초과분은 추가 1회 판정) + MinHazard(이번 전투 유효 위험도가 미만이면 건너뜀).
/// 몬스터 지정은 MonsterStatDB.DropTable, 비면 DEFAULT_NORMAL / DEFAULT_ELITE / DEFAULT_BOSS. 장비 등급은 EquipGradeData.RollGrade가 따로 굴린다.
/// </summary>
public static class DropTableData
{
    public enum Kind { EQUIP, POTION }

    public class Entry
    {
        public string tableId;
        public Kind kind;
        public string itemNo;
        public int chance;      // % — 100 초과 가능
        public int minHazard;
    }

    /// <summary>굴림 결과 1개 — 장비면 equipNo, 물약이면 potionNo</summary>
    public struct Drop
    {
        public bool isEquip;
        public string itemNo;
    }

    static Dictionary<string, List<Entry>> tables;

    static void EnsureLoaded()
    {
        if (tables != null) return;
        tables = new Dictionary<string, List<Entry>>();
        CsvTable table = CsvTable.LoadFromResources("DB/DropTableDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var entry = new Entry
                {
                    tableId = row.Get("TableId").Trim(),
                    kind = (Kind)Enum.Parse(typeof(Kind), row.Get("Kind").Trim()),
                    itemNo = row.Get("ItemNo").Trim(),
                    chance = row.GetInt("Chance"),
                    minHazard = int.TryParse(row.Get("MinHazard"), out int minHazard) ? minHazard : 0,
                };
                if (entry.tableId.Length == 0) continue;
                if (!tables.TryGetValue(entry.tableId, out List<Entry> list)) tables[entry.tableId] = list = new List<Entry>();
                list.Add(entry);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DropTableData] DropTableDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static bool HasTable(string tableId)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(tableId) && tables.ContainsKey(tableId);
    }

    /// <summary>몬스터의 테이블 ID — MonsterStatDB.DropTable이 있으면 그것, 없으면 등급별 기본</summary>
    public static string TableFor(string dropTable, MonsterGrade grade)
    {
        if (HasTable(dropTable)) return dropTable;
        switch (grade)
        {
            case MonsterGrade.ELITE: return "DEFAULT_ELITE";
            case MonsterGrade.BOSS: return "DEFAULT_BOSS";
            default: return "DEFAULT_NORMAL";
        }
    }

    /// <summary>
    /// 테이블 1회 굴림 (서버). chanceMultiplierPercent = 멀티면 ITEM_DROP_MULTIPLAYER_PERCENT(150), 싱글 100.
    /// 항목마다 확률 = Chance × 배율/100 → 100 이하 몫은 확정, 넘는 몫은 추가 1회 판정 (Q24). 유효 위험도 < MinHazard면 건너뜀
    /// </summary>
    public static List<Drop> Roll(string tableId, int hazard, int chanceMultiplierPercent)
    {
        EnsureLoaded();
        var result = new List<Drop>();
        if (!tables.TryGetValue(tableId, out List<Entry> entries)) return result;
        foreach (Entry entry in entries)
        {
            if (hazard < entry.minHazard) continue;
            int chance = entry.chance * chanceMultiplierPercent / 100;
            int count = chance / 100;
            if (BattleActions.RollChance(chance % 100)) count++;
            for (int i = 0; i < count; i++)
            {
                string itemNo = Resolve(entry);
                if (itemNo != null) result.Add(new Drop { isEquip = entry.kind == Kind.EQUIP, itemNo = itemNo });
            }
        }
        return result;
    }

    // 풀 지정자 → 실제 번호 (DropWeight 가중)
    static string Resolve(Entry entry)
    {
        if (entry.kind == Kind.POTION)
        {
            if (entry.itemNo == "ANY_POTION") return ConsumableData.PickWeighted()?.potionNo;
            return ConsumableData.Get(entry.itemNo) != null ? entry.itemNo : LogMissing(entry);
        }
        switch (entry.itemNo)
        {
            case "ANY_EQUIP": return EquipData.PickWeighted()?.equipNo;
            case "ANY_WEAPON": return EquipData.PickWeighted(def => def.slot == EquipSlot.WEAPON)?.equipNo;
            case "ANY_ARMOR": return EquipData.PickWeighted(def => def.IsArmorPiece)?.equipNo;
            case "ANY_ACCESSORY": return EquipData.PickWeighted(def => def.slot == EquipSlot.ACCESSORY)?.equipNo;
            default: return EquipData.Get(entry.itemNo) != null ? entry.itemNo : LogMissing(entry);
        }
    }

    static string LogMissing(Entry entry)
    {
        Debug.LogError($"[DropTableData] {entry.tableId}: 없는 항목 {entry.kind} {entry.itemNo}");
        return null;
    }
}
