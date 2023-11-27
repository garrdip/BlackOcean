using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectD;
using Mirror;
using AYellowpaper.SerializedCollections;

public class M_NetworkRoomManager : NetworkRoomManager
{
    public delegate void OnClientDisconnected(GamePlayer gamePlayer);
    public OnClientDisconnected onClientDisconnected;
    public Color[] colors = new Color[]{ Color.red, Color.green, Color.blue };

    [Header("DDOL 매니저 오브젝트")]
    [SerializedDictionary("Name", "Manager")]
    public SerializedDictionary<string, GameObject> persistentManagers = new SerializedDictionary<string, GameObject>(); // 네트워크 매니저에서 관리할 NetworkBehaviour 매니저클래스 오브젝트 목록

    [Header("DDOL 컴포넌트 오브젝트")]
    [SerializedDictionary("Name", "Component")]
    public SerializedDictionary<string, GameObject> persistentComponents = new SerializedDictionary<string, GameObject>(); // 네트워크 매니저에서 관리할 뷰 컴포넌트 오브젝트 목록

    public override void Awake()
    {
        base.Awake(); // 부모클래스인 NetworkRoomManager의 Awake로직은 유지
        SceneManager.activeSceneChanged += OnChangedActiveScene; // 씬 전환 이벤트 연결
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
        GameObject roomPlayer = Instantiate(roomPlayerPrefab.gameObject);
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
        roomPlayer.GetComponent<RoomPlayer>().color = colors[(int)roomPlayer.GetComponent<RoomPlayer>().order];
        NetworkServer.Spawn(roomPlayer, conn);

        // RoomPlayer 정보를 참조하는 LobbyPlayer 오브젝트 생성 
        GameObject lobbyPlayerObject = Instantiate(netManger.spawnPrefabs.Find(pref => pref.name == "LobbyPlayer"));

        // LobbyPlayer에 RoomPlayer SyncVar 변수 설정
        LobbyPlayer lobbyPlayer = lobbyPlayerObject.GetComponent<LobbyPlayer>();
        lobbyPlayer.roomPlayer = roomPlayer.GetComponent<RoomPlayer>();
        if(M_LobbyMananger.instance.lobbyPlayersCount == 0){
            lobbyPlayer.isHostLobbyPlayer = true;
        }
        NetworkServer.Spawn(lobbyPlayerObject, conn);

        // 로비플레이어 리스트에 추가
        M_LobbyMananger.instance.AddLobbyPlayer(lobbyPlayer.netId);
        return roomPlayer;
    }
    
    // 서버에서 클라이언트가 연결해제 되었을때 호출
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if(NetworkClient.active && NetworkClient.connection != null && NetworkClient.connection.identity != conn.identity){
            AssignAuthorityFromDisconnectClientToServer(conn);
            BroadCastToClientDisconnected(conn);
        }

