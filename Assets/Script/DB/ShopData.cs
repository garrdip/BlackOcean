using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 상점 판매 목록 (Resources/DB/ShopDB.csv — 아이템 개편 Phase 7-7, ToDo_RPG_ITEM 0-14, Q20·A8). 1차 ShopId = MERCURIUS.
/// Price를 비우면 EquipDB/ConsumableDB의 Price. 구매 장비는 NORMAL 인스턴스(악세사리는 옵션 1개 굴림). 플레이어 판매가 = EquipDB.Price × EquipGradeDB.SellPercent.
/// </summary>
public static class ShopData
{
    public enum Kind { EQUIP, POTION }

    public class Entry
    {
        public string shopId;
        public Kind kind;
        public string itemNo;
        public int price; // 확정 구매가 (CSV 비면 DB 가격)

        public string DisplayName => kind == Kind.EQUIP ? EquipData.Get(itemNo)?.equipName ?? itemNo : ConsumableData.Get(itemNo)?.potionName ?? itemNo;
    }

    static Dictionary<string, List<Entry>> shops;

    static void EnsureLoaded()
    {
        if (shops != null) return;
        shops = new Dictionary<string, List<Entry>>();
        CsvTable table = CsvTable.LoadFromResources("DB/ShopDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var entry = new Entry
                {
                    shopId = row.Get("ShopId").Trim(),
                    kind = (Kind)Enum.Parse(typeof(Kind), row.Get("Kind").Trim()),
                    itemNo = row.Get("ItemNo").Trim(),
                };
                if (!int.TryParse(row.Get("Price"), out entry.price))
                    entry.price = entry.kind == Kind.EQUIP ? EquipData.Get(entry.itemNo)?.price ?? 0 : ConsumableData.Get(entry.itemNo)?.price ?? 0;
                if ((entry.kind == Kind.EQUIP && EquipData.Get(entry.itemNo) == null) || (entry.kind == Kind.POTION && ConsumableData.Get(entry.itemNo) == null))
                {
                    Debug.LogError($"[ShopData] ShopDB {row.lineNumber}행 — 없는 항목 {entry.kind} {entry.itemNo}");
                    continue;
                }
                if (!shops.TryGetValue(entry.shopId, out List<Entry> list)) shops[entry.shopId] = list = new List<Entry>();
                list.Add(entry);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopData] ShopDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static List<Entry> GetEntries(string shopId)
    {
        EnsureLoaded();
        return shops.TryGetValue(shopId, out List<Entry> list) ? list : new List<Entry>();
    }

    public static Entry Find(string shopId, string itemNo)
    {
        return GetEntries(shopId).Find(entry => entry.itemNo == itemNo);
    }

    /// <summary>플레이어 판매가 = EquipDB.Price × 등급 SellPercent / 100 (최소 1)</summary>
    public static int SellPrice(EquipInstance item)
    {
        EquipData.Def def = EquipData.Get(item.equipNo);
        EquipGradeData.Def grade = EquipGradeData.Get(item.grade);
        if (def == null) return 0;
        return Mathf.Max(1, def.price * (grade != null ? grade.sellPercent : 30) / 100);
    }
}
