using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using TMPro;
using AYellowpaper.SerializedCollections;

public class BattleResultPopUp : SingletonD<BattleResultPopUp>
{
    public CanvasGroup canvasGroup;
    public List<GameObject> tabs = new List<GameObject>();
    public List<HorizontalLayoutGroup> grids = new List<HorizontalLayoutGroup>();
    public List<Button> tabButtons = new List<Button>();
    public List<Button> skipButtons = new List<Button>();


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
            tabButtons[i].onClick.AddListener(() => ChangeTab(buttonIndex));
        }
        for(int i=0; i<skipButtons.Count; i++){
            int buttonIndex = i;
            skipButtons[i].onClick.AddListener(() => SkipRewardCard(buttonIndex));
        }
    }

    // 보상카드 스킵
    private void SkipRewardCard(int index)
    {
        skipButtons[index].interactable = false;
        skipButtons[index].image.color = new Color32(255, 255, 255, 255);
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        List<GamePlayer> players = new List<GamePlayer>(playerInterface.ownedPlayers);
        if(players[index] != null){
            GamePlayer gamePlayer = players[index];
            M_TurnManager.instance.playerRewardedDic[gamePlayer] = true;
            M_TurnManager.instance.CheckAllPlayerRewarded(gamePlayer);
        }
    }

    // 클라이언트 연결 해제 이벤트 수신
    private void OnClientDisconnected(GamePlayer gamePlayer)
    {
        M_TurnManager.instance.playerRewardedDic.Add(gamePlayer, false);
        int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);
        tabButtons[index].gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    // 탭 변경
    public void ChangeTab(int index)
    {
        tabs[index].SetActive(true);
        tabButtons[index].image.color = new Color32(255, 255, 255, 255);
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].image.color = new Color32(255, 255, 255, 70);
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
        M_TurnManager.instance.ClearRewardCardAndPlayer();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
        HideAllTabs(false);
        HideAllTabButtons(false);
        ChangeAllSkipButtonState(true);
    } 
}
