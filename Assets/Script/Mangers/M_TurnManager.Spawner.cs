using System.Collections;
using UnityEngine;
using Mirror;
using ProjectD;


// M_TurnManager partial — 화면 전환(거점/스테이지/전투) 오케스트레이션.
// 모든 전환은 서버 코루틴이 같은 순서로 진행한다: 페이드 아웃(M_DimmingManager) → 검은 화면 뒤에서 이전 오브젝트 정리 + 루트 전환 + 새 오브젝트 스폰 → 페이드 인.
// 스폰/파괴가 화면이 보이는 동안 일어나지 않으므로 과도현상(오브젝트 튀어나옴/사라짐)이 없다.
// 실제 스폰 로직은 BattleSpawner로 분리됨.
public partial class M_TurnManager
{
    const float FadeMargin = 0.15f;      // 페이드 완료 후 여유 (클라 트윈 완료 보장)
    const float SpawnSettleTime = 0.35f; // 스폰 메시지가 클라이언트에 반영될 시간

    public bool isSceneTransitioning { get; private set; } // 서버: 전환 코루틴 진행 중 (M_HubManager가 입력을 막는 데 사용)

    public int currentBattleHazard; // 서버: 이번 전투의 위험도 (= 전역 위험도 M_HubManager.hazardLevel) — RewardService가 보상 배율(BalanceDB HAZARD_REWARD_PERCENT_PER_LEVEL)에 사용

    Coroutine monsterDeathRoutine; // 클라이언트: 몬스터 사망 처리 코루틴 핸들 (전투마다 중복 시작 방지)

    // ------------------------------------------------------------ 전투 진입 ------------------------------------------------------------------ //

    // 스테이지 전투 진입 — roomType: MONSTER/ELITE/BOSS, hazard: 전역 위험도(몬스터 가중치/보상), monsterGroupName: StageDB에서 고른 MonsterGroupDB 그룹 (BOSS는 미사용)
    [Server]
    public void GenerateBattleObject(RoomType roomType, int hazard, string monsterGroupName)
    {
        StartCoroutine(EnterBattleSequence(roomType, hazard, monsterGroupName));
    }

    IEnumerator EnterBattleSequence(RoomType roomType, int hazard, string monsterGroupName)
    {
        isSceneTransitioning = true;
        M_DimmingManager.instance.RpcFadeOut();                                   // 1) 페이드 아웃
        yield return new WaitForSeconds(M_DimmingManager.FadeDuration + FadeMargin);

        ClearTargetObject();                                                      // 2) 검은 화면 뒤 — 거점/스테이지 잔여 오브젝트 정리, 전투 루트로 전환, 오브젝트 로딩
        battleExpPool = 0;                                                        //    이번 전투 처치 경험치 적립 시작
        currentBattleHazard = hazard;                                             //    이번 전투 위험도 기록 (보상 배율용)
        RpcSetView(HubView.Battle);
        BattleSpawner.instance.GeneratePlayerUnit();
        if(roomType == RoomType.BOSS) BattleSpawner.instance.GenerateBossMonster(hazard);
        else BattleSpawner.instance.GenerateMonster(hazard, monsterGroupName);
        RpcCardPrefareForBattle();
        SpawnAbilityCards();
        yield return new WaitForSeconds(SpawnSettleTime);

        if(roomType == RoomType.BOSS) RpcStartBossBattleEvent();                  // 3) 전투 시작 연출(토스트/BGM) + 페이드 인
        else RpcStartBattleEvent(roomType);
        RpcBattleReady();
        M_DimmingManager.instance.RpcFadeIn();
        isSceneTransitioning = false;
        StartCoroutine(WaitingForPlayer());
    }

    // ------------------------------------------------------------ 거점 진입 ------------------------------------------------------------------ //

    // 거점 진입 — 거점 NPC 4종만 스폰 (파티 아바타는 거점에 표시하지 않음). M_HubManager.EnterHub가 호출.
    // withTransition: 스테이지/전투에서 복귀할 때 페이드 — 로딩 직후 첫 진입은 거점 루트가 이미 켜져 있으므로 연출 없이 바로 시작
    [Server]
    public void GenerateHubObject(Vector3[] npcPositions, bool withTransition)
    {
        if(withTransition) StartCoroutine(EnterHubSequence(npcPositions));
        else
        {
            BattleSpawner.instance.GenerateHubNPCs(npcPositions);
            RpcSetView(HubView.Hub);
            RpcStartHubEvent();
            phase = BattleTurn.NONE_BATTLE_SCENE;
        }
    }

