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
    [Header("랜덤으로 추출한 카드 오브젝트 리스트")]
    public List<GameObject> extractCardObjects = new List<GameObject>();

    public CanvasGroup canvasGroup;
    public List<GameObject> tabs = new List<GameObject>();
    public List<HorizontalLayoutGroup> grids = new List<HorizontalLayoutGroup>();
    public List<Button> tabButtons = new List<Button>();
    public List<Button> skipButtons = new List<Button>();

    [SerializedDictionary("게임플레이어", "보상카드목록")]
    public SerializedDictionary<GamePlayer, List<Card>> playerRewardCardsDic = new SerializedDictionary<GamePlayer, List<Card>>();

    [SerializedDictionary("게임플레이어", "보상카드선택유무")]
    public SerializedDictionary<GamePlayer, bool> playerRewardedDic = new SerializedDictionary<GamePlayer, bool>();
 
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
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        List<GamePlayer> players = new List<GamePlayer>(playerInterface.ownedPlayers);
        if(players[index] != null){
            GamePlayer gamePlayer = players[index];
            playerRewardedDic[gamePlayer] = true;
            CheckAllPlayerRewarded(gamePlayer);
        }
    }

    // 소유한 모든 플레이어가 보상 카드 받았으면 팝업 닫고, 보상카드 데이터 비움
    public void CheckAllPlayerRewarded(GamePlayer gamePlayer)
    {
        if(!playerRewardedDic.ContainsValue(false)){ // 소유한 모든 플레이어 보상받았으면 종료
            PopUpUIManager.instance.HandleHideBattleResultPopUp(); // 전투 결과 팝업 비활성화
            GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
                NetworkClient.localPlayer.GetComponent<PlayerInterface>().isRewardDone = true; 
                gamePlayer.GetComponent<GamePlayerDeck>().CmdClearRewardCards();
            });
        }
    }

    // 클라이언트 연결 해제 이벤트 수신
    private void OnClientDisconnected(GamePlayer gamePlayer)
    {
        // 방 나간 플레이어의 보상카드 Synclist값을 가져와서 팝업에 세팅
        GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
        List<Card> rewardCards = new List<Card>(gamePlayerDeck.rewardCards);
        playerRewardCardsDic.Add(gamePlayer, rewardCards);
        playerRewardedDic.Add(gamePlayer, false);    
        RemoveResultCard();
        InitResultCard();
    }

    void OnDestroy()
    {
        DOTween.Kill(canvasGroup);
    }

    public void InitResultCard()
    {
        int index = 0;
        HideAllTabButtons();
        foreach(KeyValuePair<GamePlayer, List<Card>> pair in playerRewardCardsDic){
            GamePlayer gamePlayer = pair.Key;
            List<Card> rewardCards = pair.Value;
            CreateResultCard(rewardCards, index, gamePlayer);
            tabButtons[index].gameObject.SetActive(true);
            tabButtons[index].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gamePlayer.character.ToString();
            index++;
        }
    }

    // 보상 카드 오브젝트 생성
    public void CreateResultCard(List<Card> rewardCards, int index, GamePlayer cardOwner)
    {
        foreach(Card card in rewardCards){
            GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
            cardOnDeck.transform.SetParent(grids[index].transform);
            cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
            cardOnDeck.GetComponent<CardOnDeck>().card = card;
            cardOnDeck.GetComponent<CardOnDeck>().cardOwner = cardOwner;
            extractCardObjects.Add(cardOnDeck);
        }
    }

    // 탭 변경
    private void ChangeTab(int index)
    {
        tabs[index].SetActive(true);
        tabButtons[index].image.color = new Color32(255, 255, 255, 255);
        HideOtherTabs(index);
    }

    // 선택한 탭을 제외한 다른 탭 비활성화
    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].image.color = new Color32(255, 255, 255, 70);
                tabs[i].SetActive(false);
            }
        }
    }

    // 모든 탭버튼 비활성화
    public void HideAllTabButtons()
    {
        foreach(Button tabButton in tabButtons){
            tabButton.gameObject.SetActive(false);
        }
    }

    // 생성되었던 보상 카드들 제거
    private void RemoveResultCard()
    {
        foreach(GameObject gameObject in extractCardObjects){
            Destroy(gameObject);
        }
        extractCardObjects.Clear();
    }

    // -------------------------------------------------------------------  델리게이트 이벤트 콜백 함수 -------------------------------------------------------------------------- //

    // BattleResultPopUp 활성화 콜백
    public void OnChangeBattleResultPopUpShow()
    {
        canvasGroup.DOFade(1.0f, 0.5f);
        InitResultCard();
        M_CardManager.instance.RemoveAllCurrentPlayerArrow(); // 화살표 제거
        M_CardManager.instance.ChangeCurrentPlayerCardOnHandState(false); // 남아있는 CardOnHand 오브젝트들의 상태값 초기화
    }
    
    // BattleResultPopUp 비활성화 콜백
    public void OnChangeBattleResultPopUpHide()
    {
        RemoveResultCard();
        playerRewardCardsDic.Clear();
        playerRewardedDic.Clear();
        canvasGroup.DOFade(0.0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    } 
}
