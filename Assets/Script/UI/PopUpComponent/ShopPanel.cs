using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Mirror;
using ProjectD;

/// <summary>
/// 메르크리우스 상점 팝업 (아이템 개편 Phase 7-9, Q20·A8) — 씬/프리팹 참조 없이 런타임에 구성한다 (SaveSlotPanel·LevelUpPopUp과 같은 방식).
/// 구매 탭: ShopDB(shopId) 항목 — 클릭 = M_HubManager.CmdShopBuy (NORMAL 인스턴스 / 물약 1개). 판매 탭: 선택 캐릭터(currentGamePlayer)의 **인벤토리** 장비 — 클릭 = CmdShopSell (장착 중은 목록에 없음).
/// 골드는 플레이어 공유 골드(PlayerInterface.gold). 거래 결과는 SyncList/SyncVar 동기화로 반영되며 목록은 변화마다 다시 그린다. Esc/바깥 클릭/닫기 = 닫기.
/// 정식 UI(Phase 8-4)로 교체 전까지의 임시 팝업.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    const int SortingOrder = 400;
    const float FadeTime = 0.2f;
    static readonly Color GoldColor = ColorUtils.HexToColor("#DAA520");
    static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.14f, 0.97f);
    static readonly Color RowColor = new Color(0.16f, 0.16f, 0.22f, 1f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color TabOnColor = new Color(0.30f, 0.26f, 0.16f, 1f);
    static readonly Color TabOffColor = new Color(0.16f, 0.16f, 0.22f, 1f);

    static ShopPanel instance;
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    string shopId;
    bool sellTab;
    CanvasGroup canvasGroup;
    TextMeshProUGUI titleText, goldText, emptyText;
    Image buyTabImage, sellTabImage;
    RectTransform content;
    GamePlayer boundPlayer;

    public static void Open(string shopId)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("ShopPanel");
            instance = go.AddComponent<ShopPanel>();
        }
        instance.shopId = shopId;
        instance.sellTab = false;
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
        if (instance != null) instance.CloseInternal();
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

    // 선택 캐릭터의 인벤토리/골드 변화 구독 — 거래 결과 반영
    void Bind()
    {
        Unbind();
        boundPlayer = PlayerRegistry.Local != null ? PlayerRegistry.Local.currentGamePlayer : null;
        if (boundPlayer == null) return;
        boundPlayer.inventoryEquips.Callback += OnInventoryChanged;
        boundPlayer.inventoryConsumables.Callback += OnConsumablesChanged;
        boundPlayer.onChangeGold += OnGoldChanged;
    }

    void Unbind()
    {
        if (boundPlayer == null) return;
        boundPlayer.inventoryEquips.Callback -= OnInventoryChanged;
        boundPlayer.inventoryConsumables.Callback -= OnConsumablesChanged;
        boundPlayer.onChangeGold -= OnGoldChanged;
        boundPlayer = null;
    }

    void OnInventoryChanged(SyncList<EquipInstance>.Operation op, int index, EquipInstance oldItem, EquipInstance newItem) { if (gameObject.activeSelf) Refresh(); }
    void OnConsumablesChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem) { if (gameObject.activeSelf) Refresh(); }
    void OnGoldChanged(int gold) { if (gameObject.activeSelf) Refresh(); }

    // ------------------------------------------------------------------ 내용 ------------------------------------------------------------------ //

    void Refresh()
    {
        GamePlayer player = boundPlayer;
        titleText.text = M_LanguageManager.Get("ui.shop.title", "메르크리우스 상단") + (player != null ? $"  —  {player.character}" : "");
        goldText.text = M_LanguageManager.Get("ui.shop.gold", "보유 골드 {0}").Replace("{0}", (player != null ? player.gold : 0).ToString());
        buyTabImage.color = sellTab ? TabOffColor : TabOnColor;
        sellTabImage.color = sellTab ? TabOnColor : TabOffColor;

        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        int rows = 0;
        if (!sellTab)
        {
            foreach (ShopData.Entry entry in ShopData.GetEntries(shopId))
            {
                ShopData.Entry captured = entry;
                string detail = entry.kind == ShopData.Kind.EQUIP ? DescribeBase(EquipData.Get(entry.itemNo)) : ConsumableData.Get(entry.itemNo)?.description ?? "";
                bool affordable = player != null && player.gold >= entry.price;
                CreateRow($"{entry.DisplayName}  <size=70%>{detail}</size>", M_LanguageManager.Get("ui.shop.row_price", "{0} 골드").Replace("{0}", entry.price.ToString()), affordable,
                    () => { if (player != null) M_HubManager.instance.CmdShopBuy(player.netId, shopId, captured.itemNo); });
                rows++;
            }
        }
        else if (player != null)
        {
            foreach (EquipInstance item in player.inventoryEquips)
            {
                EquipInstance captured = item;
                EquipGradeData.Def grade = EquipGradeData.Get(item.grade);
                string color = grade != null ? grade.colorHex : "#FFFFFF";
                CreateRow($"<color={color}>{GamePlayer.DescribeEquip(item)}</color>", "+" + M_LanguageManager.Get("ui.shop.row_price", "{0} 골드").Replace("{0}", ShopData.SellPrice(item).ToString()), true,
                    () => M_HubManager.instance.CmdShopSell(player.netId, captured.instanceId));
                rows++;
            }
        }
        emptyText.gameObject.SetActive(rows == 0);
        emptyText.text = sellTab ? M_LanguageManager.Get("ui.shop.empty_sell", "팔 수 있는 장비가 없습니다 (장착 중인 장비는 팔 수 없음)") : M_LanguageManager.Get("ui.shop.empty_buy", "판매 중인 물품이 없습니다");
    }

    static string DescribeBase(EquipData.Def def)
    {
        if (def == null) return "";
        var parts = new List<string> { EquipData.SlotName(def.slot) };
        string kind = EquipData.KindName(def);
        if (kind.Length > 0) parts.Add(kind);
        if (def.IsWeapon) parts.Add($"{BattleActions.AttributeName(def.attribute)}·{(def.scalesWithInt ? "지능" : "힘")} 물공{def.physicalAttack}/마공{def.magicAttack}");
        if (def.IsArmorPiece) parts.Add($"방어{def.defense}/마방{def.magicDefense}" + (def.requireStr > 0 ? $" 힘{def.requireStr}" : ""));
        if (def.requireLevel > 1) parts.Add($"Lv.{def.requireLevel}");
        return string.Join(" · ", parts);
    }

    void CreateRow(string label, string priceLabel, bool interactable, System.Action onClick)
    {
        Image row = CreateImage("Row", content, RowColor);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 6, 6);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI text = CreateText("Label", row.transform, 19, Color.white, TextAlignmentOptions.MidlineLeft);
        text.richText = true;
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        Button button = CreateButton(row.transform, priceLabel, interactable ? GoldColor : new Color(0.4f, 0.4f, 0.4f, 1f), out TextMeshProUGUI buttonLabel);
        button.GetComponent<LayoutElement>().preferredWidth = 170;
        button.interactable = interactable;
        button.onClick.AddListener(() => onClick?.Invoke());
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
        Button dimButton = dim.gameObject.AddComponent<Button>(); // 바깥 클릭 = 닫기
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(CloseInternal);

        Image panelImage = CreateImage("Panel", transform, PanelColor);
        RectTransform panel = panelImage.rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(900, 680);
        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(28, 28, 24, 24);
        panelLayout.spacing = 10;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(2, -2);

        titleText = CreateText("Title", panel, 28, GoldColor, TextAlignmentOptions.Center, FontStyles.Bold);
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
        goldText = CreateText("Gold", panel, 20, Color.white, TextAlignmentOptions.Center);
        goldText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;

        // 탭
        GameObject tabsGo = new GameObject("Tabs", typeof(RectTransform));
        tabsGo.transform.SetParent(panel, false);
        LayoutElement tabsElement = tabsGo.AddComponent<LayoutElement>();
        tabsElement.preferredHeight = 40;
        tabsElement.flexibleHeight = 0; // 가로 레이아웃 그룹의 flexibleHeight 보고로 탭이 세로로 늘어나는 것을 막는다
        HorizontalLayoutGroup tabs = tabsGo.AddComponent<HorizontalLayoutGroup>();
        tabs.spacing = 8;
        tabs.childControlWidth = true;
        tabs.childControlHeight = true;
        tabs.childForceExpandWidth = true;
        Button buyTab = CreateButton(tabsGo.transform, M_LanguageManager.Get("ui.shop.tab_buy", "구매"), TabOnColor, out _);
        buyTabImage = buyTab.GetComponent<Image>();
        buyTab.onClick.AddListener(() => { sellTab = false; Refresh(); });
        Button sellTabButton = CreateButton(tabsGo.transform, M_LanguageManager.Get("ui.shop.tab_sell", "판매"), TabOffColor, out _);
        sellTabImage = sellTabButton.GetComponent<Image>();
        sellTabButton.onClick.AddListener(() => { sellTab = true; Refresh(); });

        // 목록 (스크롤)
        Image viewport = CreateImage("Viewport", panel, new Color(0f, 0f, 0f, 0.25f));
        viewport.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.spacing = 6;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        emptyText = CreateText("Empty", panel, 18, new Color(0.7f, 0.7f, 0.7f, 1f), TextAlignmentOptions.Center);
        emptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;

        Button close = CreateButton(panel, M_LanguageManager.Get("ui.shop.close", "닫기"), TabOffColor, out _);
        close.GetComponent<LayoutElement>().preferredHeight = 40;
        close.onClick.AddListener(CloseInternal);
    }

    static Button CreateButton(Transform parent, string labelText, Color color, out TextMeshProUGUI label)
    {
        Image image = CreateImage("Button", parent, color);
        image.gameObject.AddComponent<LayoutElement>();
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = CreateText("Label", image.rectTransform, 19, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 19;
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
