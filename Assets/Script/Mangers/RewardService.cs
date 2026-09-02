using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using AYellowpaper.SerializedCollections;

// 전투 보상 시스템 — 보상 분배(서버)와 보상 UI 상태 관리(클라).
// M_TurnManager에서 분리됨. 전투 종료 흐름 제어(BattleEnd/NoneBattleEnd)와 RPC는 M_TurnManager에 유지.
public class RewardService : InstanceD<RewardService>
{
    [SerializedDictionary("게임플레이어", "보상수령완료유무")]
    public SerializedDictionary<GamePlayer, bool> playerRewardedDic = new SerializedDictionary<GamePlayer, bool>();

    public List<GameObject> rewardObjects = new List<GameObject>(); // 보상목록 오브젝트 리스트

    // 전투 종료시 각 플레이어에게 보상(골드·경험치·드랍) 지급 (서버 전용)
    public void DistributeBattleRewards()
    {
        if(!NetworkServer.active) return;
        // 경험치 = 이번 전투에서 처치한 몬스터의 경험치 합(MonsterStatDB Exp, M_TurnManager.battleExpPool). 합이 0이면 BalanceDB 폴백.
        // 파티원 각자에게 전액 지급(분배 아님), 멀티(2인 이상 접속) 시 배율 적용 (기획: 1.5배)
        int battleExp = M_TurnManager.instance.ConsumeBattleExp();
        if(battleExp <= 0) battleExp = BalanceData.Get("BATTLE_REWARD_EXP", 20);
        if(NetworkServer.connections.Count > 1)
            battleExp = battleExp * BalanceData.Get("EXP_MULTIPLAYER_PERCENT", 150) / 100;

        // 위험도 보상 배율 (위험도 시스템) — 이번 전투 유효 위험도 1당 보상 % 증가 (BalanceDB), 경험치/골드에 적용
        int battleHazard = M_TurnManager.instance.currentBattleHazard;
        int rewardPercent = 100 + battleHazard * BalanceData.Get("HAZARD_REWARD_PERCENT_PER_LEVEL", 5);
        battleExp = battleExp * rewardPercent / 100;
        int rewardGold = BalanceData.Get("BATTLE_REWARD_GOLD", 10) * rewardPercent / 100;

        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values){
            PlayerInterface playerInterface = NetLookup.Server<PlayerInterface>(conn.identity.netId);
            if(playerInterface == null) continue;
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();

                // TODO : 보상테이블 데이터 DB에서 조회해서 보상아이템 세팅 (임시로 골드 보상만)
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Gold, rewardGold = rewardGold });

                // 경험치 보상 — 선택 없이 서버가 즉시 지급 (레벨업/스킬 포인트는 GamePlayer.AddExp)
                // TODO(Phase 2): 보상 목록 UI에 EXP 항목 표시 (Reward_Type.Exp)
                gamePlayer.AddExp(battleExp);

                // 장비/소모품 드랍 — 확률 지급, 인벤토리로 직행 (Phase 4)
                // 장비는 위험도 하한선(EQUIP_DROP_MIN_HAZARD) 이상에서만 드랍 (위험도 시스템 — 파고들기 요소)
                if(battleHazard >= BalanceData.Get("EQUIP_DROP_MIN_HAZARD", 0)
                    && Random.Range(0, 100) < BalanceData.Get("EQUIP_DROP_PERCENT", 30))
                    gamePlayer.ServerAddRandomEquip();
                if(Random.Range(0, 100) < BalanceData.Get("POTION_DROP_PERCENT", 40))
                    gamePlayer.ServerAddRandomConsumable();

                // 플레이어 보상 상태 데이터 세팅
                gamePlayerDeck.TargetPlayerRewarded(gamePlayerDeck.GetComponent<NetworkIdentity>().connectionToClient);
            }
        }
    }

    // 소유한 모든 플레이어가 보상을 받았는지 체크
    public void CheckAllPlayerRewarded(GamePlayer gamePlayer)
    {
        if(!playerRewardedDic.ContainsValue(false) && gamePlayer.isOwned){ // 소유한 모든 플레이어 보상받았으면 종료
            PlayerRegistry.Local.isRewardDone = true;
        }
    }

    // 보상 목록 오브젝트 모두 제거
    public void ClearRewardListItem()
    {
        foreach(GameObject rewardObject in rewardObjects){
            Destroy(rewardObject);
        }
        rewardObjects.Clear();
    }

    // 보상 목록 오브젝트 단일 제거
    public void RemoveRewardListItem(GameObject rewardObject)
    {
        rewardObjects.Remove(rewardObject);
        Destroy(rewardObject);
    }
}