    IEnumerator EnterHubSequence(Vector3[] npcPositions)
    {
        isSceneTransitioning = true;
        M_DimmingManager.instance.RpcFadeOut();
        yield return new WaitForSeconds(M_DimmingManager.FadeDuration + FadeMargin);

        ClearTargetObject();                                                      // 전투 아바타/몬스터 정리 (검은 화면 뒤)
        RpcSetView(HubView.Hub);
        BattleSpawner.instance.GenerateHubNPCs(npcPositions);
        yield return new WaitForSeconds(SpawnSettleTime);

        RpcStartHubEvent();
        M_DimmingManager.instance.RpcFadeIn();
        isSceneTransitioning = false;
        phase = BattleTurn.NONE_BATTLE_SCENE;
    }

    // ------------------------------------------------------------ 스테이지(미로) 화면 진입 ----------------------------------------------------- //

    // 출정 직후(거점 NPC 정리) 와 방 전투 승리 후(전투 오브젝트 정리) 미로 화면으로
    [Server]
    public void EnterStageView()
    {
        StartCoroutine(EnterStageSequence());
    }

    IEnumerator EnterStageSequence()
    {
        isSceneTransitioning = true;
        M_DimmingManager.instance.RpcFadeOut();
        yield return new WaitForSeconds(M_DimmingManager.FadeDuration + FadeMargin);

        ClearTargetObject();
        RpcSetView(HubView.Stage);
        yield return new WaitForSeconds(FadeMargin);

        M_DimmingManager.instance.RpcFadeIn();
        isSceneTransitioning = false;
        phase = BattleTurn.NONE_BATTLE_SCENE;
    }

    // ------------------------------------------------------------ 공용 ----------------------------------------------------------------------- //

    // 어빌리티 카드 생성 (카드 시스템 잔재 — 2B-5 카드 제거 시 함께 정리)
    [Server]
    void SpawnAbilityCards()
    {
        foreach(GamePlayerDeck gamePlayerDeck in FindObjectsByType<GamePlayerDeck>(FindObjectsSortMode.None))
        {
            if(gamePlayerDeck.abilityCard == null)gamePlayerDeck.SpawnAbilityCardRPC();
        }
    }

    // 모든 플레이어의 타겟오브젝트 초기화가 끝나면 전투 페이즈 진입
    IEnumerator WaitingForPlayer()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        int cnt = 0;
        while(true)
        {
            cnt = 0;
            yield return new WaitForSeconds(0.1f);
            foreach(PlayerInterface user in PlayerRegistry.All)
                if(user.isTargetObjectInitDone) cnt++;
            if(cnt != netManager.roomSlots.Count) continue;

            phase = BattleTurn.BATTLE_INITIALIZE;
            break;
        }
        ClearTargetObjectInitFlag();
    }

    [ClientRpc]
    void ClearTargetObjectInitFlag()
    {
        NetworkClient.connection.identity.GetComponent<PlayerInterface>().isTargetObjectInitDone = false;
    }

    // ------------------------------------------------------------ ClientRpc Method -------------------------------------------------------------- //

    // 화면 루트 전환 (검은 화면 뒤에서 호출됨)
    [ClientRpc]
    void RpcSetView(HubView view)
    {
        M_HubManager.instance.SetView(view);
    }

    // 전투 오브젝트 스폰 완료 — 타겟오브젝트 초기화 대기 + 몬스터 사망 처리 코루틴 시작
    [ClientRpc]
    void RpcBattleReady()
    {
        StartCoroutine(CheckTargetObject());
        if(monsterDeathRoutine == null)
            monsterDeathRoutine = StartCoroutine(ProcessMonsterDeathCoroutine());
    }

    // 자신의 타겟오브젝트가 스폰되면 초기화 완료 플래그를 올린다 (서버의 WaitingForPlayer가 집계)
    IEnumerator CheckTargetObject()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.1f);
            PlayerInterface local = NetworkClient.connection.identity.GetComponent<PlayerInterface>();
            GamePlayer gamePlayer = local.currentGamePlayer;
            if(gamePlayer != null && gamePlayer.GetComponent<GamePlayerTarget>().targetObject != 0)
            {
                local.isTargetObjectInitDone = true;
                break;
            }
        }
    }
}
