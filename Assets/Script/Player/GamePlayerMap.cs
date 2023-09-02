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

    
    // ------------------------------------------------------------------------------ Command Method ----------------------------------------------------------------------------//

    // 맵에서 사용될 플레이어 권한을 가진 삼각형 오브젝트 생성
    [Command]
    public void CmdSpawnMapPlayerPiece()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject mapPlayerPieceObject = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerPiece"),
            Vector3.zero,
            Quaternion.identity
        );

        MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        mapPlayerPiece.steamId =  SteamFriends.GetFriendPersonaName((CSteamID)gamePlayer.steamID); // 스팀아이디 값 세팅
        mapPlayerPiece.gamePlayer = gamePlayer; // 게임 플레이어 참조값 세팅
        currentMapPlayerPiece = mapPlayerPiece; // 자신소유의 mapPlayerPiece 참조값 세팅
        M_MapManager.instance.mapPlayerPieces.Add(mapPlayerPieceObject); // 매니저의 리스트에 생성된 맵 플레이어 추가

        NetworkServer.Spawn(mapPlayerPieceObject, connectionToClient);
    }

    // 맵플레이어가 이동할 위치를 표시하는 오브젝트 생성
    [Command]
    public void CmdSpawnMapPlayerDestination()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject mapPlayerDestinationObject = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerDestination"),
            Vector3.zero,
            Quaternion.identity
        );
        MapPlayerDestination mapPlayerDestination = mapPlayerDestinationObject.GetComponent<MapPlayerDestination>();
        mapPlayerDestination.gamePlayer = GetComponent<GamePlayer>(); // 게임 플레이어 참조값 세팅
        NetworkServer.Spawn(mapPlayerDestinationObject, connectionToClient);

        currentMapPlayerDestination = mapPlayerDestination; // 자신소유의 currentMapPlayerDestination 참조값 세팅
    }


    // 맵 플레이어가 이동하려는 방 표시 오브젝트의 위치 변경 및 거리값 계산 (서버 전용)
    [Command]
    public void CmdChangeMapPlayerDestinationPosition(HexagonMapRoom endAt, Vector3 position, NetworkIdentity networkIdentity)
    {
        if(currentMapPlayerDestination != null){
            // 맵에 보스 출현 시 1칸 이상 이동 불가
            if(M_MapManager.instance.mapBoss != null && M_MapManager.instance.GetDistanceFromCurrentCoordinate(endAt.coordinate) > 1){
                return;
            }

            // 거점지역인 경우 아직 비활성화 상태면 이동 불가
            if(endAt.isRegion && !endAt.isActive){
                return;
            }
     
            // 시작지점은 CurretnRoom 또는 StartPosition
            HexagonMapRoom startAt = M_MapManager.instance.currentRoom != null ? M_MapManager.instance.currentRoom : M_MapManager.instance.hexagonMapRooms[0];
     
            // MapPlayerDestination 초기 위치 설정
            currentMapPlayerDestinationPosition = position;
                    
            // 경로검색
            List<HexagonMapRoom> findPath = M_MapManager.instance.FindPath(startAt, endAt);
            if(findPath.Count > 0){
                currentMapPlayerDestinationPosition = findPath[findPath.Count-1].transform.position; // MapPlayerDestination 위치는 findPath 마지막 노드 위치
                RpcVisualizePath(startAt, findPath, networkIdentity.netId); // 경로표시
            }else{
                RpcHidePath(networkIdentity.netId); // 경로제거
            }
            
            // findPath 리스트의 카운트 = 거리값
            currentMapPlayerDestination.distanceFromCurrentCoordinate = findPath.Count;
        
            // 선택한 MapRoom 투표
            VoteHexagonMapRoom(endAt, netIdentity);
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

    // ------------------------------------------------------------------------------ Server Method ----------------------------------------------------------------------------//

    // 맵플레이어가 선택한 MapRoom값을 Dictionary<NetworkIdentity, MapRoom> 형태로 저장
    [Server]
    private void VoteHexagonMapRoom(HexagonMapRoom hexagonMapRoom, NetworkIdentity networkIdentity)
    {
        if(M_MapManager.instance.playerVoteHexagonMapRoom.ContainsKey(networkIdentity)){
            M_MapManager.instance.playerVoteHexagonMapRoom[networkIdentity] = hexagonMapRoom;
        }else{
            M_MapManager.instance.playerVoteHexagonMapRoom.Add(networkIdentity, hexagonMapRoom);
        }
    }

    // ------------------------------------------------------------------------------ Client Method -------------------------------------------------------------------------------//

    // 맵 플레이어가 이동하려는 방 표시 오브젝트의 위치 변경 및 거리값 계산 (로컬 클라이언트 전용)
    // : 네트워크 딜레이를 감안하여 로컬플레이어의 MapPlayerDestination의 위치이동 및 애니매이션은 서버요청과 별도로 로컬에서 수행.
    // : 각 플레이어는 [로컬 로직] + [커맨드를 통한 서버요청 로직]을 둘다 수행하지만 수신되는 이벤트에서 로컬유저 소유가 아닌 경우로 분기처리하여, 다른유저의 변경 이벤트만 뷰 업데이트 수행.
    [Client]
    public void ClientChangeMapPlayerDestinationPosition(HexagonMapRoom endAt, Vector3 position, NetworkIdentity networkIdentity)
    {
        if(currentMapPlayerDestination != null){
            // 맵에 보스 출현 시 1칸 이상 이동 불가
            if(M_MapManager.instance.mapBoss != null && M_MapManager.instance.GetDistanceFromCurrentCoordinate(endAt.coordinate) > 1){
                return;
            }

            // 거점지역인 경우 아직 비활성화 상태면 이동 불가
            if(endAt.isRegion && !endAt.isActive){
                return;
            } 

            // 시작지점은 CurretnRoom 또는 StartPosition
            HexagonMapRoom startAt = M_MapManager.instance.currentRoom != null ? M_MapManager.instance.currentRoom : NetworkClient.spawned[M_MapManager.instance.hexagonMapRoomNetIds[0]].GetComponent<HexagonMapRoom>();

            // MapPlayerDestination 활성화 및 초기 위치 설정
            currentMapPlayerDestination.gameObject.SetActive(true); 
            currentMapPlayerDestination.transform.localPosition = position;
            currentMapPlayerDestination.MoveBounce(true);

            // 경로검색
            List<HexagonMapRoom> findPath = M_MapManager.instance.FindPath(startAt, endAt);
            if(findPath.Count > 0){
                // MapPlayerDestination 오브젝트의 위치 변경
                currentMapPlayerDestination.gameObject.SetActive(true);
                currentMapPlayerDestination.transform.localPosition = findPath[findPath.Count-1].transform.position; // MapPlayerDestination 위치는 findPath 마지막 노드 위치
                currentMapPlayerDestination.MoveBounce(true);

                // 경로표시
                M_MapManager.instance.RemoveExistLineRenderer(networkIdentity.netId);
                M_MapManager.instance.RenderVisualizePath(startAt, findPath, networkIdentity.netId, currentMapPlayerDestination);
                currentMapPlayerDestination.imageDistanceCount.gameObject.SetActive(true); 
            }else{
                // 경로제거
                M_MapManager.instance.RemoveExistLineRenderer(networkIdentity.netId);
                currentMapPlayerDestination.imageDistanceCount.gameObject.SetActive(false);
            }
            
            // findPath 리스트의 카운트 = 거리값
            currentMapPlayerDestination.textDistanceCount.text = findPath.Count.ToString();
        }
    }

    // ------------------------------------------------------------------------------ ClientRpc Method ----------------------------------------------------------------------------//

    // 검색된 경로를 표시하는 라인랜더러 랜더링
    [ClientRpc (includeOwner = false)]
    public void RpcVisualizePath(HexagonMapRoom startAt, List<HexagonMapRoom> findPath, uint netId)
    {   
        if(currentMapPlayerDestination != null){
            M_MapManager.instance.RemoveExistLineRenderer(netId); // 기존 경로 삭제
            M_MapManager.instance.RenderVisualizePath(startAt, findPath, netId, currentMapPlayerDestination); // 새 경로 랜더링
            currentMapPlayerDestination.imageDistanceCount.gameObject.SetActive(true);
        }
    }

    // 검색된 경로를 표시하는 라인랜더러 및 카운트 마크 제거
    [ClientRpc (includeOwner = false)]
    public void RpcHidePath(uint netId)
    {   
        if(currentMapPlayerDestination != null){
            M_MapManager.instance.RemoveExistLineRenderer(netId); // 기존 경로 삭제
            currentMapPlayerDestination.imageDistanceCount.gameObject.SetActive(false);
        }
    }


    // ------------------------------------------------------------------------------ SyncVar hook ----------------------------------------------------------------------------//

    // 맵 플레이어가 이동하려는 방의 위치를 알려주는 표시 변경 수신
    public void OnChangeCurrentMapPlayerDestination(Vector3 oldPosition, Vector3 newPosition)
    {
        if(currentMapPlayerDestination != null && !currentMapPlayerDestination.isOwned){
            currentMapPlayerDestination.gameObject.SetActive(true);
            currentMapPlayerDestination.transform.localPosition = newPosition;
            currentMapPlayerDestination.MoveBounce(oldPosition != newPosition);
        }
    }

}
