using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;

// 전투 오브젝트 스폰 팩토리 (플레이어 유닛/몬스터/보스/NPC).
// M_TurnManager에서 분리된 서버 전용 로직 — NetworkBehaviour가 아니므로 [Server] 대신 수동 가드 사용.
// 전투 진입 오케스트레이션(RPC 연출 포함)은 M_TurnManager.GenerateBattleObject가 담당.
public class BattleSpawner : InstanceD<BattleSpawner>
{
    static readonly Vector3 npcSpawnPosition = new Vector3(11, -3, 0); // NPC 공통 스폰 위치 (전투 슬롯 4번과 동일 지점)

    public void GeneratePlayerUnit()
    {
        if(!NetworkServer.active) return;
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_TurnManager turnManager = M_TurnManager.instance;
        for(int i = 0 ;i < turnManager.playerOrder.Count ; i ++){
            if(turnManager.playerOrder[i] != 0 ){
                if(NetworkServer.spawned.TryGetValue(turnManager.playerOrder[i], out NetworkIdentity networkIdentity)){
                    GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();

                    Vector3 avatarOrderPosition = turnManager.targetObjectPosition[gamePlayer.selectOrder]; // 게임플레이어의 오더값에 맞춰 생성될 아바타 위치 설정
                    GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), avatarOrderPosition, Quaternion.identity);
                    TargetObject targetObject = avatar.GetComponent<TargetObject>();
                    targetObject.objectType = ProjectD.ObjectType.PLAYER;
                    targetObject.player = gamePlayer;
                    targetObject.playerMaxHP = gamePlayer.MaxHP;
                    targetObject.playerHP = gamePlayer.HP;
                    NetworkServer.Spawn(avatar);

                    turnManager.spawnedPlayerList.Add(targetObject);
                    turnManager.spawnedPlayerSyncList.Add(targetObject.netId);
                    gamePlayer.GetComponent<GamePlayerTarget>().targetObject = avatar.GetComponent<NetworkIdentity>().netId;
                }
            }else{
                turnManager.spawnedPlayerSyncList.Add(turnManager.playerOrder[i]);
            }
        }
    }

    public void GenerateMonster(HexagonMapRoom currentRoom)
    {
        if(!NetworkServer.active) return;
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_TurnManager turnManager = M_TurnManager.instance;
        MonsterGroup selectedMonsterGroup = MonsterData.instance.GetMonsterGroup(currentRoom.hazard);
        for(int i = 0 ; i < selectedMonsterGroup.monsters.Count ; i ++)
        {
            Vector3 position = turnManager.targetObjectPosition[i + 3];
            var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == selectedMonsterGroup.monsters[i].name), position, Quaternion.identity).GetComponent<SpawnedMonster>();
            monster.monster = selectedMonsterGroup.monsters[i];
            monster.index = selectedMonsterGroup.monsters.Count - i;
            monster.MAXHP = selectedMonsterGroup.monsters[i].MAXHP;
            monster.HP = selectedMonsterGroup.monsters[i].MAXHP;
            monster.monsterName = selectedMonsterGroup.monsters[i].name;
            NetworkServer.Spawn(monster.gameObject);

            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), position, Quaternion.identity);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;
            NetworkServer.Spawn(avatar);

            turnManager.spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            turnManager.spawnedMonsterSyncList.Add(avatar.GetComponent<TargetObject>().netId);
            monster.parent = avatar.GetComponent<TargetObject>(); // monster 오브젝트의 부모오브젝트 참조값 설정
        }
    }

    // 방타입에 따라 NPC 생성
    public void GnenrateNPCByRoomTpye(RoomType roomType)
    {
        if(!NetworkServer.active) return;
        switch(roomType){
            case RoomType.CAMP:
                GenerateCampNPC();
                break;
            case RoomType.CARD_NPC:
                GenerateCardShopNPC();
                break;
            case RoomType.ITEM_NPC:
                GenerateItemNPC();
                break;
        }
    }

    // 전초기지 NPC 생성
    public void GenerateCampNPC()
    {
        if(!NetworkServer.active) return;
        // RyuJinSol 또는 Sophia 중 랜덤 생성
        string npcName = Random.Range(0, 2) == 0 ? "NPC_RyuJinSol" : "NPC_Sophia";
        SpawnMonsterWithAvatar(npcName, npcSpawnPosition, ObjectType.NPC, false);

        // 각 플레이어별 체력 회복 횟수 제한을 1로 설정
        M_TurnManager turnManager = M_TurnManager.instance;
        for(int i=0; i<turnManager.playerOrder.Count; i++){
            if(NetworkClient.spawned.TryGetValue(turnManager.playerOrder[i], out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.recoveryLimitCount = 1;
            }
        }
    }

    // 아이템상점 NPC 생성
    public void GenerateItemNPC()
    {
        if(!NetworkServer.active) return;
        SpawnMonsterWithAvatar("NPC_ShadowMan", npcSpawnPosition, ObjectType.NPC, false);
    }

    // 카드상점 NPC 생성
    public void GenerateCardShopNPC()
    {
        if(!NetworkServer.active) return;
        // 상점판매용 캐릭터별 카드 추출해서 각 플레이어의 shopCards Synclist에 추가
        foreach(uint netId in M_TurnManager.instance.playerOrder){
            if(netId != 0 && NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                int shopCardCount = gamePlayerDeck.maxShopCardCount; // 플레이어별로 설정된 구매가능한 상점카드 최대 갯수
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < shopCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        Card shopCard = cardsByCharacter[randomIndex].CardDeepCopy(false);
                        shopCard.guid = System.Guid.NewGuid().ToString();
                        shopCard.cardPrice = BalanceData.Get("SHOP_CARD_PRICE", 1); // 가격표 확정 시 BalanceDB에서 갱신
                        cardsByCharacter.RemoveAt(randomIndex);
                        gamePlayerDeck.shopCards.Add(shopCard); // 각 플레이어의 shopCards synclist에 상점카드 데이터 추가
                    }
                }
            }
        }
        SpawnMonsterWithAvatar("NPC_Mercurius", npcSpawnPosition, ObjectType.NPC, false);
    }

    public void GenerateBossMonster()
    {
        if(!NetworkServer.active) return;
        // 3종 보스 중 랜덤 생성 (기존 3중 복붙 블록을 공통 경로로 통합)
        string[] bossNames = { "Boss_Momos", "Boss_Apates", "Boss_Geras" };
        string bossName = bossNames[Random.Range(0, bossNames.Length)];
        SpawnMonsterWithAvatar(bossName, M_TurnManager.instance.targetObjectPosition[4], ObjectType.ENEMY, true);
    }

    // 몬스터/NPC + 타겟오브젝트(아바타) 공통 스폰 경로.
    // addToSyncList: 보스/일반 몬스터는 spawnedMonsterSyncList에도 추가(클라 인디케이터 생성), NPC는 로컬 리스트에만 추가
    private SpawnedMonster SpawnMonsterWithAvatar(string monsterName, Vector3 position, ObjectType objectType, bool addToSyncList)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_TurnManager turnManager = M_TurnManager.instance;

        GameObject monsterPrefab = netManager.spawnPrefabs.Find(prefab => prefab.name == monsterName);
        Monster monsterData = MonsterData.instance.monsterDataList.Find(monster => monster.name.Equals(monsterName));
        if(monsterPrefab == null || monsterData == null){
            Debug.LogError($"[BattleSpawner] '{monsterName}' 스폰 실패 — 프리팹({(monsterPrefab == null ? "없음" : "OK")}) / MonsterDB({(monsterData == null ? "없음" : "OK")})");
            return null;
        }

        var spawned = Instantiate(monsterPrefab, position, Quaternion.identity).GetComponent<SpawnedMonster>();
        spawned.MAXHP = monsterData.MAXHP;
        spawned.HP = monsterData.MAXHP;
        spawned.monsterName = monsterData.name;
        NetworkServer.Spawn(spawned.gameObject);

        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), position, Quaternion.identity);
        TargetObject targetObject = avatar.GetComponent<TargetObject>();
        targetObject.objectType = objectType;
        targetObject.monster = spawned;
        NetworkServer.Spawn(avatar);

        turnManager.spawnedMonsterList.Add(targetObject);
        if(addToSyncList)
            turnManager.spawnedMonsterSyncList.Add(targetObject.netId);
        spawned.parent = targetObject; // monster 오브젝트의 부모오브젝트 참조값 설정
        return spawned;
    }
}
