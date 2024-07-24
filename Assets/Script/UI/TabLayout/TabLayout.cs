using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;

public class TabLayout : MonoBehaviour
{
    public List<GameObject> tabFrames = new List<GameObject>();
    public List<Button> tabButtons = new List<Button>();
    public int currentIndex = 0;
    public Sprite georkIcon;
    public Sprite danhyangIcon;
    public Sprite erisIcon;


    void Awake()
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.onClientDisconnected += OnClientDisconnected;
    }

    void Start()
    {
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i; // C# 에서 람다식 클로저
            tabButtons[i].onClick.AddListener(() => ShowTab(buttonIndex));
        }
        SetTabButtonByOwnedPlayersCount();
    }

    // 클라이언트 연결 해제 이벤트 수신
    public void OnClientDisconnected(GamePlayer gamePlayer)
    {
        SetTabButtonByOwnedPlayersCount();
    }

    // 제어하는 플레이어 수에 따라 탭 버튼 활성화 상태 변경
    public void SetTabButtonByOwnedPlayersCount()
    {
        HideAllTabButtons();
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        if(playerInterface.ownedPlayers.Count > 1){ // 제어할 플레이어가 2명 이상이면
            for(int i=0; i<playerInterface.ownedPlayers.Count; i++){
                GamePlayer gamePlayer = playerInterface.ownedPlayers[i];
                int index = gamePlayer.selectOrder;
                tabButtons[index].gameObject.SetActive(true); // 플레이어 수만큼 탭버튼 활성화
                if(gamePlayer.netId == playerInterface.currentGamePlayerNetId){
                    tabButtons[index].GetComponent<CanvasGroup>().alpha = 1f; // 제어할 플레이어중 현재 플레이어의 탭버튼 알파값 1 설정
                }
                switch(gamePlayer.character)
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
        }else{ // 제어할 플레이어가 1명인 경우
            GamePlayer gamePlayer = playerInterface.currentGamePlayer.GetComponent<GamePlayer>();
            ShowTab(gamePlayer.selectOrder);
        }
    }

    // 선택한 탭 활성화
    public void ShowTab(int index)
    {
        currentIndex = index;
        tabFrames[index].SetActive(true);
        tabButtons[index].GetComponent<CanvasGroup>().alpha = 1f;
        HideOtherTabs(index);
    }

    // 선택한 탭을 제외한 다른 탭 비활성화
    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].GetComponent<CanvasGroup>().alpha = 0.5f;
                tabFrames[i].SetActive(false);
            }
        }
    }

    // 모든 탭버튼 비활성화
    public void HideAllTabButtons()
    {
        foreach(Button tabButton in tabButtons){
            tabButton.gameObject.SetActive(false);
            tabButton.GetComponent<CanvasGroup>().alpha = 0.5f;
        }
    }

    // 현재 활성화된 탭에 해당하는 플레이어의 GamePlayerDeck을 조회하여 반환
    // - 카드 강화 및 제거 팝업은 MercuriusPopUp을 통해서 넘어오는 UX임. 
    // - 다수의 플레이어 조종 시 MercuriusPopUp의 currentIndex를 통해 현재 어떤 플레이어의 카드 강화 및 제거를 수행하는지 조회하여 기능 수행.
    public GamePlayerDeck GetSelectedGamePlayerDeck()
    {
        uint netId = M_TurnManager.instance.playerOrder[currentIndex];
        if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
            GamePlayerDeck gamePlayerDeck = networkIdentity.GetComponent<GamePlayerDeck>();
            return gamePlayerDeck;
        }
        return null;
    }
}
