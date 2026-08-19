using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// GamePlayer partial — 장비/소모품 (RPG 전환 Phase 4).
// 장비는 스탯 가산형(EquipDB): 합산 스탯은 Total* 프로퍼티로 조회하고(전투 코드가 사용),
// 최대치(MaxHP/MaxResource)는 착탈 시점에 델타로 반영한다. 착탈은 전투 중 불가.
// UI는 정식 인벤토리 팝업 전까지 OnGUI 임시 창 (스킬트리 아래 '장비' 버튼).
public partial class GamePlayer
{
    public readonly SyncList<string> equippedItems = new SyncList<string>();        // 장착 중인 EquipNo (슬롯은 EquipDB에서 해석)
    public readonly SyncList<string> inventoryEquips = new SyncList<string>();      // 미장착 보유 장비
    public readonly SyncList<string> inventoryConsumables = new SyncList<string>(); // 보유 소모품 (인스턴스당 1항목 = 중복이 곧 개수)

    const int AccessoryslotCount = 2;

    // ------------------------------------------------------------- 합산 스탯 (기본 + 장비) — 전투 코드가 사용 -------------------------------------------------------------//

    public int TotalStrength => strength + EquipAttackBonusFor(false);
    public int TotalIntelligence => intelligence + EquipAttackBonusFor(true);
    public int TotalAgility => agility + SumEquip(def => def.agility);
    public int TotalDefense => defense + SumEquip(def => def.defense);
    public int TotalMagicDefense => magicDefense + SumEquip(def => def.magicDefense);
    /// <summary>장비의 '스킬레벨' 옵션 합 — GetSkillLevel(스킬트리)에 가산된다</summary>
    public int EquipSkillLevelBonus => SumEquip(def => def.skillLevel);

    // Attack 옵션은 캐릭터의 공격 스탯(힘 또는 지능) 쪽에만 붙는다
    int EquipAttackBonusFor(bool intelligenceSide)
    {
        CharacterStatData.Entry stat = CharacterStatData.Get(character);
        bool scalesWithInt = stat != null && stat.attackScalesWithInt;
        if (scalesWithInt != intelligenceSide) return 0;
        return SumEquip(def => def.attack);
    }

    int SumEquip(System.Func<EquipData.Def, int> selector)
    {
        int sum = 0;
        foreach (string equipNo in equippedItems)
        {
            EquipData.Def def = EquipData.Get(equipNo);
            if (def != null) sum += selector(def);
        }
        return sum;
    }

    // ------------------------------------------------------------- 지급/획득 (서버) -------------------------------------------------------------//

    [Server]
    public void ServerAddEquip(string equipNo)
    {
        if (EquipData.Get(equipNo) != null) inventoryEquips.Add(equipNo);
    }

    [Server]
    public void ServerAddConsumable(string potionNo)
    {
        if (ConsumableData.Get(potionNo) != null) inventoryConsumables.Add(potionNo);
    }

    /// <summary>전투 보상 드랍 — 이 캐릭터가 쓸 수 있는 장비 중 요구 레벨을 만족하는 것 하나를 랜덤 지급</summary>
    [Server]
    public void ServerAddRandomEquip()
    {
        List<EquipData.Def> pool = EquipData.GetUsableBy(character).FindAll(def => def.requireLevel <= level + 2); // 근접 레벨대만
        if (pool.Count == 0) return;
        EquipData.Def picked = pool[Random.Range(0, pool.Count)];
        inventoryEquips.Add(picked.equipNo);
        Debug.Log($"[Equipment] {character} 장비 획득: {picked.equipName}");
    }

    [Server]
    public void ServerAddRandomConsumable()
    {
        List<ConsumableData.Def> pool = ConsumableData.GetAll();
        if (pool.Count == 0) return;
        inventoryConsumables.Add(pool[Random.Range(0, pool.Count)].potionNo);
    }

