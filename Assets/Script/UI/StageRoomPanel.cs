using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectD;

/// <summary>
/// 스테이지 화면 우측하단 미니맵 — 미로(격자)의 방을 옛 맵 아이콘(UI_Map/Top Icon: 프레임 Based + 아이콘 Icon + 연결선 Line + 핀)으로 그린다.
/// 표시 규칙(안개): **현재 방과 바로 옆(상하좌우) 방만** 보인다. (showVisitedRooms를 켜면 방문한 방도 어둡게 남는다)
/// - 시작 방(입구, 인덱스 0): 전용 프레임 + "입구" 라벨
/// - 현재 방: 라이트 프레임 + 핀 / 옆 방: 클릭 가능 (미방문이면 라이트 프레임 + 종류 아이콘, 방문이면 일반 프레임)
/// - 빈 방(EMPTY)은 프레임만, 출구(EXIT)는 출구 아이콘, 보스는 스테이지 아이콘. 보이는 방 사이에는 연결선
/// M_HubManager.stageVersion(SyncVar)이 바뀔 때마다 다시 그린다. "귀환" 버튼은 CmdRetreat.
/// </summary>
public class StageRoomPanel : MonoBehaviour
{
    [Header("방 프레임 (옛 맵 UI_Map/Top Icon '1 - 1. B N Based' 재활용) — 일반/라이트")]
    public Sprite frameSprite;
    public Sprite frameSpriteLight;

    [Header("시작 방(입구) 프레임 — 일반/라이트 (비어 있으면 일반 프레임)")]
    public Sprite startFrameSprite;
    public Sprite startFrameSpriteLight;

    [Header("방 아이콘 (옛 맵 UI_Map/Top Icon) — 일반/라이트")]
    public Sprite iconMonster;
    public Sprite iconMonsterLight;
    public Sprite iconElite;
    public Sprite iconEliteLight;
    public Sprite iconCamp;
    public Sprite iconCampLight;
    public Sprite iconEvent;
    public Sprite iconEventLight;
    public Sprite iconItem;
    public Sprite iconItemLight;
    public Sprite iconBoss; // 스테이지 아이콘 재활용
    public Sprite iconExit; // 출구 — 비어 있으면 이벤트 아이콘

    [Header("연결선 / 현재 위치 핀 (옛 맵 '2 - 1. Line', '4. PIN')")]
    public Sprite lineSprite;
    public Sprite pinSprite;

    [Header("레이아웃")]
    public RectTransform roomContainer; // 미니맵 영역 (자유 배치 — 레이아웃 그룹 없음)
    public Button retreatButton;
    public TMP_Text titleText;
    public float iconSize = 96f;        // 방 하나(프레임) 크기
    public float cellGap = 28f;         // 방 사이 간격 (연결선이 보이는 구간)
    [Range(0.3f, 1f)] public float iconInnerRatio = 0.62f; // 프레임 안 아이콘 비율
    public bool showVisitedRooms = false; // true면 방문한 먼 방도 어둡게 표시 (Darkest Dungeon식). 기본은 옆 방만

    static readonly Color visitedFarColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
    static readonly Color lineColor = new Color(1f, 1f, 1f, 0.8f);

    int lastVersion = -1;
    readonly List<GameObject> built = new List<GameObject>();

    void Start()
    {
        if(retreatButton != null)
            retreatButton.onClick.AddListener(() => { if(M_HubManager.instance != null) M_HubManager.instance.CmdRetreat(); });
    }

    void OnEnable()
    {
        lastVersion = -1; // 화면이 켜질 때 즉시 갱신
    }

    void Update()
    {
        M_HubManager hub = M_HubManager.instance;
        if(hub == null) return;
        if(hub.stageVersion != lastVersion)
        {
            lastVersion = hub.stageVersion;
            if(!Rebuild(hub)) lastVersion = -1; // 영역 크기가 아직 계산되지 않았으면 다음 프레임에 다시
        }
    }

