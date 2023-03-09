using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class M_NetworkRoomManager : NetworkRoomManager
{
    // 모든 플레이어가 레디상태면 시작버튼 활성화 (called only server user)
    public override void OnRoomServerPlayersReady()
    {
        if(RoomUI.instance != null){
            RoomUI.instance.buttonStart.gameObject.SetActive(true);
        }
    }

    // 모든 플레이어가 레디상태가 아니면 시작버튼 비활성화 (called only server user)
    public override void OnRoomServerPlayersNotReady()
    {
        if(RoomUI.instance != null){
            RoomUI.instance.buttonStart.gameObject.SetActive(false);
        }
    }

    // 클라이언트에서 호출되는 씬 전환 이벤트 콜백함수
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);

        // 게임씬으로 넘어갈 경우 룸씬의 UI들 비활성화
        if(RoomUI.instance != null && newSceneName.Equals(GameplayScene)){
            RoomUI.instance.gameObject.SetActive(false);
        }
    }
}

