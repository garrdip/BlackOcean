using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;
using Spine.Unity;
using DG.Tweening;

public class M_TurnManager : NetworkSingletonD<M_TurnManager>
{
    // 서버에서 관리할 PlayerOrder SyncList : 요소값이 0인 인덱스는 빈슬롯을 의미. 플레이어들이 추가될 때 0인 인덱스의 값을 제거하고 해당 플레이어의 netId를 추가
    public readonly SyncList<uint> playerOrder = new SyncList<uint>(){ 0, 0, 0 };

    // 각 클라이언트에서 참조할 현재 참가한 플레이어들의 타겟오브젝트 목록
    public readonly SyncList<uint> spawnedPlayerSyncList = new SyncList<uint>();
    
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
    public bool ironDemonPassiveOperating = false;

    // 카드와 타겟을 한쌍으로 저장하는 큐
    public Queue<(GamePlayerDeck, int , CardOnHand, List<TargetObject>)> cardTargetPairQueue = new Queue<(GamePlayerDeck, int, CardOnHand, List<TargetObject>)>();
    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */
    
    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
    }
    
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
            return NetworkServer.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkServer.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
        }else{
            return NetworkClient.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkClient.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
        }
    }

    // 현재 페이즈가 PLAYER_ACTIVE 상태인지 체크
    public bool IsActivePhase()
    {
        return phase == BattleTurn.PLAYER_ACTIVE ? true : false;
    }


    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(ProcessCardQueue());
    }

    public override void OnStartClient()
    {
        playerOrder.Callback += OnPlayerOrderUpdated;
    }

    // -------------------------------------------------------------------- Server Method ---------------------------------------------------------------------//

    [Server]
    public void ProcessCardPredict(Card card,List<TargetObject> tar)
    {
        CardData.instance.RunCard(card,tar);
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
            case BattleTurn.BATTLE_STANDBY :
                BattleStandby();
                break;
            case BattleTurn.PLAYER_PREEFFECT :
                PlayerPreEffect();
                break;
            case BattleTurn.PLAYER_DRAW :
                PlayerCardDraw();
                break;
            case BattleTurn.PLAYER_ACTIVE :
                break;
            case BattleTurn.PLAYER_ACTIVE_DONE :
                StartWaitCardQueue();
                break;
            case BattleTurn.PLAYER_END :
                PlayerEndTurn();
                break;
            case BattleTurn.MONSTER_ORDERSELECT :
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
    IEnumerator MonsterPreEffect()
    {
        // 몬스터 방어도 초기화
        foreach(TargetObject tar in spawnedMonsterList)
        {
            tar.defense = 0;
        }
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.character == Character.HONGDANHYANG)
            {
                ironDemonPassiveOperating = true;
                while(true)
                {
                    yield return new WaitForSeconds(0.01f);
                    if(monsterDeathOperating) continue;
                    break;
                }
                if(tar.ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
                {
                    AnimIronDemon("Buff0",tar);
                    tar.ironDemonLocation.defense += tar.sizeOfIronDemon;
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
                    tar.ironDemonLocation.DamageToMonster(tar.sizeOfIronDemon, tar);
                    yield return new WaitForSeconds(0.6f);
                }
                ironDemonPassiveOperating = false;
                AnimIronDemon("Idle",tar);
                yield return new WaitForSeconds(0.1f);
            }
        }
        phase = BattleTurn.MONSTER_ACTIVE;
    }

    [Server]
    public void PlayerCardDraw()
    {
        foreach(uint netId in playerOrder){
            if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer player = networkIdentity.GetComponent<GamePlayer>();
                player.GetComponent<GamePlayerDeck>().currentIchi = player.GetComponent<GamePlayerDeck>().maxIchi; 
            }
        }     
        EachPlayerCardDraw();
        phase = BattleTurn.PLAYER_ACTIVE;
    }

    [Server]
    public void PlayerPreEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList)
        {
            tar.defense = 0;
        }
        phase = BattleTurn.PLAYER_DRAW;
    }

    [Server]
    public void PlayerEndTurn()
    {
        ResetEndTurnState();
        phase = BattleTurn.MONSTER_ORDERSELECT;
        EachPlayerEndTurn();
    }

    [Server]
    public void ResetEndTurnState()
    {
        foreach(TargetObject user in spawnedPlayerList)
        {
            user.player.objectOwner.SetEndTurnActiveStateDefault();
        }
    }

    [Server]
    public void BattleEnd()
    {   
        // TODO : 전투 종료 혹은 이벤트방에서 개인별로 먼저 수행하고 넘어가는게 맞을지?, 팀원이 모두 수행을 끝낼때까지 기다리는게 맞을지?
        
        // 전투 종료시 플레이어들의 캐릭터별 보상카드 랜덤추출하여 각 플레이어들에게 전달
        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values){
            PlayerInterface playerInterface = NetworkServer.spawned[conn.identity.netId].GetComponent<PlayerInterface>();
            PlayerInterfaceServer playerInterfaceServer = playerInterface.GetComponent<PlayerInterfaceServer>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                List<Card> rewardCards = new List<Card>();
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                int rewardCardCount = gamePlayerDeck.maxRewardCardCount; // 플레이어별로 설정된 보상 카드 최대 갯수
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < rewardCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        rewardCards.Add(cardsByCharacter[randomIndex]);
                        gamePlayerDeck.rewardCards.Add(cardsByCharacter[randomIndex]);
                        cardsByCharacter.RemoveAt(randomIndex);
                    }
                }
                gamePlayerDeck.TargetSetBattleRewardCard(gamePlayerDeck.GetComponent<NetworkIdentity>().connectionToClient, rewardCards); // 보상 카드 세팅 RPC 이벤트
            }
            playerInterfaceServer.TargetBattleRewardPopUp(playerInterface.GetComponent<NetworkIdentity>().connectionToClient); // 보상 팝업 RPC 이벤트
        }
        ResetEndTurnState(); // 턴종료 상태 리셋
    }

    [Server]
    public void NoneBattleEnd()
    {
        EachPlayerNoneBattleEnd();
        foreach(PlayerInterface player in FindObjectsOfType<PlayerInterface>()){
            player.SetIsReadyStateDefault(); // 레디 상태 모두 확인후 다시 false 되돌림 (여러군데서 사용 예정)
            player.SetEndTurnActiveStateDefault(); // 앤드 턴 상태 모두 확인후 다시 false 되돌림
        }
        foreach(HexagonMapRoom hexagonMapRoom in M_MapManager.instance.hexagonMapRooms){
            hexagonMapRoom.isSelected = false; // 맵 선택상태 모두 false 초기화
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
        spawnedPlayerSyncList.Clear();
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

    public IEnumerator ProcessCardQueue()
    {
        // 무한루프에서 인스턴스 생성시 생기는 가비지 방지를 위해 함수호출에서 미리 인스턴스 생성하여 캐싱후 루프 안에서 사용
        WaitForSeconds waitForLoop = new WaitForSeconds(0.01f);
        while (true)
        {
            yield return waitForLoop;
            if(CardData.instance.isCardOperating || monsterDeathOperating){
                continue;
            }
            else
            {
                if(cardTargetPairQueue.Count != 0){
                    CardData.instance.isCardOperating = true;
                    isCardQueueOperating = true;
                    // TODO : 큐에서 하나씩 빼서 카드의 타겟에 대한 로직 수행
                    (GamePlayerDeck gpd, int totalCost, CardOnHand cardOnHand,List<TargetObject> tar) = cardTargetPairQueue.Dequeue();
                    Debug.Log(cardOnHand.card.baseCard.name + " : 카드 디큐!");
                    if(cardOnHand.card.baseCard.isTargetable && tar[1] == null)
                    {
                        Debug.Log("타겟 실 종 !");
                        gpd.ReturnToCardOnHand(cardOnHand);
                        gpd.currentIchi += totalCost;
                        CardData.instance.isCardOperating = false;
                    }
                    else
                    {
                        CardData.instance.RunCard(cardOnHand.card,tar);
                        gpd.destroyCardList.Add(cardOnHand);
                    }
                }
                else
                {
                    isCardQueueOperating = false;
                }
            }
        }
    }

    public void ProcessMonsterDeath(TargetObject tar)
    {
        StartCoroutine(ProcessMonsterDeathCoroutine(tar));
    }

    public IEnumerator ProcessMonsterDeathCoroutine(TargetObject tar)
    {
        while(true)
        {
            // 철구 돌려 보내기
            yield return new WaitForSeconds(0.01f);
            if(CardData.instance.isCardOperating || ironDemonPassiveOperating) continue;
            foreach(TargetObject target in spawnedPlayerList)
            {
                if(target.player.character == Character.HONGDANHYANG)
                    if(target.ironDemonLocation == tar || target.ironDemonLocation == null)
                    {
                        M_TurnManager.instance.AnimIronDemon("TeleportGo",target); // 철귀 사라짐
                        yield return new WaitForSeconds(0.333f); // 철귀 완전히 사라지는 시간
                        M_TurnManager.instance.MoveIronDemon(target,target); // 철귀 적으로 이동
                        M_TurnManager.instance.AnimIronDemon("TeleportBack",target); // 철귀 나타나기 시작
                        yield return new WaitForSeconds(0.2f); // 적당히 나타날때까지 기다림
                        M_TurnManager.instance.AnimIronDemon("Idle",target); // 철귀 나타나기 시작
                        target.ironDemonLocation = target;
                    }
            }
            monsterDeathOperating = false;
            break;
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

    [Server]
    public void MonsterActive()
    {
        StartCoroutine(MonsterActionSeuqence());
    }

    IEnumerator MonsterActionSeuqence()
    {
        WaitForSeconds loopWait = new WaitForSeconds(0.01f);
        foreach(TargetObject target in spawnedMonsterList)
        {

            target.monster.isActive = true;
            StartCoroutine(target.monster.DoAction());
            while(true)
            {
                if(target.monster.isActive == false) break;
                yield return loopWait;
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
        //phase = BattleTurn.MONSTER_PREEFFECT;
    }

    [Server]
    public void GeneratePlayerUnit()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int i = 0 ;i < playerOrder.Count ; i ++){
            if(playerOrder[i] != 0 && NetworkServer.spawned.TryGetValue(playerOrder[i], out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();

                Vector3 avatarOrderPosition = targetObjectPosition[gamePlayer.selectOrder]; // 게임플레이어의 오더값에 맞춰 생성될 아바타 위치 설정
                GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), avatarOrderPosition, Quaternion.identity);
                NetworkServer.Spawn(avatar);
                avatar.GetComponent<TargetObject>().player = gamePlayer;
                avatar.GetComponent<TargetObject>().playerMaxHP = gamePlayer.MaxHP;
                avatar.GetComponent<TargetObject>().playerHP = gamePlayer.HP;
                avatar.GetComponent<TargetObject>().conn = gamePlayer.netIdentity;
                avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
                if(avatar.GetComponent<TargetObject>().player.character == Character.HONGDANHYANG){
                    avatar.GetComponent<TargetObject>().sizeOfIronDemon = 4;
                }
                gamePlayer.GetComponent<GamePlayerTarget>().targetObject = avatar.GetComponent<NetworkIdentity>().netId;
                spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
                spawnedPlayerSyncList.Add(avatar.GetComponent<NetworkIdentity>().netId);
            }
        }
    }

    [Server]
    public void GenerateMonster()
    {
        int num;
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        while(true)
        {
            //위험도 일치하는 선에서 랜덤한 몹 찾아야함
            num = Random.Range(0,M_MonsterManager.instance.monsterGroups.Count - 1);
            break;
        }
        for(int i = 0 ; i < M_MonsterManager.instance.monsterGroups[num].monsters.Count ; i ++)
        {
            Debug.Log(M_MonsterManager.instance.monsterGroups[num].monsters[i].name);
            var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == M_MonsterManager.instance.monsterGroups[num].monsters[i].name),targetObjectPosition[i+3],Quaternion.identity).GetComponent<SpawnedMonster>();
            NetworkServer.Spawn(monster.gameObject);
            monster.monsterData = M_MonsterManager.instance.monsterGroups[num].monsters[i];
            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[i+3],Quaternion.identity);
            NetworkServer.Spawn(avatar);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;
            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            // monster 오브젝트의 부모오브젝트 참조값 설정
            monster.parent = avatar.GetComponent<TargetObject>();
        }
    }

    [Server]
    public void GenerateNPC(string npcName)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == npcName),new Vector3(11,3,0),Quaternion.identity).GetComponent<SpawnedMonster>();
        NPC_Mercurius mercurius = monster.GetComponent<NPC_Mercurius>();
        mercurius.isOrigin = true;

        // 상점판매용 캐릭터별 카드 추출해서 NPC_Mercurius SyncDictionary에 추가
        foreach(uint netId in playerOrder){
            if(netId != 0 && NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                int shopCardCount = gamePlayerDeck.maxShopCardCount; // 플레이어별로 설정된 구매가능한 상점카드 최대 갯수
                List<Card> shopCards = new List<Card>();
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < shopCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        shopCards.Add(cardsByCharacter[randomIndex]);
                        cardsByCharacter.RemoveAt(randomIndex);
                    }
                }
                mercurius.shopCardDictionary.Add(gamePlayer, shopCards); // NPC_Mercurius의 SyncDictionary에 각 플레이어와 추출한 랜덤카드를 한쌍의 데이터로 저장
            }
        }

        NetworkServer.Spawn(monster.gameObject);
        monster.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name == npcName);
        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(11,-3,0),Quaternion.identity);
        NetworkServer.Spawn(avatar);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
        avatar.GetComponent<TargetObject>().monster = monster;
        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        // monster 오브젝트의 부모오브젝트 참조값 설정
        monster.parent = avatar.GetComponent<TargetObject>();
    }

    [Server]
    public void GenerateBossMonster()
    {
        // TODO : 보스 몬스터 생성
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Momos"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
        NetworkServer.Spawn(monster.gameObject);
        monster.monsterData = M_MonsterManager.instance.monsterDataList.Find(x => x.name == "Boss_Momos");
        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
        NetworkServer.Spawn(avatar);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
        avatar.GetComponent<TargetObject>().monster = monster;
        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        // monster 오브젝트의 부모오브젝트 참조값 설정
        monster.parent = avatar.GetComponent<TargetObject>();
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
            phase = BattleTurn.PLAYER_END;
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

    [Server]
    public void GenerateBattleObject(HexagonMapRoom hexagonMapRoom)
    {
        if(isServer)
        {
            GeneratePlayerUnit();
            if(hexagonMapRoom.roomType == RoomType.BOSS){
                GenerateBossMonster();
                RpcCardPrefareForBattle();
                RpcStartBossBattleEvent();
            }else if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE){
                GenerateMonster();
                RpcCardPrefareForBattle();
                RpcStartBattleEvent(hexagonMapRoom.roomType);
            }else{
                GenerateNPC("NPC_Mercurius");
                RpcStartNoneBattleEvent(hexagonMapRoom.roomType);
            }
            // 전투 시작 이치 초기화 및 어빌리티 카드 생성
            foreach(GamePlayerDeck gamePlayerDeck in FindObjectsOfType<GamePlayerDeck>())
            {
                if(gamePlayerDeck.abilityCard == null)gamePlayerDeck.SpawnAbilityCardRPC();
            }
            StartCoroutine(WaitingForPlayer(hexagonMapRoom));
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
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
                if(user.isTargetObjectInitDone) cnt++;
            if(cnt != netManager.roomSlots.Count) continue;

            if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE || hexagonMapRoom.roomType == RoomType.BOSS)
                phase = BattleTurn.BATTLE_STANDBY;
            else
                phase = BattleTurn.NONE_BATTLE_SCENE;
            break;
        }
        ClearTargetObjectInitFlag();
    }


    // -------------------------------------------------------------------- ClientRpc Method -----------------------------------------------------------------//

    [ClientRpc]
    void RpcChangePhase(BattleTurn phase)
    {
        GameUIManager.instance.textCurrentPhase.text = phase.ToString(); // 페이즈 상태 텍스트 업데이트
    }

    // 전투에 필요한 카드 준비 요청
    [ClientRpc]
    void RpcCardPrefareForBattle()
    {
        M_CardManager.instance.PrefareCardWithSuffle(); // 카드데이터 셔플 수행후 PrefareDeck에 추가
        M_CardManager.instance.ChangeAbilityButtonActiveState(true); // 어빌리티 버튼 활성화
    }
 
    // 보스전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBossBattleEvent()
    {
        M_MessageManager.instance
            .Position(ToastPosition.Top)
            .FadeInTime(1.5f)
            .FadeOutTime(1.5f)
            .MessageBoxColor(ColorUtils.HexToColor("#E700FF"))
            .TextColor(Color.white)
            .Text("전투 : 보스")
            .Show();
    }

    // 일반 몬스터 혹은 엘리트전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBattleEvent(RoomType roomType)
    {
        switch(roomType)
        {
            case RoomType.MONSTER:
                M_MessageManager.instance
                    .Position(ToastPosition.Top)
                    .FadeInTime(1.5f)
                    .FadeOutTime(1.5f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("전투 : 일반 몬스터")
                    .Show();
                break;
            case RoomType.ELITE:
                M_MessageManager.instance
                    .Position(ToastPosition.Top)
                    .FadeInTime(1.5f)
                    .FadeOutTime(1.5f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("전투 : 엘리트 몬스터")
                    .Show();
                break;
        }
    }

    // 엔피씨 방문 수신 이벤트
    [ClientRpc]
    public void RpcStartNoneBattleEvent(RoomType roomType)
    {
        switch(roomType)
        {
            case RoomType.EVENT:
                M_MessageManager.instance
                    .Position(ToastPosition.Top)
                    .FadeInTime(2.5f)
                    .FadeOutTime(1.5f)
                    .MessageBoxColor(Color.yellow)
                    .TextColor(Color.white)
                    .Text("이벤트")
                    .Show();
                break;
            case RoomType.CAMP:
                M_MessageManager.instance
                    .Position(ToastPosition.Top)
                    .FadeInTime(2.5f)
                    .FadeOutTime(1.5f)
                    .MessageBoxColor( Color.green)
                    .TextColor(Color.white)
                    .Text("전초기지")
                    .Show();
                break;
            case RoomType.CARD_NPC:
                M_MessageManager.instance
                    .Position(ToastPosition.Top)
                    .FadeInTime(2.5f)
                    .FadeOutTime(1.5f)
                    .MessageBoxColor(Color.magenta)
                    .TextColor(Color.white)
                    .Text("상점 : 카드 상인 NPC")
                    .Show();
                break;
            case RoomType.ITEM_NPC:
                M_MessageManager.instance
                    .Position(ToastPosition.Top)
                    .FadeInTime(2.5f)
                    .FadeOutTime(1.5f)
                    .MessageBoxColor(Color.blue)
                    .TextColor(Color.white)
                    .Text("상점 : 아이템 상인 NPC")
                    .Show();
                break;
        }
    }

    [ClientRpc]
    void ClearTargetObjectInitFlag()
    {
        NetworkClient.connection.identity.GetComponent<PlayerInterface>().isTargetObjectInitDone = false;
    }

    [ClientRpc]
    public void StartAnimation(TargetObject tar, int trackIndex,string animationName, bool loop )
    {
        if(tar != null)
        {
            SkeletonAnimation anim = tar.avatar.GetComponent<SkeletonAnimation>();
            Spine.TrackEntry track = anim.state.SetAnimation(trackIndex,animationName,loop);
            track.MixBlend = Spine.MixBlend.Replace;
        }
    }

    [ClientRpc]
    public void MoveIronDemon(TargetObject target ,TargetObject tar)
    {
        int transformOffset = CalcOffset(tar); 
        if(target.monster != null)
            if(target.monster.monsterName == "Boss_Momos") // 모모스 키 적용 TODO: 몬스터 키적용 코드 추가
                tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,5,0);
            else
                tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
        else
            tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
        int offset = (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer == tar.player) ? 0 : 2;
        if(target.objectType == ObjectType.PLAYER) tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[0+offset];
        else tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[1+offset];
        tar.ironDemon.GetComponent<SkeletonAnimation>().Initialize(true);
        tar.ironDemon.GetComponent<MeshRenderer>().material = null;
    }

    int CalcOffset(TargetObject tar)
    {
        int retVal = 0;
        if(tar.player == (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer)) retVal = 0;
        else
        {
            int addval = 0;
            foreach(uint netId in playerOrder){
                if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                    if(tar.player == networkIdentity.GetComponent<GamePlayer>())
                        break;
                    if(tar.player == (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer))
                        continue;
                    else
                        addval++;
                }
            }
            if(addval == 0) retVal = -1;
            else retVal = 1;
        }
        return retVal;
    }

    [ClientRpc]
    public void AnimIronDemon(string anim ,TargetObject tar)
    {
        bool isLoop = anim == "Idle" ? true : false;
        tar.ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,anim,isLoop);
        tar.ApllyIronDemonAnimationCallbackFunction();
    }


    [ClientRpc]
    public void EachPlayerCardDraw()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
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

    [ClientRpc]
    public void EachPlayerNoneBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 삭제, 삭제 시 Trash Deck에 추가하지 않음.
        M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck(); // 플레이어의 PrefareDeck, TrashDeck 삭제
        M_CardManager.instance.ChangeAbilityButtonActiveState(false); // 어빌리티 버튼 비활성화
        ReturnToMap();
    }

    public void ReturnToMap()
    {
        if(isServer){
            ClearTargetObject(); // 타겟오브젝트 정리
            M_MapManager.instance.ClearPlayerVoteHexagonMapRooms(); // 방 투표 목록 비움
            M_MapManager.instance.SetRoomStateComplete(); // 방 완료상태로 변경
            M_MapManager.instance.DecreaseTotalActionCost(); // 행동비용 감소
            M_MapManager.instance.ApproachBossToPlayer(); // 보스가 플레이어에게로 이동
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface player in users){
                player.SetIsReadyStateDefault();
            }
        }
        GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
            // 카메라 위치 리셋
            Vector3 currLoc = M_MapManager.instance.currentRoom.transform.position;
            Camera.main.transform.position = currLoc + new Vector3(0,0,-8);
            //Camera.main.orthographic = false; 

            // UI 활성화 상태 변경
            M_MapManager.instance.MapScene.SetActive(true);
            M_MapManager.instance.BattleScene.SetActive(false);
            GameUIManager.instance.GameUI.SetActive(false);
            GameUIManager.instance.GameBackGround.SetActive(false);

            // 임시 테스트용 UI
            GameUIManager.instance.TestUI.gameObject.SetActive(false);
            
            // Dim배경 상태 변경
            blackCurtain.gameObject.SetActive(false);
            blackCurtain.DOFade(0.0f, 0.5f); // 원래 알파값으로 변경
        });
    }

    public void MoveToPlayer(GamePlayer player, MoveDirection direction)
    {
        TargetObject forwarding = null,backwarding = null;
        Vector3 forwardingDestination = new Vector3(0,0,0),backwardingDestination = new Vector3(0,0,0);
        if(direction == MoveDirection.FORWARD)
        {
            if(player.selectOrder == 0) return;
            uint netId = playerOrder[player.selectOrder - 1];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer swap = networkIdentity.GetComponent<GamePlayer>();
                playerOrder[player.selectOrder-1] = player.netId;
                playerOrder[player.selectOrder] = swap.netId;
                player.SetPlayerOrder(player.selectOrder - 1);
                swap.SetPlayerOrder(swap.selectOrder + 1);
                foreach(TargetObject tar in spawnedPlayerList)
                {
                    if(tar.player == player)
                    {   
                        forwarding = tar;
                        backwardingDestination = tar.transform.position;
                    }
                    if(tar.player == swap)
                    {
                        backwarding = tar;
                        forwardingDestination = tar.transform.position;
                    }
                }
            }
            
        }
        if(direction == MoveDirection.BACKWARD)
        {
            if(player.selectOrder == NetworkServer.connections.Count - 1) return;
            uint netId = playerOrder[player.selectOrder + 1];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer swap = networkIdentity.GetComponent<GamePlayer>();
                playerOrder[player.selectOrder+1] = player.netId;
                playerOrder[player.selectOrder] = swap.netId;
                player.SetPlayerOrder(player.selectOrder + 1);
                swap.SetPlayerOrder(swap.selectOrder - 1);
                foreach(TargetObject tar in spawnedPlayerList)
                {
                    if(tar.player == player)
                    {   
                        backwarding = tar;
                        forwardingDestination = tar.transform.position;
                    }
                    if(tar.player == swap)
                    {
                        forwarding = tar;
                        backwardingDestination = tar.transform.position;
                    }
                }
            }
        }
        forwarding.transform.DOMove(forwardingDestination,0.5f,false).SetEase(Ease.OutQuart);
        backwarding.transform.DOMove(backwardingDestination,0.5f,false).SetEase(Ease.OutQuart);
    }

    public TargetObject[] GetTargetObjectFromActionTarget(ActionTarget target)
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

    // 플레이어 오더 변경에 따른 뷰 업데이트
    public void SetPlayerOrderViewSwap(uint netId, int index)
    {
        if(isServer){
            if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.selectOrder = index;
            }
        }else{
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.selectOrder = index;
            }
        }
    }

    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//
    private void OnPlayerOrderUpdated(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
            
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:

                break;
            case SyncList<uint>.Operation.OP_SET:
                M_MapManager.instance.SetMapPlayerSwap(newVal, index);
                MapUI.instance.ChangeSwapButtonsState(newVal, index);
                //SetPlayerOrderViewSwap(newVal, index);
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }
}
