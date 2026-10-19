using System;
using ProjectD;

/// <summary>
/// 장비 인스턴스 (아이템 개편 Phase 3, ToDo_RPG_ITEM 0-8·3-7) — 드랍·상점·초기 지급 때 EquipGenerator가 만든다.
/// 기본 옵션은 EquipDB(equipNo)가, 등급과 추가/전설 옵션은 인스턴스가 가진다. Mirror SyncList 동기화를 위해 기본형·배열 필드만 둔다 (List 금지 — Weaver 자동 직렬화).
/// optionValues: NUMBER = 수치, ATTRIBUTE = (int)AttackAttribute, FLAG = 1. 착탈·판매는 instanceId(서버 GUID 문자열) 기준.
/// </summary>
[Serializable]
public struct EquipInstance
{
    public string instanceId;
    public string equipNo;
    public EquipGrade grade;
    public int[] optionTypes;   // (int)EquipOptionType
    public int[] optionValues;

    public bool IsValid => !string.IsNullOrEmpty(instanceId) && !string.IsNullOrEmpty(equipNo);
    public int OptionCount => optionTypes == null ? 0 : optionTypes.Length;
    public EquipOptionType OptionTypeAt(int index) => (EquipOptionType)optionTypes[index];
    public int OptionValueAt(int index) => optionValues == null || index >= optionValues.Length ? 0 : optionValues[index];

    /// <summary>이 인스턴스의 해당 옵션 수치 합 (같은 옵션이 여러 개면 합산)</summary>
    public int OptionSum(EquipOptionType type)
    {
        int sum = 0;
        for (int i = 0; i < OptionCount; i++)
            if (optionTypes[i] == (int)type) sum += OptionValueAt(i);
        return sum;
    }

    public bool HasOption(EquipOptionType type)
    {
        for (int i = 0; i < OptionCount; i++)
            if (optionTypes[i] == (int)type) return true;
        return false;
    }

    public EquipInstanceSave ToSave()
    {
        return new EquipInstanceSave
        {
            instanceId = instanceId, equipNo = equipNo, grade = (int)grade,
            optionTypes = optionTypes != null ? (int[])optionTypes.Clone() : new int[0],
            optionValues = optionValues != null ? (int[])optionValues.Clone() : new int[0],
        };
    }

    public static EquipInstance FromSave(EquipInstanceSave save)
    {
        return new EquipInstance
        {
            instanceId = string.IsNullOrEmpty(save.instanceId) ? Guid.NewGuid().ToString("N") : save.instanceId,
            equipNo = save.equipNo, grade = (EquipGrade)save.grade,
            optionTypes = save.optionTypes ?? new int[0], optionValues = save.optionValues ?? new int[0],
        };
    }
}

/// <summary>세이브(JSON) 형식 — GameSaveService.ProfileData.equippedInstances / inventoryInstances</summary>
[Serializable]
public class EquipInstanceSave
{
    public string instanceId;
    public string equipNo;
    public int grade;
    public int[] optionTypes;
    public int[] optionValues;
}
