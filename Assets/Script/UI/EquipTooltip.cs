using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ProjectD;

/// <summary>
/// 장비 툴팁 문자열 빌더 (아이템 개편 Phase 8-2) — 인벤토리 팝업·상점이 공용. TMP 리치 텍스트.
/// 표시: 이름(등급 색)·등급·슬롯·종류·요구 레벨 / 무기: 속성·계수 스탯·물리/마법 공격력 / 방어구: 힘 요구치(미달 경고)·방어·마방·종류 약점 /
/// 추가 옵션(전설 옵션 별색, 자원 불일치 무효 표시) / 보는 캐릭터의 세트 상태. 무기 공격 계수 불일치 경고는 띄우지 않는다 (Q22).
/// </summary>
public static class EquipTooltip
{
    public const string LegendColor = "#FF9800";
    public const string WarnColor = "#FF6060";
    public const string DimColor = "#9A9A9A";

    public static string Build(EquipInstance item, GamePlayer viewer, bool includeSetStatus = true)
    {
        EquipData.Def def = EquipData.Get(item.equipNo);
        if (def == null) return item.equipNo;
        var sb = new StringBuilder();
        EquipGradeData.Def grade = EquipGradeData.Get(item.grade);
        string gradeColor = grade != null ? grade.colorHex : "#FFFFFF";
        sb.Append($"<size=115%><color={gradeColor}>{EquipData.DisplayName(def)}</color></size>\n");

        // 등급 · 슬롯 · 종류 · 요구 레벨
        var header = new List<string> { EquipGradeData.DisplayName(item.grade), EquipData.SlotName(def.slot) };
        string kind = EquipData.KindName(def);
        if (kind.Length > 0) header.Add(kind);
        string levelText = M_LanguageManager.Get("ui.item.level_req", "Lv.{0} 필요").Replace("{0}", def.requireLevel.ToString());
        if (viewer != null && def.requireLevel > viewer.level) levelText += $" <color={WarnColor}>{M_LanguageManager.Get("ui.item.level_short", "레벨 부족")}</color>";
        header.Add(levelText);
        sb.Append(string.Join(" · ", header)).Append('\n');

        if (def.IsWeapon)
        {
            sb.Append($"{BattleActions.AttributeName(def.attribute)} · {(def.scalesWithInt ? M_LanguageManager.Get("ui.item.scale_int", "지능 계수") : M_LanguageManager.Get("ui.item.scale_str", "힘 계수"))}");
            sb.Append($" · {M_LanguageManager.Get("ui.item.physical_attack", "물리공격력")} +{def.physicalAttack} · {M_LanguageManager.Get("ui.item.magic_attack", "마법공격력")} +{def.magicAttack}\n");
        }
        if (def.IsArmorPiece)
        {
            string strText = M_LanguageManager.Get("ui.item.str_req", "힘 요구치 {0}").Replace("{0}", def.requireStr.ToString());
            if (viewer != null && def.requireStr > viewer.CurrentStrengthForRequirement()) strText += $" <color={WarnColor}>{M_LanguageManager.Get("ui.item.str_short", "힘 부족 — 민첩 ½")}</color>";
            sb.Append(strText).Append('\n');
            sb.Append($"{M_LanguageManager.Get("ui.item.defense", "방어력")} +{def.defense} · {M_LanguageManager.Get("ui.item.magic_defense", "마법방어")} +{def.magicDefense}\n");
            sb.Append($"<color={DimColor}>{kind} — {M_LanguageManager.Get("ui.item.weak_list", "{0} 약점").Replace("{0}", BattleActions.AttributeListName(ArmorTypeData.GetWeaknesses(def.armorType)))}</color>\n");
        }

        // 추가 옵션
        if (item.OptionCount > 0)
        {
            sb.Append('\n');
            CharacterStatData.Entry stat = viewer != null ? CharacterStatData.Get(viewer.character) : null;
            for (int i = 0; i < item.OptionCount; i++)
            {
                EquipOptionType type = item.OptionTypeAt(i);
                bool legend = EquipOptionData.IsLegendOption(type);
                string line = EquipOptionData.Format(type, item.OptionValueAt(i), item.grade);
                bool invalid = stat != null && ((type == EquipOptionType.MAX_MP && stat.resource != BattleResourceType.MP) || (type == EquipOptionType.MAX_RAGE && stat.resource != BattleResourceType.RAGE));
                if (invalid) line = $"<color={DimColor}>{line} {M_LanguageManager.Get("ui.item.option_invalid", "(무효 — 자원 불일치)")}</color>";
                else if (legend) line = $"<color={LegendColor}>★ {line}</color>";
                sb.Append(line).Append('\n');
            }
        }

        // 세트 상태 (방어구만, 보는 캐릭터 기준)
        if (includeSetStatus && def.IsArmorPiece && viewer != null)
            sb.Append($"\n<color={DimColor}>{M_LanguageManager.Get("ui.item.set_status", "현재 세트: {0}").Replace("{0}", viewer.ArmorSetStatus())} · {M_LanguageManager.Get("ui.item.weak_now", "약점 {0}").Replace("{0}", viewer.WeaknessSummary())}</color>\n");

        return sb.ToString().TrimEnd();
    }

    /// <summary>장착 중인 같은 슬롯 장비와의 기본 옵션 차이 — "+2 방어 / −1 마방 / +3 물공" (무기·방어구). 악세사리는 옵션 비교 대신 빈 문자열</summary>
    public static string CompareBase(EquipInstance candidate, EquipInstance equipped)
    {
        EquipData.Def a = EquipData.Get(candidate.equipNo);
        EquipData.Def b = EquipData.Get(equipped.equipNo);
        if (a == null || b == null) return "";
        var parts = new List<string>();
        void Add(string label, int delta) { if (delta != 0) parts.Add($"<color={(delta > 0 ? "#7CFC00" : WarnColor)}>{(delta > 0 ? "+" : "")}{delta} {label}</color>"); }
        Add(M_LanguageManager.Get("ui.item.physical_attack", "물리공격력"), a.physicalAttack - b.physicalAttack);
        Add(M_LanguageManager.Get("ui.item.magic_attack", "마법공격력"), a.magicAttack - b.magicAttack);
        Add(M_LanguageManager.Get("ui.item.defense", "방어력"), a.defense - b.defense);
        Add(M_LanguageManager.Get("ui.item.magic_defense", "마법방어"), a.magicDefense - b.magicDefense);
        Add(M_LanguageManager.Get("ui.item.str_req_short", "힘 요구치"), a.requireStr - b.requireStr);
        return parts.Count == 0 ? M_LanguageManager.Get("ui.item.compare_same", "기본 옵션 동일") : string.Join(" / ", parts);
    }
}
