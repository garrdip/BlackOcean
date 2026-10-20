using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using AYellowpaper.SerializedCollections;

// 전투 보상 시스템 — 보상 분배(서버)와 보상 UI 상태 관리(클라).
// M_TurnManager에서 분리됨. 전투 종료 흐름 제어(BattleEnd/NoneBattleEnd)와 RPC는 M_TurnManager에 유지.
// 아이템 개편 Phase 7 (Q24·Q34·Q35·Q39·G4): 드랍은 처치한 몬스터마다 DropTableDB 테이블을 굴리고, 골드·드랍 굴림 횟수는 싱글 = 파티 1회 / 멀티 = 캐릭터마다.
// 멀티는 드랍 확률 ITEM_DROP_MULTIPLAYER_PERCENT(150%). 장비 등급은 EquipGradeDB(처치 등급 보너스·위험도 하한). 경험치는 캐릭터마다 전액 (기존).
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
        bool multiplayer = NetworkServer.connections.Count > 1;
        int battleExp = M_TurnManager.instance.ConsumeBattleExp();
        if(battleExp <= 0) battleExp = BalanceData.Get("BATTLE_REWARD_EXP", 20);
        if(multiplayer)
            battleExp = battleExp * BalanceData.Get("EXP_MULTIPLAYER_PERCENT", 150) / 100;

        // 위험도 보상 배율 (위험도 시스템) — 이번 전투 유효 위험도 1당 보상 % 증가 (BalanceDB), 경험치/골드에 적용
        int battleHazard = M_TurnManager.instance.currentBattleHazard;
        int rewardPercent = 100 + battleHazard * BalanceData.Get("HAZARD_REWARD_PERCENT_PER_LEVEL", 5);
        battleExp = battleExp * rewardPercent / 100;
        int rewardGold = BalanceData.Get("BATTLE_REWARD_GOLD", 10) * rewardPercent / 100;

        // 드랍 — 처치 기록(몬스터 이름·등급·테이블)마다 테이블 굴림. 멀티는 확률 배율
        List<(string name, MonsterGrade grade, string dropTable)> killed = M_TurnManager.instance.ConsumeKilledMonsters();
        int dropMultiplier = multiplayer ? BalanceData.Get("ITEM_DROP_MULTIPLAYER_PERCENT", 150) : 100;

        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values){
            PlayerInterface playerInterface = NetLookup.Server<PlayerInterface>(conn.identity.netId);
            if(playerInterface == null) continue;
            // 굴림 단위 (Q39·G4): 싱글 = 파티 1회 → 선택 중인 캐릭터(currentGamePlayer, 없으면 첫 캐릭터)가 받는다. 멀티 = 캐릭터마다
            GamePlayer singleReceiver = null;
            if(!multiplayer){
                singleReceiver = playerInterface.currentGamePlayer;
                if(singleReceiver == null) foreach(GamePlayer candidate in playerInterface.ownedPlayers) if(candidate != null){ singleReceiver = candidate; break; }
            }
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                if(gamePlayer == null) continue;
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                bool rollsHere = multiplayer || gamePlayer == singleReceiver;

                if(rollsHere){
                    // 골드 보상 — 수령 시 플레이어(접속자) 공유 골드에 가산 (Q35, GamePlayerDeck.CmdRewardRemove)
                    gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Gold, rewardGold = rewardGold });
                    // 드랍 — 인벤토리로 직행, 보상 목록에는 표시 전용 항목
                    foreach((string name, MonsterGrade grade, string dropTable) in killed){
                        string tableId = DropTableData.TableFor(dropTable, grade);
                        foreach(DropTableData.Drop drop in DropTableData.Roll(tableId, battleHazard, dropMultiplier)){
                            if(drop.isEquip){
                                EquipGrade equipGrade = EquipGradeData.RollGrade(grade, battleHazard);
                                EquipInstance item = gamePlayer.ServerAddEquip(drop.itemNo, equipGrade);
                                if(!item.IsValid) continue;
                                EquipData.Def def = EquipData.Get(drop.itemNo);
                                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Equip,
                                    rewardName = $"{(equipGrade == EquipGrade.NORMAL ? "" : "[" + EquipGradeData.DisplayName(equipGrade) + "] ")}{def.equipName}",
                                    rewardColorHex = EquipGradeData.Get(equipGrade)?.colorHex ?? "" });
                                Debug.Log($"[Reward] {gamePlayer.character} 드랍 ({name}/{tableId}): [{EquipGradeData.DisplayName(equipGrade)}] {def.equipName} 옵션 {item.OptionCount}개");
                            }else{
                                ConsumableData.Def potion = ConsumableData.Get(drop.itemNo);
                                if(potion == null) continue;
                                gamePlayer.ServerAddConsumable(drop.itemNo);
                                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Potion, rewardName = potion.potionName });
                                Debug.Log($"[Reward] {gamePlayer.character} 드랍 ({name}/{tableId}): {potion.potionName}");
                            }
                        }
                    }
                }

                // 경험치 보상 — 선택 없이 서버가 즉시 지급, 캐릭터마다 전액 (레벨업/스킬 포인트는 GamePlayer.AddExp)
                gamePlayer.AddExp(battleExp);

                // 플레이어 보상 상태 데이터 세팅 — 보상 목록이 있는 캐릭터만 (싱글은 수령 캐릭터 1명). 목록이 없는 캐릭터를 등록하면 보상 팝업이 그 탭을 끝낼 방법이 없어 멈춘다 (2026-10-20 플레이 검증)
                if(gamePlayerDeck.rewards.Count > 0)
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
