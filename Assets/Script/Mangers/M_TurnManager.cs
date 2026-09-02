using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


// 전투 상태 머신 소유자 — 페이즈(BattleTurn), 대열(playerOrder), 스폰 목록, 몬스터 사망 처리, 파티 공용 아티팩트.
// 실제 전투 진행은 TP 턴제 루프(M_TurnManager.TpBattle.cs). 구 카드 전투 페이즈 머신(드로우/카드 큐/턴 종료 효과)은 카드 시스템 제거와 함께 삭제됨 (2026-09-01).
public partial class M_TurnManager : NetworkSingletonD<M_TurnManager>
{
    // Turn 관리는 서버
    [SyncVar]
    public BattleTurn Phase;
    public BattleTurn phase {get{
        return Phase;
    }
    set{
        Phase = value;
        OnChangedPhase();
    }}


    // 서버에서 관리할 PlayerOrder SyncList : 요소값이 0인 인덱스는 빈 슬롯을 의미. 플레이어들이 추가될 때 0인 인덱스의 값을 제거하고 해당 플레이어의 netId를 추가
    public readonly SyncList<uint> playerOrder = new SyncList<uint>(){ 0, 0, 0 };

    // 각 클라이언트에서 참조할 현재 참가한 플레이어들의 타겟오브젝트 목록
    public readonly SyncList<uint> spawnedPlayerSyncList = new SyncList<uint>();

    // 각 클라이언트에서 참조할 현재 전투에 생성된 몬스터들의 타겟오브젝트 목록
    public readonly SyncList<uint> spawnedMonsterSyncList = new SyncList<uint>();

    // 파티 공용 아티팩트 목록 — 지역거점 클리어 보상으로 획득하며, 발동 시점마다 모든 플레이어에게 효과가 적용된다
    public readonly SyncList<Item> teamArtifacts = new SyncList<Item>();

    public Vector3[] targetObjectPosition = {
        new Vector3(-15,-3,0),
        new Vector3(-11,-3,0),
        new Vector3(-7,-3,0),
        new Vector3(7,-3,0),
        new Vector3(11,-3,0),
        new Vector3(15,-3,0)
    };


    public List<TargetObject> spawnedPlayerList = new List<TargetObject>();
    public List<TargetObject> spawnedMonsterList = new List<TargetObject>();

