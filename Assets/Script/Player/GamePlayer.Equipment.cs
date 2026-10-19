using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// GamePlayer partial — 장비/소모품 (아이템 개편 Phase 3, ToDo_RPG_ITEM 3-5~3-10 / 소모품 사용 경로는 Phase 6-3).
// 장비는 EquipInstance(등급 + 추가 옵션)로 보유·착용하고(SyncList<EquipInstance>), 기본 옵션은 EquipDB가 가진다. 누구나 어떤 장비든 착용할 수 있다 (Q22).
// 합산 스탯은 Total* 프로퍼티로 조회한다 — 힘·지능 스탯과 무기 공격력은 다른 개념(Q25): TotalStrength는 스탯만, 무기 공격력은 GetAttackPower가 속성에 맞춰 더한다 (Q46).
// 최대 HP/자원은 고유치(MaxHP/maxResource)만 저장하고 TotalMaxHP/TotalMaxResource를 매번 계산한다 (Q43 — 구 델타 방식 폐기). 착탈은 전투 중 불가.
// UI는 정식 인벤토리 팝업(Phase 8) 전까지 OnGUI 임시 창 (스킬트리 아래 '장비' 버튼).
public partial class GamePlayer
{
    public readonly SyncList<EquipInstance> equippedItems = new SyncList<EquipInstance>();   // 장착 중 (슬롯은 EquipDB에서 해석)
    public readonly SyncList<EquipInstance> inventoryEquips = new SyncList<EquipInstance>(); // 미장착 보유 장비
    public readonly SyncList<string> inventoryConsumables = new SyncList<string>();          // 보유 소모품 (인스턴스당 1항목 = 중복이 곧 개수)

    const int AccessoryslotCount = 2;

    // ------------------------------------------------------------- 합산 스탯 (기본 + 장비) — 전투 코드가 사용 -------------------------------------------------------------//

    /// <summary>힘 스탯 = 기본 + STRENGTH 옵션 (무기 공격력 제외 — 힘 요구치 비교에도 이 값)</summary>
    public int TotalStrength => strength + EquipOptionSum(EquipOptionType.STRENGTH);
    public int TotalIntelligence => intelligence + EquipOptionSum(EquipOptionType.INTELLIGENCE);
    /// <summary>민첩 = (기본 + AGILITY 옵션) × 힘 요구치 미달이면 ARMOR_STR_PENALTY_AGI_PERCENT(50)% (Q16·Q42 — 부위 수와 무관하게 1회). TP 충전이 매 틱 읽으므로 버프로 요구치를 넘으면 그동안 풀린다</summary>
    public int TotalAgility
    {
        get
        {
            int value = agility + EquipOptionSum(EquipOptionType.AGILITY);
            return IsStrengthRequirementUnmet ? value * BalanceData.Get("ARMOR_STR_PENALTY_AGI_PERCENT", 50) / 100 : value;
        }
    }

    /// <summary>힘 요구치 비교값 (Q42) = TotalStrength(기본 + STRENGTH 옵션) + 전투 중 STAT_STRENGTH 버프. 무기 공격력·공격력 버프는 제외</summary>
    public int CurrentStrengthForRequirement()
    {
        int value = TotalStrength;
        TargetObject self = M_TurnManager.instance != null && M_TurnManager.instance.tpBattleActive ? M_TurnManager.instance.GetCurrentPlayerTargetObject(this) : null;
        if (self != null) value += self.GetBuffValue(BuffType.STAT_STRENGTH);
        return value;
    }

    /// <summary>착용 방어구 중 RequireStr > 현재 힘 인 부위가 하나라도 있는지 (민첩 ½ 패널티 조건)</summary>
    public bool IsStrengthRequirementUnmet
    {
        get
        {
            int strengthNow = -1;
            foreach (EquipInstance item in equippedItems)
            {
                EquipData.Def def = EquipData.Get(item.equipNo);
                if (def == null || !def.IsArmorPiece || def.requireStr <= 0) continue;
                if (strengthNow < 0) strengthNow = CurrentStrengthForRequirement();
                if (def.requireStr > strengthNow) return true;
            }
            return false;
        }
    }

