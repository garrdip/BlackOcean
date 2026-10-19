using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 장비 인스턴스 생성기 (서버 전용 — 아이템 개편 Phase 3-7). 드랍·상점·초기 지급·디버그 치트가 공용.
/// Create(equipNo, grade): 등급 행(EquipGradeDB)의 옵션 개수를 굴림 → 슬롯·등급이 맞는 일반 옵션 행에서 Weight 가중 **무중복** 추출 → 각 Min~Max 굴림
/// → LEGEND면 전설 옵션 풀에서 LegendOptionCount개 추가. 악세사리는 AccessoryOptionCount*를 쓴다 (노멀도 1개). ATTRIBUTE형(약점 상쇄)은 속성을 굴린다 (A6).
/// </summary>
public static class EquipGenerator
{
    public static EquipInstance Create(string equipNo, EquipGrade grade)
    {
        EquipData.Def def = EquipData.Get(equipNo);
        if (def == null)
        {
            Debug.LogError($"[EquipGenerator] EquipDB에 없는 장비: {equipNo}");
            return default;
        }
        EquipGradeData.Def gradeDef = EquipGradeData.Get(grade);
        var types = new List<int>();
        var values = new List<int>();
        if (gradeDef != null)
        {
            bool accessory = def.slot == EquipSlot.ACCESSORY;
            int min = accessory ? gradeDef.accessoryOptionCountMin : gradeDef.optionCountMin;
            int max = accessory ? gradeDef.accessoryOptionCountMax : gradeDef.optionCountMax;
            int normalCount = UnityEngine.Random.Range(min, max + 1);
            RollOptions(def.slot, grade, false, normalCount, types, values);
            if (gradeDef.legendOptionCount > 0)
                RollOptions(def.slot, grade, true, gradeDef.legendOptionCount, types, values);
        }
        return new EquipInstance
        {
            instanceId = Guid.NewGuid().ToString("N"),
            equipNo = equipNo,
            grade = grade,
            optionTypes = types.ToArray(),
            optionValues = values.ToArray(),
        };
    }

    // 가중 무중복 추출 — 같은 옵션 종류는 한 인스턴스에 한 번만 (약점 상쇄도 1개)
    static void RollOptions(EquipSlot slot, EquipGrade grade, bool legend, int count, List<int> types, List<int> values)
    {
        List<EquipOptionData.Def> pool = EquipOptionData.GetRollable(slot, grade, legend);
        pool.RemoveAll(option => types.Contains((int)option.type));
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int total = 0;
            foreach (EquipOptionData.Def option in pool) total += option.weight;
            int roll = UnityEngine.Random.Range(0, total);
            EquipOptionData.Def picked = pool[pool.Count - 1];
            foreach (EquipOptionData.Def option in pool)
            {
                roll -= option.weight;
                if (roll < 0) { picked = option; break; }
            }
            types.Add((int)picked.type);
            values.Add(picked.RollValue());
            pool.Remove(picked);
        }
    }
}
