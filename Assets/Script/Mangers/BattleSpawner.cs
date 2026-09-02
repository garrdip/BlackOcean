using UnityEngine;
using Mirror;
using ProjectD;

// 전투/거점 오브젝트 스폰 팩토리 (플레이어 유닛/몬스터/보스/거점 NPC).
// M_TurnManager에서 분리된 서버 전용 로직 — NetworkBehaviour가 아니므로 [Server] 대신 수동 가드 사용.
// 전투 진입 오케스트레이션(RPC 연출 포함)은 M_TurnManager.GenerateBattleObject / GenerateHubObject가 담당.
public class BattleSpawner : InstanceD<BattleSpawner>
{
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

    // StageDB가 고른 MonsterGroupDB 그룹(monsterGroupName)을 스폰 — 몬스터별 위험도 보너스 스탯(MonsterStatDB) x 전역 위험도로 체력/공격/방어를 보정
    public void GenerateMonster(int hazard, string monsterGroupName)
    {
        if(!NetworkServer.active) return;
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_TurnManager turnManager = M_TurnManager.instance;
        MonsterGroup selectedMonsterGroup = MonsterData.instance.GetMonsterGroupByName(monsterGroupName);
        if(selectedMonsterGroup == null) return;
        for(int i = 0 ; i < selectedMonsterGroup.monsters.Count ; i ++)
        {
            Vector3 position = turnManager.targetObjectPosition[i + 3];
            var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == selectedMonsterGroup.monsters[i].name), position, Quaternion.identity).GetComponent<SpawnedMonster>();
            monster.monster = selectedMonsterGroup.monsters[i];
            monster.index = selectedMonsterGroup.monsters.Count - i;
            monster.hazard = hazard;
            monster.MAXHP = selectedMonsterGroup.monsters[i].MAXHP + Mathf.RoundToInt(hazard * selectedMonsterGroup.monsters[i].hazardHpBonus);
            monster.HP = monster.MAXHP;
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

    // 거점 NPC 4종 스폰 (M_HubManager.HubNpcNames 순서, positions 인덱스와 1:1)
    public void GenerateHubNPCs(Vector3[] positions)
    {
        if(!NetworkServer.active) return;
        for(int i = 0; i < M_HubManager.HubNpcNames.Length; i++)
        {
            Vector3 position = (positions != null && i < positions.Length) ? positions[i] : M_TurnManager.instance.targetObjectPosition[4];
            SpawnMonsterWithAvatar(M_HubManager.HubNpcNames[i], position, ObjectType.NPC, false);
        }
    }

    public void GenerateBossMonster(int hazard)
    {
        if(!NetworkServer.active) return;
        // 3종 보스 중 랜덤 생성 (기존 3중 복붙 블록을 공통 경로로 통합)
        string[] bossNames = { "Boss_Momos", "Boss_Apates", "Boss_Geras" };
        string bossName = bossNames[Random.Range(0, bossNames.Length)];
        SpawnMonsterWithAvatar(bossName, M_TurnManager.instance.targetObjectPosition[4], ObjectType.ENEMY, true, hazard);
    }

    // 몬스터/NPC + 타겟오브젝트(아바타) 공통 스폰 경로.
    // addToSyncList: 보스/일반 몬스터는 spawnedMonsterSyncList에도 추가(클라 인디케이터 생성), NPC는 로컬 리스트에만 추가
    // hazard: 몬스터별 위험도 보너스 스탯(MonsterStatDB) x 위험도 적용 — 거점 NPC는 0(가중치 없음)으로 스폰
    private SpawnedMonster SpawnMonsterWithAvatar(string monsterName, Vector3 position, ObjectType objectType, bool addToSyncList, int hazard = 0)
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
        spawned.monster = monsterData; // MonsterDB/MonsterStatDB 데이터 참조 (처치 경험치 등)
        spawned.hazard = hazard;
        spawned.MAXHP = monsterData.MAXHP + Mathf.RoundToInt(hazard * monsterData.hazardHpBonus);
        spawned.HP = spawned.MAXHP;
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

        // MonsterDB 상시 버프 행 적용 (Guardian SUHOJA 등) — user는 자기 자신
        foreach(Buff constantBuff in monsterData.buffList)
            targetObject.GainBuff(constantBuff.type, constantBuff.value, constantBuff.isDebuff, constantBuff.isInfinity, false, false, targetObject);
        return spawned;
    }
}
