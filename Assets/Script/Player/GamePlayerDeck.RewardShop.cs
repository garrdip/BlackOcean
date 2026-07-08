using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


// GamePlayerDeck partial — 보상 및 상점 카드 커맨드
public partial class GamePlayerDeck
{

    // 보상목록 Synclist 데이터에서 netId값이 동일한 첫번째 reward 데이터를 검색해서 제거
    [Command]
    public void CmdRewardRemove(string guid, Reward_Type reward_Type)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        int index = rewards.FindIndex((reward) => reward.guid.Equals(guid) && reward.reward_Type == reward_Type);
        if(index != -1){
            if(reward_Type == Reward_Type.Gold){
                gamePlayer.gold += rewards[index].rewardGold; // 골드 보상인 경우 플레이어 소유 골드에 추가
            }
            rewards.RemoveAt(index);
        }
    }


    // 보상목록 Synclist 요소 모두 제거
    [Command]
    public void CmdRewardClear()
    {
        rewards.Clear();
    }


    // 보상카드 Synclist 요소 모두 제거
    [Command]
    public void CmdClearRewardCards()
    {
        rewardCards.Clear();
    }


    // 상점 카드 구매 요청 커맨드
    [Command]
    public void CmdPurchaseShopCard(string guid)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        int index = shopCards.FindIndex((c) => c.guid.Equals(guid));
        if(index != -1){
            Card purchasedCard = shopCards[index];
            purchasedCard.isSoldout = true;
            shopCards[index] = purchasedCard;
            gamePlayer.gold -= shopCards[index].cardPrice; // 구매한 플레이어가 소유한 골드에서 카드 가격만큼 감소
        }
    }

    
    // 전투 보상 데이터 세팅 RPC 수신
    [TargetRpc]
    public void TargetPlayerRewarded(NetworkConnectionToClient target)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        if(!RewardService.instance.playerRewardedDic.ContainsKey(gamePlayer)){ // 키 중복 방지
            RewardService.instance.playerRewardedDic.Add(gamePlayer, false);
        }
    }
}
