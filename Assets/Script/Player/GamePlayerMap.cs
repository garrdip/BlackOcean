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
                RpcHidePath(networkIdentity.netId); // 경로제거
                if(M_MapManager.instance.mapBoss != null){
                    M_MapManager.instance.VoteHexagonMapRoom(endAt, netIdentity);
                }
            }
            
            // findPath 리스트의 카운트 = 거리값
            currentMapPlayerDestination.distanceFromCurrentCoordinate = findPath.Count;
        }
    }

    [Client]
    public void DisplayFindPath(HexagonMapRoom endAt, Vector3 position, NetworkIdentity networkIdentity)
    {
        M_MapManager.instance.findPaths.Clear();

        // 시작지점은 CurretnRoom 또는 StartPosition
        HexagonMapRoom startAt = M_MapManager.instance.currentRoom != null ? M_MapManager.instance.currentRoom : NetworkClient.spawned[M_MapManager.instance.hexagonMapRoomNetIds[0]].GetComponent<HexagonMapRoom>();

        // 검색된 경로 표시 그리드 활성화
        List<HexagonMapRoom> findPath = FindPath(startAt, endAt);
        if(findPath.Count > 0){
            for(int i=0; i<findPath.Count; i++){
                M_MapManager.instance.findPaths.Add(findPath[i]);
            }
        }
    }

    [Client]
    private List<HexagonMapRoom> FindPath(HexagonMapRoom start, HexagonMapRoom destination)
    { 
        List<HexagonMapRoom> openSet = new List<HexagonMapRoom>(); // 아직 방문하지 않은 노드들 목록
        HashSet<HexagonMapRoom> closedSet = new HashSet<HexagonMapRoom>(); // 이미 방문한 노드들의 목록(중복제거)
        openSet.Add(start); // 시작점 추가

        // 검색 시작
        while(openSet.Count > 0)
        {
            // FCost와 HCost를 비교해서 오름차순 정렬
            openSet.Sort((nodeA, nodeB) => {
                int costComparison = nodeA.FCost.CompareTo(nodeB.FCost); // openSet에서 FCost가 가장 낮은 노드를 선택
                if (costComparison == 0) { // FCost 같은 경우 HCost가 낮은 노드를 선택
                    return nodeA.HCost.CompareTo(nodeB.HCost);
                }
                return costComparison;
            });
            HexagonMapRoom currentNode = openSet[0];

            openSet.Remove(currentNode); // openset에서 현재 노드 제거
            closedSet.Add(currentNode); // closedSet에 현재 노드 추가

            // 현재 노트가 목적지 노드와 같다면 목적지에 도달한 것이므로 경로를 생성해서 반환
            if(currentNode.coordinate == destination.coordinate)
            {
                return CreatePath(start, currentNode);
            }

            List<HexagonMapRoom> neighbours = GetNeighbours(currentNode, destination.coordinate); // 현재 노드의 주변 이웃 노드 조회
            foreach (HexagonMapRoom neighbour in neighbours)
            {
                if(closedSet.Contains(neighbour)) // 이웃노드가 방문한 목록에 있으면 그대로 진행
                    continue;

                // Cost값 갱신
                int newGCost = currentNode.GCost + 1;
                int newHCost = M_MapManager.instance.CalculateHeuristics(neighbour.coordinate, destination.coordinate);
                int newFCost = newGCost + newHCost;

                if(newFCost < neighbour.FCost || !openSet.Contains(neighbour))
                {
                    // 이웃노드들의 Cost값 갱신
                    neighbour.GCost = newGCost;
                    neighbour.HCost = newHCost;
                    neighbour.previousNode = currentNode;

                    // openSet리스트에 중복을 허용하지 않고 이웃노드 추가
                    if(!openSet.Contains(neighbour)) 
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }
        return new List<HexagonMapRoom>(); // 경로 검색에 실패하면 빈 리스트 반환
    }

    [Client]
    private List<HexagonMapRoom> GetNeighbours(HexagonMapRoom currentHexagonRoom, Vector2Int destinationCoord)
    {
        List<HexagonMapRoom> neighbours = new List<HexagonMapRoom>();
        for(int i = 0; i < 6; i++)
        {
            HexagonMapRoom neighbour = FindNeighbours(i, currentHexagonRoom);
            neighbours.Add(neighbour);
        }
        return neighbours;
    }

    [Client]
    private HexagonMapRoom FindNeighbours(int index, HexagonMapRoom currentHexagonRoom)
    {
        foreach (uint netId in M_MapManager.instance.hexagonMapRoomNetIds){
            HexagonMapRoom mapRoom = NetworkClient.spawned[netId].GetComponent<HexagonMapRoom>();
            if(mapRoom.coordinate == currentHexagonRoom.coordinate + M_MapManager.instance.offSets[index]){
                return mapRoom;
            }
        }
        return null;
    }

    [Client]
    private List<HexagonMapRoom> CreatePath(HexagonMapRoom startNode, HexagonMapRoom endNode)
    {
        List<HexagonMapRoom> path = new List<HexagonMapRoom>();
        HexagonMapRoom currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.previousNode;
        }
        path.Reverse();
        return path;
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
