using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 장비 테이블 (Resources/DB/EquipDB.csv) — 무기(캐릭터 고정) + 방어구(갑옷/투구/신발/악세사리x2 공용).
/// 스탯 가산형: Attack은 착용 캐릭터의 공격 스탯(힘 또는 지능)에 가산된다.
/// 착용/보유 상태는 GamePlayer.Equipment.cs(SyncList), 합산은 GamePlayer.Total* 프로퍼티가 담당.
/// </summary>
public static class EquipData
{
    public class Def
    {
        public string equipNo;
        public string equipName;
        public EquipSlot slot;
        public Character character;   // NONE = 공용 (방어구), 무기는 캐릭터 고정
        public int requireLevel;
        public ItemGrade grade;
        public int attack;            // 공격 스탯 가산 (캐릭터의 AttackStat 쪽에 붙는다)
        public int agility;
        public int defense;
        public int magicDefense;
        public int maxHP;
        public int maxResource;
        public int skillLevel;        // 모든 스킬 강화 레벨 가산
        public int price;
        public string description;
    }

    static Dictionary<string, Def> equips;

    static void EnsureLoaded()
    {
        if (equips != null) return;
        equips = new Dictionary<string, Def>();
        CsvTable table = CsvTable.LoadFromResources("DB/EquipDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var def = new Def
                {
                    equipNo = row.Get("EquipNo").Trim(),
                    equipName = row.Get("Name").Trim(),
                    slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), row.Get("Slot").Trim()),
                    character = (Character)Enum.Parse(typeof(Character), row.Get("Character").Trim()),
                    requireLevel = row.GetInt("RequireLevel"),
                    grade = (ItemGrade)Enum.Parse(typeof(ItemGrade), row.Get("Grade").Trim()),
                    attack = row.GetInt("Attack"),
                    agility = row.GetInt("Agility"),
                    defense = row.GetInt("Defense"),
                    magicDefense = row.GetInt("MagicDefense"),
                    maxHP = row.GetInt("MaxHP"),
                    maxResource = row.GetInt("MaxResource"),
                    skillLevel = row.GetInt("SkillLevel"),
                    price = row.GetInt("Price"),
                    description = row.Get("Description"),
                };
                equips[def.equipNo] = def;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipData] EquipDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    public static Def Get(string equipNo)
    {
        EnsureLoaded();
        return equips.TryGetValue(equipNo, out Def def) ? def : null;
    }

    /// <summary>해당 캐릭터가 착용 가능한 장비 목록 (공용 + 캐릭터 전용)</summary>
    public static List<Def> GetUsableBy(Character character)
    {
        EnsureLoaded();
        var result = new List<Def>();
        foreach (Def def in equips.Values)
            if (def.character == Character.NONE || def.character == character) result.Add(def);
        return result;
    }
}
