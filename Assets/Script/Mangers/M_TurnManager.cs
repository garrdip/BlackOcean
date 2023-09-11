using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;
using Spine.Unity;
using DG.Tweening;

public class M_TurnManager : NetworkBehaviour
{
    public static M_TurnManager Instance = null;

    [SyncVar]
    public NPC_Mercurius npc_Mercurius;

    // 서버에서 관리할 player 리스트
    public List<GamePlayer> players;

    // 서버에서 관리하지만 유저도 쓸지 몰라서 일단 SyncList
    public readonly SyncList<GamePlayer> playerOrder = new SyncList<GamePlayer>();

    // 각 클라이언트에서 참조할 현재 참가한 플레이어들의 타겟오브젝트 목록
    public readonly SyncList<uint> spawnedPlayerSyncList = new SyncList<uint>();
    
    Vector3[] targetObjectPosition = {new Vector3(-7,-3,0),new Vector3(-11,-3,0),new Vector3(-15,-3,0),new Vector3(7,-3,0),new Vector3(11,-3,0),new Vector3(15,-3,0)};

    public GameObject orderUI;

    public bool isOrderSelect = false;
    public bool isMyTurn = false;
    public bool isCardQueueOperating = false;

    [Header("Scene Change Black Curtain")]
    public GameObject blackCurtain;

    public List<Button> selectOrderButtons;


    [Header("Spwan Object Transfrom Group")]
    public Transform playerSpawnLocation;
    public Transform monsterSpawnLocation;

    public List<TargetObject> spawnedPlayerList = new List<TargetObject>();
    public List<TargetObject> clonePlayerList = new List<TargetObject>();
    public List<TargetObject> spawnedMonsterList = new List<TargetObject>();
    public List<TargetObject> cloneMonsterList = new List<TargetObject>();
    List<TargetObject> monsterOrderList = new List<TargetObject>();
    
    public bool monsterDeathOperating = false;
    public bool ironDemonPassiveOperating = false;

    // 카드와 타겟을 한쌍으로 저장하는 큐
    public Queue<(Card, List<TargetObject>)> cardTargetPairQueue = new Queue<(Card, List<TargetObject>)>();
    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */
    
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

