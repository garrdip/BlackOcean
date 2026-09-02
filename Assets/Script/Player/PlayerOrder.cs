using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;
using TMPro;
using ProjectD;

// 좌상단 오더(대열) 배너 — 파티원별 골드/스탯 표시, 클릭으로 제어 캐릭터 전환·거점 대열 교환.
// 구 '다른 플레이어 패 보기'(카드포켓 스왑) 기능은 카드 시스템 제거로 삭제됨 (2026-09-01) — 프리팹의 LastCardLayout은 항상 숨긴다.
public class PlayerOrder : NetworkBehaviour
{
    // 활성(선택) 배너 — 클라이언트 로컬. 소유 배너를 클릭하면 활성화되며 그 캐릭터가 제어 대상(currentGamePlayer — 스킬트리/장비/스탯 창)이 된다.
    // 활성 상태에서 다른 소유 배너를 클릭하면 두 캐릭터의 대열 위치를 서로 바꾼다 (거점 전용, 서버 M_TurnManager.CmdSwapPlayerOrderInHub) (2026-09-01)
    static PlayerOrder activeBanner;
    Collider2D bannerCollider; // BaseLayout의 PolygonCollider2D — Update 클릭 판정용
    [Header("Layout")]
    public GameObject BaseLayout;
    public GameObject TopLayout;
    public GameObject LastCardLayout; // (구 패 보기 레이아웃 — 항상 숨김)

    [Header("BaseLayout Components")]
    public GameObject uLight;
    public GameObject uBase;
    public GameObject uBaseC;
    public GameObject uLine;
    public GameObject uLineLight;
    public GameObject uMyLine;
    public GameObject uMyLineLight;

    [Header("TopLayout Components")]
    public GameObject topBase;
    public GameObject topBaseLight;
    public GameObject topMy;
    public GameObject topMyLight;
    public GameObject topSee;
    public GameObject topSeeLight;
    public GameObject topReady;
    public GameObject topReadyLight;

    public TextMeshProUGUI textGold;

    // 임시 스탯 표시 (공격력/방어력) — 정식 UI 전까지. TextGold를 복제해 골드 아래 줄에 배치
    TextMeshProUGUI textStats;
    GamePlayer statGamePlayer;
    int lastShownAttack = int.MinValue;
    int lastShownDefense = int.MinValue;

    [SyncVar]
    public uint gamePlayerNetId;


    void Awake()
    {
        EventTrigger baseEventTrigger = BaseLayout.AddComponent<EventTrigger>();

        EventTrigger.Entry baseEnterEntry = new EventTrigger.Entry();
        baseEnterEntry.eventID = EventTriggerType.PointerEnter;
        baseEnterEntry.callback.AddListener((data) => { OnPointerEnterBase((PointerEventData)data); });
        baseEventTrigger.triggers.Add(baseEnterEntry);

        EventTrigger.Entry baseExitEntry = new EventTrigger.Entry();
        baseExitEntry.eventID = EventTriggerType.PointerExit;
        baseExitEntry.callback.AddListener((data) => { OnPointerExitBase((PointerEventData)data); });
        baseEventTrigger.triggers.Add(baseExitEntry);

        // 클릭 판정용 콜라이더 — BaseLayout(레이어 5 UI)의 PolygonCollider2D. 카메라 Physics2DRaycaster의 eventMask가 UI 레이어를 제외해
        // EventTrigger/OnMouseDown 경로로는 배너에 포인터 이벤트가 오지 않으므로, Update에서 마우스 클릭 시 OverlapPoint로 직접 판정한다
        bannerCollider = BaseLayout.GetComponent<Collider2D>();
    }

    public void OnPointerEnterBase(PointerEventData eventData)
    {
        uMyLineLight.SetActive(isOwned && uMyLine.activeSelf);
        topMyLight.SetActive(isOwned && topMy.activeSelf);
        uLineLight.SetActive(true);
        topBaseLight.SetActive(true);
    }

