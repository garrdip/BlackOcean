using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 장비 기본 데이터 테이블 (Resources/DB/EquipDB.csv — 아이템 개편 Phase 3, ToDo_RPG_ITEM 0-4·3-4, Q22·Q25·Q46).
/// 행 = 기본 옵션만: 무기는 종류·속성·계수 스탯(ScaleStat)·물리/마법 공격력, 방어구는 종류(중갑/경갑/로브)·힘 요구치·방어력·마법방어, 악세사리는 수치 없음.
/// 착용 제한 없음(캐릭터 컬럼 삭제). 등급과 추가 옵션은 EquipInstance가 가진다. 보유/착용은 GamePlayer.Equipment.cs, 합산은 GamePlayer.Total*.
/// </summary>
public static class EquipData
{
    public class Def
    {
        public string equipNo;
        public string equipName;
        public EquipSlot slot;
        public WeaponType weaponType;     // 무기 행만 (방어구·악세사리 NONE)
        public ArmorType armorType;       // 갑옷·투구·신발 행만 — 세트 약점 판정 (Phase 4)
        public AttackAttribute attribute; // 무기 행: 기본 공격 속성 (Q25). 그 외 NONE
        public bool scalesWithInt;        // 무기 행: 평타 계수 스탯 — ScaleStat INT면 지능, STR이면 힘 (Q46). 핵은 공명 속성 + STR
        public int requireLevel;          // 착용 조건 (드랍 레벨 필터는 없다 — F3)
        public int requireStr;            // 방어구 힘 요구치 — 미달 부위가 있으면 민첩 ½ (Phase 4, Q16·Q42)
        public int physicalAttack;        // 무기 물리공격력 — 최종 물리공격력 = 힘 + Σ
        public int magicAttack;           // 무기 마법공격력 — 최종 마법공격력 = 지능 + Σ
        public int defense;
        public int magicDefense;
        public int price;                 // 상점 구매가. 판매가 = Price x EquipGradeDB.SellPercent
        public int dropWeight;            // 풀 지정자(ANY_*) 드랍·상점 가중치. 0 = 풀에서 제외
        public string description;

        public bool IsWeapon => slot == EquipSlot.WEAPON;
        public bool IsArmorPiece => slot == EquipSlot.ARMOR || slot == EquipSlot.HELMET || slot == EquipSlot.BOOTS;
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
                string attribute = row.Get("Attribute").Trim();
                string weaponType = row.Get("WeaponType").Trim();
                string armorType = row.Get("ArmorType").Trim();
                var def = new Def
                {
                    equipNo = row.Get("EquipNo").Trim(),
                    equipName = row.Get("Name").Trim(),
                    slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), row.Get("Slot").Trim()),
                    weaponType = weaponType.Length == 0 ? WeaponType.NONE : (WeaponType)Enum.Parse(typeof(WeaponType), weaponType),
                    armorType = armorType.Length == 0 ? ArmorType.NONE : (ArmorType)Enum.Parse(typeof(ArmorType), armorType),
                    attribute = attribute.Length == 0 ? AttackAttribute.NONE : (AttackAttribute)Enum.Parse(typeof(AttackAttribute), attribute),
                    scalesWithInt = row.Get("ScaleStat").Trim() == "INT",
                    requireLevel = row.GetInt("RequireLevel"),
                    requireStr = row.GetInt("RequireStr"),
                    physicalAttack = row.GetInt("PhysicalAttack"),
                    magicAttack = row.GetInt("MagicAttack"),
                    defense = row.GetInt("Defense"),
                    magicDefense = row.GetInt("MagicDefense"),
                    price = row.GetInt("Price"),
                    dropWeight = row.GetInt("DropWeight"),
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

    public static List<Def> GetAll()
    {
        EnsureLoaded();
        return new List<Def>(equips.Values);
    }

    /// <summary>조건에 맞는 장비 중 DropWeight 가중 랜덤 1개 (DropWeight 0은 제외). 없으면 null — 드랍 풀(ANY_*)·상점이 사용</summary>
    public static Def PickWeighted(Predicate<Def> filter = null)
    {
        EnsureLoaded();
        int total = 0;
        var pool = new List<Def>();
        foreach (Def def in equips.Values)
        {
            if (def.dropWeight <= 0 || (filter != null && !filter(def))) continue;
            pool.Add(def);
            total += def.dropWeight;
        }
        if (total <= 0) return null;
        int roll = UnityEngine.Random.Range(0, total);
        foreach (Def def in pool)
        {
            roll -= def.dropWeight;
            if (roll < 0) return def;
        }
        return pool[pool.Count - 1];
    }

    /// <summary>슬롯 한글 표기 (임시 UI)</summary>
    public static string SlotName(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.WEAPON: return "무기";
            case EquipSlot.ARMOR: return "갑옷";
            case EquipSlot.HELMET: return "투구";
            case EquipSlot.BOOTS: return "신발";
            case EquipSlot.ACCESSORY: return "악세사리";
            default: return slot.ToString();
        }
    }

    /// <summary>무기/방어구 종류 한글 표기 (임시 UI — 정식 로컬라이즈는 Phase 8)</summary>
    public static string KindName(Def def)
    {
        if (def == null) return "";
        switch (def.weaponType)
        {
            case WeaponType.GREATSWORD: return "대검";
            case WeaponType.LONGSWORD: return "장검";
            case WeaponType.STAFF: return "지팡이";
            case WeaponType.BOOK: return "책";
            case WeaponType.ORB: return "수정구";
            case WeaponType.CORE: return "핵";
        }
        switch (def.armorType)
        {
            case ArmorType.HEAVY: return "중갑";
            case ArmorType.LIGHT: return "경갑";
            case ArmorType.ROBE: return "로브";
        }
        return "";
    }
}
