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


// M_TurnManager partial — 전투 오브젝트 스폰 팩토리 (플레이어 유닛/몬스터/보스/NPC)
public partial class M_TurnManager
{

    [Server]
    public void GeneratePlayerUnit()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int i = 0 ;i < playerOrder.Count ; i ++){
            if(playerOrder[i] != 0 ){
                if(NetworkServer.spawned.TryGetValue(playerOrder[i], out NetworkIdentity networkIdentity)){
                    GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();

                    Vector3 avatarOrderPosition = targetObjectPosition[gamePlayer.selectOrder]; // 게임플레이어의 오더값에 맞춰 생성될 아바타 위치 설정
                    GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), avatarOrderPosition, Quaternion.identity);
                    avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
                    avatar.GetComponent<TargetObject>().player = gamePlayer;
                    avatar.GetComponent<TargetObject>().playerMaxHP = gamePlayer.MaxHP;
                    avatar.GetComponent<TargetObject>().playerHP = gamePlayer.HP;
                    NetworkServer.Spawn(avatar);

                    spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
                    spawnedPlayerSyncList.Add(avatar.GetComponent<TargetObject>().netId);
                    gamePlayer.GetComponent<GamePlayerTarget>().targetObject = avatar.GetComponent<NetworkIdentity>().netId;
                }
            }else{
                spawnedPlayerSyncList.Add(playerOrder[i]);
            }
        }
    }


    [Server]
    public void GenerateMonster(HexagonMapRoom currentRoom)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        MonsterGroup selectedMonsterGroup = MonsterData.instance.GetMonsterGroup(currentRoom.hazard);
        for(int i = 0 ; i < selectedMonsterGroup.monsters.Count ; i ++)
        {   
            Vector3 position = targetObjectPosition[i + 3];
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

            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            spawnedMonsterSyncList.Add(avatar.GetComponent<TargetObject>().netId);
            monster.parent = avatar.GetComponent<TargetObject>(); // monster 오브젝트의 부모오브젝트 참조값 설정
        }
    }


    // 방타입에 따라 NPC 생성
    [Server]
    public void GnenrateNPCByRoomTpye(RoomType roomType)
    {
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
    [Server]
    public void GenerateCampNPC()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        int randomNumber = Random.Range(0, 2);
        if(randomNumber == 0){
            // RyuJinSol 생성
            var campRyuJinSol = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_RyuJinSol"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
            Monster monster = MonsterData.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_RyuJinSol"));
            campRyuJinSol.MAXHP = monster.MAXHP;
            campRyuJinSol.HP = monster.MAXHP;
            campRyuJinSol.monsterName = monster.name;
            NetworkServer.Spawn(campRyuJinSol.gameObject);

            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
            avatar.GetComponent<TargetObject>().monster = campRyuJinSol;
            NetworkServer.Spawn(avatar);
            
            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            campRyuJinSol.parent = avatar.GetComponent<TargetObject>();
        }else{
            // Sophia 생성
            var campSophia = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_Sophia"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
            Monster monster = MonsterData.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_Sophia"));
            campSophia.MAXHP = monster.MAXHP;
            campSophia.HP = monster.MAXHP;
            campSophia.monsterName = monster.name;
            NetworkServer.Spawn(campSophia.gameObject);

            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
            avatar.GetComponent<TargetObject>().monster = campSophia;
            NetworkServer.Spawn(avatar);
            
            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            campSophia.parent = avatar.GetComponent<TargetObject>();
        }
        // 각 플레이어별 체력 회복 횟수 제한을 1로 설정
        for(int i=0; i<playerOrder.Count; i++){
            if(NetworkClient.spawned.TryGetValue(playerOrder[i], out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.recoveryLimitCount = 1;
            }
        }
    }


    // 아이템상점 NPC 생성
    [Server]
    public void GenerateItemNPC()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var itemShopNPC = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_ShadowMan"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
        Monster monster = MonsterData.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_ShadowMan"));
        itemShopNPC.MAXHP = monster.MAXHP;
        itemShopNPC.HP = monster.MAXHP;
        itemShopNPC.monsterName = monster.name;
        NetworkServer.Spawn(itemShopNPC.gameObject);

        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
        avatar.GetComponent<TargetObject>().monster = itemShopNPC;
        NetworkServer.Spawn(avatar);
        
        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        itemShopNPC.parent = avatar.GetComponent<TargetObject>();
    }


    // 카드상점 NPC 생성
    [Server]
    public void GenerateCardShopNPC()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var cardNPC = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_Mercurius"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
        NPC_Mercurius mercurius = cardNPC.GetComponent<NPC_Mercurius>();

        // 상점판매용 캐릭터별 카드 추출해서 NPC_Mercurius SyncDictionary에 추가
        foreach(uint netId in playerOrder){
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
                        shopCard.cardPrice = 1; // TODO : 카드 가격 설정. 임시로 가격 1원 설정
                        cardsByCharacter.RemoveAt(randomIndex);
                        gamePlayerDeck.shopCards.Add(shopCard); // 각 플레이어의 shopCards synclist에 상점카드 데이터 추가
                    }
                }
            }
        }
        Monster monster = MonsterData.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_Mercurius"));
        mercurius.MAXHP = monster.MAXHP;
        mercurius.HP = monster.MAXHP;
        mercurius.monsterName = monster.name;
        NetworkServer.Spawn(cardNPC.gameObject);

        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
        avatar.GetComponent<TargetObject>().monster = cardNPC;
        NetworkServer.Spawn(avatar);

        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        cardNPC.parent = avatar.GetComponent<TargetObject>();  // monster 오브젝트의 부모오브젝트 참조값 설정
    }


    [Server]
    public void GenerateBossMonster()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        int randomNumber = Random.Range(0, 3);
        switch(randomNumber){
            case 0:
                var bossMoMos = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Momos"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
                Monster momosData = MonsterData.instance.monsterDataList.Find(x => x.name == "Boss_Momos");
                bossMoMos.MAXHP = momosData.MAXHP;
                bossMoMos.HP = momosData.MAXHP;
                bossMoMos.monsterName = momosData.name;
                NetworkServer.Spawn(bossMoMos.gameObject);
                var bossMoMosAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
                bossMoMosAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
                bossMoMosAvatar.GetComponent<TargetObject>().monster = bossMoMos;
                NetworkServer.Spawn(bossMoMosAvatar);
                spawnedMonsterList.Add(bossMoMosAvatar.GetComponent<TargetObject>());
                spawnedMonsterSyncList.Add(bossMoMosAvatar.GetComponent<TargetObject>().netId);
                bossMoMos.parent = bossMoMosAvatar.GetComponent<TargetObject>(); // monster 오브젝트의 부모오브젝트 참조값 설정
                break;
            case 1:
                var bossApates = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Apates"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
                Monster apatesData = MonsterData.instance.monsterDataList.Find(x => x.name == "Boss_Apates");
                bossApates.MAXHP = apatesData.MAXHP;
                bossApates.HP = apatesData.MAXHP;
                bossApates.monsterName = apatesData.name;
                NetworkServer.Spawn(bossApates.gameObject);
                var bossApatesAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
                bossApatesAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
                bossApatesAvatar.GetComponent<TargetObject>().monster = bossApates;
                NetworkServer.Spawn(bossApatesAvatar);
                spawnedMonsterList.Add(bossApatesAvatar.GetComponent<TargetObject>());
                spawnedMonsterSyncList.Add(bossApatesAvatar.GetComponent<TargetObject>().netId);
                bossApates.parent = bossApatesAvatar.GetComponent<TargetObject>();
                break;
            case 2:
                var bossGeras = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Geras"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
                Monster gerasData = MonsterData.instance.monsterDataList.Find(x => x.name == "Boss_Geras");
                bossGeras.MAXHP = gerasData.MAXHP;
                bossGeras.HP = gerasData.MAXHP;
                bossGeras.monsterName = gerasData.name;
                NetworkServer.Spawn(bossGeras.gameObject);
                var bossGerasAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
                bossGerasAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
                bossGerasAvatar.GetComponent<TargetObject>().monster = bossGeras;
                NetworkServer.Spawn(bossGerasAvatar);
                spawnedMonsterList.Add(bossGerasAvatar.GetComponent<TargetObject>());
                spawnedMonsterSyncList.Add(bossGerasAvatar.GetComponent<TargetObject>().netId);
                bossGeras.parent = bossGerasAvatar.GetComponent<TargetObject>();
                break;
        }
    }


    [Server]
    public void GenerateBattleObject(HexagonMapRoom hexagonMapRoom)
    {
        GeneratePlayerUnit();
        if(hexagonMapRoom.roomType == RoomType.BOSS){ // 보스 몬스터 생성
            GenerateBossMonster();
            RpcCardPrefareForBattle();
            RpcStartBossBattleEvent();
        }else if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE){ // 일반 or 엘리트 몬스터 생성
            GenerateMonster(hexagonMapRoom);
            RpcCardPrefareForBattle();
            RpcStartBattleEvent(hexagonMapRoom.roomType);
        }else{ // NPC 생성
            GnenrateNPCByRoomTpye(hexagonMapRoom.roomType);
            RpcStartNoneBattleEvent(hexagonMapRoom.roomType);
        }
        // 전투 시작 이치 초기화 및 어빌리티 카드 생성
        foreach(GamePlayerDeck gamePlayerDeck in FindObjectsByType<GamePlayerDeck>(FindObjectsSortMode.None))
        {
            if(gamePlayerDeck.abilityCard == null)gamePlayerDeck.SpawnAbilityCardRPC();
        }
        StartCoroutine(WaitingForPlayer(hexagonMapRoom));
    }
}
