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


    public override void OnStartLocalPlayer()
    {
        CmdSpawnMapPlayerPiece(); // MapPlayerPiece 오브젝트 생성 서버 요청
        CmdSpawnMapPlayerDestination(); // MapPlayerDestination 오브젝트 생성 서버 요청
    }


    // 맵에서 사용될 플레이어 권한을 가진 삼각형 오브젝트 생성
    [Command]
    public void CmdSpawnMapPlayerPiece()
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
        mapPlayerPiece.GetComponent<MapPlayerPiece>().gamePlayer = GetComponent<GamePlayer>();  // 게임 플레이어 참조값 세팅
        currentMapPlayerPiece = mapPlayerPiece.GetComponent<MapPlayerPiece>(); // 자신소유의 mapPlayerPiece 참조값 세팅

        // 매니저의 리스트에 생성된 맵 플레이어 추가
        M_MapManager.instance.mapPlayerPieces.Add(mapPlayerPiece);
    }

    // 맵플레이어가 이동할 위치를 표시하는 오브젝트 생성
    [Command]
    public void CmdSpawnMapPlayerDestination()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject mapPlayerDestination = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerDestination"),
            Vector3.zero,
            Quaternion.identity
        );
        NetworkServer.Spawn(mapPlayerDestination, connectionToClient);

        mapPlayerDestination.GetComponent<MapPlayerDestination>().gamePlayer = GetComponent<GamePlayer>();   // 게임 플레이어 참조값 세팅
        currentMapPlayerDestination = mapPlayerDestination.GetComponent<MapPlayerDestination>(); // 자신소유의 currentMapPlayerDestination 참조값 세팅
    }


    // 맵 플레이어가 이동하려는 방 표시 오브젝트의 위치 변경 및 거리값 계산
    [Command]
    public void CmdChangeMapPlayerDestinationPosition(HexagonMapRoom endAt, Vector3 position, uint netId)
    {
        // MapPlayerDestination 오브젝트의 위치 변경
        currentMapPlayerDestinationPosition = position;
        
        // 시작지점은 CurretnRoom 또는 StartPosition
        HexagonMapRoom startAt = M_MapManager.instance.currentRoom ? M_MapManager.instance.currentRoom : M_MapManager.instance.hexagonMapRooms[0];
        
        // 거리 계산을 위해 시작지점과 끝지점 설정
        M_MapManager.instance.startAt = startAt;
        M_MapManager.instance.endAt = endAt;
        
        // 경로검색
        List<HexagonMapRoom> findPath = M_MapManager.instance.FindPath(M_MapManager.instance.startAt , M_MapManager.instance.endAt);
        if(findPath.Count > 0){
            RpcVisualizePath(findPath, netId); // 경로표시
        }else{
            RpcHidePath(netId); // 경로제거
        }
        
        // findPath 리스트의 카운트 = 거리값
        currentMapPlayerDestination.distanceFromCurrentCoordinate = findPath.Count;
    }

    // 검색된 경로를 표시하는 라인랜더러 랜더링
    [ClientRpc]
    public void RpcVisualizePath(List<HexagonMapRoom> findPath, uint netId)
    {   
        M_MapManager.instance.RemoveExistLineRenderer(netId); // 기존 경로 삭제
        M_MapManager.instance.RenderVisualizePath(findPath, netId); // 새 경로 랜더링
        currentMapPlayerDestination.imageDistanceCount.gameObject.SetActive(true);
    }

    // 검색된 경로를 표시하는 라인랜더러 및 카운트 마크 제거
    [ClientRpc]
    public void RpcHidePath(uint netId)
    {    
        M_MapManager.instance.RemoveExistLineRenderer(netId); // 기존 경로 삭제
        currentMapPlayerDestination.imageDistanceCount.gameObject.SetActive(false);
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
            currentMapPlayerDestination.gameObject.SetActive(true);
            currentMapPlayerDestination.transform.localPosition = newPosition;
            currentMapPlayerDestination.MoveBounce(oldPosition != newPosition);
        }
    }

}
