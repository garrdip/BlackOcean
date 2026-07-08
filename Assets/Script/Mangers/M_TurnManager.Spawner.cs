using UnityEngine;
using Mirror;
using ProjectD;


// M_TurnManager partial — 전투 진입 오케스트레이션.
// 실제 스폰 로직은 BattleSpawner로 분리됨. 이 파일은 스폰 + RPC 연출 + 대기 코루틴의 흐름만 담당.
public partial class M_TurnManager
{
    [Server]
    public void GenerateBattleObject(HexagonMapRoom hexagonMapRoom)
    {
        BattleSpawner.instance.GeneratePlayerUnit();
        if(hexagonMapRoom.roomType == RoomType.BOSS){ // 보스 몬스터 생성
            BattleSpawner.instance.GenerateBossMonster();
            RpcCardPrefareForBattle();
            RpcStartBossBattleEvent();
        }else if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE){ // 일반 or 엘리트 몬스터 생성
            BattleSpawner.instance.GenerateMonster(hexagonMapRoom);
            RpcCardPrefareForBattle();
            RpcStartBattleEvent(hexagonMapRoom.roomType);
        }else{ // NPC 생성
            BattleSpawner.instance.GnenrateNPCByRoomTpye(hexagonMapRoom.roomType);
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
