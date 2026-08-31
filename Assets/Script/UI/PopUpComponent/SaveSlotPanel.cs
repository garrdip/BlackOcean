using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectD;

/// <summary>
/// 세이브 슬롯 선택 패널 — 메뉴 '처음부터 시작 / 이어하기'(MenuUI)와 멀티 '방 만들기'(CreateLobby)가 공용으로 쓴다.
/// 슬롯(GameSaveService.SlotCount)마다 요약(저장 시각 / 해금 스테이지 / 위험도 / 캐릭터·레벨)을 보여주고,
/// NewGame 모드: 모든 슬롯 선택 가능 — 데이터가 있는 슬롯은 '덮어쓰기'로 표시되고 두 번 클릭해야 확정.
/// Continue 모드: 데이터가 있는 슬롯만 선택 가능.
/// 삭제 버튼도 두 번 클릭 확정. Esc/돌아가기 = 취소. 씬/프리팹 참조 없이 런타임에 UI를 구성한다 (LevelUpPopUp과 같은 방식).
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    public enum Mode { NewGame, Continue }

    const int SortingOrder = 400;
    const float FadeTime = 0.2f;
    static readonly Color GoldColor = ProjectD.ColorUtils.HexToColor("#DAA520");
    static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.14f, 0.97f);
    static readonly Color SlotColor = new Color(0.16f, 0.16f, 0.22f, 1f);
    static readonly Color SlotEmptyColor = new Color(0.13f, 0.13f, 0.17f, 1f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color DangerColor = new Color(0.75f, 0.25f, 0.25f, 1f);

    static SaveSlotPanel instance;

    /// <summary>패널 표시 중 — MenuUI가 Esc 처리를 양보하는 기준</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    Mode mode;
    System.Action<int> onSelected;
    System.Action onCancel;
    int pendingConfirmSlot = -1;   // 두 번 클릭 확정 대기 중인 슬롯
    bool pendingConfirmIsDelete;

    CanvasGroup canvasGroup;
    TextMeshProUGUI titleText;
    readonly List<SlotView> slotViews = new List<SlotView>();

    class SlotView
    {
        public Image background;
        public TextMeshProUGUI title, line1, line2;
        public Button selectButton, deleteButton;
        public TextMeshProUGUI selectLabel, deleteLabel;
    }

    // ------------------------------------------------------------------ 진입점 ------------------------------------------------------------------ //

    /// <summary>패널 열기. onSelected(slot)은 확정된 슬롯 인덱스(0~)로 호출되며 패널은 닫힌다. 취소(Esc/돌아가기) 시 onCancel</summary>
    public static void Open(Mode mode, System.Action<int> onSelected, System.Action onCancel = null)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("SaveSlotPanel");
            instance = go.AddComponent<SaveSlotPanel>();
        }
        instance.mode = mode;
        instance.onSelected = onSelected;
        instance.onCancel = onCancel;
        instance.pendingConfirmSlot = -1;
        instance.gameObject.SetActive(true);
        instance.Refresh();
        instance.canvasGroup.alpha = 0f;
        instance.canvasGroup.interactable = true;
        instance.canvasGroup.blocksRaycasts = true;
        DOTween.Kill(instance.canvasGroup);
        instance.canvasGroup.DOFade(1f, FadeTime);
    }

    public static void Close()
    {
        if (instance == null) return;
        instance.CloseInternal(false);
    }

    void Awake()
    {
        BuildUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseInternal(true);
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
        if (instance == this) instance = null;
    }

    void CloseInternal(bool cancelled)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        DOTween.Kill(canvasGroup);
        canvasGroup.DOFade(0f, FadeTime).OnComplete(() => gameObject.SetActive(false));
        if (cancelled) onCancel?.Invoke();
    }

    // ------------------------------------------------------------------ 내용 ------------------------------------------------------------------ //

    void Refresh()
    {
        titleText.text = mode == Mode.NewGame
            ? M_LanguageManager.Get("ui.save.title_new", "새 게임 — 저장할 슬롯을 선택하세요")
            : M_LanguageManager.Get("ui.save.title_continue", "이어하기 — 불러올 슬롯을 선택하세요");

        for (int i = 0; i < slotViews.Count; i++)
        {
            SlotView view = slotViews[i];
            GameSaveService.RpgSaveData data = GameSaveService.Peek(i);
            bool exists = data != null;
            view.background.color = exists ? SlotColor : SlotEmptyColor;
            view.title.text = M_LanguageManager.Get("ui.save.slot", "슬롯 {0}").Replace("{0}", (i + 1).ToString());

            if (exists)
            {
                view.line1.text = M_LanguageManager.Get("ui.save.summary", "저장 {0}  |  해금 스테이지 {1}  |  위험도 {2}")
                    .Replace("{0}", string.IsNullOrEmpty(data.savedAt) ? "-" : data.savedAt)
                    .Replace("{1}", Mathf.Max(1, data.unlockedStageCount).ToString())
                    .Replace("{2}", data.hazardLevel.ToString());
                view.line2.text = DescribeProfiles(data);
            }
            else
            {
                view.line1.text = M_LanguageManager.Get("ui.save.empty", "빈 슬롯");
                view.line2.text = "";
            }

            bool confirming = pendingConfirmSlot == i;
            // 선택 버튼
            bool selectable = mode == Mode.NewGame || exists;
            view.selectButton.interactable = selectable;
            if (mode == Mode.NewGame && exists)
                view.selectLabel.text = confirming && !pendingConfirmIsDelete
                    ? M_LanguageManager.Get("ui.save.confirm_overwrite", "정말 덮어쓸까요? 다시 클릭")
                    : M_LanguageManager.Get("ui.save.overwrite", "덮어쓰기");
            else
                view.selectLabel.text = M_LanguageManager.Get("ui.save.select", "선택");
            view.selectButton.image.color = (mode == Mode.NewGame && exists) ? DangerColor : GoldColor;

            // 삭제 버튼 — 데이터가 있을 때만
            view.deleteButton.gameObject.SetActive(exists);
            view.deleteLabel.text = confirming && pendingConfirmIsDelete
                ? M_LanguageManager.Get("ui.save.confirm_delete", "정말 삭제할까요? 다시 클릭")
                : M_LanguageManager.Get("ui.save.delete", "삭제");
        }
    }

    static string DescribeProfiles(GameSaveService.RpgSaveData data)
    {
        if (data.profiles == null || data.profiles.Count == 0) return "";
        var parts = new List<string>();
        foreach (GameSaveService.ProfileData profile in data.profiles)
            parts.Add($"{M_LanguageManager.GetCharacterName(profile.character)} Lv.{profile.level}");
        return string.Join("   ", parts);
    }

    void OnClickSelect(int slot)
    {
        PlayClick();
        bool exists = GameSaveService.HasSaveFile(slot);
        if (mode == Mode.Continue && !exists) return;

        if (mode == Mode.NewGame && exists && !(pendingConfirmSlot == slot && !pendingConfirmIsDelete))
        {
            pendingConfirmSlot = slot; // 덮어쓰기 1차 클릭 — 확인 문구로 바꾸고 대기
            pendingConfirmIsDelete = false;
            Refresh();
            return;
        }

        pendingConfirmSlot = -1;
        System.Action<int> callback = onSelected;
        CloseInternal(false);
        callback?.Invoke(slot);
    }

    void OnClickDelete(int slot)
    {
        PlayClick();
        if (!(pendingConfirmSlot == slot && pendingConfirmIsDelete))
        {
            pendingConfirmSlot = slot; // 삭제 1차 클릭
            pendingConfirmIsDelete = true;
            Refresh();
            return;
        }
        pendingConfirmSlot = -1;
        GameSaveService.DeleteSlot(slot);
        Refresh();
    }

    static void PlayClick()
    {
        if (M_SoundManager.instance == null) return;
        AudioClip clip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        if (clip != null) M_SoundManager.instance.PlaySFX(clip, clip.length);
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

        Image dim = CreateImage("Dim", transform, DimColor);
        Stretch(dim.rectTransform);
        Button dimButton = dim.gameObject.AddComponent<Button>(); // 바깥 클릭 = 취소
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(() => CloseInternal(true));

        Image panelImage = CreateImage("Panel", transform, PanelColor);
        RectTransform panel = panelImage.rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(820, 0);
        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(32, 32, 28, 28);
        panelLayout.spacing = 12;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(2, -2);

        titleText = CreateText("Title", panel, 30, GoldColor, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateSpacer(panel, 6);

        for (int i = 0; i < GameSaveService.SlotCount; i++)
        {
            int slot = i;
            SlotView view = new SlotView();
            view.background = CreateImage($"Slot{slot + 1}", panel, SlotColor);
            view.background.gameObject.AddComponent<LayoutElement>().preferredHeight = 112;
            HorizontalLayoutGroup row = view.background.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 20, 12, 12);
            row.spacing = 16;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            // 왼쪽: 슬롯 제목 + 요약 2줄
            GameObject infoGo = new GameObject("Info", typeof(RectTransform));
            infoGo.transform.SetParent(view.background.transform, false);
            infoGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            VerticalLayoutGroup info = infoGo.AddComponent<VerticalLayoutGroup>();
            info.spacing = 4;
            info.childAlignment = TextAnchor.MiddleLeft;
            info.childControlWidth = true;
            info.childControlHeight = true;
            info.childForceExpandWidth = true;
            info.childForceExpandHeight = false;
            view.title = CreateText("Title", infoGo.transform, 24, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            view.line1 = CreateText("Line1", infoGo.transform, 18, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.MidlineLeft);
            view.line2 = CreateText("Line2", infoGo.transform, 18, GoldColor, TextAlignmentOptions.MidlineLeft);

            // 오른쪽: 삭제 / 선택 버튼
            view.deleteButton = CreateButton("Delete", view.background.transform, 150, 44, new Color(0.35f, 0.35f, 0.4f, 1f), out view.deleteLabel);
            view.deleteButton.onClick.AddListener(() => OnClickDelete(slot));
            view.selectButton = CreateButton("Select", view.background.transform, 210, 56, GoldColor, out view.selectLabel);
            view.selectButton.onClick.AddListener(() => OnClickSelect(slot));

            slotViews.Add(view);
        }

        CreateSpacer(panel, 4);
        Button back = CreateButton("Back", panel, 200, 48, new Color(0.3f, 0.3f, 0.36f, 1f), out TextMeshProUGUI backLabel);
        backLabel.text = M_LanguageManager.Get("ui.save.back", "돌아가기");
        back.onClick.AddListener(() => { PlayClick(); CloseInternal(true); });
    }

    static Button CreateButton(string name, Transform parent, float width, float height, Color color, out TextMeshProUGUI label)
    {
        Image image = CreateImage(name, parent, color);
        LayoutElement element = image.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
        element.flexibleWidth = 0;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = CreateText("Label", image.rectTransform, 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold); // 기본 폰트가 검정 외곽선이라 흰 글자
        label.enableAutoSizing = true; // 확인 문구("정말 덮어쓸까요? 다시 클릭")처럼 긴 라벨은 줄여서 한 줄에
        label.fontSizeMin = 13;
        label.fontSizeMax = 20;
        label.margin = new Vector4(6, 2, 6, 2);
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
