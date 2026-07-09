using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using AYellowpaper.SerializedCollections;

// 전투 보상 시스템 — 보상 분배(서버)와 보상 UI 상태 관리(클라).
// M_TurnManager에서 분리됨. 전투 종료 흐름 제어(BattleEnd/NoneBattleEnd)와 RPC는 M_TurnManager에 유지.
public class RewardService : InstanceD<RewardService>
{
    [SerializedDictionary("게임플레이어", "보상카드선택유무")]
    public SerializedDictionary<GamePlayer, bool> playerRewardedDic = new SerializedDictionary<GamePlayer, bool>();

    public List<GameObject> rewardObjects = new List<GameObject>(); // 보상목록 오브젝트 리스트
    public List<GameObject> rewardCardObjects = new List<GameObject>(); // 보상카드 오브젝트 리스트

    // 전투 종료시 플레이어들의 캐릭터별 보상카드 랜덤추출하여 각 플레이어들에게 전달 (서버 전용)
    public void DistributeBattleRewards()
    {
        if(!NetworkServer.active) return;
        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values){
            PlayerInterface playerInterface = NetLookup.Server<PlayerInterface>(conn.identity.netId);
            if(playerInterface == null) continue;
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();

                // TODO : 보상테이블 데이터 DB에서 조회해서 보상아이템 세팅(임시로 골드 + 카드 보상)
                string cardRewardGuid = System.Guid.NewGuid().ToString();
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = cardRewardGuid, reward_Type = Reward_Type.Card });
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Gold, rewardGold = 10 });

                // 카드 보상 데이터 세팅
                int rewardCardCount = gamePlayerDeck.maxRewardCardCount; // 플레이어별로 설정된 보상 카드 최대 갯수
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < rewardCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        Card rewardCard = cardsByCharacter[randomIndex].CardDeepCopy(false);
                        rewardCard.guid = cardRewardGuid;
                        gamePlayerDeck.rewardCards.Add(rewardCard);
                        cardsByCharacter.RemoveAt(randomIndex);
                    }
                }
                // 플레이어 보상 상태 데이터 세팅
                gamePlayerDeck.TargetPlayerRewarded(gamePlayerDeck.GetComponent<NetworkIdentity>().connectionToClient);

                // 플레이어의 모든 카드 데이터 제거
                gamePlayerDeck.trashDeck.Clear();
                gamePlayerDeck.prefareDeck.Clear();
                gamePlayerDeck.forgottenDeck.Clear();

                //코스트 리셋
                gamePlayerDeck.maxIchi = 3;
                gamePlayerDeck.currentIchi = 3;

                //해방 카드를 위한 카드 카운팅 종료
                gamePlayerDeck.numOfUsedCard = 0;

                //저주카드 획득량 제거
                gamePlayerDeck.gainCurseCardCount = 0;

                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    NetworkServer.Destroy(cardOnHand.gameObject);
                }
                gamePlayerDeck.cardOnHands.Clear();
            }
        }
    }

    // 소유한 모든 플레이어가 보상 카드 받았는지 체크
    public void CheckAllPlayerRewarded(GamePlayer gamePlayer)
    {
        if(!playerRewardedDic.ContainsValue(false) && gamePlayer.isOwned){ // 소유한 모든 플레이어 보상받았으면 종료
            PlayerRegistry.Local.isRewardDone = true;
            gamePlayer.GetComponent<GamePlayerDeck>().CmdClearRewardCards();
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

    // 보상 카드 오브젝트 제거 및 플레이어 보상 상태 데이터 정리
    public void ClearRewardCardAndPlayer()
    {
        foreach(GameObject rewardCardObject in rewardCardObjects){
            Destroy(rewardCardObject);
        }
        rewardCardObjects.Clear();
    }
}
