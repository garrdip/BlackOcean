using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectD;

/// <summary>
/// 전투 아이템 액션 바 (아이템 개편 Phase 8-5) — TP 전투 OnGUI 액션 바의 '아이템' 버튼이 연다. 런타임 구성(캔버스), 화면 하단 중앙.
/// 소모품마다 버튼(이름 ×수량), 사용 불가(자원 불일치·비전투 전용 등)는 비활성 + 사유. 클릭 = onPick(potionNo) → M_TurnManager가 CmdSubmitTpAction(ITEM).
/// 대상 선택(ValidTarget MEMBER/ENEMY)은 후순위지만, onPick 뒤에 대상 선택 단계를 끼울 수 있게 선택과 제출을 분리해 둔다 (Q5).
/// </summary>
public class BattleItemBar : MonoBehaviour
{
    const int SortingOrder = 380;
    static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    static readonly Color ButtonColor = new Color(0.20f, 0.20f, 0.28f, 1f);
    static readonly Color DisabledColor = new Color(0.14f, 0.14f, 0.18f, 1f);

    static BattleItemBar instance;
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    RectTransform row;
    TextMeshProUGUI titleText;
    Action<string> onPick;
    Action onCancel;

    public static void Open(GamePlayer actingPlayer, Action<string> onPick, Action onCancel)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("BattleItemBar");
            instance = go.AddComponent<BattleItemBar>();
        }
        instance.onPick = onPick;
        instance.onCancel = onCancel;
        instance.gameObject.SetActive(true);
        instance.Refresh(actingPlayer);
    }

    public static void Close()
    {
        if (instance != null) instance.gameObject.SetActive(false);
    }

    void Awake() { BuildUI(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Cancel();
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void Cancel()
    {
        gameObject.SetActive(false);
        onCancel?.Invoke();
    }

    void Refresh(GamePlayer player)
    {
        for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
        titleText.text = M_LanguageManager.Get("ui.battle.item_title", "사용할 아이템") + (player != null ? $" — {M_LanguageManager.GetCharacterName(player.character)}" : "");
        if (player != null)
        {
            var counts = new Dictionary<string, int>();
            foreach (string potionNo in player.inventoryConsumables) { counts.TryGetValue(potionNo, out int c); counts[potionNo] = c + 1; }
            foreach (KeyValuePair<string, int> entry in counts)
            {
                ConsumableData.Def potion = ConsumableData.Get(entry.Key);
                if (potion == null) continue;
                string captured = entry.Key;
                bool usable = ConsumableData.CanUse(potion, player, true, out string reasonKey);
                string label = $"{ConsumableData.DisplayName(potion)} ×{entry.Value}" + (usable ? "" : $"\n<size=70%>{ConsumableData.ReasonText(reasonKey)}</size>");
                Button button = CreateButton(row, label, usable ? ButtonColor : DisabledColor);
                button.interactable = usable;
                button.onClick.AddListener(() => { gameObject.SetActive(false); onPick?.Invoke(captured); });
            }
        }
        Button cancel = CreateButton(row, M_LanguageManager.Get("ui.battle.cancel", "취소"), DisabledColor);
        cancel.onClick.AddListener(Cancel);
    }

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

        GameObject panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(transform, false);
        Image panel = panelGo.AddComponent<Image>();
        panel.color = PanelColor;
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 230f); // OnGUI 액션 바 위
        VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText("Title", panelGo.transform, 18, ColorUtils.HexToColor("#DAA520"), TextAlignmentOptions.Center, FontStyles.Bold);

        GameObject rowGo = new GameObject("Row", typeof(RectTransform));
        rowGo.transform.SetParent(panelGo.transform, false);
        row = rowGo.GetComponent<RectTransform>();
        HorizontalLayoutGroup rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
    }

    static Button CreateButton(Transform parent, string labelText, Color color)
    {
        GameObject go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredWidth = 190;
        element.preferredHeight = 56;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI label = CreateText("Label", go.transform, 17, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        label.richText = true;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11;
        label.fontSizeMax = 17;
        label.margin = new Vector4(6, 2, 6, 2);
        label.text = labelText;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return button;
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
}