        base.OnServerDisconnect(conn);
    }

    // 게임에서 나간 클라이언트 소유의 오브젝트 권한을 서버로 이전
    private void AssignAuthorityFromDisconnectClientToServer(NetworkConnectionToClient conn)
    {
        if(Utils.IsSceneActive(GameplayScene)){
            PlayerInterface serverPlayer = NetworkServer.spawned[NetworkClient.connection.identity.netId].GetComponent<PlayerInterface>(); // 서버 플레이어
            PlayerInterface disconnectedPlayer = NetworkServer.spawned[conn.identity.netId].GetComponent<PlayerInterface>(); // 방 나간 플레이어
            GamePlayer disconnectedGamePlayer = NetworkServer.spawned[disconnectedPlayer.currentGamePlayerNetId].GetComponent<GamePlayer>(); // 방 나간 플레이어의 GamePlayer 컴포넌트
            disconnectedGamePlayer.objectOwner = serverPlayer; // 방 나간 플레이어의 GamePlayer 오브젝트의 부모 오브젝트를 서버 플레이어의 PlayerInterface로 변경
            serverPlayer.ownedPlayers.Add(disconnectedGamePlayer); // 서버플레이어의 ownedPlayers SyncList에 방 나간 플레이어 추가

            // 방 나간 클라이언트 소유의 오브젝트들 중 플레이어 오브젝트를 제외한 모든 오브젝트의 권한을 서버에게 이전
            HashSet<NetworkIdentity> copyHashSet = new HashSet<NetworkIdentity>(conn.owned);
            foreach(NetworkIdentity networkIdentity in copyHashSet){
                if(networkIdentity.GetComponent<RoomPlayer>() == null && networkIdentity.GetComponent<PlayerInterface>() == null){
                    AssignMapPlayerInterfaceNetId(networkIdentity);
                    networkIdentity.RemoveClientAuthority();
                    networkIdentity.AssignClientAuthority(NetworkClient.connection.identity.connectionToClient);
                }
            }
            OnClientDisconnectFromServer(disconnectedGamePlayer); // 클라 연결 해제 델리게이트 구독한 컴포넌트에 이벤트 전송
        }
    }

    // 접속한 모든 클라에게 어떤 클라이언트가 나갔는지 전송
    private void BroadCastToClientDisconnected(NetworkConnectionToClient conn)
    {
        if(Utils.IsSceneActive(RoomScene)){
            RoomPlayer oldRoomPlayer = NetworkServer.spawned[conn.identity.netId].GetComponent<RoomPlayer>();
            RoomPlayer newRoomPlayer = NetworkServer.spawned[NetworkClient.connection.identity.netId].GetComponent<RoomPlayer>();
            M_MessageManager.instance.RpcOtherPlayerDisconnectedInRoomScene(oldRoomPlayer.steamPersonaName, newRoomPlayer.steamPersonaName);
        }else if(Utils.IsSceneActive(GameplayScene)){
            PlayerInterface oldPlayerInterface = NetworkServer.spawned[conn.identity.netId].GetComponent<PlayerInterface>();
            PlayerInterface newPlayerInterface = NetworkServer.spawned[NetworkClient.connection.identity.netId].GetComponent<PlayerInterface>();
            M_MessageManager.instance.RpcOtherPlayerDisconnectedInGameScene(oldPlayerInterface.steamPersonaName, newPlayerInterface.steamPersonaName);
        }
    }

    // 클라연결 끊어지면 컴포넌트들에 델리게이트 이벤트 전송
    private void OnClientDisconnectFromServer(GamePlayer gamePlayer)
    {
        if(onClientDisconnected != null){
            onClientDisconnected.Invoke(gamePlayer);
        }
    }

    // MapPlayerPiece와 MapPlayerDestination에 SyncVar 참조변수로 있는 playerIntefaceNetId값을 서버의 NetId로 변경
    private void AssignMapPlayerInterfaceNetId(NetworkIdentity networkIdentity)
    {
        if(networkIdentity.GetComponent<MapPlayerPiece>() != null){
            networkIdentity.GetComponent<MapPlayerPiece>().playerIntefaceNetId = NetworkClient.connection.identity.netId;
        }
        if(networkIdentity.GetComponent<MapPlayerDestination>() != null){
            networkIdentity.GetComponent<MapPlayerDestination>().playerIntefaceNetId = NetworkClient.connection.identity.netId;
        }
    }

    // 씬 전환 이벤트(Offline Scene에서도 씬 전환 이벤트 수신 가능)
    private void OnChangedActiveScene(Scene current, Scene next)
    {
        // 메뉴씬 갈때 managers에 할당되었던 DDOL 매니저 오브젝트들 + 뷰 컴포넌트들 모두 제거 
        if(next.name.Equals("MenuScene")){
            foreach(GameObject manager in persistentManagers.Values){
                Destroy(manager.gameObject);
            }
            persistentManagers.Clear();
            foreach(GameObject component in persistentComponents.Values){
                Destroy(component);
            }
            persistentComponents.Clear();
        }
    }
}
