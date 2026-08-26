using UnityEngine;
using Mirror;
using ProjectD;


/// <summary>화면 루트 종류 — 거점 / 스테이지(미로 진행) / 전투</summary>
public enum HubView { Hub, Stage, Battle }


/// <summary>
/// 거점(Hub) 매니저 — 맵 타일 시스템(2D 육각형 맵 / 3D 구체 맵)을 대체한다.
/// GameScene은 세 개의 화면 루트를 가진다: 거점(HubScene) / 스테이지(StageScene, 미로 진행 화면) / 전투(BattleScene, 씬의 "Game").
/// 옛 맵과 같은 구조로 화면 전환 시 루트를 토글한다 (SetView).
/// 거점: NPC 4종(류진솔·소피아·메르크리우스·그림자꾼)만 상주(파티 아바타 없음), 각 NPC는 자신의 집(houses 앵커) 아래.
/// 출정: 류진솔 "출정" → 스테이지 선택(StageDB, 해금분만) → StartStage(입장마다 미로 랜덤 생성) → 스테이지 화면(미니맵, StageRoomPanel)
///       → 파티는 입구에서 시작, **현재 방의 인접 방만 보이고 이동 가능**(CmdEnterRoom). 방문한 방은 계속 보이며 자유롭게 되돌아갈 수 있다.
///       → 미방문 방 진입 시 종류별 처리(전투 / 빈 방 / 전초기지 / 이벤트) → 클리어 후 그 방으로 이동
///       → 출구(EXIT) 또는 보스(BOSS) 방 클리어 시 스테이지 클리어 → 다음 스테이지 해금 + 거점 복귀.
///       스테이지 화면에서 "귀환"(CmdRetreat)으로 언제든 거점으로 이탈 가능 (전투 중에는 불가).
/// </summary>
public class M_HubManager : NetworkSingletonD<M_HubManager>
{
    [Header("거점 화면 루트 (거점 배경/집 오브젝트)")]
    public GameObject HubScene;

    [Header("스테이지 화면 루트 (미로 진행 — 배경 + 우측하단 미니맵)")]
    public GameObject StageScene;

    [Header("전투 화면 루트 (씬의 Game)")]
    public GameObject BattleScene;

    [Header("거점 NPC 집 앵커 (류진솔, 소피아, 메르크리우스, 그림자꾼 순) — NPC는 스폰 후 이 집 오브젝트 아래에 배치된다")]
    public Transform[] houses = new Transform[4];

    [Header("거점 카메라 크기")]
    public float hubCameraSize = 10.8f;

    // 거점 NPC 프리팹 이름 (MonsterDB 행 이름과 동일) — houses 인덱스와 1:1
    public static readonly string[] HubNpcNames = { "NPC_RyuJinSol", "NPC_Sophia", "NPC_Mercurius", "NPC_ShadowMan" };

    // 집 앵커가 비어 있을 때의 폴백 위치 — 화면 가로(±19, 카메라 10.8/16:9)에 고르게 분포
    static readonly Vector3[] fallbackNpcPositions = {
        new Vector3(-13.5f, -3f, 0f),
        new Vector3(-4.5f, -3f, 0f),
        new Vector3(4.5f, -3f, 0f),
        new Vector3(13.5f, -3f, 0f)
    };

    [SyncVar]
    public bool isInHub; // 거점 상태 (true) / 스테이지 진행·전투 중 (false)

    [SyncVar]
    public int unlockedStageCount = 1; // 해금된 스테이지 수 (StageDB 행 순서 기준) — 처음엔 1-1만. 저장/복원 대상

    [SyncVar]
    public int hazardLevel; // 전역 위험도 (위험도 시스템) — 일반 스테이지 클리어 +1 / 엘리트 처치 +3 / 보스 처치 +5 (BalanceDB), 소피아에게 골드를 주고 하향. 저장/복원 대상.
                            // 전투의 유효 위험도 = 스테이지 기본 위험도(StageDB Hazard) + 전역 위험도 → 몬스터별 보너스 스탯(MonsterStatDB Hazard*) x 위험도 / 보상 배율(BalanceDB)

