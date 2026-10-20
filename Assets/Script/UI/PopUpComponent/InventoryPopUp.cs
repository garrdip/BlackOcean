using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Mirror;
using ProjectD;

/// <summary>
/// 인벤토리·장비·스탯 팝업 (아이템 개편 Phase 8-3·8-6) — 씬/프리팹 참조 없이 런타임에 구성한다 (SaveSlotPanel·ShopPanel과 같은 방식).
/// 왼쪽: 현재 조작 캐릭터(currentGamePlayer)의 스탯(기본 + 장비 기여분 괄호)·보조 스탯·약점·힘 요구치·옵션 요약.
/// 가운데: 장착 슬롯 6칸(무기/갑옷/투구/신발/악세사리 2) — 클릭 = 선택 → 상세(툴팁)·해제. 오른쪽: 보유 장비 / 소모품 탭 — 클릭 = 선택 → 상세·비교·장착/사용.
/// 전투 중에는 착탈 버튼이 비활성(서버도 거부). 스킬트리 팝업과 상호 배타. Esc/바깥 클릭/닫기 = 닫기. 우상단 OnGUI '장비' 버튼(GamePlayer.SkillTree)이 토글한다.
/// </summary>
public class InventoryPopUp : MonoBehaviour
{
    const int SortingOrder = 390;
    const float FadeTime = 0.15f;
    static readonly Color GoldColor = ColorUtils.HexToColor("#DAA520");
    static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.14f, 0.97f);
    static readonly Color ColumnColor = new Color(0f, 0f, 0f, 0.25f);
    static readonly Color RowColor = new Color(0.16f, 0.16f, 0.22f, 1f);
    static readonly Color SelectedColor = new Color(0.30f, 0.26f, 0.16f, 1f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color TabOnColor = new Color(0.30f, 0.26f, 0.16f, 1f);
    static readonly Color TabOffColor = new Color(0.16f, 0.16f, 0.22f, 1f);
    static readonly EquipSlot[] SlotOrder = { EquipSlot.WEAPON, EquipSlot.ARMOR, EquipSlot.HELMET, EquipSlot.BOOTS, EquipSlot.ACCESSORY, EquipSlot.ACCESSORY };

    static InventoryPopUp instance;
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    GamePlayer player;
    bool consumableTab;
    string selectedInstanceId;   // 선택한 장비 (장착/인벤토리 공용 — instanceId)
    bool selectedIsEquipped;
    string selectedPotionNo;

    CanvasGroup canvasGroup;
    TextMeshProUGUI titleText, statsText, detailText, actionLabel, emptyText;
    RectTransform slotsRoot, listContent;
    Image equipTabImage, potionTabImage;
    Button actionButton;

    // ------------------------------------------------------------------ 진입점 ------------------------------------------------------------------ //

    public static void Open()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("InventoryPopUp");
            instance = go.AddComponent<InventoryPopUp>();
        }
        SkillTreeUIManager.instance?.Show(false); // 상호 배타
        instance.gameObject.SetActive(true);
        instance.Bind();
        instance.Refresh();
        instance.canvasGroup.alpha = 0f;
        instance.canvasGroup.interactable = true;
        instance.canvasGroup.blocksRaycasts = true;
        DOTween.Kill(instance.canvasGroup);
        instance.canvasGroup.DOFade(1f, FadeTime);
    }

    public static void Close()
    {
        if (instance != null && instance.gameObject.activeSelf) instance.CloseInternal();
    }

    public static void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    void Awake() { BuildUI(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseInternal();
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
        Unbind();
        if (instance == this) instance = null;
    }

    void CloseInternal()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        DOTween.Kill(canvasGroup);
        canvasGroup.DOFade(0f, FadeTime).OnComplete(() => { Unbind(); gameObject.SetActive(false); });
    }

    void Bind()
    {
        Unbind();
        player = PlayerRegistry.Local != null ? PlayerRegistry.Local.currentGamePlayer : null;
        if (player == null) return;
        player.equippedItems.Callback += OnEquipListChanged;
        player.inventoryEquips.Callback += OnEquipListChanged;
        player.inventoryConsumables.Callback += OnConsumablesChanged;
        player.onChangeGold += OnGoldChanged;
        M_LanguageManager.onLocaleChanged += OnLocaleChanged;
    }

    void Unbind()
    {
        M_LanguageManager.onLocaleChanged -= OnLocaleChanged;
        if (player == null) return;
        player.equippedItems.Callback -= OnEquipListChanged;
        player.inventoryEquips.Callback -= OnEquipListChanged;
        player.inventoryConsumables.Callback -= OnConsumablesChanged;
        player.onChangeGold -= OnGoldChanged;
        player = null;
    }

    void OnEquipListChanged(SyncList<EquipInstance>.Operation op, int index, EquipInstance oldItem, EquipInstance newItem) { if (gameObject.activeSelf) Refresh(); }
    void OnConsumablesChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem) { if (gameObject.activeSelf) Refresh(); }
    void OnGoldChanged(int gold) { if (gameObject.activeSelf) Refresh(); }
    void OnLocaleChanged() { if (gameObject.activeSelf) Refresh(); }

    static bool InBattle => M_TurnManager.instance != null && M_TurnManager.instance.tpBattleActive;

    // ------------------------------------------------------------------ 내용 ------------------------------------------------------------------ //

    void Refresh()
    {
        if (player == null || PlayerRegistry.Local == null || PlayerRegistry.Local.currentGamePlayer != player) Bind(); // 캐릭터 전환 추적
        titleText.text = M_LanguageManager.Get("ui.inventory.title", "장비 · 스탯") + (player != null ? $"  —  {M_LanguageManager.GetCharacterName(player.character)}  |  {M_LanguageManager.Get("ui.shop.gold", "보유 골드 {0}").Replace("{0}", player.gold.ToString())}" : "");
        statsText.text = player != null ? BuildStats(player) : "";
        equipTabImage.color = consumableTab ? TabOffColor : TabOnColor;
        potionTabImage.color = consumableTab ? TabOnColor : TabOffColor;
        RebuildSlots();
        RebuildList();
        RefreshDetail();
    }

    static string BuildStats(GamePlayer p)
    {
        string Row(string name, int baseValue, int total) { int d = total - baseValue; return $"{name} {total}" + (d != 0 ? $" <color=#9A9A9A>({baseValue}{(d > 0 ? "+" : "")}{d})</color>" : "") + "\n"; }
        CharacterStatData.Entry stat = CharacterStatData.Get(p.character);
        var sb = new System.Text.StringBuilder();
        sb.Append($"<color=#DAA520>Lv.{p.level}</color>  HP {p.HP}/{p.TotalMaxHP}" + (p.TotalMaxHP != p.MaxHP ? $" <color=#9A9A9A>({p.MaxHP}+{p.TotalMaxHP - p.MaxHP})</color>" : "") + "\n");
        if (p.maxResource > 0) sb.Append($"{(stat != null ? stat.resource.ToString() : "자원")} {p.currentResource}/{p.TotalMaxResource}\n");
        sb.Append('\n');
        sb.Append(Row(M_LanguageManager.Get("ui.stat.str", "힘"), p.strength, p.TotalStrength));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.agi", "민첩"), p.agility, p.TotalAgility));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.int", "지능"), p.intelligence, p.TotalIntelligence));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.def", "방어력"), p.defense, p.TotalDefense));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.mdef", "마법방어"), p.magicDefense, p.TotalMagicDefense));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.ctrl", "제어"), p.control, p.TotalControl));
        sb.Append($"{M_LanguageManager.Get("ui.item.physical_attack", "물리공격력")} {p.TotalPhysicalAttack} · {M_LanguageManager.Get("ui.item.magic_attack", "마법공격력")} {p.TotalMagicAttack}\n");
        sb.Append($"{M_LanguageManager.Get("ui.stat.basic_attack", "평타")} {BattleActions.AttributeName(p.BasicAttackAttribute)} / {(p.BasicAttackScalesWithInt ? M_LanguageManager.Get("ui.item.scale_int", "지능 계수") : M_LanguageManager.Get("ui.item.scale_str", "힘 계수"))}\n");
        sb.Append('\n');
        int baseEvade = stat != null ? stat.baseEvade : 5, baseAcc = stat != null ? stat.baseAccuracy : 95, baseCrit = stat != null ? stat.baseCrit : 5;
        sb.Append(Row(M_LanguageManager.Get("ui.stat.evade", "회피율 %"), baseEvade, p.TotalEvade));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.accuracy", "명중률 %"), baseAcc, p.TotalAccuracy));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.crit", "치명타율 %"), baseCrit, p.TotalCritRate));
        sb.Append(Row(M_LanguageManager.Get("ui.stat.crit_damage", "치명 배수 %"), BalanceData.Get("CRIT_DAMAGE_PERCENT", 150), p.TotalCritDamage));
        sb.Append('\n');
        sb.Append($"{M_LanguageManager.Get("ui.item.weak_now", "약점 {0}").Replace("{0}", p.WeaknessSummary())}\n");
        sb.Append($"<color=#9A9A9A>{M_LanguageManager.Get("ui.item.set_status", "현재 세트: {0}").Replace("{0}", p.ArmorSetStatus())}</color>\n");
        if (p.IsStrengthRequirementUnmet) sb.Append($"<color={EquipTooltip.WarnColor}>{M_LanguageManager.Get("ui.item.str_short", "힘 부족 — 민첩 ½")} ({M_LanguageManager.Get("ui.stat.str", "힘")} {p.CurrentStrengthForRequirement()})</color>\n");
        // 옵션 요약 (0이 아닌 것만)
        var options = new List<string>();
        void Opt(EquipOptionType type, string suffix = "") { int v = p.EquipOptionSum(type); if (v != 0) options.Add($"{EquipOptionData.DisplayName(type)} {v}{suffix}"); }
        Opt(EquipOptionType.LIFE_STEAL, "%"); Opt(EquipOptionType.SKILL_DAMAGE, "%"); Opt(EquipOptionType.COMBO_DAMAGE, "%"); Opt(EquipOptionType.REFLECT_FLAT); Opt(EquipOptionType.REFLECT_PERCENT, "%"); Opt(EquipOptionType.HEAL_RECEIVED, "%"); Opt(EquipOptionType.SHIELD);
        if (p.HasEquipFlag(EquipOptionType.FREE_MOVE)) options.Add(EquipOptionData.DisplayName(EquipOptionType.FREE_MOVE));
        if (p.HasEquipFlag(EquipOptionType.NO_ROW_PENALTY)) options.Add(EquipOptionData.DisplayName(EquipOptionType.NO_ROW_PENALTY));
        if (options.Count > 0) sb.Append($"\n<color=#9A9A9A>{string.Join(" · ", options)}</color>");
        return sb.ToString();
    }

    void RebuildSlots()
    {
        for (int i = slotsRoot.childCount - 1; i >= 0; i--) Destroy(slotsRoot.GetChild(i).gameObject);
        if (player == null) return;
        var accessories = new List<EquipInstance>();
        var bySlot = new Dictionary<EquipSlot, EquipInstance>();
        foreach (EquipInstance item in player.equippedItems)
        {
            EquipData.Def def = EquipData.Get(item.equipNo);
            if (def == null) continue;
            if (def.slot == EquipSlot.ACCESSORY) accessories.Add(item); else bySlot[def.slot] = item;
        }
        int accessoryIndex = 0;
        foreach (EquipSlot slot in SlotOrder)
        {
            EquipInstance item = default;
            bool has = slot == EquipSlot.ACCESSORY ? accessoryIndex < accessories.Count && (item = accessories[accessoryIndex++]).IsValid : bySlot.TryGetValue(slot, out item);
            string label = $"<color=#9A9A9A>[{EquipData.SlotName(slot)}]</color> " + (has ? ColoredName(item) : $"<color=#666666>{M_LanguageManager.Get("ui.inventory.empty", "비어 있음")}</color>");
            bool selected = has && selectedIsEquipped && selectedInstanceId == item.instanceId;
            string captured = has ? item.instanceId : null;
            CreateRow(slotsRoot, label, selected, 46, () => { if (captured == null) return; selectedIsEquipped = true; selectedInstanceId = captured; selectedPotionNo = null; Refresh(); });
        }
    }

    void RebuildList()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);
        int rows = 0;
        if (player != null && !consumableTab)
        {
            foreach (EquipInstance item in player.inventoryEquips)
            {
                EquipInstance captured = item;
                EquipData.Def def = EquipData.Get(item.equipNo);
                if (def == null) continue;
                bool selected = !selectedIsEquipped && selectedInstanceId == item.instanceId;
                string label = $"<color=#9A9A9A>[{EquipData.SlotName(def.slot)}]</color> {ColoredName(item)}" + (def.requireLevel > player.level ? $" <color={EquipTooltip.WarnColor}>Lv.{def.requireLevel}</color>" : "");
                CreateRow(listContent, label, selected, 40, () => { selectedIsEquipped = false; selectedInstanceId = captured.instanceId; selectedPotionNo = null; Refresh(); });
                rows++;
            }
        }
        else if (player != null)
        {
            var counts = new Dictionary<string, int>();
            foreach (string potionNo in player.inventoryConsumables) { counts.TryGetValue(potionNo, out int c); counts[potionNo] = c + 1; }
            foreach (KeyValuePair<string, int> entry in counts)
            {
                ConsumableData.Def potion = ConsumableData.Get(entry.Key);
                if (potion == null) continue;
                string captured = entry.Key;
                bool usable = ConsumableData.CanUse(potion, player, InBattle, out string reasonKey);
                string label = $"{ConsumableData.DisplayName(potion)} ×{entry.Value}" + (usable ? "" : $" <color=#9A9A9A>[{ConsumableData.ReasonText(reasonKey)}]</color>");
                CreateRow(listContent, label, selectedPotionNo == entry.Key, 40, () => { selectedPotionNo = captured; selectedInstanceId = null; Refresh(); });
                rows++;
            }
        }
        emptyText.gameObject.SetActive(rows == 0);
    }

    void RefreshDetail()
    {
        actionButton.gameObject.SetActive(false);
        if (player == null) { detailText.text = ""; return; }
        bool inBattle = InBattle;
        if (selectedPotionNo != null)
        {
            ConsumableData.Def potion = ConsumableData.Get(selectedPotionNo);
            if (potion == null || !player.inventoryConsumables.Contains(selectedPotionNo)) { selectedPotionNo = null; detailText.text = M_LanguageManager.Get("ui.inventory.select_hint", "항목을 선택하세요"); return; }
            bool usable = ConsumableData.CanUse(potion, player, false, out string reasonKey);
            detailText.text = $"<size=115%>{ConsumableData.DisplayName(potion)}</size>\n{ConsumableData.DisplayDescription(potion)}" + (usable ? "" : $"\n<color={EquipTooltip.WarnColor}>{ConsumableData.ReasonText(reasonKey)}</color>") + (inBattle ? $"\n<color=#9A9A9A>{M_LanguageManager.Get("ui.inventory.in_battle_item", "전투 중에는 아이템 액션으로 사용")}</color>" : "");
            SetAction(M_LanguageManager.Get("ui.inventory.use", "사용"), usable && !inBattle && player.isOwned, () => player.CmdUsePotionOnMap(selectedPotionNo));
            return;
        }
        if (selectedInstanceId != null)
        {
            bool found = selectedIsEquipped ? player.TryFindEquipped(selectedInstanceId, out EquipInstance item) : player.TryFindInventory(selectedInstanceId, out item);
            if (!found) { selectedInstanceId = null; detailText.text = M_LanguageManager.Get("ui.inventory.select_hint", "항목을 선택하세요"); return; }
            string text = EquipTooltip.Build(item, player);
            if (!selectedIsEquipped)
            {
                EquipData.Def def = EquipData.Get(item.equipNo);
                EquipInstance equipped = FindEquippedForSlot(def != null ? def.slot : EquipSlot.WEAPON);
                if (equipped.IsValid)
                    text += $"\n\n<color=#9A9A9A>{M_LanguageManager.Get("ui.item.equipped_now", "장착 중: {0}").Replace("{0}", EquipData.DisplayName(EquipData.Get(equipped.equipNo)))}</color>\n{EquipTooltip.CompareBase(item, equipped)}";
                bool canEquip = def != null && def.requireLevel <= player.level && !inBattle && player.isOwned;
                if (inBattle) text += $"\n<color=#9A9A9A>{M_LanguageManager.Get("ui.inventory.in_battle", "전투 중에는 착탈할 수 없습니다")}</color>";
                SetAction(M_LanguageManager.Get("ui.inventory.equip", "장착"), canEquip, () => player.CmdEquip(selectedInstanceId));
            }
            else
            {
                if (inBattle) text += $"\n<color=#9A9A9A>{M_LanguageManager.Get("ui.inventory.in_battle", "전투 중에는 착탈할 수 없습니다")}</color>";
                SetAction(M_LanguageManager.Get("ui.inventory.unequip", "해제"), !inBattle && player.isOwned, () => player.CmdUnequip(selectedInstanceId));
            }
            detailText.text = text;
            return;
        }
        detailText.text = M_LanguageManager.Get("ui.inventory.select_hint", "항목을 선택하세요");
    }

    EquipInstance FindEquippedForSlot(EquipSlot slot)
    {
        foreach (EquipInstance item in player.equippedItems)
        {
            EquipData.Def def = EquipData.Get(item.equipNo);
            if (def != null && def.slot == slot) return item; // 악세사리는 첫 칸과 비교
        }
        return default;
    }

    void SetAction(string label, bool interactable, System.Action onClick)
    {
        actionButton.gameObject.SetActive(true);
        actionLabel.text = label;
        actionButton.interactable = interactable;
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() => onClick?.Invoke());
    }

    static string ColoredName(EquipInstance item)
    {
        EquipData.Def def = EquipData.Get(item.equipNo);
        EquipGradeData.Def grade = EquipGradeData.Get(item.grade);
        string color = grade != null ? grade.colorHex : "#FFFFFF";
        string gradeTag = item.grade == EquipGrade.NORMAL ? "" : $"[{EquipGradeData.DisplayName(item.grade)}] ";
        return $"<color={color}>{gradeTag}{EquipData.DisplayName(def)}</color>";
    }

    void CreateRow(RectTransform parent, string label, bool selected, float height, System.Action onClick)
    {
        Image row = CreateImage("Row", parent, selected ? SelectedColor : RowColor);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row;
        button.onClick.AddListener(() => onClick?.Invoke());
        TextMeshProUGUI text = CreateText("Label", row.transform, 18, Color.white, TextAlignmentOptions.MidlineLeft);
        text.richText = true;
        text.margin = new Vector4(12, 2, 12, 2);
        Stretch(text.rectTransform);
        text.text = label;
    }

    // ------------------------------------------------------------------ UI 구성 ------------------------------------------------------------------ //

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image dim = CreateImage("Dim", transform, DimColor);
        Stretch(dim.rectTransform);
        Button dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(CloseInternal);

        Image panelImage = CreateImage("Panel", transform, PanelColor);
        RectTransform panel = panelImage.rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1440, 860);
        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(24, 24, 20, 20);
        panelLayout.spacing = 10;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(2, -2);

        titleText = CreateText("Title", panel, 26, GoldColor, TextAlignmentOptions.Center, FontStyles.Bold);
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

        // 3열
        GameObject columnsGo = new GameObject("Columns", typeof(RectTransform));
        columnsGo.transform.SetParent(panel, false);
        columnsGo.AddComponent<LayoutElement>().flexibleHeight = 1;
        HorizontalLayoutGroup columns = columnsGo.AddComponent<HorizontalLayoutGroup>();
        columns.spacing = 12;
        columns.childControlWidth = true;
        columns.childControlHeight = true;
        columns.childForceExpandWidth = false;
        columns.childForceExpandHeight = true;

        // 왼쪽: 스탯
        Image statsColumn = CreateImage("Stats", columnsGo.transform, ColumnColor);
        statsColumn.gameObject.AddComponent<LayoutElement>().preferredWidth = 400;
        statsText = CreateText("StatsText", statsColumn.transform, 18, Color.white, TextAlignmentOptions.TopLeft);
        statsText.richText = true;
        statsText.margin = new Vector4(14, 12, 14, 12);
        Stretch(statsText.rectTransform);

        // 가운데: 장착 슬롯
        Image slotsColumn = CreateImage("Slots", columnsGo.transform, ColumnColor);
        slotsColumn.gameObject.AddComponent<LayoutElement>().preferredWidth = 440;
        VerticalLayoutGroup slotsLayout = slotsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        slotsLayout.padding = new RectOffset(10, 10, 10, 10);
        slotsLayout.spacing = 6;
        slotsLayout.childControlWidth = true;
        slotsLayout.childControlHeight = true;
        slotsLayout.childForceExpandWidth = true;
        slotsLayout.childForceExpandHeight = false;
        TextMeshProUGUI slotsTitle = CreateText("SlotsTitle", slotsColumn.transform, 20, GoldColor, TextAlignmentOptions.Center, FontStyles.Bold);
        slotsTitle.text = M_LanguageManager.Get("ui.inventory.equipped", "장착 중");
        slotsTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        GameObject slotsRootGo = new GameObject("SlotRows", typeof(RectTransform));
        slotsRootGo.transform.SetParent(slotsColumn.transform, false);
        slotsRoot = slotsRootGo.GetComponent<RectTransform>();
        VerticalLayoutGroup slotRows = slotsRootGo.AddComponent<VerticalLayoutGroup>();
        slotRows.spacing = 6;
        slotRows.childControlWidth = true;
        slotRows.childControlHeight = true;
        slotRows.childForceExpandWidth = true;
        slotRows.childForceExpandHeight = false;
        slotsRootGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        // 상세(툴팁) — 슬롯 아래
        Image detailBox = CreateImage("Detail", slotsColumn.transform, new Color(0f, 0f, 0f, 0.35f));
        detailBox.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        VerticalLayoutGroup detailLayout = detailBox.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayout.padding = new RectOffset(12, 12, 10, 10);
        detailLayout.spacing = 8;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = true;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;
        detailText = CreateText("DetailText", detailBox.transform, 17, Color.white, TextAlignmentOptions.TopLeft);
        detailText.richText = true;
        detailText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        actionButton = CreateButton(detailBox.transform, "", TabOnColor, out actionLabel);
        actionButton.GetComponent<LayoutElement>().preferredHeight = 40;
        actionButton.gameObject.SetActive(false);

        // 오른쪽: 인벤토리 (탭 + 스크롤)
        Image listColumn = CreateImage("Inventory", columnsGo.transform, ColumnColor);
        listColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        VerticalLayoutGroup listLayout = listColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset(10, 10, 10, 10);
        listLayout.spacing = 8;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        GameObject tabsGo = new GameObject("Tabs", typeof(RectTransform));
        tabsGo.transform.SetParent(listColumn.transform, false);
        tabsGo.AddComponent<LayoutElement>().preferredHeight = 36;
        HorizontalLayoutGroup tabs = tabsGo.AddComponent<HorizontalLayoutGroup>();
        tabs.spacing = 8;
        tabs.childControlWidth = true;
        tabs.childControlHeight = true;
        tabs.childForceExpandWidth = true;
        Button equipTab = CreateButton(tabsGo.transform, M_LanguageManager.Get("ui.inventory.tab_equip", "장비"), TabOnColor, out _);
        equipTabImage = equipTab.GetComponent<Image>();
        equipTab.onClick.AddListener(() => { consumableTab = false; Refresh(); });
        Button potionTab = CreateButton(tabsGo.transform, M_LanguageManager.Get("ui.inventory.tab_potion", "소모품"), TabOffColor, out _);
        potionTabImage = potionTab.GetComponent<Image>();
        potionTab.onClick.AddListener(() => { consumableTab = true; Refresh(); });

        Image viewport = CreateImage("Viewport", listColumn.transform, new Color(0f, 0f, 0f, 0.2f));
        viewport.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        listContent = contentGo.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.offsetMin = Vector2.zero;
        listContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(6, 6, 6, 6);
        contentLayout.spacing = 5;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = listContent;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        emptyText = CreateText("Empty", listColumn.transform, 17, new Color(0.7f, 0.7f, 0.7f, 1f), TextAlignmentOptions.Center);
        emptyText.text = M_LanguageManager.Get("ui.inventory.empty_list", "보유한 항목이 없습니다");
        emptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

        Button close = CreateButton(panel, M_LanguageManager.Get("ui.inventory.close", "닫기"), TabOffColor, out _);
        close.GetComponent<LayoutElement>().preferredHeight = 38;
        close.onClick.AddListener(CloseInternal);
    }

    static Button CreateButton(Transform parent, string labelText, Color color, out TextMeshProUGUI label)
    {
        Image image = CreateImage("Button", parent, color);
        image.gameObject.AddComponent<LayoutElement>();
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = CreateText("Label", image.rectTransform, 18, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 18;
        label.margin = new Vector4(6, 2, 6, 2);
        label.text = labelText;
        Stretch(label.rectTransform);
        return button;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, float size, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (M_LanguageManager.currnetFont != null) text.font = M_LanguageManager.currnetFont;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
