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
    public List<Button> swapButtons = new List<Button>();

    [Header("옵션 버튼 캔버스")]
    public GameObject optionCanvas;

    [Header("레디 버튼 캔버스")]
    public GameObject readyCanvas;


    void Start()
    {
        DontDestroyOnLoad(optionCanvas);
        DontDestroyOnLoad(readyCanvas);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentComponents.Add(optionCanvas.name, optionCanvas);
        networkRoomManager.persistentComponents.Add(readyCanvas.name, readyCanvas);
        for(int i=0; i<swapButtons.Count; i++){
            int buttonIndex = i;
            swapButtons[i].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            swapButtons[i].transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            swapButtons[i].onClick.AddListener(() => HandleLobbyPlayerSwap(buttonIndex));
        }
        ExitButton.onClick.AddListener(() => HandleBackToMainScene());
    }

    void OnDestroy()
    {
        KillTweenSwapButtons();
    }

    // RoomUI에 있는 오브젝트에 등록된 트위닝 제거
    public void KillTweenSwapButtons()
    {
        foreach(Button swapButtons in swapButtons){
            swapButtons.GetComponent<CanvasGroup>().DOKill();
            swapButtons.GetComponent<RectTransform>().DOKill();
        }
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

    // 스왑버튼 상태 변경
    public void ChangeSwapButtonsState(uint netId, int index)
    {
        if(netId == 0){
            swapButtons[index].transform.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 0.5f);
            swapButtons[index].transform.DORotate(new Vector3(0f, 0f, 45f), 0.5f);
        }else{
            swapButtons[index].transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f);
            swapButtons[index].transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f);
        }  
    }
}