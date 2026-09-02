using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using ProjectD;


public class BattleResultPopUp : SingletonD<BattleResultPopUp>
{
    public CanvasGroup canvasGroup;
    public List<GameObject> titles = new List<GameObject>();
    public List<GameObject> tabs = new List<GameObject>();
    public List<VerticalLayoutGroup> rewardLayoutGroups = new List<VerticalLayoutGroup>(); // 전체 보상 목록 레이아웃 리스트
    public List<Button> tabButtons = new List<Button>();
    public List<Button> skipButtons = new List<Button>();
    public Sprite georkIcon;
    public Sprite danhyangIcon;
    public Sprite erisIcon;


    protected override void Awake()
    {
        PopUpUIManager.instance.onChangeBattleResultPopUpShow += OnChangeBattleResultPopUpShow;
        PopUpUIManager.instance.onChangeBattleResultPopUpHide += OnChangeBattleResultPopUpHide;
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.onClientDisconnected += OnClientDisconnected;
    }

    void Start()
    {
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i; // C# 에서 람다식 클로저
            tabButtons[i].onClick.AddListener(() => HandleChangeTab(buttonIndex));
        }
        for(int i=0; i<skipButtons.Count; i++){
            int buttonIndex = i;
            skipButtons[i].onClick.AddListener(() => SkipReward(buttonIndex));
        }
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void HandleChangeTab(int index)
    {
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        ChangeTab(index);
    }