    /// <summary>현재 약점 집합 (Q16 — 기획 「방어구」 세트 규칙): 갑옷·투구·신발 중 미착용 부위 있음 → 전 속성 / 1종 → 그 종류 약점 / 2종 → 합집합 / 3종 → 전 속성.
    /// 그다음 약점 상쇄 옵션(WEAKNESS_NULLIFY)의 속성을 뺀다 — "전 속성" 상태에도 적용 (Q23·A6). 몬스터 약점 판정과 같은 코드·같은 BalanceDB 키 (Q18)</summary>
    public HashSet<AttackAttribute> GetWeaknesses()
    {
        var types = new HashSet<ArmorType>();
        int pieces = 0;
        foreach (EquipInstance item in equippedItems)
        {
            EquipData.Def def = EquipData.Get(item.equipNo);
            if (def == null || !def.IsArmorPiece) continue;
            pieces++;
            types.Add(def.armorType);
        }
        var result = new HashSet<AttackAttribute>();
        if (pieces < 3 || types.Count >= 3 || types.Contains(ArmorType.NONE))
            result.UnionWith(ArmorTypeData.AllAttributes);
        else
            foreach (ArmorType type in types) result.UnionWith(ArmorTypeData.GetWeaknesses(type));
        result.ExceptWith(EquipOptionAttributes(EquipOptionType.WEAKNESS_NULLIFY));
        return result;
    }

    /// <summary>약점 요약 텍스트 (임시 UI) — "타격·공명" / "전 속성(미착용)" / "없음"</summary>
    public string WeaknessSummary()
    {
        HashSet<AttackAttribute> weaknesses = GetWeaknesses();
        if (weaknesses.Count >= ArmorTypeData.AllAttributes.Length) return "전 속성";
        return BattleActions.AttributeListName(weaknesses);
    }
    public int TotalControl => control + EquipOptionSum(EquipOptionType.CONTROL);
    public int TotalDefense => defense + SumEquipBase(def => def.defense) + EquipOptionSum(EquipOptionType.DEFENSE);
    public int TotalMagicDefense => magicDefense + SumEquipBase(def => def.magicDefense) + EquipOptionSum(EquipOptionType.DEFENSE);

    /// <summary>최종 물리공격력 = 힘 + Σ 무기 물리공격력 (스탯창 표시용 — 실제 계산은 GetAttackPower)</summary>
    public int TotalPhysicalAttack => TotalStrength + WeaponPhysicalAttack;
    /// <summary>최종 마법공격력 = 지능 + Σ 무기 마법공격력 (스탯창 표시용)</summary>
    public int TotalMagicAttack => TotalIntelligence + WeaponMagicAttack;
    public int WeaponPhysicalAttack => SumEquipBase(def => def.physicalAttack);
    public int WeaponMagicAttack => SumEquipBase(def => def.magicAttack);

    /// <summary>공격력 (Q46) = 계수 스탯(scalesWithInt ? 지능 : 힘) + 속성에 맞는 쪽의 무기 공격력(마법 속성이면 마법공격력, 물리면 물리공격력, NONE은 계수 스탯 쪽).
    /// 평타는 무기의 ScaleStat·Attribute로, 스킬은 SkillDB의 ScaleStat·Attribute로 부른다 (J1 — 에리스 공명 스킬 = 힘 + 핵 마법공격력)</summary>
    public int GetAttackPower(bool scalesWithInt, AttackAttribute attribute)
    {
        int stat = scalesWithInt ? TotalIntelligence : TotalStrength;
        bool useMagicAttack = attribute == AttackAttribute.NONE ? scalesWithInt : BattleActions.IsMagic(attribute);
        return stat + (useMagicAttack ? WeaponMagicAttack : WeaponPhysicalAttack);
    }

