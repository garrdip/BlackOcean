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
    //public TextMeshProUGUI readyButton;
    public Button ExitButton;
    public HorizontalLayoutGroup horizontalLayoutGroup;
    public List<GameObject> topIconList = new List<GameObject>();


    void Start()
    {
        buttonReady.onClick.AddListener(() => HandleRadeyState());
        ExitButton.onClick.AddListener(() => HandleBackToMainScene());
    }

    public void SetReadyButton(string str)
    {
        //readyButton.text = str;
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
                    //if(readyButton.text == "START" )HandleChangeGameScene();
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
        M_LoadingManager.instance.state = LOADING_STATE.SCENE_LOADING;
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.ServerChangeScene(M_NetworkRoomManager.GameplayScene);
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