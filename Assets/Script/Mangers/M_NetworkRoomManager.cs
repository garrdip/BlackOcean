using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class M_NetworkRoomManager : NetworkRoomManager
{
    public Color[] colors = new Color[]{ Color.red, Color.green, Color.blue };

    // 클라이언트에서 호출되는 씬 전환 이벤트 콜백함수
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);

        // 게임씬으로 넘어갈 경우 룸씬의 UI들 비활성화
        if(newSceneName.Equals(GameplayScene)){
            RoomUI.instance.gameObject.SetActive(false);
        }
    }

    // 룸씬 진입 시 RoomUI 활성화
    public override void OnRoomClientEnter()
    {
        base.OnRoomClientEnter();
       
        RoomUI.instance.gameObject.SetActive(true);
    }

    // 룸씬에서 클라연결 종료 시 룸씬의 UI들 비활성화
    // OnClientChangeScene 콜백함수가 룸씬에서 클라연결 종료이벤트를 통해 메뉴씬으로 이동시에는 호출되지 않기때문에 클라연결 종료 시 메인화면으로 가는 경우에도 룸씬의 UI들 비활성화
    public override void OnRoomStopClient()
    {
        base.OnRoomStopClient();
        
        //RoomUI.instance.gameObject.SetActive(false);
    }


    // 룸씬에서 게임씬으로 넘어갈때 룸씬의 플레이어 오브젝트와 게임씬의 플레이어 오브젝트의 정보들을 동기화
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<GamePlayer>().character = roomPlayer.GetComponent<RoomPlayer>().character;
        gamePlayer.GetComponent<GamePlayer>().selectOrder = (int)roomPlayer.GetComponent<RoomPlayer>().order; //int => PlayOder Type으로 변경 필요
        gamePlayer.GetComponent<GamePlayer>().steamPersonaName = roomPlayer.GetComponent<RoomPlayer>().steamPersonaName;
        gamePlayer.GetComponent<GamePlayer>().steamID = roomPlayer.GetComponent<RoomPlayer>().steamID;
        gamePlayer.GetComponent<GamePlayer>().color = roomPlayer.GetComponent<RoomPlayer>().color;
        return true;
    }

    public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
    {
        NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        RoomPlayer[] roomPlayers = FindObjectsOfType<RoomPlayer>();
        GameObject roomPlayer = Instantiate(netManger.spawnPrefabs.Find(pref => pref.name == "RoomPlayer"));
        NetworkServer.Spawn(roomPlayer,conn);
        if(roomPlayers.Length == 0){
            roomPlayer.GetComponent<RoomPlayer>().order = PlayOrder.FIRST;
            roomPlayer.GetComponent<RoomPlayer>().color = colors[0];
        }
        // Play Order 겹치지 않도록 새로운 RoomPlayer에게 배정
        for(int i = 0 ;i < 3 ;i ++)
        for(int j = 0 ; j < roomPlayers.Length ; j++)
        {
            if(roomPlayers[j].order == (PlayOrder)i)
                break;
            else if(j == roomPlayers.Length - 1){
                roomPlayer.GetComponent<RoomPlayer>().order = (PlayOrder)i;
                roomPlayer.GetComponent<RoomPlayer>().color = colors[i];
                i = 3;
                break;
            }
        }
        return roomPlayer;
    }
}