    /// <summary>장착 무기 기본 데이터 (없으면 null)</summary>
    public EquipData.Def WeaponDef
    {
        get
        {
            foreach (EquipInstance item in equippedItems)
            {
                EquipData.Def def = EquipData.Get(item.equipNo);
                if (def != null && def.IsWeapon) return def;
            }
            return null;
        }
    }
    /// <summary>기본 공격 속성 = 장착 무기의 Attribute (무기 없음 = NONE — Q25, 캐릭터 고정 폐기)</summary>
    public AttackAttribute BasicAttackAttribute => WeaponDef != null ? WeaponDef.attribute : AttackAttribute.NONE;
    /// <summary>기본 공격 계수 스탯 = 장착 무기의 ScaleStat (무기 없음 = 힘)</summary>
    public bool BasicAttackScalesWithInt => WeaponDef != null && WeaponDef.scalesWithInt;

    /// <summary>최대 HP = 고유치(기본 + 성장 + HP 노드) + MAX_HP 옵션 합 (Q43 — 계산 프로퍼티, 세이브에는 MaxHP만)</summary>
    public int TotalMaxHP => MaxHP + EquipOptionSum(EquipOptionType.MAX_HP);
    /// <summary>최대 자원 = 고유치 + 자원 종류가 맞는 옵션 합 (MP 캐릭터는 MAX_MP, 분노 캐릭터는 MAX_RAGE — D25). HP 자원(에리스)은 0 유지</summary>
    public int TotalMaxResource
    {
        get
        {
            if (maxResource <= 0) return maxResource;
            CharacterStatData.Entry stat = CharacterStatData.Get(character);
            BattleResourceType resource = stat != null ? stat.resource : BattleResourceType.NONE;
            if (resource == BattleResourceType.MP) return maxResource + EquipOptionSum(EquipOptionType.MAX_MP);
            if (resource == BattleResourceType.RAGE) return maxResource + EquipOptionSum(EquipOptionType.MAX_RAGE);
            return maxResource;
        }
    }

    /// <summary>장착 장비의 기본 옵션(EquipDB) 합</summary>
    int SumEquipBase(System.Func<EquipData.Def, int> selector)
    {
        int sum = 0;
        foreach (EquipInstance item in equippedItems)
        {
            EquipData.Def def = EquipData.Get(item.equipNo);
            if (def != null) sum += selector(def);
        }
        return sum;
    }

    /// <summary>장착 장비의 인스턴스 옵션 수치 합 (수치형 — 여러 장비에 같은 옵션이 있으면 합산, 상한 없음 5-13)</summary>
    public int EquipOptionSum(EquipOptionType type)
    {
        int sum = 0;
        foreach (EquipInstance item in equippedItems) sum += item.OptionSum(type);
        return sum;
    }

    /// <summary>플래그 옵션 보유 여부 (FREE_MOVE / NO_ROW_PENALTY — OR)</summary>
    public bool HasEquipFlag(EquipOptionType type)
    {
        foreach (EquipInstance item in equippedItems) if (item.HasOption(type)) return true;
        return false;
    }

    /// <summary>속성형 옵션(약점 상쇄)의 속성 집합 — 같은 속성 중복 무효 (A6)</summary>
    public HashSet<AttackAttribute> EquipOptionAttributes(EquipOptionType type)
    {
        var result = new HashSet<AttackAttribute>();
        foreach (EquipInstance item in equippedItems)
            for (int i = 0; i < item.OptionCount; i++)
                if (item.optionTypes[i] == (int)type) result.Add((AttackAttribute)item.OptionValueAt(i));
        return result;
    }

    // ------------------------------------------------------------- 조회 -------------------------------------------------------------//

    public bool TryFindEquipped(string instanceId, out EquipInstance found)
    {
        foreach (EquipInstance item in equippedItems) if (item.instanceId == instanceId) { found = item; return true; }
        found = default; return false;
    }

    public bool TryFindInventory(string instanceId, out EquipInstance found)
    {
        foreach (EquipInstance item in inventoryEquips) if (item.instanceId == instanceId) { found = item; return true; }
        found = default; return false;
    }

    static int IndexOfInstance(SyncList<EquipInstance> list, string instanceId)
    {
        for (int i = 0; i < list.Count; i++) if (list[i].instanceId == instanceId) return i;
        return -1;
    }