    // 전투 보상 스킵 — 탭 index는 대열 슬롯(playerOrder 인덱스)이므로 해당 슬롯의 GamePlayer를 스킵 처리한다.
    // 팝업 닫기는 여기서 하지 않는다: 소유 파티원 전원의 보상이 끝나면 isRewardDone → PlayerInterface.OnCompleteReward가 닫는다.
    // (종전에는 스킵 즉시 닫아 3인 파티(싱글)에서 나머지 파티원 보상이 영영 완료되지 않아 방/거점 복귀가 멈췄음 — 2026-09-01 수정)
    private void SkipReward(int index)
    {
        skipButtons[index].interactable = false;
        skipButtons[index].image.color = new Color32(255, 255, 255, 255);
        GamePlayer gamePlayer = GetOwnedPlayerAtOrder(index);
        if(gamePlayer != null){
            gamePlayer.GetComponent<GamePlayerDeck>().CmdRewardClear();
            RewardService.instance.playerRewardedDic[gamePlayer] = true;
            RewardService.instance.CheckAllPlayerRewarded(gamePlayer);
            ShowNextPendingTab();
        }
        AudioClip audioClip = M_SoundManager.instance.GetSFXClip(SFX_TYPE.MainUI, "main_menu_mouseclick");
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 대열 슬롯(playerOrder 인덱스)의 GamePlayer — 내가 소유한 경우에만. 소유 플레이어가 1명이면 슬롯과 무관하게 그 플레이어
    GamePlayer GetOwnedPlayerAtOrder(int orderIndex)
    {
        PlayerInterface playerInterface = PlayerRegistry.Local;
        if(playerInterface == null) return null;
        if(playerInterface.ownedPlayers.Count == 1) return playerInterface.ownedPlayers[0];
        if(orderIndex < 0 || orderIndex >= M_TurnManager.instance.playerOrder.Count) return null;
        uint netId = M_TurnManager.instance.playerOrder[orderIndex];
        foreach(GamePlayer owned in playerInterface.ownedPlayers)
            if(owned != null && owned.netId == netId) return owned;
        return null;
    }

    // 아직 보상이 남은 소유 파티원의 탭으로 전환 (3인 파티 — 한 명 처리 후 다음 파티원 보상으로 안내). 남은 파티원이 없으면 아무 것도 하지 않는다
    public void ShowNextPendingTab()
    {
        if(!PopUpUIManager.instance.isBattleResultPopUpOpen) return;
        PlayerInterface playerInterface = PlayerRegistry.Local;
        if(playerInterface == null) return;
        for(int orderIndex = M_TurnManager.instance.playerOrder.Count - 1; orderIndex >= 0; orderIndex--){ // 전열 → 후열 순
            GamePlayer owned = GetOwnedPlayerAtOrder(orderIndex);
            if(owned == null) continue;
            if(RewardService.instance.playerRewardedDic.TryGetValue(owned, out bool rewarded) && rewarded) continue;
            if(owned.GetComponent<GamePlayerDeck>().rewards.Count == 0) continue;
            ChangeTab(orderIndex);
            return;
        }
    }

    // 클라이언트 연결 해제 이벤트 수신
    private void OnClientDisconnected(PlayerInterface playerInterface, GamePlayer gamePlayer)
    {
        // 연결해제된 클라이언트의 보상이 남아있을 경우
        // 1. 보상 팝업이 활성화 상태의 경우 : 보상을 이어받을수 있도록, 나간 클라이언트의 데이터를 조회해서 다시 세팅
        // 2. 보상 팝업이 비활성화 상태의 경우 (이미 보상팝업 끝나고 페이드 된 상태) : 보상데이터 모두 클리어하고 맵화면으로 전환
        GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
        if(gamePlayerDeck.rewards.Count > 0){
            if(gameObject.activeSelf){
                // 기존의 보상 오브젝트 제거
                List<GameObject> disconnectPlayerRewards = RewardService.instance.rewardObjects.FindAll(rewardObject => rewardObject.GetComponent<RewardListItem>().reward.netId == gamePlayer.netId);
                foreach (GameObject rewardToRemove in disconnectPlayerRewards){
                    Destroy(rewardToRemove);
                }
                RewardService.instance.rewardObjects.RemoveAll(rewardObject => rewardObject.GetComponent<RewardListItem>().reward.netId == gamePlayer.netId);

                // 연결해제된 클라이언트의 보상데이터를 다시 조회하여 보상 오브젝트 세팅
                foreach(Reward reward in gamePlayerDeck.rewards){
                    int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);          
                    GameObject rewardListItemObject = Instantiate(PopUpUIManager.instance.RewardListItemPrefab);
                    RewardListItem rewardListItem = rewardListItemObject.GetComponent<RewardListItem>();
                    rewardListItem.reward = reward;
                    rewardListItem.rewardOwner = gamePlayer;
                    rewardListItem.transform.SetParent(rewardLayoutGroups[orderIndex].transform);
                    rewardListItem.transform.localScale = new Vector3(1, 1, 1);
                    RewardService.instance.rewardObjects.Add(rewardListItemObject);
                }
                RewardService.instance.playerRewardedDic.Add(gamePlayer, false);
                int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);
                tabButtons[index].gameObject.SetActive(true);
            }else{
                RewardService.instance.ClearRewardListItem();
                M_TurnManager.instance.NoneBattleEnd();
            }
        }
    }

    // 탭 변경
    public void ChangeTab(int index)
    {
        tabs[index].SetActive(true);
        tabButtons[index].GetComponent<CanvasGroup>().alpha = 1f;
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].GetComponent<CanvasGroup>().alpha = 0.5f;
                tabs[i].SetActive(false);
            }
        }
    }

    // 탭 레이아웃 상태 변경
    public void HideAllTabs(bool isActive)
    {
        foreach(GameObject tab in tabs){
            tab.SetActive(isActive);
        }
    }

    // 탭 버튼 상태 변경
    public void HideAllTabButtons(bool isActive)
    {
        foreach(Button tabButton in tabButtons){
            tabButton.gameObject.SetActive(isActive);
        }
    }

    // 스킵 버튼 상태 변경
    public void ChangeAllSkipButtonState(bool isActive)
    {
        foreach(Button skipButton in skipButtons){
            skipButton.interactable = isActive;
            skipButton.image.color = isActive ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 70);
        }
    }

    // 탭 버튼 아이콘 현재 캐릭터의 클래스 이미지로 세팅
    public void SetTabButtonIconByClass(Character character, int index)
    {
        switch(character)
        {
            case Character.GEORK:
                tabButtons[index].transform.GetChild(2).GetComponent<Image>().sprite = georkIcon;
                break;
            case Character.HONGDANHYANG:
                tabButtons[index].transform.GetChild(2).GetComponent<Image>().sprite = danhyangIcon;
                break;
            case Character.ERIS:
                tabButtons[index].transform.GetChild(2).GetComponent<Image>().sprite = erisIcon;
                break;
        }
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // BattleResultPopUp 활성화 콜백
    public void OnChangeBattleResultPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
    }

    // BattleResultPopUp 비활성화 콜백
    public void OnChangeBattleResultPopUpHide()
    {
        RewardService.instance.ClearRewardListItem();
        RewardService.instance.playerRewardedDic.Clear();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
        HideAllTabs(false);
        HideAllTabButtons(false);
        ChangeAllSkipButtonState(true);
    }
}
