using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class M_NetworkRoomManager : NetworkRoomManager
{
    [Header("RoomPlayerForUI Prefab")]
    public GameObject roomPlayerForUI; // RoomPlayer 클래스를 참조하는 UI용 오브젝트

    [Header("RoomPlayerForUI List")]
    public List<GameObject> listRoomPlayerForUI; // RoomPlayer 클래스를 참조하는 UI용 오브젝트 리스트

    [Header("Player Start Position")]
    Vector3 startPosition;


    // 모든 플레이어가 레디상태면 시작버튼 활성화 (called only server user)
    public override void OnRoomServerPlayersReady()
    {
        RoomUI.instance.buttonStart.gameObject.SetActive(true);
    }

    // 모든 플레이어가 레디상태가 아니면 시작버튼 비활성화 (called only server user)
    public override void OnRoomServerPlayersNotReady()
    {
        RoomUI.instance.buttonStart.gameObject.SetActive(false);
    }

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
        
        RoomUI.instance.gameObject.SetActive(false);
    }

    // 룸씬에서 게임씬으로 넘어갈때 룸씬의 플레이어 오브젝트와 게임씬의 플레이어 오브젝트의 정보들을 동기화
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        gamePlayer.GetComponent<GamePlayer>().character = roomPlayer.GetComponent<RoomPlayer>().character;
        return true;
    }

    // 룸씬에서 게임씬으로 넘어갈때 룸씬의 character값에 따라 오브젝트 분류 생성
    /*
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        GameObject instantiatedGameObject;
        switch(roomPlayer.GetComponent<RoomPlayer>().character)
        {
            case Character.GEORK :
                Debug.Log("GEORK 생성");
                instantiatedGameObject = Instantiate(spawnPrefabs[1],startPosition,Quaternion.identity);
                break;
            case Character.ERIS :
                Debug.Log("ERIS 생성");
                instantiatedGameObject = Instantiate(spawnPrefabs[2],startPosition,Quaternion.identity);
                break;
            case Character.HONGDANHYANG :
                Debug.Log("HONGDANHYANG 생성");
                instantiatedGameObject = Instantiate(spawnPrefabs[2],startPosition,Quaternion.identity);
                break;
            default :
                instantiatedGameObject = Instantiate(spawnPrefabs[1],startPosition,Quaternion.identity);
                break;
        }
        NetworkServer.Spawn(instantiatedGameObject, conn); 
        return instantiatedGameObject;
    }
    */
}
