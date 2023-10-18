using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;
using Mirror;

public class M_NetworkRoomManager : NetworkRoomManager
{
    public delegate void OnClientDisconnected(GamePlayer gamePlayer);
    public OnClientDisconnected onClientDisconnected;
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
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject playerInteface)
    {
        playerInteface.GetComponent<PlayerInterface>().character = roomPlayer.GetComponent<RoomPlayer>().character;
        playerInteface.GetComponent<PlayerInterface>().selectOrder = (int)roomPlayer.GetComponent<RoomPlayer>().order; //int => PlayOder Type으로 변경 필요
        playerInteface.GetComponent<PlayerInterface>().steamPersonaName = roomPlayer.GetComponent<RoomPlayer>().steamPersonaName;
        playerInteface.GetComponent<PlayerInterface>().steamID = roomPlayer.GetComponent<RoomPlayer>().steamID;
        playerInteface.GetComponent<PlayerInterface>().color = roomPlayer.GetComponent<RoomPlayer>().color;
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
        }
        // Play Order 겹치지 않도록 새로운 RoomPlayer에게 배정
        for(int i = 0 ;i < 3 ;i ++)
        for(int j = 0 ; j < roomPlayers.Length ; j++)
        {
            if(roomPlayers[j].order == (PlayOrder)i)
                break;
            else if(j == roomPlayers.Length - 1){
                roomPlayer.GetComponent<RoomPlayer>().order = (PlayOrder)i;
                i = 3;
                break;
            }
        }
        roomPlayer.GetComponent<RoomPlayer>().color = colors[clientIndex - 1];

        return roomPlayer;
    }
    
    // 서버에서 클라이언트가 연결해제 되었을때 호출
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        AssignAuthorityFromDisconnectClientToServer(conn);
        BroadCastToClientDisconnected();

        base.OnServerDisconnect(conn);
    }

    // 게임에서 나간 클라이언트 소유의 오브젝트 권한을 서버로 이전
    private void AssignAuthorityFromDisconnectClientToServer(NetworkConnectionToClient conn)
    {
         if(Utils.IsSceneActive(GameplayScene)){
            if(NetworkClient.connection != null && NetworkClient.active && NetworkClient.connection.identity != conn.identity){ // 서버 본인이 나갈때는 이벤트 전송 X
                PlayerInterface serverPlayer = NetworkServer.spawned[NetworkClient.connection.identity.netId].GetComponent<PlayerInterface>(); // 서버 플레이어
                PlayerInterface disconnectedPlayer = NetworkServer.spawned[conn.identity.netId].GetComponent<PlayerInterface>(); // 방 나간 플레이어
                GamePlayer disconnectedGamePlayer = NetworkServer.spawned[disconnectedPlayer.currentGamePlayerNetId].GetComponent<GamePlayer>(); // 방 나간 플레이어의 GamePlayer 컴포넌트
                disconnectedGamePlayer.objectOwner = serverPlayer; // 방 나간 플레이어의 GamePlayer 오브젝트의 부모 오브젝트를 서버 플레이어의 PlayerInterface로 변경
                serverPlayer.ownedPlayers.Add(disconnectedGamePlayer); // 서버플레이어의 ownedPlayers SyncList에 방 나간 플레이어 추가

                // 방 나간 클라이언트 소유의 오브젝트들 중 플레이어 오브젝트를 제외한 모든 오브젝트의 권한을 서버에게 이전
                HashSet<NetworkIdentity> copyHashSet = new HashSet<NetworkIdentity>(conn.owned);
                foreach(NetworkIdentity networkIdentity in copyHashSet){
                    if(networkIdentity.GetComponent<RoomPlayer>() == null && networkIdentity.GetComponent<PlayerInterface>() == null){
                        networkIdentity.RemoveClientAuthority();
                        networkIdentity.AssignClientAuthority(NetworkClient.connection.identity.connectionToClient);
                    }
                }
                OnClientDisconnectFromServer(disconnectedGamePlayer); // 클라 연결 해제 델리게이트 구독한 컴포넌트에 이벤트 전송
            }
        }
    }

    // 접속한 모든 클라에게 어떤 클라이언트가 나갔는지 전송
    private void BroadCastToClientDisconnected()
    {
        if(Utils.IsSceneActive(RoomScene)){
            foreach(NetworkConnectionToClient connectionToClient in NetworkServer.connections.Values){
                RoomPlayer roomPlayer = NetworkServer.spawned[connectionToClient.identity.netId].GetComponent<RoomPlayer>();
                roomPlayer.RpcOtherPlayerDisconnected(connectionToClient, roomPlayer);
            }
        }else if(Utils.IsSceneActive(GameplayScene)){
            foreach(NetworkConnectionToClient connectionToClient in NetworkServer.connections.Values){
                PlayerInterface playerInterface = NetworkServer.spawned[connectionToClient.identity.netId].GetComponent<PlayerInterface>();
                PlayerInterfaceServer playerInterfaceServer = playerInterface.GetComponent<PlayerInterfaceServer>();
                playerInterfaceServer.RpcOtherPlayerDisconnected(connectionToClient, playerInterface);
            }
        }
    }

    // 클라연결 끊어지면 컴포넌트들에 델리게이트 이벤트 전송
    private void OnClientDisconnectFromServer(GamePlayer gamePlayer)
    {
        if(onClientDisconnected != null){
            onClientDisconnected.Invoke(gamePlayer);
        }
    }
}