    /// <summary>초기 장비 지급 — 기본 무기 장착 + 물약 (PlayerInterface.GenerateGamePlayer에서 호출, 스폰 전이라 SyncList 초기 상태로 전달됨)</summary>
    [Server]
    public void ServerGrantInitialGear()
    {
        string basicWeapon = character == Character.GEORK ? "GW1" : character == Character.HONGDANHYANG ? "HW1" : "EW1";
        if (EquipData.Get(basicWeapon) != null)
        {
            equippedItems.Add(basicWeapon);
            ApplyEquipMaxDeltas(EquipData.Get(basicWeapon), +1);
        }
        ServerAddConsumable("PO1");
        ServerAddConsumable("PO1");
    }

    // ------------------------------------------------------------- 착용/해제 (서버 검증) -------------------------------------------------------------//

    [Command]
    public void CmdEquip(string equipNo)
    {
        if (IsBattleActive()) return; // 전투 중 착탈 불가
        EquipData.Def def = EquipData.Get(equipNo);
        if (def == null || !inventoryEquips.Contains(equipNo)) return;
        if (def.character != Character.NONE && def.character != character) return; // 캐릭터 전용(무기) 검증
        if (def.requireLevel > level) return;

        // 슬롯 자리 확보 — 꽉 찼으면 같은 슬롯의 기존 장비를 해제 (악세사리는 2개까지)
        int slotLimit = def.slot == EquipSlot.ACCESSORY ? AccessoryslotCount : 1;
        var occupants = new List<string>();
        foreach (string worn in equippedItems)
        {
            EquipData.Def wornDef = EquipData.Get(worn);
            if (wornDef != null && wornDef.slot == def.slot) occupants.Add(worn);
        }
        if (occupants.Count >= slotLimit)
            UnequipInternal(occupants[0]);

        inventoryEquips.Remove(equipNo);
        equippedItems.Add(equipNo);
        ApplyEquipMaxDeltas(def, +1);
    }

    [Command]
    public void CmdUnequip(string equipNo)
    {
        if (IsBattleActive()) return;
        if (!equippedItems.Contains(equipNo)) return;
        UnequipInternal(equipNo);
    }

    [Server]
    void UnequipInternal(string equipNo)
    {
        EquipData.Def def = EquipData.Get(equipNo);
        equippedItems.Remove(equipNo);
        inventoryEquips.Add(equipNo);
        if (def != null) ApplyEquipMaxDeltas(def, -1);
    }

    // MaxHP/MaxResource 옵션은 착탈 시점에 델타로 반영 (증가분은 현재치도 회복, 감소 시 현재치 클램프)
    [Server]
    void ApplyEquipMaxDeltas(EquipData.Def def, int sign)
    {
        if (def.maxHP != 0)
        {
            MaxHP += def.maxHP * sign;
            if (sign > 0) HP += def.maxHP;
            HP = Mathf.Clamp(HP, 1, MaxHP);
        }
        if (def.maxResource != 0)
        {
            maxResource = Mathf.Max(0, maxResource + def.maxResource * sign);
            currentResource = Mathf.Clamp(currentResource, 0, maxResource);
        }
    }

    // ------------------------------------------------------------- 소모품 사용 -------------------------------------------------------------//

    /// <summary>소모품 1개 소비 (전투 아이템 액션/맵 사용 공용). 보유하지 않았으면 false</summary>
    [Server]
    public bool ServerConsumePotion(string potionNo)
    {
        return inventoryConsumables.Remove(potionNo);
    }

    /// <summary>맵(비전투)에서 물약 사용 — HP 물약만 의미 있음. 전투 중에는 TpAction.ITEM 경로 사용</summary>
    [Command]
    public void CmdUsePotionOnMap(string potionNo)
    {
        if (IsBattleActive()) return;
        ConsumableData.Def potion = ConsumableData.Get(potionNo);
        if (potion == null || !ServerConsumePotion(potionNo)) return;
        switch (potion.type)
        {
            case ConsumableType.HEAL_HP:
                HP = Mathf.Min(MaxHP, HP + potion.value);
                break;
            case ConsumableType.RESTORE_RESOURCE:
                currentResource = Mathf.Min(maxResource, currentResource + potion.value);
                break;
        }
    }

