using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;
using ProjectD;

public class ReadyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GameObject readyBaseL1;
    public GameObject readyBaseL2;
    public GameObject readyBaseL3;
    public GameObject readySBase;
    public GameObject readyS1;
    public GameObject readyS2;
    public TextMeshProUGUI textReady;


    public void OnPointerClick(PointerEventData pointerEventData)
    {
        HandleRadeyState();
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        readyBaseL1.SetActive(true);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        readyBaseL1.SetActive(false);
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

    // 게임씬 이동
    public void HandleChangeGameScene()
    {
        M_LoadingManager.instance.SetLoadingScreen(true);
        M_LoadingManager.instance.state = LOADING_STATE.SCENE_LOADING;
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        M_NetworkRoomManager.ServerChangeScene(M_NetworkRoomManager.GameplayScene);
    }
}