    public TargetObject GetPlayer(NetworkIdentity conn)
    {     
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.netIdentity == conn){
                Debug.Log(tar.player.netIdentity + " and " + conn);
                return tar;
            }
        }
        return null;
    }

    public TargetObject GetClonePlayer(NetworkIdentity conn)
    {
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.netIdentity == conn)
            return tar.clone;
        }
        return null;
    }

    public List<TargetObject> GetPlayerObjects()
    {
        return spawnedPlayerList;
    }

    public List<TargetObject> GetClonePlayerObjects()
    {
        return clonePlayerList;
    }
    public List<TargetObject> GetMonsterObjects()
    {
        return spawnedMonsterList;
    }

    public List<TargetObject> GetCloneMonsterObjects()
    {
        return cloneMonsterList;
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

    public static M_TurnManager instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<M_TurnManager>();
            }
            return Instance;
        }
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

    public void SetOrderButtonListener()
    {
        selectOrderButtons[0].onClick.AddListener(() => SelectOrder(1));
        selectOrderButtons[1].onClick.AddListener(() => SelectOrder(2));
        selectOrderButtons[2].onClick.AddListener(() => SelectOrder(3));
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
        if(actionCost > M_MapManager.instance.totalActionCost){
            Debug.Log($"[행동 비용이 모자랍니다] 총 비용 : {M_MapManager.instance.totalActionCost} / 남은 비용 : {actionCost}");
        }else{
            // 맵 플레이어들 위치 이동
            foreach(GameObject mapPlayerPieceObject in M_MapManager.instance.mapPlayerPieces){
                MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
                mapPlayerPiece.RpcChangeMapPlayerPiecePosition(hexagonMapRoom.transform.position);
                M_MapManager.instance.SetDirection(hexagonMapRoom);
            }
            M_MapManager.instance.StartBattle(hexagonMapRoom);
        }
    }

    [Server]
    public void GenerateBattleObject(HexagonMapRoom hexagonMapRoom)
    {
        if(isServer)
        {
            GeneratePlayerUnit();
            if(hexagonMapRoom.mapBoss != null){
                GenerateBossMonster();
                RpcCardPrefareForBattle();
                RpcStartBossBattle();
            }else if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE){
                GenerateMonster();
                RpcCardPrefareForBattle();
                RpcStartMonsterBattle();
            }else{
                GenerateNPC("NPC_Mercurius");
                RpcStartNpcVisit();
            }
            // 전투 시작 이치 초기화 및 어빌리티 카드 생성
            foreach(GamePlayerDeck gamePlayerDeck in FindObjectsOfType<GamePlayerDeck>())
            {
                gamePlayerDeck.SetInitialIchi();
                if(gamePlayerDeck.abilityCard == null)gamePlayerDeck.SpawnAbilityCardRPC();
            }
            RpcGenerateAbilityButton();
            StartCoroutine(WaitingForPlayer(hexagonMapRoom));
        }
    }

    // 플레이어 패시브 버튼 생성 요청
    [ClientRpc]
    void RpcGenerateAbilityButton()
    {
        if(NetworkClient.connection.identity.GetComponent<GamePlayer>().character == Character.HONGDANHYANG)
            NetworkClient.connection.identity.GetComponent<GamePlayerDeck>().CmdGenerateAbilityButton();
    }

    // 전투에 필요한 카드 준비 요청
    [ClientRpc]
    void RpcCardPrefareForBattle()
    {
        // 플레이어 카드 셔플 수행후 PrefareDeck에 추가 요청
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdAddPrefareDeckWithShuffle();
            }
        }
    }
 
    // 보스전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBossBattle()
    {
        Debug.Log("========================= 보스와 전투가 시작되었습니다. =========================");
    }

    // 일반 몬스터 혹은 엘리트전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartMonsterBattle()
    {
        Debug.Log("====== 몬스터와 전투가 시작되었습니다. ======");
    }

    // 엔피씨 방문 수신 이벤트
    [ClientRpc]
    public void RpcStartNpcVisit()
    {
        Debug.Log("== 엔피씨를 방문했습니다. ==");
    }

    IEnumerator WaitingForPlayer(HexagonMapRoom hexagonMapRoom)
    {
        int cnt = 0;
        while(true)
        {
            cnt = 0;
            yield return new WaitForSeconds(0.1f);
            foreach(GamePlayer user in players)
                if(user.isTargetObjectInitDone) cnt++;
            if(cnt != players.Count) continue;

            if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE)
                phase = BattleTurn.BATTLE_STANDBY;
            else
                phase = BattleTurn.NONE_BATTLE_SCENE;
            break;
        }
        ClearTargetObjectInitFlag();
    }

    [ClientRpc]
    void ClearTargetObjectInitFlag()
    {
        NetworkClient.connection.identity.GetComponent<GamePlayer>().isTargetObjectInitDone = false;
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
                    (Card card,List<TargetObject> tar) = cardTargetPairQueue.Dequeue();

                    CardData.instance.RunCard(card,tar);
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
            foreach(TargetObject target in clonePlayerList)
            {
                if(target.ironDemonLocation == tar || target.ironDemonLocation == null)
                {
                    target.ironDemonLocation = target;
                }
            }
            monsterDeathOperating = false;
            break;
        }
      
    }


    [ClientRpc]
    public void StartAnimation(TargetObject tar, int trackIndex,string animationName, bool loop )
    {
        if(!tar.isCloneData) // Clone 데이터 검증은 Animation 스킵
        {
            SkeletonAnimation anim = tar.avatar.GetComponent<SkeletonAnimation>();
            anim.state.SetAnimation(trackIndex,animationName,loop);
        }
    }

    [ClientRpc]
    public void MoveIronDemon(TargetObject target ,TargetObject tar)
    {
        tar.ironDemon.GetComponent<MeshRenderer>().sortingLayerName = "default";
        tar.ironDemon.GetComponent<MeshRenderer>().sortingOrder = -1;
        int transformOffset = CalcOffset(tar); 
        tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
        int offset = (NetworkClient.connection.identity.GetComponent<GamePlayer>() == tar.player) ? 0 : 2;
        if(target.objectType == ObjectType.PLAYER) tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[0+offset];
        else tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[1+offset];
        tar.ironDemon.GetComponent<SkeletonAnimation>().Initialize(true);
    }
    int CalcOffset(TargetObject tar)
    {
        int retVal = 0;
        if(tar.player == (NetworkClient.connection.identity.GetComponent<GamePlayer>())) retVal = 0;
        else
        {
            int addval = 0;
            foreach(GamePlayer user in playerOrder)
            {
                if(tar.player == user)
                    break;
                if(tar.player == (NetworkClient.connection.identity.GetComponent<GamePlayer>()))
                    continue;
                else
                    addval++;
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

        StartCoroutine(DelayToShowIronDemon(tar));
    }

    IEnumerator DelayToShowIronDemon(TargetObject tar)
    {
        yield return new WaitForSeconds(0.03f);
        ShowIronDemon(tar);
    }

    public void ShowIronDemon(TargetObject tar)
    {
        tar.ironDemon.GetComponent<MeshRenderer>().sortingLayerName = "IronDemon";
        tar.ironDemon.GetComponent<MeshRenderer>().sortingOrder = 0;
    }

    [Server]
    public void ProcessCardPredict(Card card,List<TargetObject> tar)
    {
        CardData.instance.RunCard(card,tar);
    }

    [Server]
    public void PlayerOrderSelectPhase()
    {
        foreach(GamePlayer player in players)
        {
            int order = 0;
            for(int i = 0 ; i < players.Count ; i ++)
            {
                if(player.selectOrder > players[i].selectOrder) order++;
            }
            playerOrder[order] = player;
        }
    }

    [Server]
    public void OnChangedPhase()
    {
        Debug.Log("Phase is " + phase);
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
    IEnumerator MonsterPreEffect()
    {
        // 몬스터 방어도 초기화
        foreach(TargetObject tar in spawnedMonsterList)
        {
            tar.clone.defense = 0;
            tar.defense = 0;
        }

        //철귀 패시브 발동
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
                    tar.ironDemonLocation.DamageToMonster(tar.sizeOfIronDemon);
                    yield return new WaitForSeconds(0.6f);
                }
                ironDemonPassiveOperating = false;
                AnimIronDemon("Idle",tar);
                yield return new WaitForSeconds(0.1f);
            }
        }
        foreach(TargetObject tar in clonePlayerList) // 클론도 적용
        {
            if(tar.player.character == Character.HONGDANHYANG)
            {
                if(tar.ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
                {
                    tar.ironDemonLocation.defense += tar.sizeOfIronDemon;
                }
                else // 몬스터의 경우 데미지
                {
                    tar.ironDemonLocation.DamageToMonster(tar.sizeOfIronDemon);
                }
            }
        }
        phase = BattleTurn.MONSTER_ACTIVE;
    }

    [Server]
    public void PlayerCardDraw()
    {
        foreach(GamePlayer player in playerOrder)
            player.GetComponent<GamePlayerDeck>().currentIchi = player.GetComponent<GamePlayerDeck>().maxIchi; 
            
        EachPlayerCardDraw();
        phase = BattleTurn.PLAYER_ACTIVE;
    }

    [ClientRpc]
    public void EachPlayerCardDraw()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdSpawnCardOnHand();
            }
        }
    }

    [Server]
    public void PlayerPreEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList)
        {
            tar.clone.defense = 0;
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
            user.player.SetEndTurnActiveStateDefault();
        }
    }

    [ClientRpc]
    public void EachPlayerEndTurn()
    {
        // 각 플레이어들의 모든 카드와 화살표 제거
        M_CardManager.instance.RemoveAllCurrentPlayerArrow();
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHands();
    }


    [Server]
    public void GeneratePlayerUnit()
    {
        PlayerOrderSelectPhase();
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int i = 0 ;i < playerOrder.Count ; i ++)
        {
            GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[i],Quaternion.identity);
            NetworkServer.Spawn(avatar);
            avatar.GetComponent<TargetObject>().player = playerOrder[i];
            avatar.GetComponent<TargetObject>().playerHP = playerOrder[i].HP;
            avatar.GetComponent<TargetObject>().playerMaxHP = playerOrder[i].MaxHP;
            avatar.GetComponent<TargetObject>().conn = playerOrder[i].netIdentity;
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
            avatar.GetComponent<TargetObject>().sizeOfIronDemon = 4;
            playerOrder[i].GetComponent<GamePlayerTarget>().targetObject = avatar.GetComponent<NetworkIdentity>().netId;
            spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
            spawnedPlayerSyncList.Add(avatar.GetComponent<NetworkIdentity>().netId);
            
            // 타겟 유효 판단을 위한 클론 데이터 //
            GameObject clone = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);
            NetworkServer.Spawn(clone);
            clone.GetComponent<TargetObject>().conn = playerOrder[i].netIdentity;
            clone.GetComponent<TargetObject>().player = playerOrder[i];
            clone.GetComponent<TargetObject>().playerHP = playerOrder[i].HP;
            clone.GetComponent<TargetObject>().playerMaxHP = playerOrder[i].MaxHP;
            clone.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
            clone.GetComponent<TargetObject>().sizeOfIronDemon = 4;
            clone.GetComponent<TargetObject>().isCloneData = true;

            avatar.GetComponent<TargetObject>().clone = clone.GetComponent<TargetObject>();
            clonePlayerList.Add(clone.GetComponent<TargetObject>());
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
            var cloneMonster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == M_MonsterManager.instance.monsterGroups[num].monsters[i].name),new Vector3(-300,-300,0),Quaternion.identity).GetComponent<SpawnedMonster>();

            NetworkServer.Spawn(monster.gameObject);
            NetworkServer.Spawn(cloneMonster.gameObject);

            monster.monsterData = M_MonsterManager.instance.monsterGroups[num].monsters[i];
            cloneMonster.monsterData = M_MonsterManager.instance.monsterGroups[num].monsters[i];


            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[i+3],Quaternion.identity);
            var cloneAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);

            NetworkServer.Spawn(avatar);
            NetworkServer.Spawn(cloneAvatar);

            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;

            cloneAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            cloneAvatar.GetComponent<TargetObject>().monster = cloneMonster;
            cloneAvatar.GetComponent<TargetObject>().isCloneData = true;
            cloneAvatar.GetComponent<TargetObject>().origin = avatar.GetComponent<TargetObject>();
            avatar.GetComponent<TargetObject>().clone = cloneAvatar.GetComponent<TargetObject>();

            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            cloneMonsterList.Add(cloneAvatar.GetComponent<TargetObject>());

            // monster 오브젝트의 부모오브젝트 참조값 설정
            monster.parent = avatar.GetComponent<TargetObject>();
            cloneMonster.parent = cloneAvatar.GetComponent<TargetObject>();
        }
    }

    [Server]
    public void GenerateNPC(string npcName)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == npcName),new Vector3(11,3,0),Quaternion.identity).GetComponent<SpawnedMonster>();
        var cloneMonster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == npcName),new Vector3(-300,-300,0),Quaternion.identity).GetComponent<SpawnedMonster>();
        
        monster.GetComponent<NPC_Mercurius>().isOrigin = true;
        NetworkServer.Spawn(monster.gameObject);
        NetworkServer.Spawn(cloneMonster.gameObject);

        npc_Mercurius = monster.GetComponent<NPC_Mercurius>();

        monster.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name == npcName);
        cloneMonster.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name == npcName);

        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(11,3,0),Quaternion.identity);
        var cloneAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);

        NetworkServer.Spawn(avatar);
        NetworkServer.Spawn(cloneAvatar);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
        avatar.GetComponent<TargetObject>().monster = monster;
        cloneAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
        cloneAvatar.GetComponent<TargetObject>().monster = cloneMonster;
        cloneAvatar.GetComponent<TargetObject>().isCloneData = true;
        cloneAvatar.GetComponent<TargetObject>().origin = avatar.GetComponent<TargetObject>();
        avatar.GetComponent<TargetObject>().clone = cloneAvatar.GetComponent<TargetObject>();
        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        cloneMonsterList.Add(cloneAvatar.GetComponent<TargetObject>());
        // monster 오브젝트의 부모오브젝트 참조값 설정
        monster.parent = avatar.GetComponent<TargetObject>();
        cloneMonster.parent = cloneAvatar.GetComponent<TargetObject>();
    }

    [Server]
    public void GenerateBossMonster()
    {
        // TODO : 보스 몬스터 생성
    }

    [Server]
    public void InitiateGamePlayerList()
    {
        players = new List<GamePlayer>(FindObjectsOfType<GamePlayer>());
        foreach(GamePlayer player in players)
        {
            playerOrder.Add(player);
        }
    }

    public void SelectOrder(int num)
    {
        NetworkClient.localPlayer.GetComponent<GamePlayer>().SetOrderByUI(num);
    }

    // 플레이어 정보 및 턴 정보 뷰 세팅
    public void SetPlayerOrderView(int index)
    {
        GamePlayer gamePlayer = M_TurnManager.instance.playerOrder[index];
        OrderUI orderUI = GameUIManager.instance.playerOrderList[index].GetComponent<OrderUI>();
        orderUI.textPlayerName.text = SteamFriends.GetFriendPersonaName((CSteamID)gamePlayer.steamID);
        if(gamePlayer.isLocalPlayer){
            orderUI.playerOwnMenu.gameObject.SetActive(true); // 전용 메뉴 활성화
            float width = orderUI.buttonPlayerOrder.GetComponent<RectTransform>().rect.width;
            float height = orderUI.buttonPlayerOrder.GetComponent<RectTransform>().rect.height;
            orderUI.buttonPlayerOrder.GetComponent<RectTransform>().sizeDelta = new Vector2(width + 30f, height + 30f); // 버튼 크기 크게 변경(변경된 값이 native size)
        }
    }

    [ClientRpc]
    public void PopUpOrderUI()
    {
        orderUI.SetActive(true);
        isOrderSelect = true;
    }

    [Server]
    public void OnChangedMonsterList()
    {
        if(spawnedMonsterList.Count == 0)
            phase = BattleTurn.BATTLE_END;
    }

    [Server]
    public void BattleEnd()
    {   
        // TODO : 전투 종료 혹은 이벤트방에서 개인별로 먼저 수행하고 넘어가는게 맞을지?, 팀원이 모두 수행을 끝낼때까지 기다리는게 맞을지?
        EachPlayerBattleEnd();
        ResetEndTurnState();
    }

    [Server]
    public void NoneBattleEnd()
    {
        EachPlayerNoneBattleEnd();
    }

    [ClientRpc]
    public void EachPlayerBattleEnd()
    {
        PopUpUIManager.instance.HandleShowBattleResultPopUp(); // 전투 결과 보상 팝업 활성화
    }

    [ClientRpc]
    public void EachPlayerNoneBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 삭제, 삭제 시 Trash Deck에 추가하지 않음.
        M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck(); // 플레이어의 PrefareDeck, TrashDeck 삭제
        ReturnToMap();
    }

    [Server]
    public void ClearTargetObject()
    {
        ClearTargetObjectList(cloneMonsterList);
        ClearTargetObjectList(spawnedMonsterList);
        ClearTargetObjectList(clonePlayerList);
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

    public void ReturnToMap()
    {
        if(isServer){
            ClearTargetObject(); // 타겟오브젝트 정리
            M_MapManager.instance.SetRoomStateComplete(); // 방 완료상태로 변경
            M_MapManager.instance.DecreaseTotalActionCost(); // 행동비용 감소
            M_MapManager.instance.ApproachBossToPlayer(); // 보스가 플레이어에게로 이동
        }
        GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
            // 카메라 위치 리셋
            Vector3 currLoc = M_MapManager.instance.currentRoom.transform.position;
            Camera.main.transform.position = currLoc + new Vector3(0,0,-8);
            Camera.main.orthographic = false; 
            // UI 활성화 상태 변경
            M_MapManager.instance.roommaps.SetActive(true);
            M_MapManager.instance.game.SetActive(false);
            GameUIManager.instance.GameUI.gameObject.SetActive(false);
            GameUIManager.instance.ChatUI.gameObject.SetActive(false);
            GameUIManager.instance.GameBackGround.gameObject.SetActive(false);

            // 임시 테스트용 UI
            GameUIManager.instance.TestUI.gameObject.SetActive(false);
            
            // Dim배경 상태 변경
            blackCurtain.gameObject.SetActive(false);
            blackCurtain.DOFade(0.0f, 0.5f); // 원래 알파값으로 변경
        });
    }

    public void MoveToPlayer(GamePlayer player, MoveDirection direction)
    {
        Debug.Log("ChangePlayerOrder Called");
        TargetObject forwarding = null,backwarding = null;
        Vector3 forwardingDestination = new Vector3(0,0,0),backwardingDestination = new Vector3(0,0,0);
        if(direction == MoveDirection.FORWARD)
        {
            if(player.selectOrder == 0) return;
            GamePlayer swap = playerOrder[player.selectOrder-1];
            playerOrder[player.selectOrder-1] = player;
            playerOrder[player.selectOrder] = swap;
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
        if(direction == MoveDirection.BACKWARD)
        {
            if(player.selectOrder == NetworkServer.connections.Count - 1) return;
            GamePlayer swap = playerOrder[player.selectOrder+1];
            playerOrder[player.selectOrder+1] = player;
            playerOrder[player.selectOrder] = swap;
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
                (target == ActionTarget.FRONT && tar.player.selectOrder == 0) ||
                (target == ActionTarget.MIDDLE && tar.player.selectOrder == 1) ||
                (target == ActionTarget.BACK && tar.player.selectOrder == 2) ||
                (target == ActionTarget.FRONT_MIDDLE && tar.player.selectOrder != 2) ||
                (target == ActionTarget.MIDDLE_BACK && tar.player.selectOrder != 0) ||
                (target == ActionTarget.FRONT_BACK && tar.player.selectOrder != 1) )
                retVal.Add(tar);
        }
        if(retVal.Count == 0)
            retVal.AddRange(spawnedPlayerList);

        return retVal.ToArray();
    }

    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//
    private void OnPlayerOrderUpdated(SyncList<GamePlayer>.Operation op, int index, GamePlayer oldGamePlayer, GamePlayer newGamePlayer)
    {
        switch (op)
        {
            case SyncList<GamePlayer>.Operation.OP_ADD:
                SetPlayerOrderView(index);
                break;
            case SyncList<GamePlayer>.Operation.OP_INSERT:
                
                break;
            case SyncList<GamePlayer>.Operation.OP_REMOVEAT:

                break;
            case SyncList<GamePlayer>.Operation.OP_SET:
                // TODO : 인덱스가 바뀔 때(플레이어 정렬 순서 바뀔 때 로직 구현부)
                break;
            case SyncList<GamePlayer>.Operation.OP_CLEAR:
                
                break;
        }
    }
}
