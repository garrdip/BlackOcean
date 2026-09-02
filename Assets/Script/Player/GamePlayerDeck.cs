using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


// 플레이어별 전투 보상 목록 (골드 등) — GamePlayer 프리팹의 컴포넌트.
// 원래 카드 게임의 덱/패/이치 관리 컴포넌트였으나 카드 시스템 제거(2026-09-01)로 보상 목록만 남았다.
// 클래스 이름은 프리팹 컴포넌트 참조를 유지하기 위해 그대로 둔다 (이름 변경 시 GamePlayer 프리팹 재바인딩 필요).
public class GamePlayerDeck : NetworkBehaviour
{
    public readonly SyncList<Reward> rewards = new SyncList<Reward>(); // 전투 보상 전체 목록 (RewardService가 채우고, 수령/스킵 시 비운다)


    public override void OnStartClient()
    {
        rewards.Callback += OnRewardUpdated;
    }

    // ---------------------------------------------------------------------- Command -----------------------------------------------------------------//

    // 보상목록 Synclist 데이터에서 guid·종류가 같은 첫 reward를 제거 (골드는 수령 처리)
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

    // 보상목록 Synclist 요소 모두 제거 (스킵)
    [Command]
    public void CmdRewardClear()
    {
        rewards.Clear();
    }

    // ---------------------------------------------------------------------- TargetRpc -----------------------------------------------------------------//

    // 전투 보상 데이터 세팅 RPC 수신 — 보상 완료 집계 사전에 등록
    [TargetRpc]
    public void TargetPlayerRewarded(NetworkConnectionToClient target)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        if(!RewardService.instance.playerRewardedDic.ContainsKey(gamePlayer)){ // 키 중복 방지
            RewardService.instance.playerRewardedDic.Add(gamePlayer, false);
        }
    }

    // ---------------------------------------------------------------------- SyncList Callback -------------------------------------------------------------//

    // 전체 보상 리스트 콜백 — 보상 팝업의 해당 파티원 탭에 항목을 만들고, 전부 수령하면 완료 처리
    void OnRewardUpdated(SyncList<Reward>.Operation op, int index, Reward oldVal, Reward newVal)
    {
        switch (op)
        {
            case SyncList<Reward>.Operation.OP_ADD:
                BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
                GamePlayer gamePlayer = GetComponent<GamePlayer>();
                int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);
                GameObject rewardListItemObject = Instantiate(PopUpUIManager.instance.RewardListItemPrefab);
                RewardListItem rewardListItem = rewardListItemObject.GetComponent<RewardListItem>();
                rewardListItem.reward = newVal;
                rewardListItem.rewardOwner = gamePlayer;
                rewardListItem.transform.SetParent(battleResultPopUp.rewardLayoutGroups[orderIndex].transform);
                rewardListItem.transform.localScale = new Vector3(1, 1, 1);
                RewardService.instance.rewardObjects.Add(rewardListItemObject);
                // 탭 활성화 — 소유 파티원의 탭 버튼을 켜고, 아직 열린 탭이 없으면 이 탭을 연다 (3인 파티는 첫 파티원 탭, 나머지는 버튼만)
                if(isOwned && orderIndex >= 0){
                    battleResultPopUp.SetTabButtonIconByClass(gamePlayer.character, orderIndex);
                    battleResultPopUp.tabButtons[orderIndex].gameObject.SetActive(true);
                    if(battleResultPopUp.tabs.FindIndex(tab => tab.activeSelf) < 0) battleResultPopUp.ChangeTab(orderIndex);
                }
                break;
            case SyncList<Reward>.Operation.OP_REMOVEAT:
                if(isOwned && rewards.Count <= 0){
                    // 더 보상받을 데이터 없는 경우 보상완료상태 세팅 — 다른 소유 파티원(3인 파티)의 보상이 남았으면 그 탭으로 전환
                    RewardService.instance.playerRewardedDic[GetComponent<GamePlayer>()] = true;
                    RewardService.instance.CheckAllPlayerRewarded(GetComponent<GamePlayer>());
                    PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>().ShowNextPendingTab();
                }
                break;
        }
    }
}