    // 미니맵 다시 그리기. 영역 크기를 아직 알 수 없으면 false (호출자가 다음 프레임에 재시도)
    bool Rebuild(M_HubManager hub)
    {
        foreach(GameObject go in built) Destroy(go);
        built.Clear();

        StageData.Entry stage = StageData.Get(hub.currentStageNo);
        int clearedCount = 0;
        foreach(StageRoomInfo room in hub.stageRooms) if(room.cleared) clearedCount++;
        if(titleText != null)
            titleText.text = stage != null ? $"{stage.name}  ({clearedCount}/{hub.stageRooms.Count})" : "";
        if(retreatButton != null)
            retreatButton.interactable = hub.isInStage && hub.battleRoomIndex == -1;
        if(roomContainer == null || hub.stageRooms.Count == 0) return true;

        // 영역 크기 — 켜진 직후에는 레이아웃이 계산되기 전이라 0일 수 있다
        Rect area = roomContainer.rect;
        if(area.width < 10f || area.height < 10f)
        {
            Canvas.ForceUpdateCanvases();
            area = roomContainer.rect;
            if(area.width < 10f || area.height < 10f) return false;
        }

        // 격자 → 미니맵 좌표 (전체 미로 크기 기준으로 중앙 정렬, 영역보다 크면 축소)
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach(StageRoomInfo room in hub.stageRooms)
        {
            minX = Mathf.Min(minX, room.x); maxX = Mathf.Max(maxX, room.x);
            minY = Mathf.Min(minY, room.y); maxY = Mathf.Max(maxY, room.y);
        }
        float pitch = iconSize + cellGap;
        float mapWidth = (maxX - minX + 1) * pitch;
        float mapHeight = (maxY - minY + 1) * pitch;
        float scale = Mathf.Clamp(Mathf.Min(area.width / mapWidth, area.height / mapHeight), 0.35f, 1f);
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;
        Vector2 Position(StageRoomInfo room) => new Vector2((room.x - centerX) * pitch * scale, (room.y - centerY) * pitch * scale);

        int party = hub.partyRoomIndex;
        bool[] revealed = new bool[hub.stageRooms.Count];
        for(int i = 0; i < revealed.Length; i++)
            revealed[i] = i == party || hub.IsAdjacent(party, i) || (showVisitedRooms && hub.stageRooms[i].cleared);

        // 연결선 — 둘 다 보이는 인접 방 사이 (방 아래에 깔리도록 먼저 생성)
        for(int i = 0; i < hub.stageRooms.Count; i++)
        {
            if(!revealed[i]) continue;
            for(int j = i + 1; j < hub.stageRooms.Count; j++)
            {
                if(!revealed[j] || !hub.IsAdjacent(i, j)) continue;
                CreateLine(Position(hub.stageRooms[i]), Position(hub.stageRooms[j]), scale);
            }
        }

        // 방
        for(int i = 0; i < hub.stageRooms.Count; i++)
        {
            if(!revealed[i]) continue;
            StageRoomInfo room = hub.stageRooms[i];
            bool isStart = i == 0;
            bool isCurrent = i == party;
            bool adjacent = hub.IsAdjacent(party, i);
            bool canMove = hub.CanMoveTo(i);
            bool highlight = isCurrent || (adjacent && !room.cleared);
            Color tint = (isCurrent || adjacent) ? Color.white : visitedFarColor;

            GameObject roomObject = new GameObject($"Room{i}_{room.RoomType}", typeof(RectTransform), typeof(Image), typeof(Button));
            roomObject.transform.SetParent(roomContainer, false);
            RectTransform rect = roomObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(iconSize * scale, iconSize * scale);
            rect.anchoredPosition = Position(room);

            Image frame = roomObject.GetComponent<Image>();
            frame.sprite = GetFrame(isStart, highlight);
            frame.preserveAspect = true;
            frame.color = tint;

            Button button = roomObject.GetComponent<Button>();
            button.interactable = canMove;
            int roomIndex = i;
            button.onClick.AddListener(() => { if(M_HubManager.instance != null) M_HubManager.instance.CmdEnterRoom(roomIndex); });

            if(isStart)
                CreateLabel(roomObject.transform, "입구", iconSize * scale, tint);
            else
            {
                Sprite iconSprite = GetIcon(room.RoomType, highlight);
                if(iconSprite != null)
                    CreateOverlay(roomObject.transform, "Icon", iconSprite, iconSize * iconInnerRatio * scale, Vector2.zero, tint);
            }
            if(isCurrent && pinSprite != null)
                CreateOverlay(roomObject.transform, "Pin", pinSprite, iconSize * 0.5f * scale, new Vector2(0f, iconSize * 0.55f * scale), Color.white);

            built.Add(roomObject);
        }
        return true;
    }

    void CreateLine(Vector2 from, Vector2 to, float scale)
    {
        GameObject lineObject = new GameObject("Line", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(roomContainer, false);
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        Vector2 delta = to - from;
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, 10f * scale);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Image image = lineObject.GetComponent<Image>();
        image.sprite = lineSprite;
        image.color = lineColor;
        image.raycastTarget = false;
        built.Add(lineObject);
    }

    void CreateOverlay(Transform parent, string name, Sprite sprite, float size, Vector2 offset, Color color)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = offset;
        Image image = overlay.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = color;
    }

    // 시작 방 "입구" 라벨 — 제목 텍스트의 폰트(한글 글리프) 재사용
    void CreateLabel(Transform parent, string text, float size, Color color)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if(titleText != null && titleText.font != null) label.font = titleText.font;
        label.text = text;
        label.fontSize = size * 0.3f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
    }

    Sprite GetFrame(bool isStart, bool light)
    {
        if(isStart)
        {
            Sprite start = light && startFrameSpriteLight != null ? startFrameSpriteLight : startFrameSprite;
            if(start != null) return start;
        }
        return (light && frameSpriteLight != null) ? frameSpriteLight : frameSprite;
    }

    Sprite GetIcon(RoomType room, bool light)
    {
        switch(room)
        {
            case RoomType.EMPTY: return null;
            case RoomType.MONSTER: return light && iconMonsterLight != null ? iconMonsterLight : iconMonster;
            case RoomType.ELITE: return light && iconEliteLight != null ? iconEliteLight : iconElite;
            case RoomType.BOSS: return iconBoss != null ? iconBoss : (light && iconEliteLight != null ? iconEliteLight : iconElite);
            case RoomType.EXIT: return iconExit != null ? iconExit : (light && iconEventLight != null ? iconEventLight : iconEvent);
            case RoomType.CAMP: return light && iconCampLight != null ? iconCampLight : iconCamp;
            case RoomType.ITEM_NPC:
            default: return light && iconEventLight != null ? iconEventLight : iconEvent; // EVENT_POSITIIVE / EVENT_NEGATIVE
        }
    }
}