    public bool monsterDeathOperating = false;
    public bool monsterShieldInitialize = false; // 몬스터 실드 일괄 초기화 중 — TargetObject.OnChangedDefense가 실드 파괴 연출을 생략하는 데 사용 (TP 전투는 턴 시작에 개별 리셋)
    public List<TargetObject> dyingMonsers = new List<TargetObject>();


    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
    }

    public override void OnStartClient()
    {
        playerOrder.Callback += OnPlayerOrderUpdated;
        spawnedPlayerSyncList.Callback += OnChangeSpawnedPlayerUpdated;
        spawnedMonsterSyncList.Callback += OnChangeSpawnedMonsterUpdated;
    }

    // -------------------------------------------------------------------- Normal Method ---------------------------------------------------------------------//

    public List<TargetObject> GetPlayerObjects()
    {
        return spawnedPlayerList;
    }

    public List<TargetObject> GetMonsterObjects()
    {
        return spawnedMonsterList;
    }

    // 현재 플레이어의 TargetObject를 반환
    public TargetObject GetCurrentPlayerTargetObject(GamePlayer gamePlayer)
    {
        if(NetworkServer.activeHost){
            // spawnedPlayerSyncList(타겟오브젝트 리스트)에서 현재 플레이어의 참조값을 가진 타겟오브젝트의 netId 조회
            uint targetObjectNetId = M_TurnManager.instance.spawnedPlayerSyncList.Find(gemePlayerNetId => {
                if(gemePlayerNetId != 0){
                    TargetObject spawnedTarget = NetLookup.Server<TargetObject>(gemePlayerNetId);
                    return spawnedTarget != null && spawnedTarget.player == gamePlayer;
                }
                return false;
            });
            if(targetObjectNetId != 0){
                return NetLookup.Server<TargetObject>(targetObjectNetId); // 조회된 netId로 타겟오브젝트 반환
            }
        }else{
            uint targetObjectNetId = M_TurnManager.instance.spawnedPlayerSyncList.Find(gemePlayerNetId => {
                if(gemePlayerNetId != 0){
                    TargetObject spawnedTarget = NetLookup.Client<TargetObject>(gemePlayerNetId);
                    return spawnedTarget != null && spawnedTarget.player == gamePlayer;
                }
                return false;
            });
            if(targetObjectNetId != 0){
                return NetLookup.Client<TargetObject>(targetObjectNetId);
            }
        }
        return null;
    }

    // -------------------------------------------------------------------- Server Method ---------------------------------------------------------------------//

    // 공용 아티팩트 지급 (서버) — 지역거점 클리어 보상 지점에서 호출. ONCEGET 효과는 즉시 모든 플레이어에게 발동한다.
    [Server]
    public void AddTeamArtifact(Item artifact)
    {
        teamArtifacts.Add(artifact);
        if(artifact.effectTime != ItemEffectTime.ONCEGET) return;
        foreach(PlayerInterface playerInterface in PlayerRegistry.All)
        {
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers)
            {
                GamePlayerItem gamePlayerItem = gamePlayer.GetComponent<GamePlayerItem>();
                gamePlayerItem.InvokeSingleEffect(artifact, gamePlayer.GetComponent<GamePlayerTarget>().GetTargetObject());
            }
        }
    }

    // 플레이어 오더 스왑 — TP 행동 '이동'(전투)과 거점 배너 교환(CmdSwapPlayerOrderInHub)이 사용
    [Server]
    public void SwapPlayerOrder(int oldIndex, int newIndex)
    {
        uint temp = playerOrder[oldIndex];
        playerOrder[oldIndex] = playerOrder[newIndex];
        playerOrder[newIndex] = temp;
    }

    // 거점 대열 교환 — 좌상단 오더 배너(PlayerOrder) 클릭: 활성 배너와 다른 배너를 클릭하면 두 캐릭터의 위치를 서로 바꾼다 (2026-09-01).
    // 두 GamePlayer가 모두 요청자 소유(싱글 3인 파티)일 때만, 거점(비전투)에서만 — 전투 중 대열 이동은 TP 행동 '이동'이 담당
    [Command(requiresAuthority = false)]
    public void CmdSwapPlayerOrderInHub(uint netIdA, uint netIdB, NetworkConnectionToClient sender = null)
    {
        if(phase != BattleTurn.NONE_BATTLE_SCENE || isSceneTransitioning) return;
        if(M_HubManager.instance == null || !M_HubManager.instance.isInHub) return;
        GamePlayer playerA = NetLookup.Server<GamePlayer>(netIdA);
        GamePlayer playerB = NetLookup.Server<GamePlayer>(netIdB);
        if(playerA == null || playerB == null || playerA == playerB) return;
        if(sender != null && (playerA.connectionToClient != sender || playerB.connectionToClient != sender)) return;
        int indexA = playerOrder.IndexOf(netIdA);
        int indexB = playerOrder.IndexOf(netIdB);
        if(indexA < 0 || indexB < 0) return;
        SwapPlayerOrder(indexA, indexB);
    }

    [Server]
    public void OnChangedPhase()
    {
        Debug.Log("Phase is " + phase);
        RpcChangePhase(phase);
        switch(phase)
        {
            case BattleTurn.NONE_BATTLE_SCENE :
                break;
            case BattleTurn.BATTLE_INITIALIZE :
                BattleInitialize();
                break;
            case BattleTurn.BATTLE_END :
                BattleEnd();
                break;
            case BattleTurn.NONE_BATTLE_END :
                NoneBattleEnd();
                break;
        }
    }

    // ---------------- 전원 상태 집계 판정 (PlayerInterface SyncVar 훅에서 알림 수신) ----------------

    // 모든 플레이어가 보상을 받았으면 비전투 종료 처리
    [Server]
    public void CheckAllPlayersRewardDone()
    {
        foreach(PlayerInterface player in PlayerRegistry.All)
        {
            if(!player.isRewardDone) return;
        }
        foreach(PlayerInterface player in PlayerRegistry.All) player.SetCompleteRewardStateDefault();
        NoneBattleEnd();
    }

    // 플레이어 오더 슬롯 등록 — 게임플레이어 소유 오브젝트 생성 시(PlayerInterfaceServer) 룸에서 정한 오더 인덱스에 netId를 기록
    [Server]
    public void RegisterPlayerOrder(int index, uint gamePlayerNetId)
    {
        if(index < 0 || index >= playerOrder.Count) return;
        // RemoveAt+Insert: OP_SET 콜백(오더 스왑 연출/인디케이터 갱신)을 등록 시점에 태우지 않기 위해 기존 방식 유지
        playerOrder.RemoveAt(index);
        playerOrder.Insert(index, gamePlayerNetId);
    }

    [Server]
    public void OnChangedMonsterList()
    {
        if(spawnedMonsterList.Count == 0)
            phase = BattleTurn.BATTLE_END;
    }

    [Server]
    public void ClearTargetObject()
    {
        ClearTargetObjectList(spawnedMonsterList);
        ClearTargetObjectList(spawnedPlayerList);
        TargetIndicatorController.instance.ClearTargetIndicators();
        spawnedPlayerSyncList.Clear();
        spawnedMonsterSyncList.Clear();
    }

    private void ClearTargetObjectList(List<TargetObject> targets)
    {
        for(int i = targets.Count - 1 ; i >=0 ; i--)
        {
            TargetObject removeItem = targets[i];
            targets.Remove(removeItem);
            NetworkServer.Destroy(removeItem.gameObject);
        }
    }

    public int battleExpPool = 0; // 이번 전투에서 처치한 몬스터의 경험치 합 (서버 전용) — 전투 시작 시 0, 종료 시 RewardService가 소비

    // 전투 종료 보상용 경험치 인출 (처치 몬스터 경험치 합, 0이면 BalanceDB 폴백은 호출부에서)
    [Server]
    public int ConsumeBattleExp()
    {
        int exp = battleExpPool;
        battleExpPool = 0;
        return exp;
    }

    public int eliteKillCountOnGame = 0; // 이번 게임 동안 처치한 엘리트 수 (서버 전용)

    public int bossKillCountOnGame = 0; // 이번 게임 동안 처치한 보스 수 (서버 전용)

    public void ProcessMonsterDeath(TargetObject tar)
    {
        if(!dyingMonsers.Exists(x => x == tar))dyingMonsers.Add(tar);
    }

    public IEnumerator ProcessMonsterDeathCoroutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);
            if(!monsterDeathOperating)continue;
            foreach(TargetObject monster in dyingMonsers)
                if(monster.gameObject.activeSelf)monster.gameObject.SetActive(false);//우선 사망한 적 비활성화

            foreach(TargetObject monster in dyingMonsers) // 사망 몬스터 순차 처리
            {
                foreach(TargetObject target in spawnedPlayerList) // 철귀가 붙은 몬스터일경우 철귀 복귀
                {
                    if(target.player.character == Character.HONGDANHYANG)
                        if(target.ironDemonLocation == monster )
                        {
                            target.ironDemonLocation = target;
                            StartCoroutine(IronDemonReturnProcess(target));
                        }
                }
                // 엘리트/보스 처치 집계 (게임 지속) + 전역 위험도 상승 (위험도 시스템)
                if(monster.monster != null && monster.monster.monsterGrade != MonsterGrade.NORMAL)
                {
                    if(monster.monster.monsterGrade == MonsterGrade.ELITE)
                    {
                        eliteKillCountOnGame++;
                        M_HubManager.instance.RaiseHazard(BalanceData.Get("HAZARD_RISE_ELITE_KILL", 3));
                    }
                    else
                    {
                        bossKillCountOnGame++;
                        M_HubManager.instance.RaiseHazard(BalanceData.Get("HAZARD_RISE_BOSS_KILL", 5));
                    }
                }
                // 처치 경험치 적립 (MonsterStatDB Exp) — 전투 종료 시 파티 전원에게 합산 지급
                if(monster.monster != null && monster.monster.monster != null)
                    battleExpPool += monster.monster.monster.exp;
                // 실제 오브젙트 삭제 과정
                spawnedMonsterList.Remove(monster);
                spawnedMonsterSyncList.Remove(monster.netId);
                NetworkServer.Destroy(monster.gameObject);
                OnChangedMonsterList();
            }
            dyingMonsers.Clear();
            monsterDeathOperating = false;
        }
    }

    // 전투 시작 — 시작 시점 아이템/아티팩트 효과 발동 후 TP 턴제 루프 진입 (M_TurnManager.TpBattle.cs)
    public void BattleInitialize()
    {
        foreach(TargetObject player in spawnedPlayerList)
        {
            // 전투 시작 시점 효과 발동 — 개인 아이템은 소유자에게, 공용 아티팩트는 모든 플레이어에게
            GamePlayerItem gamePlayerItem = player.player.GetComponent<GamePlayerItem>();
            gamePlayerItem.InvokeItemEffects(ItemEffectTime.STARTBATTLE, player);
            gamePlayerItem.InvokeEffects(teamArtifacts, ItemEffectTime.STARTBATTLE, player);
        }
        StartTpBattle();
    }

    // 전투/거점 진입 대기(WaitingForPlayer)와 진입 연출 RPC는 M_TurnManager.Spawner.cs 참조

    public TargetObject[] GetTargetObjectFromActionTarget(ActionTarget target)
    {
        if(target == ActionTarget.FIXEDPLAYER || target == ActionTarget.RANDOM || target == ActionTarget.NONE){
            Debug.Log("ERROR : Next Target Error");
        }
        List<TargetObject> retVal = new List<TargetObject>();
        // playerOrder의 netId가 스폰 목록에서 사라진 타이밍(사망/접속해제)에는 해당 타겟을 건너뛴다
        void AddIfSpawned(uint netId)
        {
            GamePlayerTarget gamePlayerTarget = NetLookup.Server<GamePlayerTarget>(netId);
            TargetObject targetObject = gamePlayerTarget != null ? gamePlayerTarget.GetTargetObject() : null;
            if(targetObject != null) retVal.Add(targetObject);
        }
        switch(target)
        {
            case ActionTarget.FRONT :
                if(M_TurnManager.instance.playerOrder[2] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[2]);
                else retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.MIDDLE :
                if(M_TurnManager.instance.playerOrder[1] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[1]);
                else retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.BACK :
                if(M_TurnManager.instance.playerOrder[0] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[0]);
                else retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.FRONT_BACK :
                if(M_TurnManager.instance.playerOrder[2] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[2]);
                if(M_TurnManager.instance.playerOrder[0] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[0]);
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.FRONT_MIDDLE :
                if(M_TurnManager.instance.playerOrder[2] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[2]);
                if(M_TurnManager.instance.playerOrder[1] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[1]);
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.MIDDLE_BACK :
                if(M_TurnManager.instance.playerOrder[1] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[1]);
                if(M_TurnManager.instance.playerOrder[0] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[0]);
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.WHOLE :
                if(M_TurnManager.instance.playerOrder[0] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[0]);
                if(M_TurnManager.instance.playerOrder[2] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[2]);
                if(M_TurnManager.instance.playerOrder[1] != 0) AddIfSpawned(M_TurnManager.instance.playerOrder[1]);
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
        }

        // 지정 타겟이 전부 무효(스폰 해제 등)면 전체 플레이어로 폴백
        if(retVal.Count == 0)
            retVal.AddRange(spawnedPlayerList);

        return retVal.ToArray();
    }


    public List<TargetObject> GetTargetObjectFromActionTargetList(ActionTarget target)
    {
        if(target == ActionTarget.FIXEDPLAYER || target == ActionTarget.RANDOM || target == ActionTarget.NONE){
            Debug.Log("ERROR : Next Target Error");
        }
        List<TargetObject> retVal = new List<TargetObject>();
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if( target == ActionTarget.WHOLE ||
                (target == ActionTarget.FRONT && tar.player.selectOrder == 2) ||
                (target == ActionTarget.MIDDLE && tar.player.selectOrder == 1) ||
                (target == ActionTarget.BACK && tar.player.selectOrder == 0) ||
                (target == ActionTarget.FRONT_MIDDLE && tar.player.selectOrder != 0) ||
                (target == ActionTarget.MIDDLE_BACK && tar.player.selectOrder != 1) ||
                (target == ActionTarget.FRONT_BACK && tar.player.selectOrder != 2) )
                retVal.Add(tar);
        }
        if(retVal.Count == 0)
            retVal.AddRange(spawnedPlayerList);

        return retVal;
    }

    // Synclist에서 오더 인덱스 변경 이벤트 수신하여 GamePlayer의 selectOrder Syncvar값을 변경
    public void SetGamePlayerOrder(uint gamePlayerNetId, int index)
    {
        if(isServer){
            if(NetworkServer.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.selectOrder = index;
                gamePlayer.OnChangedSelectOrder(index, index);
                gamePlayer.objectOwner.selectOrder = index;
            }
        }else{
            if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.selectOrder = index;
                gamePlayer.OnChangedSelectOrder(index, index);
                gamePlayer.objectOwner.selectOrder = index;
            }
        }
    }


    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//

    private void OnPlayerOrderUpdated(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_SET:
                SetGamePlayerOrder(newVal, index);
                TargetIndicatorController.instance.SetTargetIndicatorOrder(newVal, index);
                break;
        }
    }

    private void OnChangeSpawnedPlayerUpdated(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
                if(newVal == 0){
                    TargetIndicatorController.instance.CreateIndicator(0, targetObjectPosition[index]);
                }else{
                    TargetObject targetObject = isServer ? NetLookup.Server<TargetObject>(newVal) :  NetLookup.Client<TargetObject>(newVal);
                    // SyncList 델타가 스폰 메시지보다 먼저 도착한 경우 타겟오브젝트가 아직 없을 수 있으므로 슬롯 위치로 폴백
                    Vector3 indicatorPosition = targetObject != null ? targetObject.transform.position : targetObjectPosition[index];
                    TargetIndicatorController.instance.CreateIndicator(newVal, indicatorPosition);
                }
                break;
        }
    }

    private void OnChangeSpawnedMonsterUpdated(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
                TargetObject targetObject = isServer ? NetLookup.Server<TargetObject>(newVal) :  NetLookup.Client<TargetObject>(newVal);
                // SyncList 델타가 스폰 메시지보다 먼저 도착한 경우 타겟오브젙트가 아직 없을 수 있음 — 인디케이터는 생성하고 위치는 이후 갱신에 맡긴다
                Vector3 monsterIndicatorPosition = targetObject != null ? targetObject.transform.position : Vector3.zero;
                TargetIndicatorController.instance.CreateIndicator(newVal, monsterIndicatorPosition);
                break;
        }
    }
}
