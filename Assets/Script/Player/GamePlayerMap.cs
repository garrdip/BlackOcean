using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class GamePlayerMap : NetworkBehaviour
{
    [SyncVar]
    public MapPlayerPiece currentMapPlayerPiece; // 맵플레이어 오브젝트

    [SyncVar]
    public MapPlayerDestination currentMapPlayerDestination; // 맵플레이어가 이동할 방의 위치를 표시해주는 오브젝트

    [SyncVar (hook = nameof(OnChangeCurrentMapPlayerDestination))]
    public Vector3 currentMapPlayerDestinationPosition;

    // ------------------------------------------------------------------------------ Command Method ----------------------------------------------------------------------------//

    // 맵 플레이어가 이동하려는 방 표시 오브젝트의 위치 변경 및 거리값 계산 (서버 전용)
    [Command]
    public void CmdChangeMapPlayerDestinationPosition(HexagonMapRoom endAt, Vector3 position, NetworkIdentity networkIdentity)
    {
        if(currentMapPlayerDestination != null && M_MapManager.instance.currentRoom != null){
            // 맵에 보스 출현 시 1칸 이상 이동 불가
            if(M_MapManager.instance.mapBoss != null && M_MapManager.instance.GetDistanceFromCurrentCoordinate(M_MapManager.instance.currentRoom.coordinate, endAt.coordinate) > 1){
                return;
            }

            // 비활성화 상태면 이동 불가
            if(!endAt.isActive){
                return;
            }
            
            // 시작지점은 CurretnRoom 또는 StartPosition
            HexagonMapRoom startAt = M_MapManager.instance.currentRoom != null ? M_MapManager.instance.currentRoom : M_MapManager.instance.hexagonMapRooms[0];
     
            // MapPlayerDestination 초기 위치 설정
            currentMapPlayerDestinationPosition = position;
                    
            // 경로검색(현재 행동비용값을 기반으로 경로 검색)
            List<HexagonMapRoom> findPath = M_MapManager.instance.FindPath(startAt, endAt);
            if(findPath.Count > 0){
                currentMapPlayerDestinationPosition = findPath[findPath.Count-1].transform.position; // MapPlayerDestination 위치는 findPath 마지막 노드 위치
                RpcVisualizePath(startAt, findPath, networkIdentity.netId); // 경로표시
                M_MapManager.instance.VoteHexagonMapRoom(findPath[findPath.Count-1], netIdentity); // 검색된 경로의 마지막 위치에 있는 HexagonMapRoom을 투표
            }else{
                if(M_MapManager.instance.mapBoss != null){
                    M_MapManager.instance.VoteHexagonMapRoom(endAt, netIdentity);
                }
            }
            
            // findPath 리스트의 카운트 = 거리값
            currentMapPlayerDestination.distanceFromCurrentCoordinate = findPath.Count;
        }
    }

    // ------------------------------------------------------------------------------ ClientRpc Method ----------------------------------------------------------------------------//

    // 검색된 경로를 표시하는 라인랜더러 랜더링
    [ClientRpc]
    public void RpcVisualizePath(HexagonMapRoom startAt, List<HexagonMapRoom> findPath, uint netId)
    {   
        if(currentMapPlayerDestination != null){
            M_MapManager.instance.RemoveExistLineRenderer(netId); // 기존 경로 삭제
            M_MapManager.instance.RenderVisualizePath(startAt, findPath, netId, currentMapPlayerDestination); // 새 경로 랜더링
            HexagonMapRoom endAt = findPath[findPath.Count - 1];
            endAt.textMyRequireCost.text = findPath.Count.ToString();
            endAt.textAnotherRequireCost.text = findPath.Count.ToString();
        }
    }

    // 검색된 경로를 표시하는 라인랜더러 및 카운트 마크 제거
    [ClientRpc]
    public void RpcHidePath(uint netId)
    {   
        if(currentMapPlayerDestination != null){
            M_MapManager.instance.RemoveExistLineRenderer(netId); // 기존 경로 삭제
        }
    }

    // ------------------------------------------------------------------------------ SyncVar hook ----------------------------------------------------------------------------//

    // 맵 플레이어가 이동하려는 방의 위치를 알려주는 표시 변경 수신
    public void OnChangeCurrentMapPlayerDestination(Vector3 oldPosition, Vector3 newPosition)
    {
        /*
        if(currentMapPlayerDestination != null){
            currentMapPlayerDestination.gameObject.SetActive(true);
            currentMapPlayerDestination.transform.localPosition = newPosition;
            currentMapPlayerDestination.MoveBounce(oldPosition != newPosition);
        }
        */
    }

}