    // ------------------------------------------------------------- 지급/획득 (서버) -------------------------------------------------------------//

    [Server]
    public void ServerAddEquip(EquipInstance item)
    {
        if (!item.IsValid || EquipData.Get(item.equipNo) == null) return;
        inventoryEquips.Add(item);
    }

    /// <summary>장비 생성 + 인벤토리 지급 (드랍·상점·치트 공용)</summary>
    [Server]
    public EquipInstance ServerAddEquip(string equipNo, EquipGrade grade)
    {
        EquipInstance item = EquipGenerator.Create(equipNo, grade);
        if (item.IsValid) inventoryEquips.Add(item);
        return item;
    }

    [Server]
    public void ServerAddConsumable(string potionNo)
    {
        if (ConsumableData.Get(potionNo) != null) inventoryConsumables.Add(potionNo);
    }

    /// <summary>인벤토리(미장착) 장비 1개 제거 — 상점 판매 (Phase 7-8). 없으면 false</summary>
    [Server]
    public bool ServerRemoveInventoryEquip(string instanceId)
    {
        int index = IndexOfInstance(inventoryEquips, instanceId);
        if (index < 0) return false;
        inventoryEquips.RemoveAt(index);
        return true;
    }

    /// <summary>전투 보상 드랍 (임시 — Phase 7 드랍 테이블 전까지): DropWeight 가중 랜덤 장비 1개, 등급은 위험도 기준 EquipGradeDB 굴림. 레벨 필터 없음 (F3)</summary>
    [Server]
    public void ServerAddRandomEquip()
    {
        EquipData.Def picked = EquipData.PickWeighted();
        if (picked == null) return;
        int hazard = M_TurnManager.instance != null ? M_TurnManager.instance.currentBattleHazard : 0;
        EquipInstance item = ServerAddEquip(picked.equipNo, EquipGradeData.RollGrade(MonsterGrade.NORMAL, hazard));
        Debug.Log($"[Equipment] {character} 장비 획득: [{EquipGradeData.DisplayName(item.grade)}] {picked.equipName} (옵션 {item.OptionCount}개)");
    }

    [Server]
    public void ServerAddRandomConsumable()
    {
        ConsumableData.Def picked = ConsumableData.PickWeighted(); // DropWeight 가중 (임시 — Phase 7 드랍 테이블 전까지)
        if (picked != null) inventoryConsumables.Add(picked.potionNo);
    }

    /// <summary>캐릭터별 초기 세트 — 무기 + 같은 종류 방어구 3부위 (A5·Q36: 게오르크 대검+중갑 / 에리스 핵+경갑 / 홍단향 지팡이+로브, 요구치 ≤ 원래 기본 힘)</summary>
    public static string[] InitialGearFor(Character character)
    {
        switch (character)
        {
            case Character.GEORK: return new[] { "GW1", "AR2", "HE2", "BO3" };
            case Character.HONGDANHYANG: return new[] { "HW1", "AR4", "HE4", "BO4" };
            default: return new[] { "EW1", "AR1", "HE1", "BO1" };
        }
    }

    /// <summary>초기 장비 지급 — 초기 세트를 NORMAL 인스턴스로 생성·장착 + 물약 (PlayerInterface.GenerateGamePlayer에서 호출, 스폰 전이라 SyncList 초기 상태로 전달됨)</summary>
    [Server]
    public void ServerGrantInitialGear()
    {
        foreach (string equipNo in InitialGearFor(character))
        {
            EquipInstance item = EquipGenerator.Create(equipNo, EquipGrade.NORMAL);
            if (item.IsValid) equippedItems.Add(item);
        }
        ServerAddConsumable("PO1");
        ServerAddConsumable("PO1");
        if (character == Character.GEORK) ServerAddConsumable("PO4"); // 분노의 물약 시험 지급 (6-6 — 테스트 후 결정)
    }

