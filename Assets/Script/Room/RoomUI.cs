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
    public Button buttonReady;
    public TMP_InputField messageInput;
    public Scrollbar scrollbar;
    public TextMeshProUGUI chatMessage;
    public List<GameObject> orderEffector;

    public TextMeshProUGUI readyButton;
    public Button ExitButton;

    public void SetReadyButton(string str)
    {
        readyButton.text = str;
    }
    void Start()
    {
        buttonReady.onClick.AddListener(() => HandleRadeyState());
        ExitButton.onClick.AddListener(() => HandleBackToMainScene());
    }

    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.Return)){
            SendChatMessage(messageInput.text);
            messageInput.ActivateInputField();       
        }
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
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.ServerChangeScene(M_NetworkRoomManager.GameplayScene);
    }


    // 채팅 메시지 전송
    public void SendChatMessage(string input)
    {
        if (NetworkClient.connection != null && !string.IsNullOrWhiteSpace(messageInput.text)){
            RoomPlayer roomPlayer = NetworkClient.connection.identity.gameObject.GetComponent<RoomPlayer>();
            roomPlayer.CmdSend(messageInput.text.Trim());
            messageInput.ActivateInputField();
            messageInput.text = string.Empty;;
        }
    }

    // 채팅 메시지 추가
    public void AppendMessage(string playerName, string message)
    {
        chatMessage.text += playerName + " : " + message + "\n";
        StartCoroutine(ScrollToBottom());
    }

    // 스크롤 이동
    IEnumerator ScrollToBottom()
    {
        // it takes 2 frames for the UI to update ?!?!
        yield return null;
        yield return null;

        scrollbar.value = 0;
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