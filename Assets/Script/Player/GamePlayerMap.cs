using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class GamePlayerMap : NetworkBehaviour
{
    [SyncVar]
    public MapPlayerPiece currentMapPlayerPiece; // 맵플레이어 오브젝트

    [SyncVar]
    public MapPlayerDestination currentMapPlayerDestination; // 맵플레이어가 이동할 방의 위치를 표시해주는 오브젝트

    [SyncVar (hook = nameof(OnChangeCurrentMapPlayerDestination))]
    public Vector3 currentMapPlayerDestinationPosition;


    public override void OnStartServer()
    {
        SpawnMapPlayerPiece();
        SpawnMapPlayerDestination();
    }

    // 맵에서 사용될 플레이어 권한을 가진 삼각형 오브젝트 생성
    [Server]
    public void SpawnMapPlayerPiece()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject mapPlayerPiece = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerPiece"),
            Vector3.zero,
            Quaternion.identity
        );
        NetworkServer.Spawn(mapPlayerPiece, connectionToClient);

        // 스팀아이디 값 세팅
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        mapPlayerPiece.GetComponent<MapPlayerPiece>().steamId =  SteamFriends.GetFriendPersonaName((CSteamID)gamePlayer.steamID);

        // 매니저의 리스트에 생성된 맵 플레이어 추가
        M_MapManager.instance.mapPlayerPieces.Add(mapPlayerPiece);
    }

    // 맵플레이어가 이동할 위치를 표시하는 오브젝트 생성
    [Server]
    public void SpawnMapPlayerDestination()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject mapPlayerDestination = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerDestination"),
            Vector3.zero,
            Quaternion.identity
        );
        NetworkServer.Spawn(mapPlayerDestination, connectionToClient);
    }


    // 맵 플레이어가 이동하려는 방의 위치를 알려주는 표시 변경
    [Command]
    public void CmdChangeMapPlayerDestinationPosition(HexagonMapRoom hexagonMapRoom, Vector3 position)
    {
        currentMapPlayerDestinationPosition = position;
    }


    // 맵플레이어가 선택한 MapRoom값을 Dictionary<NetworkIdentity, MapRoom> 형태로 저장
    [Command]
    public void CmdSelectHexagonMapRoom(HexagonMapRoom hexagonMapRoom, NetworkIdentity networkIdentity)
    {
        if(M_MapManager.instance.playerVoteHexagonMapRoom.ContainsKey(networkIdentity)){
            M_MapManager.instance.playerVoteHexagonMapRoom[networkIdentity] = hexagonMapRoom;
        }else{
            M_MapManager.instance.playerVoteHexagonMapRoom.Add(networkIdentity, hexagonMapRoom);
        }
    }
    
    // 생성된 MapPlayerPiece 참조값 세팅
    [Command]
    public void CmdSetOwnMapPlayerPiece(MapPlayerPiece mapPlayerPiece)
    {
        currentMapPlayerPiece = mapPlayerPiece;
        currentMapPlayerPiece.gamePlayer = GetComponent<GamePlayer>();  // 게임 플레이어 참조값 세팅
    }

    // 생성된 MapPlayerPiece 참조값 세팅
    [Command]
    public void CmdSetOwnMapPlayerDestination(MapPlayerDestination mapPlayerDestination)
    {
        currentMapPlayerDestination = mapPlayerDestination;
        currentMapPlayerDestination.gamePlayer = GetComponent<GamePlayer>();   // 게임 플레이어 참조값 세팅
    }

    // 맵 플레이어가 이동하려는 방의 위치를 알려주는 표시 변경 수신
    public void OnChangeCurrentMapPlayerDestination(Vector3 oldPosition, Vector3 newPosition)
    {
        if(currentMapPlayerDestination != null){
            currentMapPlayerDestination.transform.localPosition = newPosition;
            currentMapPlayerDestination.MoveBounce(oldPosition != newPosition);
        }
    }

}