    /// <summary>방어구 빈 부위를 초기 세트로 보충 (구 세이브 변환 — A5). 보충한 수를 반환</summary>
    [Server]
    public int ServerFillMissingArmor()
    {
        int filled = 0;
        foreach (string equipNo in InitialGearFor(character))
        {
            EquipData.Def def = EquipData.Get(equipNo);
            if (def == null || !def.IsArmorPiece) continue;
            bool occupied = false;
            foreach (EquipInstance worn in equippedItems)
            {
                EquipData.Def wornDef = EquipData.Get(worn.equipNo);
                if (wornDef != null && wornDef.slot == def.slot) { occupied = true; break; }
            }
            if (occupied) continue;
            EquipInstance item = EquipGenerator.Create(equipNo, EquipGrade.NORMAL);
            if (item.IsValid) { equippedItems.Add(item); filled++; }
        }
        return filled;
    }

    // ------------------------------------------------------------- 착용/해제 (서버 검증) -------------------------------------------------------------//

    [Command]
    public void CmdEquip(string instanceId)
    {
        if (IsBattleActive()) return; // 전투 중 착탈 불가
        if (!TryFindInventory(instanceId, out EquipInstance item)) return;
        EquipData.Def def = EquipData.Get(item.equipNo);
        if (def == null) return;
        if (def.requireLevel > level) return; // 착용 제한은 요구 레벨뿐 (캐릭터 제한 없음 — Q22)

        // 슬롯 자리 확보 — 꽉 찼으면 같은 슬롯의 기존 장비를 해제 (악세사리는 2개까지)
        int slotLimit = def.slot == EquipSlot.ACCESSORY ? AccessoryslotCount : 1;
        var occupants = new List<string>();
        foreach (EquipInstance worn in equippedItems)
        {
            EquipData.Def wornDef = EquipData.Get(worn.equipNo);
            if (wornDef != null && wornDef.slot == def.slot) occupants.Add(worn.instanceId);
        }
        if (occupants.Count >= slotLimit)
            UnequipInternal(occupants[0]);

        inventoryEquips.RemoveAt(IndexOfInstance(inventoryEquips, instanceId));
        equippedItems.Add(item);
        ClampToTotals();
    }

    [Command]
    public void CmdUnequip(string instanceId)
    {
        if (IsBattleActive()) return;
        if (!TryFindEquipped(instanceId, out _)) return;
        UnequipInternal(instanceId);
        ClampToTotals();
    }

    [Server]
    void UnequipInternal(string instanceId)
    {
        int index = IndexOfInstance(equippedItems, instanceId);
        if (index < 0) return;
        EquipInstance item = equippedItems[index];
        equippedItems.RemoveAt(index);
        inventoryEquips.Add(item);
    }

    /// <summary>착탈 후 현재치 클램프 — 최대치는 계산 프로퍼티라 저장값은 바뀌지 않는다 (Q43)</summary>
    [Server]
    public void ClampToTotals()
    {
        HP = Mathf.Clamp(HP, HP > 0 ? 1 : 0, TotalMaxHP);
        currentResource = Mathf.Clamp(currentResource, 0, Mathf.Max(0, TotalMaxResource));
    }

    // ------------------------------------------------------------- 소모품 사용 -------------------------------------------------------------//

    /// <summary>소모품 1개 소비 (전투 아이템 액션/맵 사용 공용). 보유하지 않았으면 false</summary>
    [Server]
    public bool ServerConsumePotion(string potionNo)
    {
        return inventoryConsumables.Remove(potionNo);
    }

