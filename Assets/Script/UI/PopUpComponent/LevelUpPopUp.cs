using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectD;

/// <summary>
/// 레벨업 연출 팝업 — 전투 종료 후 경험치 지급으로 레벨업하면 소유자 화면에 스탯 변화표를 띄운다.
/// 각 스탯은 "현재 값 (+증가치)" 형식으로 표시하고, 증가치는 행마다 시차를 두고 나타난다.
/// 씬/프리팹 참조 없이 런타임에 UI를 직접 구성한다 (RPG 전환 임시 UI 관례 — 정식 팝업 프리팹으로 교체 가능).
/// 여러 캐릭터가 동시에 레벨업하면 큐로 순서대로 보여준다. 호출: GamePlayer.RpcLevelUp → LevelUpPopUp.Show(...)
/// </summary>
public class LevelUpPopUp : MonoBehaviour
{
    struct Entry
    {
        public Character character;
        public int fromLevel, toLevel, gainedPoints;
        public GamePlayer.LevelUpStats before, after;
    }

    const int SortingOrder = 300;            // 전투 보상 팝업보다 위에 표시
    const float FadeTime = 0.3f;
    const float RowRevealInterval = 0.12f;   // 증가치(+N) 행별 등장 간격
    static readonly Color GoldColor = ProjectD.ColorUtils.HexToColor("#DAA520");
    static readonly Color GainColor = ProjectD.ColorUtils.HexToColor("#7CFC00");
    static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.14f, 0.96f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);

    static LevelUpPopUp instance;

    readonly Queue<Entry> queue = new Queue<Entry>();
    bool showing;

    CanvasGroup canvasGroup;
    RectTransform panel;
    TextMeshProUGUI titleText, levelText, pointsText, confirmLabel;
    RectTransform rowsRoot;
    Button confirmButton;
    readonly List<TextMeshProUGUI> rowValueTexts = new List<TextMeshProUGUI>();
    readonly List<TextMeshProUGUI> rowGainTexts = new List<TextMeshProUGUI>();
    Coroutine revealRoutine;

    // ------------------------------------------------------------------ 진입점 ------------------------------------------------------------------ //

    /// <summary>레벨업 연출 요청 (소유 클라이언트). 표시 중이면 큐에 쌓였다가 확인 후 순서대로 나온다</summary>
    public static void Show(Character character, int fromLevel, int toLevel, int gainedPoints, GamePlayer.LevelUpStats before, GamePlayer.LevelUpStats after)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("LevelUpPopUp");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LevelUpPopUp>();
        }
        instance.queue.Enqueue(new Entry { character = character, fromLevel = fromLevel, toLevel = toLevel, gainedPoints = gainedPoints, before = before, after = after });
        instance.TryShowNext();
    }

    void Awake()
    {
        BuildUI();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    void Update()
    {
        if (showing && canvasGroup.interactable && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            OnConfirm();
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
        DOTween.Kill(panel);
        if (instance == this) instance = null;
    }

    // ------------------------------------------------------------------ 표시/숨김 ------------------------------------------------------------------ //

    void TryShowNext()
    {
        if (showing || queue.Count == 0) return;
        showing = true;
        Entry entry = queue.Dequeue();
        Fill(entry);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false; // 연출 중 클릭 방지 — 행 등장이 끝나면 활성화
        DOTween.Kill(canvasGroup);
        DOTween.Kill(panel);
        canvasGroup.alpha = 0f;
        panel.localScale = Vector3.one * 0.85f;
        canvasGroup.DOFade(1f, FadeTime);
        panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack);

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RevealGains());
    }

    // 증가치(+N)를 행마다 시차를 두고 표시
    IEnumerator RevealGains()
    {
        yield return new WaitForSeconds(FadeTime);
        foreach (TextMeshProUGUI gain in rowGainTexts)
        {
            if (string.IsNullOrEmpty(gain.text)) continue;
            gain.alpha = 1f;
            RectTransform rect = gain.rectTransform;
            rect.localScale = Vector3.one * 1.4f;
            rect.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(RowRevealInterval);
        }
        canvasGroup.interactable = true;
        revealRoutine = null;
    }

    void OnConfirm()
    {
        if (!showing) return;
        PlayClick();
        if (revealRoutine != null) { StopCoroutine(revealRoutine); revealRoutine = null; }
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        DOTween.Kill(canvasGroup);
        canvasGroup.DOFade(0f, FadeTime).OnComplete(() =>
        {
            showing = false;
            TryShowNext();
        });
    }

    static void PlayClick()
    {
        if (M_SoundManager.instance == null) return;
        AudioClip clip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        if (clip != null) M_SoundManager.instance.PlaySFX(clip, clip.length);
    }

    // ------------------------------------------------------------------ 내용 채우기 ------------------------------------------------------------------ //

    void Fill(Entry entry)
    {
        string name = CharacterName(entry.character);
        titleText.text = M_LanguageManager.Get("ui.levelup.title", "{0} 레벨 업!").Replace("{0}", name);
        levelText.text = M_LanguageManager.Get("ui.levelup.level", "Lv.{0} → Lv.{1}")
            .Replace("{0}", entry.fromLevel.ToString()).Replace("{1}", entry.toLevel.ToString());
        pointsText.text = M_LanguageManager.Get("ui.levelup.skill_points", "스킬 포인트 +{0}").Replace("{0}", entry.gainedPoints.ToString());
        confirmLabel.text = M_LanguageManager.Get("ui.levelup.confirm", "확인");

        // 행 순서: HP / 힘 / 민첩 / 지능 / 방어력 / 마법방어 / 제어 (GamePlayer.LevelUpStats.ToArray 순서)
        string[] keys = { "ui.stat.hp", "ui.stat.str", "ui.stat.agi", "ui.stat.int", "ui.stat.def", "ui.stat.mdef", "ui.stat.ctrl" };
        string[] fallback = { "HP", "힘", "민첩", "지능", "방어력", "마법방어", "제어" };
        int[] before = entry.before.ToArray();
        int[] after = entry.after.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            SetRow(i, M_LanguageManager.Get(keys[i], fallback[i]), before[i], after[i]);
        }
    }

    void SetRow(int index, string label, int before, int after)
    {
        while (rowValueTexts.Count <= index) CreateRow();
        Transform row = rowsRoot.GetChild(index);
        row.GetChild(0).GetComponent<TextMeshProUGUI>().text = label;
        rowValueTexts[index].text = after.ToString();
        int delta = after - before;
        TextMeshProUGUI gain = rowGainTexts[index];
        gain.text = delta > 0 ? $"(+{delta})" : (delta < 0 ? $"({delta})" : "");
        gain.alpha = 0f; // RevealGains가 순서대로 켠다
        gain.rectTransform.localScale = Vector3.one;
    }

    static string CharacterName(Character character)
    {
        switch (character)
        {
            case Character.GEORK: return M_LanguageManager.Get("ui.LabelGeork", "게오르크");
            case Character.HONGDANHYANG: return M_LanguageManager.Get("ui.LabelDanhyang", "홍단향");
            case Character.ERIS: return M_LanguageManager.Get("ui.LabelEris", "에리스");
        }
        return character.ToString();
    }

    // ------------------------------------------------------------------ UI 구성 (런타임) ------------------------------------------------------------------ //

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

        // 어둡게 깔리는 배경 (클릭 차단)
        Image dim = CreateImage("Dim", transform, DimColor);
        Stretch(dim.rectTransform);

        // 패널
        Image panelImage = CreateImage("Panel", transform, PanelColor);
        panel = panelImage.rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(560, 0);
        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(36, 36, 28, 28);
        panelLayout.spacing = 8;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(2, -2);

        titleText = CreateText("Title", panel, 40, GoldColor, TextAlignmentOptions.Center, FontStyles.Bold);
        levelText = CreateText("Level", panel, 28, Color.white, TextAlignmentOptions.Center);
        pointsText = CreateText("Points", panel, 22, GoldColor, TextAlignmentOptions.Center);
        CreateSpacer(panel, 10);

        // 스탯 행 컨테이너
        GameObject rowsGo = new GameObject("Rows", typeof(RectTransform));
        rowsRoot = rowsGo.GetComponent<RectTransform>();
        rowsRoot.SetParent(panel, false);
        VerticalLayoutGroup rowsLayout = rowsGo.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 4;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        for (int i = 0; i < 7; i++) CreateRow();

        CreateSpacer(panel, 12);

        // 확인 버튼
        Image buttonImage = CreateImage("ConfirmButton", panel, new Color(0.85f, 0.65f, 0.13f, 1f));
        LayoutElement buttonLayout = buttonImage.gameObject.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 56;
        buttonLayout.preferredWidth = 220;
        confirmButton = buttonImage.gameObject.AddComponent<Button>();
        confirmButton.targetGraphic = buttonImage;
        confirmButton.onClick.AddListener(OnConfirm);
        confirmLabel = CreateText("Label", buttonImage.rectTransform, 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold); // 기본 폰트가 검정 외곽선이라 흰 글자여야 읽힘
        Stretch(confirmLabel.rectTransform);
    }

    // 스탯 행: [이름]                [값] [(+N)]
    void CreateRow()
    {
        GameObject row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(rowsRoot, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 34;

        TextMeshProUGUI label = CreateText("Name", row.transform, 24, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.MidlineLeft);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        TextMeshProUGUI value = CreateText("Value", row.transform, 26, Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        value.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
        TextMeshProUGUI gain = CreateText("Gain", row.transform, 24, GainColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        gain.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
        gain.alpha = 0f;

        rowValueTexts.Add(value);
        rowGainTexts.Add(gain);
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
        if (M_LanguageManager.currnetFont != null) text.font = M_LanguageManager.currnetFont; // 미지정이면 TMP 기본 폰트 + 폴백(CJK)
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }

    static void CreateSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = height;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
