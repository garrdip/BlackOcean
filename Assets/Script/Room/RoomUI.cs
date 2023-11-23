using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using ProjectD;
using DG.Tweening;

public class RoomUI : InstanceD<RoomUI>
{
    [Header("UI 컴포넌트")]
    public Button buttonReady;
    public Button ExitButton;
    public Button buttonOption;
    public TextMeshProUGUI textReady;
    public List<GameObject> topIcons = new List<GameObject>();
    public List<Image> topIconImages = new List<Image>();
    public List<Button> swapButtons = new List<Button>();

    [Header("스왑버튼 아이콘 이미지")]
    public Sprite topIconMy;
    public Sprite topIconMyLight;
    public Sprite topIconExChange;
    public Sprite topIconExChangeLight;
    public Sprite topIconReady;
    public Sprite topIconReadyLight;

    public GameObject optionCanvas;
    public GameObject readyCanvas;


    void Start()
    {
        DontDestroyOnLoad(optionCanvas);
        DontDestroyOnLoad(readyCanvas);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.components.Add(optionCanvas);
        networkRoomManager.components.Add(readyCanvas);
        for(int i=0; i<swapButtons.Count; i++){
            int buttonIndex = i; 
            swapButtons[i].onClick.AddListener(() => HandleLobbyPlayerSwap(buttonIndex));
        }
        ExitButton.onClick.AddListener(() => HandleBackToMainScene());
    }

    // 오더 스왑 제어
    public void HandleLobbyPlayerSwap(int swapTargetIndex)
    {
        int myIndex = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == M_LobbyMananger.instance.ownedLobbyPlayer);
        if(myIndex != swapTargetIndex){ // 로컬유저 본인에 대한 요청은 제외
            if( M_LobbyMananger.instance.lobbyPlayers[swapTargetIndex] == 0){
                M_LobbyMananger.instance.CmdSwapLobbyPlayer(myIndex, swapTargetIndex); // 스왑 타겟이 빈슬롯이면 해당 슬롯으로 이동되도록 요청
            }else{
                M_LobbyMananger.instance.CmdRequestSwap(myIndex, swapTargetIndex); // 스왑 타겟에 이미 로비플레이어가 있으면, 커맨드 전송하여 타겟에게 승인, 거절 UI활성화 되도록 요청
            }
        }
    }

    public void SetReadyButton(string str)
    {
        textReady.text = str;
    }

    public void HandleBackToMainScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
        NetworkServer.Shutdown();
        NetworkClient.Disconnect();
        M_SteamManager.LeaveLobby();
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