    /// <summary>
    /// 소모품 사용의 단일 경로 (Phase 6-3, Q38) — 전투 ITEM 액션(TpBattle이 yield)과 비전투(CmdUsePotionOnMap이 코루틴 시작)가 모두 이것을 부른다.
    /// 검증: 보유 · BattleOnly · UsableResource · ValidTarget(1차 NONE = 자신만). 전투 중이면 대상의 TargetObject를 구해 효과에 넘긴다 (비전투 null)
    /// </summary>
    [Server]
    public IEnumerator ServerUseConsumable(string potionNo, GamePlayer target)
    {
        ConsumableData.Def potion = ConsumableData.Get(potionNo);
        if (potion == null) yield break;
        if (target == null || potion.validTarget == ValidTarget.NONE) target = this; // NONE = 자신 (R10)
        bool inBattle = IsBattleActive();
        if (!ConsumableData.CanUse(potion, target, inBattle, out string reasonKey))
        {
            if (connectionToClient != null) TargetPotionRefused(connectionToClient, reasonKey, potion.potionName);
            yield break;
        }
        if (!ServerConsumePotion(potionNo)) yield break; // 미보유
        TargetObject targetUnit = inBattle && M_TurnManager.instance != null ? M_TurnManager.instance.GetCurrentPlayerTargetObject(target) : null;
        Debug.Log($"[Consumable] {character} → {target.character} {potion.potionName} ({potion.effectName}, {(inBattle ? "전투" : "비전투")})");
        yield return potion.execute(potion, this, target, targetUnit);
    }

    /// <summary>맵(비전투)에서 물약 사용 — 버프 물약 등 전투 전용은 거부(토스트). 전투 중에는 TpAction.ITEM 경로</summary>
    [Command]
    public void CmdUsePotionOnMap(string potionNo)
    {
        if (IsBattleActive()) return;
        StartCoroutine(ServerUseConsumable(potionNo, this));
    }

