using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보유 캐릭터 탭 (2026-11-02) — 장비창(InventoryPopUp)·스킬트리 창(SkillTreeUIManager) 좌상단에 내 소유 캐릭터(PlayerRegistry.Local.ownedPlayers)를 텍스트 탭으로 늘어놓고,
/// 클릭하면 조작 캐릭터(currentGamePlayerNetId)를 바꾼다 — 좌상단 파티 배너(PlayerOrder) 클릭과 같은 효과라 두 창이 그 캐릭터로 전환된다.
/// 싱글은 파티 3인(전열 → 후열 순 = ownedPlayers 순서), 멀티는 보통 1명(2캐릭터면 2개). 런타임 구성(씬/프리팹 참조 없음). 아이콘은 추후 이미지로 교체 — 지금은 캐릭터 이름 텍스트.
/// </summary>
public class CharacterTabBar : MonoBehaviour
{
    static readonly Color OnColor = new Color(0.30f, 0.26f, 0.16f, 1f);   // 선택 탭 — InventoryPopUp.TabOnColor와 동일
    static readonly Color OffColor = new Color(0.16f, 0.16f, 0.22f, 1f);  // 비선택 탭
    static readonly Color OnTextColor = new Color(0.855f, 0.647f, 0.125f, 1f); // 선택 탭 글자 — 금색 (#DAA520)
    static readonly Color OffTextColor = Color.white;

    /// <summary>탭 클릭으로 조작 캐릭터가 바뀐 뒤 호출 — 창이 내용을 다시 그린다</summary>
    public System.Action onSelected;

    float tabWidth = 150f, tabHeight = 36f, fontSize = 18f; // 부모 캔버스 좌표계에 맞춰 Create에서 지정 (스킬트리 팝업은 2809×1909 단위라 장비창의 약 2.5배)
    readonly List<(GamePlayer player, Image image, TMP_Text label)> tabs = new List<(GamePlayer, Image, TMP_Text)>();

    /// <summary>
    /// 탭 바 생성. 부모가 레이아웃 그룹이면 fitContent=false(LayoutElement로 높이만 보고, 폭은 부모가 준다),
    /// 자유 배치 부모(스킬트리 팝업처럼 씬에 놓인 패널)면 fitContent=true로 ContentSizeFitter가 내용 크기에 맞춘다 — 호출 측이 앵커/위치를 잡는다.
    /// </summary>
    public static CharacterTabBar Create(Transform parent, bool fitContent, float tabWidth = 150f, float tabHeight = 36f, float fontSize = 18f)
    {
        GameObject go = new GameObject("CharacterTabs", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        CharacterTabBar bar = go.AddComponent<CharacterTabBar>();
        bar.tabWidth = tabWidth;
        bar.tabHeight = tabHeight;
        bar.fontSize = fontSize;

        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = tabHeight / 6f; // 탭 크기에 비례 (장비창 36 → 6, 스킬트리 창 좌표계 90 → 15)
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (fitContent)
        {
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredHeight = tabHeight;
            element.flexibleHeight = 0; // 가로 레이아웃 그룹이 보고하는 flexibleHeight 때문에 세로로 늘어나는 것을 막는다 (InventoryPopUp 탭과 같은 처리)
        }
        return bar;
    }

    /// <summary>탭 목록·선택 표시 갱신 — 열 때와 캐릭터 전환·소유 목록 변화 시. 소유 캐릭터 구성이 바뀌었으면 탭을 다시 만든다</summary>
    public void Refresh()
    {
        PlayerInterface local = PlayerRegistry.Local;
        var owned = new List<GamePlayer>();
        if (local != null)
            foreach (GamePlayer gamePlayer in local.ownedPlayers)
                if (gamePlayer != null) owned.Add(gamePlayer);

        bool changed = owned.Count != tabs.Count;
        for (int i = 0; !changed && i < owned.Count; i++) changed = tabs[i].player != owned[i];
        if (changed) Rebuild(owned);

        GamePlayer current = local != null ? local.currentGamePlayer : null;
        foreach ((GamePlayer player, Image image, TMP_Text label) in tabs)
        {
            bool selected = player == current;
            image.color = selected ? OnColor : OffColor;
            label.color = selected ? OnTextColor : OffTextColor;
            label.text = $"{M_LanguageManager.GetCharacterName(player.character)} <size=75%>Lv.{player.level}</size>";
        }
    }

    void Rebuild(List<GamePlayer> owned)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        tabs.Clear();
        foreach (GamePlayer player in owned)
        {
            GameObject go = new GameObject("Tab_" + player.character, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            Image image = go.AddComponent<Image>();
            image.color = OffColor;
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = tabWidth;
            element.preferredHeight = tabHeight;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            GamePlayer captured = player;
            button.onClick.AddListener(() => Select(captured));

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            if (M_LanguageManager.currnetFont != null) label.font = M_LanguageManager.currnetFont;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * 0.65f;
            label.fontSizeMax = fontSize;
            label.margin = new Vector4(fontSize / 3f, 2, fontSize / 3f, 2);
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            tabs.Add((player, image, label));
        }
    }

    // 탭 클릭 — 조작 캐릭터 전환 (PlayerOrder.OnClickBanner·TabLayout과 같은 방식으로 로컬 인터페이스의 currentGamePlayerNetId를 바꾼다)
    void Select(GamePlayer player)
    {
        if (player == null || PlayerRegistry.Local == null) return;
        if (PlayerRegistry.Local.currentGamePlayerNetId != player.netId)
            PlayerRegistry.Local.currentGamePlayerNetId = player.netId;
        Refresh();
        onSelected?.Invoke();
    }
}