    // ---- 스테이지 진행 상태 (서버 권위, 클라 미니맵 표시용으로 동기화) ----
    [SyncVar]
    public bool isInStage; // 스테이지 진행 중 (미로 화면 또는 방 전투)

    [SyncVar]
    public string currentStageNo = ""; // 진행 중인 스테이지

    public readonly SyncList<StageRoomInfo> stageRooms = new SyncList<StageRoomInfo>(); // 미로의 방 목록 (인덱스 0 = 입구)

    [SyncVar]
    public int partyRoomIndex = -1; // 파티가 서 있는 방

    [SyncVar]
    public int battleRoomIndex = -1; // 진입 처리(전투) 중인 방 — 없으면 -1

    [SyncVar]
    public int stageVersion; // 스테이지 상태가 바뀔 때마다 +1 — 클라 미니맵(StageRoomPanel)이 이 값으로 갱신 시점을 잡는다

    bool stageSelectOpen; // 클라이언트 로컬: 출정 스테이지 선택 패널 표시 여부

    public HubView currentView { get; private set; } = HubView.Hub;

    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
        SetView(HubView.Hub); // 씬 시작은 거점 화면 (로딩 화면 뒤에서 대기)
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 이어서 하기: 프로필 복원(PlayerInterface.GenerateGamePlayer → GameSaveService.FindProfile)이 Loaded를 읽으므로
        // 플레이어 오브젝트가 스폰되기 전(씬 오브젝트 OnStartServer)에 파일을 로드해 둔다. 파일 없음/실패 → 일반 시작
        if(GameSaveService.pendingLoad && GameSaveService.TryLoad()){
            unlockedStageCount = Mathf.Clamp(GameSaveService.Loaded.unlockedStageCount, 1, Mathf.Max(1, StageData.Count)); // 구 세이브(필드 없음=0)는 1-1만
            hazardLevel = Mathf.Max(0, GameSaveService.Loaded.hazardLevel); // 구 세이브(필드 없음)는 0. 상한 없음
        }else
            GameSaveService.ClearPending();
    }

    // ------------------------------------------------------------ 화면 루트 토글 (클라이언트) -------------------------------------------------- //

    // 거점/스테이지/전투 화면 루트 전환 — 옛 맵의 MapScene/BattleScene 토글과 동일 구조
    public void SetView(HubView view)
    {
        currentView = view;
        if(HubScene != null) HubScene.SetActive(view == HubView.Hub);
        if(StageScene != null) StageScene.SetActive(view == HubView.Stage);
        if(BattleScene != null) BattleScene.SetActive(view == HubView.Battle);
        if(Camera.main != null) Camera.main.orthographicSize = (view == HubView.Battle) ? GameUIManager.battelSceneCameraSize : hubCameraSize;
        if(view != HubView.Hub) stageSelectOpen = false;
    }

    // NPC 스폰 위치 = 집 앵커 위치 (앵커가 없으면 폴백)
    public Vector3 GetNpcSpawnPosition(int index)
    {
        if(index >= 0 && index < houses.Length && houses[index] != null) return houses[index].position;
        return fallbackNpcPositions[Mathf.Clamp(index, 0, fallbackNpcPositions.Length - 1)];
    }

    // 스폰된 NPC(타겟오브젝트)를 이름에 맞는 집 오브젝트 아래에 넣는다 (클라이언트 — TargetObject.InitTargetObjectNPC에서 호출)
    public void AttachNpcToHouse(TargetObject npc)
    {
        if(npc == null || npc.monster == null) return;
        int index = System.Array.IndexOf(HubNpcNames, npc.monster.monsterName);
        if(index < 0 || index >= houses.Length || houses[index] == null) return;
        npc.transform.SetParent(houses[index], true);
    }

    // ------------------------------------------------------------ 미로 조회 (서버/클라 공용) --------------------------------------------------- //

    public bool IsValidRoom(int index) => index >= 0 && index < stageRooms.Count;

    /// <summary>두 방이 상하좌우로 붙어 있는지</summary>
    public bool IsAdjacent(int a, int b)
    {
        if(!IsValidRoom(a) || !IsValidRoom(b) || a == b) return false;
        return stageRooms[a].IsAdjacentTo(stageRooms[b]);
    }

    /// <summary>클라 표시 규칙 — 현재 방과 바로 옆(상하좌우) 방만 보인다 (StageRoomPanel.showVisitedRooms로 방문 방 표시 확장 가능)</summary>
    public bool IsRoomRevealed(int index)
    {
        if(!IsValidRoom(index)) return false;
        return index == partyRoomIndex || IsAdjacent(partyRoomIndex, index);
    }

    /// <summary>클라 이동 가능 규칙 — 전투/처리 중이 아니고 현재 방의 인접 방</summary>
    public bool CanMoveTo(int index)
    {
        return isInStage && battleRoomIndex == -1 && IsAdjacent(partyRoomIndex, index);
    }

    // ------------------------------------------------------------ 출정 (스테이지 선택) ------------------------------------------------------- //

    // 류진솔 "출정" 버튼 → 스테이지 선택 패널 표시 (클라이언트 로컬)
    public void OpenStageSelect()
    {
        if(!isInHub) return;
        stageSelectOpen = true;
    }

    public void CloseStageSelect()
    {
        stageSelectOpen = false;
    }

    // 스테이지 선택 → 서버에 출정 요청 (거점 NPC 상호작용과 같이 파티원 누구나 요청 가능)
    [Command(requiresAuthority = false)]
    public void CmdStartStage(string stageNo)
    {
        StartStage(stageNo);
    }

    // 해금된 스테이지만 진입 허용 — 미로를 랜덤 생성하고 스테이지 화면(미니맵)으로 들어간다. 파티는 입구(인덱스 0)에서 시작
    [Server]
    public void StartStage(string stageNo)
    {
        StageData.Entry stage = StageData.Get(stageNo);
        int index = StageData.IndexOf(stageNo);
        if(stage == null || index < 0 || index >= unlockedStageCount){
            Debug.LogWarning($"[M_HubManager] 출정 거부 — 스테이지 '{stageNo}' (해금 {unlockedStageCount}개)");
            return;
        }
        if(!isInHub || isInStage || M_TurnManager.instance.isSceneTransitioning || M_TurnManager.instance.phase != BattleTurn.NONE_BATTLE_SCENE) return;

        isInHub = false;
        isInStage = true;
        currentStageNo = stageNo;
        stageRooms.Clear();
        foreach(StageRoomInfo room in stage.GenerateLayout()) stageRooms.Add(room); // 입장할 때마다 미로 랜덤 생성 (StageDB 규칙)
        partyRoomIndex = 0;
        battleRoomIndex = -1;
        stageVersion++;

        RpcNotice("ui.msg.stage_enter", "{0} 진입 — 인접한 방을 선택해 이동하세요", stage.name, "#E700FF");
        M_TurnManager.instance.EnterStageView(); // 페이드 아웃 → 거점 NPC 정리 + 미로 화면 → 페이드 인
    }

    // 방 클릭 → 서버에 이동 요청 (현재 방의 인접 방만 허용)
    [Command(requiresAuthority = false)]
    public void CmdEnterRoom(int index)
    {
        EnterRoom(index);
    }

    [Server]
    public void EnterRoom(int index)
    {
        if(!isInStage || battleRoomIndex != -1 || M_TurnManager.instance.isSceneTransitioning || M_TurnManager.instance.phase != BattleTurn.NONE_BATTLE_SCENE) return;
        if(!IsAdjacent(partyRoomIndex, index)) return; // 인접 방만 (미로 규칙)

        StageRoomInfo room = stageRooms[index];
        if(room.cleared){ // 방문한 방 — 그냥 이동
            partyRoomIndex = index;
            stageVersion++;
            return;
        }

        StageData.Entry stage = StageData.Get(currentStageNo);
        int hazard = GetEffectiveHazard(stage); // 스테이지 기본 위험도 + 전역 위험도
        battleRoomIndex = index;
        stageVersion++;

        switch(room.RoomType)
        {
            case RoomType.MONSTER:
            case RoomType.ELITE:
            case RoomType.BOSS:
                M_TurnManager.instance.GenerateBattleObject(room.RoomType, hazard); // 아바타+몬스터 스폰, 전투 루트로 전환
                break;
            case RoomType.EMPTY:
                RpcNotice("ui.msg.room_empty", "빈 방 — 아무것도 없습니다", "", "#555555");
                OnRoomCleared(false);
                break;
            case RoomType.EXIT:
                OnRoomCleared(false); // 출구 — 스테이지 클리어
                break;
            case RoomType.CAMP:
                HealParty();
                RpcNotice("ui.msg.room_camp", "전초기지 — 파티의 체력을 회복했습니다", "", "#2E8B57");
                OnRoomCleared(false);
                break;
            default: // EVENT_POSITIIVE / EVENT_NEGATIVE / ITEM_NPC / CARD_NPC — 콘텐츠 미구현 (Phase 6-5): 통과 처리
                RpcNotice("ui.msg.room_event_stub", "이벤트 (미구현) — 통과합니다", "", "#B8860B");
                OnRoomCleared(false);
                break;
        }
    }

    // 전초기지 방 — 파티 전원 체력 회복 (거점 치유와 같은 회복량)
    [Server]
    void HealParty()
    {
        foreach(PlayerInterface playerInterface in PlayerRegistry.All)
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers)
                if(gamePlayer != null) gamePlayer.HP = Mathf.Min(gamePlayer.HP + gamePlayer.recoveryValue, gamePlayer.MaxHP);
    }

    // 전투 승리 정리 시점(M_TurnManager.NoneBattleEnd)에 호출 — 스테이지 진행 중이면 방 클리어, 아니면 거점 복귀
    [Server]
    public void OnBattleVictory()
    {
        if(isInStage){
            OnRoomCleared(true);
        }else{
            GameSaveService.SaveGame();
            EnterHub(true);
        }
    }

    // 방 클리어 — 파티가 그 방으로 이동. 출구/보스 방이면 스테이지 클리어(다음 스테이지 해금) + 거점 복귀, 아니면 미로 화면으로
    [Server]
    void OnRoomCleared(bool fromBattle)
    {
        if(!IsValidRoom(battleRoomIndex)) return;
        StageRoomInfo room = stageRooms[battleRoomIndex];
        room.cleared = true;
        stageRooms[battleRoomIndex] = room;
        partyRoomIndex = battleRoomIndex;
        battleRoomIndex = -1;
        stageVersion++;

        bool stageCleared = room.RoomType == RoomType.BOSS || room.RoomType == RoomType.EXIT;
        if(stageCleared){
            StageData.Entry stage = StageData.Get(currentStageNo);
            OnStageCleared();
            EndStage();
            GameSaveService.SaveGame(); // 자동 저장 — 보상 + 스테이지 해금 반영
            RpcNotice("ui.msg.stage_clear", "{0} 클리어 — 거점으로 귀환합니다", stage != null ? stage.name : currentStageNo, "#E700FF");
            EnterHub(true);
        }else{
            GameSaveService.SaveGame(); // 방 단위 자동 저장 (보상 반영)
            if(fromBattle) M_TurnManager.instance.EnterStageView(); // 페이드 아웃 → 전투 오브젝트 정리 + 미로 화면 → 페이드 인 (끝나면 NONE_BATTLE_SCENE)
        }
    }

    // 가장 높은 해금 스테이지를 클리어하면 다음 스테이지 해금 + 전역 위험도 상승 (위험도 시스템)
    [Server]
    void OnStageCleared()
    {
        if(string.IsNullOrEmpty(currentStageNo)) return;
        int index = StageData.IndexOf(currentStageNo);
        if(index == unlockedStageCount - 1 && unlockedStageCount < StageData.Count)
            unlockedStageCount++;

        // 위험도 상승 — 일반(비보스) 스테이지 클리어 +1. 보스 스테이지는 보스 처치 시점에 +5 (M_TurnManager.ProcessMonsterDeathCoroutine)
        StageData.Entry stage = StageData.Get(currentStageNo);
        if(stage != null && !stage.IsBossStage)
            RaiseHazard(BalanceData.Get("HAZARD_RISE_STAGE_CLEAR", 1));
    }

    // ------------------------------------------------------------ 위험도 제어 (위험도 시스템 / 소피아) --------------------------------------- //

    // 전역 위험도 상승 — 일반 스테이지 클리어 +1 / 엘리트 처치 +3 / 보스 처치 +5 (BalanceDB, 소피아에게 골드를 주고 낮출 수 있음). 상한 없음
    [Server]
    public void RaiseHazard(int amount)
    {
        if(amount <= 0) return;
        hazardLevel += amount;
        RpcNotice("ui.msg.hazard_rise", "위험도가 {0}(으)로 상승했습니다", hazardLevel.ToString(), "#B22222");
    }

    /// <summary>스테이지의 유효 위험도 = 스테이지 기본 위험도 + 전역 위험도</summary>
    public int GetEffectiveHazard(StageData.Entry stage)
    {
        return Mathf.Max(0, (stage != null ? stage.hazard : 0) + hazardLevel);
    }

    /// <summary>현재 전역 위험도를 1 낮추는 비용 (BalanceDB — 기본비용 + 현재 위험도 x 레벨당 비용). 더 낮출 수 없으면 0</summary>
    public int GetHazardReduceCost()
    {
        if(hazardLevel <= 0) return 0;
        return BalanceData.Get("HAZARD_REDUCE_COST_BASE", 30) + hazardLevel * BalanceData.Get("HAZARD_REDUCE_COST_PER_LEVEL", 10);
    }

    // 디버그 — 전역 위험도 +1 (우상단 OnGUI 버튼, 테스트 전용. GamePlayer.SkillTree의 디버그 버튼이 호출)
    [Command(requiresAuthority = false)]
    public void CmdDebugRaiseHazard()
    {
        RaiseHazard(1);
    }

    // 소피아 "위험도 낮추기" — 요청한 플레이어의 골드를 지불하고 전역 위험도 1 하향 (거점에서만)
    [Command(requiresAuthority = false)]
    public void CmdReduceHazard(uint gamePlayerNetId)
    {
        ReduceHazard(gamePlayerNetId);
    }

    [Server]
    public void ReduceHazard(uint gamePlayerNetId)
    {
        if(!isInHub || hazardLevel <= 0 || M_TurnManager.instance.isSceneTransitioning || M_TurnManager.instance.phase != BattleTurn.NONE_BATTLE_SCENE) return;
        GamePlayer gamePlayer = NetLookup.Server<GamePlayer>(gamePlayerNetId);
        int cost = GetHazardReduceCost();
        if(gamePlayer == null || gamePlayer.gold < cost) return;
        gamePlayer.gold -= cost;
        hazardLevel--;
        GameSaveService.SaveGame();
        RpcNotice("ui.msg.hazard_reduced", "위험도를 {0}(으)로 낮췄습니다", hazardLevel.ToString(), "#2E8B57");
    }

    [Server]
    void EndStage()
    {
        isInStage = false;
        currentStageNo = "";
        stageRooms.Clear();
        partyRoomIndex = -1;
        battleRoomIndex = -1;
        stageVersion++;
    }

    // "귀환" — 미로 화면(전투 사이)에서 거점으로 이탈. 진행도는 버려지고 해금은 없다
    [Command(requiresAuthority = false)]
    public void CmdRetreat()
    {
        Retreat();
    }

    [Server]
    public void Retreat()
    {
        if(!isInStage || battleRoomIndex != -1 || M_TurnManager.instance.isSceneTransitioning || M_TurnManager.instance.phase != BattleTurn.NONE_BATTLE_SCENE) return;
        EndStage();
        GameSaveService.SaveGame();
        RpcNotice("ui.msg.stage_retreat", "거점으로 귀환합니다", "", "#4682B4");
        EnterHub(true);
    }

    // ------------------------------------------------------------ Server Method -------------------------------------------------------------- //

    // 거점 진입 — 로딩 완료 시(M_LoadingManager.HUB_SCENE, withTransition=false: 로딩 화면이 걷히면 바로 거점)와
    // 스테이지 종료/귀환 후(withTransition=true: 페이드로 거점 루트 전환) 호출
    [Server]
    public void EnterHub(bool withTransition = true)
    {
        isInHub = true;
        // 거점 회복(소피아 치유 — 자신의 캐릭터 즉시 회복) 횟수 제한 — 거점 방문마다 1회
        foreach(PlayerInterface playerInterface in PlayerRegistry.All)
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers)
                if(gamePlayer != null) gamePlayer.recoveryLimitCount = 1;
        Vector3[] npcPositions = new Vector3[HubNpcNames.Length];
        for(int i = 0; i < npcPositions.Length; i++) npcPositions[i] = GetNpcSpawnPosition(i);
        M_TurnManager.instance.GenerateHubObject(npcPositions, withTransition);
    }

    // ------------------------------------------------------------ ClientRpc Method ---------------------------------------------------------- //

    // (화면 전환 RPC는 M_TurnManager.Spawner의 전환 코루틴이 담당 — 페이드 아웃 → 정리/스폰 → 페이드 인)

    // 스테이지/방 진행 안내 토스트 — key 로컬라이즈(없으면 fallback), {0}에 arg 치환
    [ClientRpc]
    void RpcNotice(string key, string fallback, string arg, string hexColor)
    {
        string text = M_LanguageManager.Get(key, fallback).Replace("{0}", arg);
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Top)
            .FadeInTime(1f)
            .FadeOutTime(1f)
            .MessageBoxColor(ColorUtils.HexToColor(hexColor))
            .TextColor(Color.white)
            .Text(text)
            .Show();
    }

    // ------------------------------------------------------------ 임시 UI (스테이지 선택 / 위험도 패널) ------------------------------------------ //
    // 정식 출정/소피아 UI(팝업 프리팹) 전까지 OnGUI로 표시

    // 위험도 숫자 표시는 거점 우상단 HazardLayout(HubHazardUI), 위험도 하향은 소피아 "기도" 팝업(CampPopUp.prayLayout)이 담당
    void OnGUI()
    {
        if(!isInHub) return;
        if(M_TurnManager.instance == null || M_TurnManager.instance.phase != BattleTurn.NONE_BATTLE_SCENE) return;
        if(stageSelectOpen) DrawStageSelect();
    }

    // 출정 — 해금된 스테이지 버튼만 나열 (처음엔 1-1만). 유효 위험도(기본 + 전역)를 함께 표시
    void DrawStageSelect()
    {
        float width = 380f;
        float height = 70f + unlockedStageCount * 34f;
        Rect windowRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(windowRect, "출정 — 스테이지 선택");
        GUILayout.BeginArea(new Rect(windowRect.x + 10f, windowRect.y + 28f, width - 20f, height - 36f));
        for(int i = 0; i < unlockedStageCount && i < StageData.Count; i++)
        {
            StageData.Entry stage = StageData.Stages[i];
            string label = $"{stage.name}  (위험도 {GetEffectiveHazard(stage)}, 방 {stage.roomCount}개{(stage.eliteCount > 0 ? $", 엘리트 {stage.eliteCount}" : "")}{(stage.IsBossStage ? ", 보스" : ", 출구")})";
            if(GUILayout.Button(label, GUILayout.Height(30f)))
            {
                stageSelectOpen = false;
                CmdStartStage(stage.stageNo);
            }
        }
        if(GUILayout.Button("닫기", GUILayout.Height(26f))) stageSelectOpen = false;
        GUILayout.EndArea();
    }
}