    public void OnPointerExitBase(PointerEventData eventData)
    {
        uMyLineLight.SetActive(false);
        topMyLight.SetActive(false);
        uLineLight.SetActive(false);
        topBaseLight.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            gamePlayer.onChangePlayerOrder += OnChangePlayerOrder;
            gamePlayer.onChangeGold += OnChangeGold;
            SetParentAndPostion(gamePlayer.selectOrder, animate: false); // 초기 배치는 즉시 — 스폰 위치(원점)에서 미끄러지는 연출 방지
            SetOwnedViewComponent();
            textGold.text = gamePlayer.gold.ToString();
            CreateStatsText(gamePlayer);
        }
    }

    // 임시 — 공격력/방어력 수치 표시. 스탯 변경 델리게이트가 없어 Update에서 값이 바뀔 때만 텍스트 갱신
    void CreateStatsText(GamePlayer gamePlayer)
    {
        statGamePlayer = gamePlayer;
        if(textGold == null) return;
        textStats = Instantiate(textGold, textGold.canvas.transform); // 같은 월드 캔버스 아래
        textStats.name = "TextStats";
        textStats.rectTransform.anchoredPosition = new Vector2(0f, 0.2f); // 골드 줄 아래
        textStats.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
        textStats.fontSize = 0.25f;
        textStats.alignment = TextAlignmentOptions.Center;
        textStats.text = "";
    }

    void Update()
    {
        // 배너 클릭 판정 — 마우스 왼쪽 버튼을 누른 프레임에 배너 콜라이더가 마우스 월드 위치를 덮고 있으면 클릭으로 처리
        if(Input.GetMouseButtonDown(0) && bannerCollider != null && Camera.main != null)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if(bannerCollider.OverlapPoint(mouseWorld)) OnClickBanner();
        }

        if(textStats == null || statGamePlayer == null) return;
        CharacterStatData.Entry stat = CharacterStatData.Get(statGamePlayer.character);
        int attack = (stat != null && stat.attackScalesWithInt) ? statGamePlayer.TotalIntelligence : statGamePlayer.TotalStrength; // 캐릭터 공격 계수 스탯 (장비 합산)
        int defenseValue = statGamePlayer.TotalDefense;
        if(attack == lastShownAttack && defenseValue == lastShownDefense) return;
        lastShownAttack = attack;
        lastShownDefense = defenseValue;
        textStats.text = $"공격 {attack}  방어 {defenseValue}";
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if(activeBanner == this) activeBanner = null; // 활성 배너가 사라지면 선택 해제
        if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
            gamePlayer.onChangePlayerOrder -= OnChangePlayerOrder;
        }
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    // 배너 클릭 — 활성 배너가 없으면(또는 자신이면) 활성 토글 + 제어 캐릭터 전환, 다른 배너가 활성이면 그 캐릭터와 대열 위치 교환
    void OnClickBanner()
    {
        if(!isOwned || PlayerRegistry.Local == null) return;
        if(activeBanner == null || activeBanner == this)
        {
            bool select = activeBanner != this;
            SetSelected(select);
            if(select) PlayerRegistry.Local.currentGamePlayerNetId = gamePlayerNetId; // 스킬트리/장비/스탯 창(GamePlayer.OnGUI)이 이 캐릭터로 바뀐다
            return;
        }
        PlayerOrder other = activeBanner;
        other.SetSelected(false);
        bool inHub = M_HubManager.instance != null && M_HubManager.instance.isInHub
            && M_TurnManager.instance != null && M_TurnManager.instance.phase == BattleTurn.NONE_BATTLE_SCENE;
        if(inHub) M_TurnManager.instance.CmdSwapPlayerOrderInHub(other.gamePlayerNetId, gamePlayerNetId); // 전투 중 대열 이동은 TP 행동 '이동'으로만
    }

    // 활성 표시 — 기본 비활성인 Ready 라이트(topReadyLight)를 선택 표시로 재사용 (topReady 본체는 프리팹 기본이 활성이라 건드리지 않는다)
    void SetSelected(bool selected)
    {
        if(selected) activeBanner = this;
        else if(activeBanner == this) activeBanner = null;
        if(topReadyLight != null) topReadyLight.SetActive(selected);
    }

    public void OnChangePlayerOrder(int order)
    {
        SetParentAndPostion(order);
    }

    public void OnChangeGold(int gold)
    {
        textGold.text = gold.ToString();
    }

    // 참조된 게임플레이어 클래스로부터 오더값 조회하여 값에 맞춰 뷰 컴포넌트 세팅
    // animate: 오더 변경(파티원 이탈 등)은 슬라이드 연출, 최초 배치(OnStartClient)는 즉시 배치
    private void SetParentAndPostion(int order, bool animate = true)
    {
        Vector3 target = new Vector3(M_TurnManager.instance.targetObjectPosition[order].x, 8f, 0f);
        transform.DOKill();
        if(animate) transform.DOMove(target, 0.5f);
        else transform.position = target;
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // 본인 소유임을 구분하는 뷰 컴포넌트 세팅
    private void SetOwnedViewComponent()
    {
        uLight.SetActive(isOwned);
        uMyLine.SetActive(isOwned);
        topMy.SetActive(isOwned);
        topSee.SetActive(!isOwned);
        topSeeLight.SetActive(false);
        if(LastCardLayout != null) LastCardLayout.SetActive(false); // 패 보기 기능 폐기
    }
}
