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
    public Button ExitButton;
    public TextMeshProUGUI textReady;
    public List<GameObject> topIcons = new List<GameObject>();
    public List<Button> swapButtons = new List<Button>();
    public Button readyButton;



    void Start()
    {
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
        if(num == players.Length - 1){
            RoomUI.instance.SetReadyButton("START");
            ReadyButtonOnRoom readyButtonOnRoom = readyButton.GetComponent<ReadyButtonOnRoom>();
            readyButtonOnRoom.SetReadyButtonViewByReadyState(true);
        }else{
            RoomUI.instance.SetReadyButton("");
            ReadyButtonOnRoom readyButtonOnRoom = readyButton.GetComponent<ReadyButtonOnRoom>();
            readyButtonOnRoom.SetReadyButtonViewByReadyState(false);
        }
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

    // 스왑버튼 아이콘 변경 : LobbyPlayer의 상태값에 따라
    public void ChangeSwapButtonsIconState()
    {
        for(int i=0; i<swapButtons.Count; i++){
            SwapButtonOnRoom swapButtonOnRoom = swapButtons[i].GetComponent<SwapButtonOnRoom>();
            uint netId = M_LobbyMananger.instance.lobbyPlayers[i];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                LobbyPlayer lobbyPlayer = networkIdentity.GetComponent<LobbyPlayer>();
                if(lobbyPlayer.roomPlayer.isReady){
                    swapButtonOnRoom.topR.SetActive(true);
                    swapButtonOnRoom.topC.SetActive(false);
                    swapButtonOnRoom.topCLight.SetActive(false);
                    swapButtonOnRoom.topMy.SetActive(false);
                    swapButtonOnRoom.topMyLight.SetActive(false);
                }else{
                    if(lobbyPlayer.roomPlayer.isOwned){
                        swapButtonOnRoom.topMy.SetActive(true);
                        swapButtonOnRoom.topC.SetActive(false);
                        swapButtonOnRoom.topCLight.SetActive(false);
                        swapButtonOnRoom.topR.SetActive(false);
                        swapButtonOnRoom.topRLight.SetActive(false);
                    }else{
                        swapButtonOnRoom.topC.SetActive(true);
                        swapButtonOnRoom.topMy.SetActive(false);
                        swapButtonOnRoom.topMyLight.SetActive(false);
                        swapButtonOnRoom.topR.SetActive(false);
                        swapButtonOnRoom.topRLight.SetActive(false);
                    }
                }
            }else{
                swapButtonOnRoom.topC.SetActive(true);
                swapButtonOnRoom.topMy.SetActive(false);
                swapButtonOnRoom.topMyLight.SetActive(false);
                swapButtonOnRoom.topR.SetActive(false);
                swapButtonOnRoom.topRLight.SetActive(false);
            }
        }
    }
}