    bool IsBattleActive()
    {
        return M_TurnManager.instance != null && M_TurnManager.instance.tpBattleActive;
    }

    // ------------------------------------------------------------- 임시 UI (OnGUI) -------------------------------------------------------------//

    bool guiEquipOpen;
    Vector2 guiEquipScroll;

    // 스킬트리 OnGUI(GamePlayer.SkillTree.cs)와 같은 컴포넌트라 별도 메서드로 분리해 거기서 호출한다
    void DrawEquipmentGUI()
    {
        Rect toggleRect = new Rect(Screen.width - 150f, 45f, 140f, 30f);
        ProjectD.Map3DGuiArea.Register(toggleRect);
        if (GUI.Button(toggleRect, guiEquipOpen ? "장비 닫기" : $"장비 ({inventoryEquips.Count + inventoryConsumables.Count})"))
        {
            guiEquipOpen = !guiEquipOpen;
            if (guiEquipOpen) guiTreeOpen = false; // 스킬트리 창과 상호 배타 (같은 자리 사용)
        }
        if (!guiEquipOpen) return;

        float windowWidth = 560f;
        float windowHeight = 430f;
        Rect windowRect = new Rect(Screen.width - windowWidth - 10f, 85f, windowWidth, windowHeight);
        ProjectD.Map3DGuiArea.Register(windowRect);
        GUI.Box(windowRect, $"{character} 장비  |  힘{TotalStrength} 민첩{TotalAgility} 지능{TotalIntelligence} 방어{TotalDefense} 마방{TotalMagicDefense}");

        guiEquipScroll = GUI.BeginScrollView(
            new Rect(windowRect.x + 10f, windowRect.y + 30f, windowWidth - 20f, windowHeight - 40f),
            guiEquipScroll, new Rect(0, 0, windowWidth - 40f, 900f));

        float y = 0f;
        bool inBattle = IsBattleActive();

        GUI.Label(new Rect(0, y, 300f, 24f), "◆ 장착 중"); y += 26f;
        foreach (string equipNo in new List<string>(equippedItems))
        {
            EquipData.Def def = EquipData.Get(equipNo);
            if (def == null) continue;
            GUI.enabled = !inBattle;
            if (GUI.Button(new Rect(20f, y, 480f, 24f), $"[{def.slot}] {def.equipName} — {def.description} (해제)"))
                CmdUnequip(equipNo);
            GUI.enabled = true;
            y += 28f;
        }

        y += 8f;
        GUI.Label(new Rect(0, y, 300f, 24f), "◆ 인벤토리 (클릭 = 장착)"); y += 26f;
        foreach (string equipNo in new List<string>(inventoryEquips))
        {
            EquipData.Def def = EquipData.Get(equipNo);
            if (def == null) continue;
            bool usable = (def.character == Character.NONE || def.character == character) && def.requireLevel <= level;
            GUI.enabled = usable && !inBattle;
            string lockNote = usable ? "" : $" — Lv.{def.requireLevel} 필요";
            if (GUI.Button(new Rect(20f, y, 480f, 24f), $"[{def.slot}] {def.equipName} — {def.description}{lockNote}"))
                CmdEquip(equipNo);
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
            GUI.enabled = !inBattle;
            if (GUI.Button(new Rect(20f, y, 480f, 24f), $"{potion.potionName} x{entry.Value} — {potion.description}"))
                CmdUsePotionOnMap(entry.Key);
            GUI.enabled = true;
            y += 28f;
        }
        GUI.EndScrollView();
    }
}
