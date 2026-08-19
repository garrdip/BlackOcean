using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 소모품 테이블 (Resources/DB/ConsumableDB.csv) — HP/자원 물약. 전투 중 '아이템' 액션과 맵에서 사용.
/// 보유는 GamePlayer.inventoryConsumables(SyncList, 인스턴스당 1행 = 중복이 곧 개수).
/// </summary>
public static class ConsumableData
{
    public class Def
    {
        public string potionNo;
        public string potionName;
        public ConsumableType type;
        public int value;
        public int price;
        public string description;
    }

    static Dictionary<string, Def> potions;

    static void EnsureLoaded()
    {
        if (potions != null) return;
        potions = new Dictionary<string, Def>();
        CsvTable table = CsvTable.LoadFromResources("DB/ConsumableDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var def = new Def
                {
                    potionNo = row.Get("PotionNo").Trim(),
                    potionName = row.Get("Name").Trim(),
                    type = (ConsumableType)Enum.Parse(typeof(ConsumableType), row.Get("Type").Trim()),
                    value = row.GetInt("Value"),
                    price = row.GetInt("Price"),
                    description = row.Get("Description"),
                };
                potions[def.potionNo] = def;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConsumableData] ConsumableDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static Def Get(string potionNo)
    {
        EnsureLoaded();
        return potions.TryGetValue(potionNo, out Def def) ? def : null;
    }

    public static List<Def> GetAll()
    {
        EnsureLoaded();
        return new List<Def>(potions.Values);
    }
}
