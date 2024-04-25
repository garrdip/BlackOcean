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
    public List<HorizontalLayoutGroup> rewardCardLayoutGroups = new List<HorizontalLayoutGroup>(); // 카드 보상 레이아웃 리스트
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
            skipButtons[i].onClick.AddListener(() => SkipRewardCard(buttonIndex));
        }
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void HandleChangeTab(int index)
    {
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
        ChangeTab(index);
    }

    // 보상카드 스킵
    private void SkipRewardCard(int index)
    {
        skipButtons[index].interactable = false;
        skipButtons[index].image.color = new Color32(255, 255, 255, 255);
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        List<GamePlayer> players = new List<GamePlayer>(playerInterface.ownedPlayers);
        int idx = players.Count == 1 ? 0 : index;
        GamePlayer gamePlayer = players[idx];
        if(gamePlayer != null){
            gamePlayer.GetComponent<GamePlayerDeck>().CmdRewardClear();
            M_TurnManager.instance.playerRewardedDic[gamePlayer] = true;
            M_TurnManager.instance.CheckAllPlayerRewarded(gamePlayer);
        }
        AudioClip audioClip = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("main_menu_mouseclick"));
        M_SoundManager.instance.PlaySFX(audioClip, audioClip.length);
    }

    // 클라이언트 연결 해제 이벤트 수신
    private void OnClientDisconnected(GamePlayer gamePlayer)
    {
        // 연결해제된 클라이언트의 보상이 남아있을 경우
        GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
        if(gamePlayerDeck.rewards.Count > 0){
            // 기존의 보상 오브젝트 제거
            List<GameObject> disconnectPlayerRewards = M_TurnManager.instance.rewardObjects.FindAll(rewardObject => rewardObject.GetComponent<RewardListItem>().reward.netId == gamePlayer.netId);
            foreach (GameObject rewardToRemove in disconnectPlayerRewards){
                Destroy(rewardToRemove);
            }
            M_TurnManager.instance.rewardObjects.RemoveAll(rewardObject => rewardObject.GetComponent<RewardListItem>().reward.netId == gamePlayer.netId);

            // 연결해제된 클라이언트의 보상데이터를 다시 조회하여 보상 오브젝트 세팅
            foreach(Reward reward in gamePlayerDeck.rewards){
                int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);          
                GameObject rewardListItemObject = Instantiate(PopUpUIManager.instance.RewardListItemPrefab);
                RewardListItem rewardListItem = rewardListItemObject.GetComponent<RewardListItem>();
                rewardListItem.reward = reward;
                rewardListItem.rewardOwner = gamePlayer;
                rewardListItem.transform.SetParent(rewardLayoutGroups[orderIndex].transform);
                rewardListItem.transform.localScale = new Vector3(1, 1, 1);
                M_TurnManager.instance.rewardObjects.Add(rewardListItemObject);
            }
            M_TurnManager.instance.playerRewardedDic.Add(gamePlayer, false);
            int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);
            tabButtons[index].gameObject.SetActive(true);
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

    // 카드 보상 레이아웃 상태 변경
    public void HidAllRewardCardLayouts(bool isActive)
    {
        foreach(HorizontalLayoutGroup horizontalLayoutGroup in rewardCardLayoutGroups){
            horizontalLayoutGroup.gameObject.SetActive(isActive);
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

    // 보상팝업 레이아웃 상태 변경(전체보상 목록, 카드보상 목록, 제목)
    public void ChangeRewardLayoutState(int index, bool isActive)
    {
        rewardCardLayoutGroups[index].gameObject.SetActive(isActive);
        titles[index].gameObject.SetActive(isActive);
        rewardLayoutGroups[index].gameObject.SetActive(!isActive);
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
        M_CardManager.instance.RemoveAllCurrentPlayerArrow(); // 화살표 제거
        M_CardManager.instance.ChangeCurrentPlayerCardOnHandState(false); // 남아있는 CardOnHand 오브젝트들의 상태값 초기화
    }
    
    // BattleResultPopUp 비활성화 콜백
    public void OnChangeBattleResultPopUpHide()
    {
        M_TurnManager.instance.ClearRewardListItem();
        M_TurnManager.instance.ClearRewardCardAndPlayer();
        M_TurnManager.instance.playerRewardedDic.Clear();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
        HideAllTabs(false);
        HideAllTabButtons(false);
        HidAllRewardCardLayouts(false);
        ChangeAllSkipButtonState(true);
    } 
}
