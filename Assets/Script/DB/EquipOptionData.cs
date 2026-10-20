using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 장비 옵션 테이블 (Resources/DB/EquipOptionDB.csv — 아이템 개편 Phase 3, ToDo_RPG_ITEM 0-3·3-3, Q3).
/// 옵션 x 등급마다 한 행: 허용 슬롯, 수치 범위(Min~Max), 값 종류(NUMBER / ATTRIBUTE / FLAG), 추출 가중치, 속성 후보(ATTRIBUTE형).
/// 일반 옵션은 NORMAL(악세사리 전용)·ADVANCED·RARE·LEGEND 행, 전설 옵션은 LEGEND 행만 둔다. 등급마다 범위가 다르다.
/// </summary>
public static class EquipOptionData
{
    public class Def
    {
        public EquipOptionType type;
        public EquipGrade grade;
        public List<EquipSlot> slots = new List<EquipSlot>();
        public int minValue, maxValue;
        public bool isPercent;
        public EquipValueKind valueKind;
        public int weight;
        public List<(AttackAttribute attribute, int weight)> attributePool = new List<(AttackAttribute, int)>();
        public string nameKey;

        public bool AllowsSlot(EquipSlot slot) => slots.Contains(slot);

        /// <summary>값 굴림 — NUMBER는 Min~Max 균등, ATTRIBUTE는 후보 가중 추출, FLAG는 1</summary>
        public int RollValue()
        {
            switch (valueKind)
            {
                case EquipValueKind.FLAG: return 1;
                case EquipValueKind.ATTRIBUTE:
                {
                    int total = 0;
                    foreach ((AttackAttribute _, int w) in attributePool) total += w;
                    if (total <= 0) return (int)AttackAttribute.NONE;
                    int roll = UnityEngine.Random.Range(0, total);
                    foreach ((AttackAttribute attribute, int w) in attributePool)
                    {
                        roll -= w;
                        if (roll < 0) return (int)attribute;
                    }
                    return (int)AttackAttribute.NONE;
                }
                default: return UnityEngine.Random.Range(minValue, maxValue + 1);
            }
        }
    }

    /// <summary>전설 옵션 집합 (기획 「장비 옵션」) — LEGEND 등급의 마지막 1칸에서만 굴린다</summary>
    static readonly HashSet<EquipOptionType> legendTypes = new HashSet<EquipOptionType>
    {
        EquipOptionType.LIFE_STEAL, EquipOptionType.SKILL_DAMAGE, EquipOptionType.COMBO_DAMAGE, EquipOptionType.CRIT_DAMAGE,
        EquipOptionType.REFLECT_FLAT, EquipOptionType.REFLECT_PERCENT, EquipOptionType.HEAL_RECEIVED, EquipOptionType.WEAKNESS_NULLIFY,
        EquipOptionType.CRIT_RATE, EquipOptionType.ACCURACY, EquipOptionType.EVADE, EquipOptionType.FREE_MOVE, EquipOptionType.NO_ROW_PENALTY, EquipOptionType.MAX_RAGE,
    };

    public static bool IsLegendOption(EquipOptionType type) => legendTypes.Contains(type);

    static List<Def> defs;