    // 소모품 사용 거부 안내 — 요청한 플레이어에게만 토스트 (사유: 전투 전용 / 자원 불일치)
    [TargetRpc]
    void TargetPotionRefused(NetworkConnectionToClient conn, string reasonKey, string potionName)
    {
        string fallback = reasonKey == "ui.msg.potion_battle_only" ? "{0} — 전투 중에만 사용할 수 있습니다"
            : reasonKey == "ui.msg.potion_resource_mismatch" ? "{0} — 이 캐릭터는 사용할 수 없습니다 (자원 종류가 다름)" : "{0} — 사용할 수 없습니다";
        string text = M_LanguageManager.Get(reasonKey, fallback).Replace("{0}", potionName);
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Top)
            .FadeInTime(1f)
            .FadeOutTime(1f)
            .MessageBoxColor(ColorUtils.HexToColor("#B22222"))
            .TextColor(Color.white)
            .Text(text)
            .Show();
    }

    bool IsBattleActive()
    {
        return M_TurnManager.instance != null && M_TurnManager.instance.tpBattleActive;
    }

    // ------------------------------------------------------------- 디버그 치트 (서버) -------------------------------------------------------------//

    /// <summary>치트: 등급 지정 랜덤 장비 생성 (슬롯 지정 가능) — 임시 OnGUI 창 버튼</summary>
    [Command]
    public void CmdDebugCreateEquip(int grade, int slot)
    {
        EquipData.Def picked = EquipData.PickWeighted(def => slot < 0 || def.slot == (EquipSlot)slot);
        if (picked == null) return;
        EquipInstance item = ServerAddEquip(picked.equipNo, (EquipGrade)grade);
        Debug.Log($"[Equipment] 치트 생성: [{EquipGradeData.DisplayName(item.grade)}] {picked.equipName} 옵션 {item.OptionCount}개");
    }

    /// <summary>치트 (5-14): 특정 옵션 하나만 붙은 전설 악세사리 생성 — 옵션 효과 검증용. value &lt; 0이면 그 옵션의 LEGEND 최대치(속성형은 랜덤 속성)</summary>
    [Command]
    public void CmdDebugCreateOptionEquip(int optionType, int value)
    {
        EquipOptionType type = (EquipOptionType)optionType;
        EquipOptionData.Def option = EquipOptionData.Get(type, EquipGrade.LEGEND);
        if (option == null) return;
        EquipInstance item = EquipGenerator.Create("AC1", EquipGrade.LEGEND);
        if (!item.IsValid) return;
        item.optionTypes = new[] { optionType };
        item.optionValues = new[] { value >= 0 ? value : (option.valueKind == EquipValueKind.NUMBER ? option.maxValue : option.RollValue()) };
        inventoryEquips.Add(item);
        Debug.Log($"[Equipment] 치트 생성(옵션 단일): {DescribeEquip(item)}");
    }

    // ------------------------------------------------------------- 임시 UI (OnGUI) -------------------------------------------------------------//

    bool guiEquipOpen;
    Vector2 guiEquipScroll;

    /// <summary>장비 1줄 표기 — [등급] 이름 (종류·속성·요구치) 기본 옵션 / 추가 옵션</summary>
    public static string DescribeEquip(EquipInstance item)
    {
        EquipData.Def def = EquipData.Get(item.equipNo);
        if (def == null) return item.equipNo;
        var parts = new List<string>();
        string kind = EquipData.KindName(def);
        if (def.IsWeapon)
        {
            parts.Add($"{kind}·{BattleActions.AttributeName(def.attribute)}·{(def.scalesWithInt ? "지능" : "힘")}");
            if (def.physicalAttack != 0) parts.Add($"물공 +{def.physicalAttack}");
            if (def.magicAttack != 0) parts.Add($"마공 +{def.magicAttack}");
        }
        else if (def.IsArmorPiece)
        {
            parts.Add(kind + (def.requireStr > 0 ? $" 힘 {def.requireStr}" : ""));
            if (def.defense != 0) parts.Add($"방어 +{def.defense}");
            if (def.magicDefense != 0) parts.Add($"마방 +{def.magicDefense}");
        }
        for (int i = 0; i < item.OptionCount; i++)
            parts.Add(EquipOptionData.Format(item.OptionTypeAt(i), item.OptionValueAt(i), item.grade));
        string gradeTag = item.grade == EquipGrade.NORMAL ? "" : $"[{EquipGradeData.DisplayName(item.grade)}] ";
        return $"{gradeTag}{def.equipName} ({string.Join(", ", parts)})";
    }

    // 스킬트리 OnGUI(GamePlayer.SkillTree.cs)와 같은 컴포넌트라 별도 메서드로 분리해 거기서 호출한다
    void DrawEquipmentGUI()
    {
        Rect toggleRect = new Rect(Screen.width - 150f, 45f, 140f, 30f);
        if (GUI.Button(toggleRect, guiEquipOpen ? "장비 닫기" : $"장비 ({inventoryEquips.Count + inventoryConsumables.Count})"))
        {
            guiEquipOpen = !guiEquipOpen;
            if (guiEquipOpen) { SkillTreeUIManager.instance?.Show(false); guiStatsOpen = false; } // 스킬트리 팝업/스탯 창과 상호 배타 (같은 자리 사용)
        }
        if (!guiEquipOpen) return;

        float windowWidth = 640f;
        float windowHeight = 480f;
        Rect windowRect = new Rect(Screen.width - windowWidth - 10f, 85f, windowWidth, windowHeight);
        GUI.Box(windowRect, $"{character} 장비  |  힘{TotalStrength} 민첩{TotalAgility}{(IsStrengthRequirementUnmet ? "(힘 부족 ½)" : "")} 지능{TotalIntelligence} 방어{TotalDefense} 마방{TotalMagicDefense}  물공{TotalPhysicalAttack} 마공{TotalMagicAttack}  HP {HP}/{TotalMaxHP}  약점 {WeaknessSummary()}");

        guiEquipScroll = GUI.BeginScrollView(
            new Rect(windowRect.x + 10f, windowRect.y + 30f, windowWidth - 20f, windowHeight - 40f),
            guiEquipScroll, new Rect(0, 0, windowWidth - 40f, 1500f));

        float y = 0f;
        bool inBattle = IsBattleActive();
        GUIStyle gradeStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 11 };

        GUI.Label(new Rect(0, y, 300f, 24f), "◆ 장착 중 (클릭 = 해제)"); y += 26f;
        foreach (EquipInstance item in new List<EquipInstance>(equippedItems))
        {
            EquipData.Def def = EquipData.Get(item.equipNo);
            if (def == null) continue;
            GUI.enabled = !inBattle;
            gradeStyle.normal.textColor = EquipGradeData.Get(item.grade)?.Color ?? Color.white;
            if (GUI.Button(new Rect(20f, y, 580f, 24f), $"[{EquipData.SlotName(def.slot)}] {DescribeEquip(item)}", gradeStyle))
                CmdUnequip(item.instanceId);
            GUI.enabled = true;
            y += 28f;
        }

        y += 8f;
        GUI.Label(new Rect(0, y, 300f, 24f), "◆ 인벤토리 (클릭 = 장착)"); y += 26f;
        foreach (EquipInstance item in new List<EquipInstance>(inventoryEquips))
        {
            EquipData.Def def = EquipData.Get(item.equipNo);
            if (def == null) continue;
            bool usable = def.requireLevel <= level;
            GUI.enabled = usable && !inBattle;
            gradeStyle.normal.textColor = EquipGradeData.Get(item.grade)?.Color ?? Color.white;
            string lockNote = usable ? "" : $" — Lv.{def.requireLevel} 필요";
            if (GUI.Button(new Rect(20f, y, 580f, 24f), $"[{EquipData.SlotName(def.slot)}] {DescribeEquip(item)}{lockNote}", gradeStyle))
                CmdEquip(item.instanceId);
            GUI.enabled = true;
            y += 28f;
        }

        y += 8f;
        GUI.Label(new Rect(0, y, 400f, 24f), "◆ 소모품 (클릭 = 맵에서 사용, 전투는 아이템 액션)"); y += 26f;
        var counts = new Dictionary<string, int>();
        foreach (string potionNo in inventoryConsumables)
        {
            counts.TryGetValue(potionNo, out int count);
            counts[potionNo] = count + 1;
        }
        foreach (KeyValuePair<string, int> entry in counts)
        {
            ConsumableData.Def potion = ConsumableData.Get(entry.Key);
            if (potion == null) continue;
            bool usable = ConsumableData.CanUse(potion, this, false, out string reasonKey); // 비전투 규칙 (전투 전용·자원 불일치는 비활성 + 사유)
            GUI.enabled = usable && !inBattle;
            string reason = usable ? "" : $" [{ConsumableData.ReasonText(reasonKey)}]";
            if (GUI.Button(new Rect(20f, y, 580f, 24f), $"{potion.potionName} x{entry.Value} — {potion.description}{reason}"))
                CmdUsePotionOnMap(entry.Key);
            GUI.enabled = true;
            y += 28f;
        }

        y += 8f;
        GUI.Label(new Rect(0, y, 400f, 24f), "◆ 치트: 장비 생성 (등급 / 슬롯 랜덤)"); y += 26f;
        float bx = 20f;
        foreach (EquipGrade grade in new[] { EquipGrade.NORMAL, EquipGrade.ADVANCED, EquipGrade.RARE, EquipGrade.LEGEND })
        {
            if (GUI.Button(new Rect(bx, y, 90f, 24f), EquipGradeData.DisplayName(grade))) CmdDebugCreateEquip((int)grade, -1);
            bx += 95f;
        }
        if (GUI.Button(new Rect(bx, y, 100f, 24f), "전설 무기")) CmdDebugCreateEquip((int)EquipGrade.LEGEND, (int)EquipSlot.WEAPON);
        bx += 105f;
        if (GUI.Button(new Rect(bx, y, 110f, 24f), "전설 악세사리")) CmdDebugCreateEquip((int)EquipGrade.LEGEND, (int)EquipSlot.ACCESSORY);
        y += 28f;
        GUI.Label(new Rect(0, y, 500f, 24f), "◆ 치트: 옵션 1개짜리 전설 악세사리 (최대치) — 옵션 효과 검증용"); y += 26f;
        bx = 20f;
        int column = 0;
        foreach (EquipOptionType type in System.Enum.GetValues(typeof(EquipOptionType)))
        {
            if (GUI.Button(new Rect(bx, y, 140f, 22f), EquipOptionData.DisplayName(type))) CmdDebugCreateOptionEquip((int)type, -1);
            bx += 145f;
            if (++column % 4 == 0) { bx = 20f; y += 24f; }
        }
        GUI.EndScrollView();
    }
}
