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


    void Start()
    {
        buttonReady.onClick.AddListener(() => HandleRadeyState());
        ExitButton.onClick.AddListener(() => HandleBackToMainScene());
        buttonOption.onClick.AddListener(() => HandleOPtionButtonClick());
        for(int i=0; i<swapButtons.Count; i++){
            int buttonIndex = i; 
            swapButtons[i].onClick.AddListener(() => HandleLobbyPlayerSwap(buttonIndex));
        }
    }

    // 오더 스왑 제어
    public void HandleLobbyPlayerSwap(int swapTargetIndex)
    {
        int ownedLobbyPlayer = M_LobbyMananger.instance.lobbyPlayers.FindIndex((netId) => netId == M_LobbyMananger.instance.ownedLobbyPlayer);
        M_LobbyMananger.instance.CmdSwapLobbyPlayer(ownedLobbyPlayer, swapTargetIndex);
    }

    public void SetReadyButton(string str)
    {
        textReady.text = str;
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
                    if(textReady.text == "START" )HandleChangeGameScene();
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

    // 옵션버튼 클릭
    public void HandleOPtionButtonClick()
    {
        OptionButton optionButton = buttonOption.GetComponent<OptionButton>();
        optionButton.isButtonClick = !optionButton.isButtonClick;
        if(optionButton.isButtonClick){
            optionButton.optionIconLight.gameObject.SetActive(true);
            optionButton.optionIconRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 90f), 0.3f);
            optionButton.optionIconLightRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 90f), 0.3f);
        }else{
            optionButton.optionIconLight.gameObject.SetActive(false);
            optionButton.optionIconRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 0f), 0.3f);
            optionButton.optionIconLightRect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, 0f), 0.3f);
        }
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