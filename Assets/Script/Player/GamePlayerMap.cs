using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class GamePlayerMap : NetworkBehaviour
{
    [SyncVar]
    public MapPlayerPiece currentMapPlayerPiece;

    [SyncVar (hook = nameof(OnChangeCurrentMapPlayerPosition))]
    public Vector3 currentMapPlayerPosition;

    public override void OnStartServer()
    {
        SpawnMapPlayerPiece();
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

        // 맵 플레이어 참조값 세팅
        currentMapPlayerPiece = mapPlayerPiece.GetComponent<MapPlayerPiece>();
    }


    // 맵플레이어 위치 변경 요청
    [Command]
    public void CmdChangeCurrentMapPlayerPosition(HexagonMapRoom hexagonMapRoom, Vector3 position)
    {
        currentMapPlayerPosition = position;
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


    // 맵 플레이어 위치 변경 수신
    public void OnChangeCurrentMapPlayerPosition(Vector3 oldPosition, Vector3 newPosition)
    {
        if(currentMapPlayerPiece != null){
            currentMapPlayerPiece.transform.position = newPosition;
        }
    }

}