    static void EnsureLoaded()
    {
        if (defs != null) return;
        defs = new List<Def>();
        CsvTable table = CsvTable.LoadFromResources("DB/EquipOptionDB");
        foreach (CsvTable.Row row in table.rows)
        {
            try
            {
                var def = new Def
                {
                    type = (EquipOptionType)Enum.Parse(typeof(EquipOptionType), row.Get("OptionType").Trim()),
                    grade = (EquipGrade)Enum.Parse(typeof(EquipGrade), row.Get("Grade").Trim()),
                    minValue = row.GetInt("MinValue"), maxValue = row.GetInt("MaxValue"),
                    isPercent = row.Get("IsPercent").Trim() == "1",
                    valueKind = (EquipValueKind)Enum.Parse(typeof(EquipValueKind), row.Get("ValueKind").Trim()),
                    weight = row.GetInt("Weight"),
                    nameKey = row.Get("NameKey").Trim(),
                };
                foreach (string slot in row.Get("Slots").Split('|'))
                    if (slot.Trim().Length > 0) def.slots.Add((EquipSlot)Enum.Parse(typeof(EquipSlot), slot.Trim()));
                foreach (string entry in row.Get("AttributePool").Split('|'))
                {
                    if (entry.Trim().Length == 0) continue;
                    string[] parts = entry.Split(':');
                    AttackAttribute attribute = (AttackAttribute)Enum.Parse(typeof(AttackAttribute), parts[0].Trim());
                    int weight = parts.Length > 1 && int.TryParse(parts[1], out int w) ? w : 1;
                    def.attributePool.Add((attribute, weight));
                }
                defs.Add(def);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EquipOptionData] EquipOptionDB 로드 실패 ({row.lineNumber}행) — {e.Message}");
            }
        }
    }

    /// <summary>해당 슬롯·등급에서 굴릴 수 있는 옵션 행 — legend = true면 전설 옵션만, false면 일반 옵션만</summary>
    public static List<Def> GetRollable(EquipSlot slot, EquipGrade grade, bool legend)
    {
        EnsureLoaded();
        var result = new List<Def>();
        foreach (Def def in defs)
            if (def.grade == grade && IsLegendOption(def.type) == legend && def.AllowsSlot(slot) && def.weight > 0)
                result.Add(def);
        return result;
    }

    /// <summary>특정 옵션·등급 행 (표시용)</summary>
    public static Def Get(EquipOptionType type, EquipGrade grade)
    {
        EnsureLoaded();
        return defs.Find(def => def.type == type && def.grade == grade);
    }

    /// <summary>옵션 표기 — 로컬라이즈 `equip.option.&lt;enum&gt;.name` (ko 기본값은 코드)</summary>
    public static string DisplayName(EquipOptionType type)
    {
        return M_LanguageManager.Get($"equip.option.{type}.name", KoreanName(type));
    }

    static string KoreanName(EquipOptionType type)
    {
        switch (type)
        {
            case EquipOptionType.STRENGTH: return "힘";
            case EquipOptionType.INTELLIGENCE: return "지능";
            case EquipOptionType.AGILITY: return "민첩";
            case EquipOptionType.CONTROL: return "제어";
            case EquipOptionType.MAX_HP: return "최대 HP";
            case EquipOptionType.MAX_MP: return "최대 MP";
            case EquipOptionType.DEFENSE: return "방어력";
            case EquipOptionType.SHIELD: return "보호막";
            case EquipOptionType.LIFE_STEAL: return "생명력 흡수";
            case EquipOptionType.SKILL_DAMAGE: return "스킬 추가 데미지";
            case EquipOptionType.COMBO_DAMAGE: return "콤보 추가 데미지";
            case EquipOptionType.CRIT_DAMAGE: return "치명타 데미지";
            case EquipOptionType.REFLECT_FLAT: return "반사 데미지(고정)";
            case EquipOptionType.REFLECT_PERCENT: return "반사 데미지(비례)";
            case EquipOptionType.HEAL_RECEIVED: return "받는 회복량";
            case EquipOptionType.WEAKNESS_NULLIFY: return "약점 상쇄";
            case EquipOptionType.CRIT_RATE: return "치명타 확률";
            case EquipOptionType.ACCURACY: return "명중률";
            case EquipOptionType.EVADE: return "회피율";
            case EquipOptionType.FREE_MOVE: return "이동 시 턴 소모 제거";
            case EquipOptionType.NO_ROW_PENALTY: return "포지션 패널티 제거";
            case EquipOptionType.MAX_RAGE: return "분노 최대치";
            default: return type.ToString();
        }
    }

    /// <summary>옵션 1개 표시 문자열 — "힘 +3", "치명타 데미지 +20%", "약점 상쇄 : 공명", "이동 시 턴 소모 제거"</summary>
    public static string Format(EquipOptionType type, int value, EquipGrade grade)
    {
        Def def = Get(type, grade);
        EquipValueKind kind = def != null ? def.valueKind : EquipValueKind.NUMBER;
        switch (kind)
        {
            case EquipValueKind.FLAG: return DisplayName(type);
            case EquipValueKind.ATTRIBUTE: return $"{DisplayName(type)} : {BattleActions.AttributeName((AttackAttribute)value)}";
            default: return $"{DisplayName(type)} {(value >= 0 ? "+" : "")}{value}{(def != null && def.isPercent ? "%" : "")}";
        }
    }
}
