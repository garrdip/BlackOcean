using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Spine.Unity;
using Spine.Unity.Examples;
using Gpm.Ui;
using AYellowpaper.SerializedCollections;
using System.Linq;


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

    // 카드 큐 데이터 저장할 Synclist
    public readonly SyncList<CardQueue> cardQueueList = new SyncList<CardQueue>();

    [Header("카드 큐")]
    public System.Action<int> onCurrentCardQueueUpdated; // 현재 카드 큐 변경 이벤트
    public int currentCardQueueIndex; // 현재 카드 큐 인덱스
    private const int currentCardQueueInitalValue = -1; // 현재 카드 큐 인덱스 초기값 (리스트 인덱스와 맞추기 위해 초기값 -1)
    public enum INDEX_OPERATION {
        INCREASE,
        DECREASE
    }

    public Vector3[] targetObjectPosition = {
        new Vector3(-15,-3,0),
        new Vector3(-11,-3,0),
        new Vector3(-7,-3,0),
        new Vector3(7,-3,0),
        new Vector3(11,-3,0),
        new Vector3(15,-3,0)
    };

    public bool isCardQueueOperating = false;


    public List<TargetObject> spawnedPlayerList = new List<TargetObject>();
    public List<TargetObject> spawnedMonsterList = new List<TargetObject>();
    List<TargetObject> monsterOrderList = new List<TargetObject>();
    
    public bool monsterDeathOperating = false;
    public bool preEffcetOperating = false;
    public bool monsterShieldInitialize = false;
    public List<TargetObject> dyingMonsers = new List<TargetObject>();

    // 카드와 타겟을 한쌍으로 저장하는 큐
    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */
    public Queue<(GamePlayerDeck, int , CardOnHand, List<TargetObject>)> cardTargetPairQueue = new Queue<(GamePlayerDeck, int, CardOnHand, List<TargetObject>)>();


    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentCardQueueIndex = currentCardQueueInitalValue;
        StartCoroutine(ProcessCardQueue());
    }

    public override void OnStartClient()
    {
        playerOrder.Callback += OnPlayerOrderUpdated;
        cardQueueList.Callback += OnCardQueueUpdated;
        spawnedPlayerSyncList.Callback += OnChangeSpawnedPlayerUpdated;
        spawnedMonsterSyncList.Callback += OnChangeSpawnedMonsterUpdated;
    }

    // -------------------------------------------------------------------- Normal Method ---------------------------------------------------------------------//

    public TargetObject GetPlayer(GamePlayerDeck conn)
    {     
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.GetComponent<GamePlayerDeck>() == conn){
                return tar;
            }
        }
        return null;
    }

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

    // 현재 페이즈가 PLAYER_ACTIVE 상태인지 체크
    public bool IsActivePhase()
    {
        return phase == BattleTurn.PLAYER_ACTIVE ? true : false;
    }

    // -------------------------------------------------------------------- Server Method ---------------------------------------------------------------------//

    // 플레이어 오더 스왑
    [Server]
    public void SwapPlayerOrder(int oldIndex, int newIndex)
    {
        if(M_TurnManager.instance.phase == BattleTurn.PLAYER_ACTIVE) // 노병의 지혜
        {
            if(NetworkServer.spawned.ContainsKey(playerOrder[oldIndex]))
            if(NetLookup.Server<GamePlayerTarget>(playerOrder[oldIndex]).GetTargetObject().HasBuff(BuffType.WISDOMOFOLDSOLDIER))
                foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
                    CardData.instance.GeneralGetDefense(NetLookup.Server<GamePlayerTarget>(playerOrder[oldIndex]).GetTargetObject(),target,5,null);
            if(NetworkServer.spawned.ContainsKey(playerOrder[newIndex]))
            if(NetLookup.Server<GamePlayerTarget>(playerOrder[newIndex]).GetTargetObject().HasBuff(BuffType.WISDOMOFOLDSOLDIER))
                foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
                    CardData.instance.GeneralGetDefense(NetLookup.Server<GamePlayerTarget>(playerOrder[newIndex]).GetTargetObject(),target,5,null);          
        }
        uint temp = playerOrder[oldIndex];
        playerOrder[oldIndex] = playerOrder[newIndex];
        playerOrder[newIndex] = temp;
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
            case BattleTurn.BATTLE_STANDBY :
                BattleStandby();
                break;
            case BattleTurn.PLAYER_PREEFFECT :
                StartCoroutine(PlayerPreEffect());
                break;
            case BattleTurn.PLAYER_DRAW :
                StartCoroutine(PlayerCardDraw());
                break;
            case BattleTurn.PLAYER_ACTIVE :
                break;
            case BattleTurn.PLAYER_ACTIVE_DONE :
                StartWaitCardQueue();
                break;
            case BattleTurn.PLAYER_END_TURN_EFFECT :
                StartCoroutine(PlayerEndTurnEffect());
                break;
            case BattleTurn.PLAYER_END :
                PlayerEndTurn();
                break;
            case BattleTurn.MONSTER_ORDERSELECT :
                PlayerCardThrowAwaySetDefault();
                MonsterSetOrder();
                phase = BattleTurn.MONSTER_PREEFFECT;
                break;
            case BattleTurn.MONSTER_PREEFFECT :
                StartCoroutine(MonsterPreEffect());
                break;
            case BattleTurn.MONSTER_ACTIVE :
                MonsterActive();
                break;
            case BattleTurn.BATTLE_END :
                BattleEnd();
                break;
            case BattleTurn.NONE_BATTLE_END :
                NoneBattleEnd();
                break;
        }
    }

    [Server]
    void PlayerCardThrowAwaySetDefault()
    {
        foreach(PlayerInterface pi in PlayerRegistry.All)
            pi.SetDefaultStateofCardThrowDone();
    }

    [Server]
    IEnumerator MonsterPreEffect()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.01f);
        // 몬스터 방어도 초기화
        preEffcetOperating =true;
        yield return DebuffPreEffect();
        preEffcetOperating =false;
        while(monsterDeathOperating)
            yield return loopTime;
        monsterShieldInitialize = true;
        foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList)
        {
            tar.defense = 0;
        }
        monsterShieldInitialize = false;
        phase = BattleTurn.MONSTER_ACTIVE;
    }

    IEnumerator DebuffPreEffect()
    {
        foreach(TargetObject tar in spawnedMonsterList)
        {
            List<int> currentKeys = tar.buffTrunBeginEffect.Keys.ToList();
            foreach(int buffIndex in currentKeys)
            { 
                yield return tar.buffTrunBeginEffect[buffIndex](tar,buffIndex,null);
            }
        }
    }

    IEnumerator IronDemonPreEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.character == Character.HONGDANHYANG)
            {
                while(true)
                {
                    yield return new WaitForSeconds(0.01f);
                    if(monsterDeathOperating) continue;
                    break;
                }
                if(tar.ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
                {
                    AnimIronDemon("Buff0",tar);
                    tar.ironDemonLocation.defense += tar.GetBuffValue(BuffType.IRONDEMON);
                    yield return new WaitForSeconds(1.33f);
                }
                else // 몬스터의 경우 데미지
                {
                    while(true)
                    {
                        yield return new WaitForSeconds(0.01f);
                        if(monsterDeathOperating) continue;
                        break;
                    }
                    if(Random.Range(0,2) == 0)AnimIronDemon("Attack0",tar);
                    else AnimIronDemon("Attack1",tar);
                    yield return new WaitForSeconds(0.4f);
                    StartCoroutine(tar.ironDemonLocation.monster.OnHitAnimation()); // 실제 피격 애니메이션
                    tar.ironDemonLocation.DamageToMonster(tar.GetBuffValue(BuffType.IRONDEMON), tar);
                    yield return new WaitForSeconds(0.6f);
                }
                AnimIronDemon("Idle",tar);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    [Server]
    public IEnumerator PlayerCardDraw()
    {
        foreach(uint netId in playerOrder){
            if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer player = networkIdentity.GetComponent<GamePlayer>();
                player.GetComponent<GamePlayerDeck>().currentIchi = player.GetComponent<GamePlayerDeck>().maxIchi; 
            }
        }
        foreach(TargetObject tar in spawnedPlayerList) // 고행2 카드를 이미 가지고 있으면 쇠락 부여 
        {
            if(tar.player.GetComponent<GamePlayerDeck>().cardOnHands.FindIndex(cardOnhand => cardOnhand.card.baseCard.cardNumber ==  "G1") != -1)
                tar.GainBuff(BuffType.SOIRAK,1,true,false,true,false,tar,null);
        }
        EachPlayerCardDraw();
        foreach(TargetObject tar in spawnedPlayerList)
        {
            foreach(int buffIndex in tar.buffCardDrowEffect.Keys)
            { 
                yield return tar.buffCardDrowEffect[buffIndex](tar,buffIndex,null);
            }
        }
        phase = BattleTurn.PLAYER_ACTIVE;
    }

    [Server]
    public IEnumerator PlayerPreEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList) 
        {
            tar.defense = 0;
            tar.player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth = 0;
            List<int> currentKeys = tar.buffTrunBeginEffect.Keys.ToList();
            foreach(int buffIndex in currentKeys)
            { 
                yield return tar.buffTrunBeginEffect[buffIndex](tar,buffIndex,null);
            }   
            int indexOfOldItem = tar.buffs.Count;
            for(int i = indexOfOldItem -1 ; i >= 0 ; i--)
            {
                if(tar.buffs[i].type == BuffType.FLOWER)
                {
                    tar.buffs.RemoveAt(i);
                    continue;
                }
                if(tar.buffs[i].isDecrease)
                {
                    Buff modItem = new Buff(tar.buffs[i]);
                    modItem.value -= 1;
                    if(modItem.value == 0)
                        tar.buffs.RemoveAt(i);
                    else
                        tar.buffs[i] = modItem;
                }
            }
        }
        foreach(TargetObject tar in spawnedMonsterList) // 몬스터 디버프 스택 감소
        {
            int indexOfOldItem = tar.buffs.Count;
            for(int i = indexOfOldItem -1 ; i >= 0 ; i--)
            {
                if(tar.buffs[i].type == BuffType.FLOWER)
                {
                    tar.buffs.RemoveAt(i);
                    continue;
                }
                if(tar.buffs[i].isDecrease)
                {
                    Buff modItem = new Buff(tar.buffs[i]);
                    modItem.value -= 1;
                    if(modItem.value == 0)
                        tar.buffs.RemoveAt(i);
                    else
                        tar.buffs[i] = modItem;
                }
            }
        }
        phase = BattleTurn.PLAYER_DRAW;
        yield return null;
    }

    
    [Server]
    public void PlayerEndTurn()
    {
        ResetEndTurnState();
        EachPlayerEndTurn();
    }

    [Server]
    public void ResetEndTurnState()
    {
        foreach(TargetObject user in spawnedPlayerList)
        {
            user.player.objectOwner.SetEndTurnActiveStateDefault();
            user.usingGOHENG = false; // 고행 사용 초기화
        }
    }

    // ---------------- 전원 상태 집계 판정 (PlayerInterface SyncVar 훅에서 알림 수신) ----------------
    // 흐름 전이 판정을 상태머신 소유자인 이곳에 모으고, 씬 전수 스캔 대신 PlayerRegistry를 사용한다.

    // 모든 플레이어가 턴 종료했으면 다음 페이즈로 전이
    [Server]
    public void CheckAllPlayersEndTurn()
    {
        foreach(PlayerInterface user in PlayerRegistry.All)
        {
            GamePlayer gamePlayer = user.currentGamePlayer; // 스폰 타이밍에 따라 null 가능
            if(!user.endTurnActive && gamePlayer != null && gamePlayer.HP > 0)return;
        }
        switch(phase)
        {
            case BattleTurn.PLAYER_ACTIVE :
                phase = BattleTurn.PLAYER_ACTIVE_DONE;
                break;
            case BattleTurn.NONE_BATTLE_SCENE :
                phase = BattleTurn.NONE_BATTLE_END;
                break;
        }
    }

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

    // 모든 플레이어가 레디 상태면 투표 결과 방으로 이동
    [Server]
    public void CheckAllPlayersReadyForMapMove()
    {
        foreach(PlayerInterface player in PlayerRegistry.All)
        {
            if(!player.isReady) return;
        }
        // 플레이어들이 투표한 결과 선택된 맵 위치로 이동
        HexagonMapRoom hexagonMapRoom = M_MapManager.instance.GetVoteHexagonMapRoomResult();
        if(hexagonMapRoom != null){
            if(hexagonMapRoom == M_MapManager.instance.currentRoom){
                if(hexagonMapRoom.roomType == RoomType.BOSS || hexagonMapRoom.roomType == RoomType.RUINS){
                    EnterTheRoom(hexagonMapRoom); // 보스방은 현재 위치한 방이어도 방 진입
                }
            }else{
                EnterTheRoom(hexagonMapRoom);
            }
        }
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

            if(CardData.instance.isCardOperating || preEffcetOperating)
            {   
                foreach(TargetObject monster in dyingMonsers)
                    if(monster.isActiveAndEnabled)monster.gameObject.SetActive(false);
                continue; // 카드 사용이 끝날때까지 기다림
            }

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
                // 실제 오브젝트 삭제 과정
                spawnedMonsterList.Remove(monster);
                spawnedMonsterSyncList.Remove(monster.netId);
                NetworkServer.Destroy(monster.gameObject);
                OnChangedMonsterList();
            }
            dyingMonsers.Clear();
            monsterDeathOperating = false;
        }
    }

    [Server]
    public void BattleStandby()
    {
        foreach(TargetObject monster in spawnedMonsterList)
        {
            monster.monster.SetNextAction();
        }
        phase = BattleTurn.PLAYER_PREEFFECT;
    }

    public void BattleInitialize()
    {
        foreach(TargetObject player in spawnedPlayerList)
        {
            if(player.player.character == Character.HONGDANHYANG)
            {
                player.GainBuff(BuffType.IRONDEMON, 4 + player.player.GetComponent<GamePlayerDeck>().AdditionalSizeOfIromDemon, false, false, false, false, player, null);
            }
        }
        phase = BattleTurn.BATTLE_STANDBY;
    }

    [Server]
    public void MonsterActive()
    {
        StartCoroutine(MonsterActionSeuqence());
    }

    IEnumerator MonsterActionSeuqence()
    {
        WaitForSeconds loopWait = new WaitForSeconds(0.01f);
        for(int i=0; i<spawnedMonsterList.Count; i++)
        {
            TargetObject target = spawnedMonsterList[i];
            target.monster.isActive = true;
            if(!target.isDying){
                StartCoroutine(target.monster.DoAction());
                while(true)
                {
                    if(target.monster.isActive == false) break;
                    yield return loopWait;
                }
            }
        }
        phase = BattleTurn.BATTLE_STANDBY;
    }

    [Server]
    public void MonsterSetOrder()
    {
        monsterOrderList.Clear();
        // 일반적으로 전열의 몬스터먼저 행동 // 다른경우 이부분 수정
        for(int i = 0 ;i < spawnedMonsterList.Count ; i ++)
        {
            monsterOrderList.Add(spawnedMonsterList[i]);
        }
    }

    [Server]
    public void StartWaitCardQueue()
    {
        StartCoroutine(WaitCardQueue());
    }

    IEnumerator WaitCardQueue()
    {
        WaitForSeconds loopWait = new WaitForSeconds(0.01f);
        while(true)
        {
            if(!isCardQueueOperating && cardTargetPairQueue.Count == 0)
            {
                break;
            }
            yield return loopWait;
        }

        if(phase == BattleTurn.PLAYER_ACTIVE_DONE) // 아무때나 동작하지 않음 (광클방지)
        {
            phase = BattleTurn.PLAYER_END_TURN_EFFECT;
        }
    }

    public IEnumerator PlayerEndTurnEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList) // 턴종료시 버프 효과들
        {
            // End Turn Card Effect
            List<int> currentKeys = tar.buffTurnEndEffect.Keys.ToList();
            foreach(int buffIndex in currentKeys)
            { 
                yield return tar.buffTurnEndEffect[buffIndex](tar,buffIndex,null);
            }   
        }
        yield return IronDemonPreEffect();
        phase = BattleTurn.PLAYER_END;
        yield return null;
    }

    [Server]
    public void EnterTheRoom(HexagonMapRoom hexagonMapRoom)
    {
        int actionCost = M_MapManager.instance.FindPath(M_MapManager.instance.currentRoom, hexagonMapRoom).Count;
        if(actionCost > M_MapManager.instance.currentActionCost){
            Debug.Log($"[행동 비용이 모자랍니다] 총 비용 : {M_MapManager.instance.currentActionCost} / 남은 비용 : {actionCost}");
        }else{
            // 맵 플레이어들 위치 이동
            foreach(GameObject mapPlayerPieceObject in M_MapManager.instance.mapPlayerPieces){
                MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
                mapPlayerPiece.RpcChangeMapPlayerPiecePosition(hexagonMapRoom.transform.position);
                M_MapManager.instance.SetDirection(hexagonMapRoom);
            }
            M_MapManager.instance.MoveToRoom();
        }
    }

    IEnumerator WaitingForPlayer(HexagonMapRoom hexagonMapRoom)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        int cnt = 0;
        while(true)
        {
            cnt = 0;
            yield return new WaitForSeconds(0.1f);
            IReadOnlyList<PlayerInterface> users = PlayerRegistry.All;
            foreach(PlayerInterface user in users)
                if(user.isTargetObjectInitDone) cnt++;
            if(cnt != netManager.roomSlots.Count) continue;

            if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE || hexagonMapRoom.roomType == RoomType.BOSS)
                phase = BattleTurn.BATTLE_INITIALIZE;
            else
                phase = BattleTurn.NONE_BATTLE_SCENE;
            break;
        }
        ClearTargetObjectInitFlag();
    }

    [ClientRpc]
    void ClearTargetObjectInitFlag()
    {
        NetworkClient.connection.identity.GetComponent<PlayerInterface>().isTargetObjectInitDone = false;
    }


    [ClientRpc]
    public void EachPlayerCardDraw()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands) // 영원 카드의 경우도 변경된 정보 제공
                    cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);
                if(NetworkClient.spawned.ContainsKey(gamePlayer.GetComponent<GamePlayerTarget>().targetObject))
                    gamePlayerDeck.CmdSpawnCardOnHand();
            }
        }
    }

    [ClientRpc]
    public void EachPlayerEndTurn()
    {
        // 각 플레이어들의 모든 카드와 화살표 제거
        M_CardManager.instance.RemoveAllCurrentPlayerArrow();
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHands();
    }

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
                // SyncList 델타가 스폰 메시지보다 먼저 도착한 경우 타겟오브젝트가 아직 없을 수 있음 — 인디케이터는 생성하고 위치는 이후 갱신에 맡긴다
                Vector3 monsterIndicatorPosition = targetObject != null ? targetObject.transform.position : Vector3.zero;
                TargetIndicatorController.instance.CreateIndicator(newVal, monsterIndicatorPosition);
                break;
        }
    }
}
