using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using ProjectD;
using Steamworks;

public class RoomUI : InstanceD<RoomUI>
{
    [Header("UI 컴포넌트")]
    public Button buttonReady;
    public TextMeshProUGUI readyButton;
    public Button ExitButton;

    [Header("플레이어 순서 표시용 컴포넌트 리스트")]
    public List<GameObject> orderEffector;

    public float scrollSpeed = 1f;

    public void SetReadyButton(string str)
    {
        readyButton.text = str;
    }
    void Start()
    {
        M_MessageManager.instance.chatContainer.SetActive(true); // 룸씬 진입시 채팅창 활성화
        M_MessageManager.instance.isChatBoxVisible = true;
        buttonReady.onClick.AddListener(() => HandleRadeyState());
        ExitButton.onClick.AddListener(() => HandleBackToMainScene());
    }

    // 레디 상태 제어 
    public void HandleRadeyState()
    {
        if (NetworkClient.connection != null){
            RoomPlayer roomPlayer = NetworkClient.connection.identity.gameObject.GetComponent<RoomPlayer>();
            if(roomPlayer.character != Character.NONE){
                if(!roomPlayer.isServer) //클라이언트만 레디
                    roomPlayer.isReady = !roomPlayer.isReady;
                else //서버 케이스
                {
                    if(readyButton.text == "START" )HandleChangeGameScene();
                }
            }
            
        }
    }
    public void HandleBackToMainScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
        NetworkServer.Shutdown();
        NetworkClient.Disconnect();
        M_SteamManager.LeaveLobby();
    }

    // 게임 씬으로 이동
    public void HandleChangeGameScene()
    {
        M_LoadingManager.instance.SetLoadingScreen(true);
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.ServerChangeScene(M_NetworkRoomManager.GameplayScene);
    }

    // 순서 바뀜에 따른 자리 표시
    public void SetActiveSelectedOrderMark(PlayOrder order)
    {
        orderEffector[0].SetActive((int)order == 0 ? true : false);
        orderEffector[1].SetActive((int)order == 1 ? true : false);
        orderEffector[2].SetActive((int)order == 2 ? true : false);
    }

    [Server]
    public void CMDReadyCheck()
    {
        int num = 0;
        RoomPlayer[] players = FindObjectsOfType<RoomPlayer>();
        for(int i = 0 ;i < players.Length ; i++)
        {
            if(players[i].isReady) num++;
            if(players[i].character == Character.NONE) num--;
        }
        if( num == players.Length - 1 ) RoomUI.instance.SetReadyButton("START");
        else RoomUI.instance.SetReadyButton("");
    }
}