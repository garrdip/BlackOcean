using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;

public class PlayerInterfaceServer : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
         
        //GenerateGamePlayer();
    }

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    [Command]
    void GenerateGamePlayer()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        PlayerInterface playerInterface = GetComponent<PlayerInterface>();
        GamePlayerDeck gamePlayerDeck = playerInterface.currentGamePlayer.GetComponent<GamePlayerDeck>();

        gamePlayerDeck.InitIchi();

        // CardPocket 오브젝트 생성
        GameObject cardPocketObject = Instantiate(
            netManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardPocket")),
            Vector3.zero,
            Quaternion.identity);
        NetworkServer.Spawn(cardPocketObject, connectionToClient);
        gamePlayerDeck.cardPocket = cardPocketObject.GetComponent<CardPocket>();
        
        // 화살표 생성 초기 위치는 화면 밖
        Vector3 arrowSpawnPosition = new Vector3(-100f, 0f, 0f);
        // 화살표 인디케이터 오브젝트 생성
        GameObject cardCtrlArrowObject = Instantiate(
            netManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(cardCtrlArrowObject, connectionToClient);
        gamePlayerDeck.cardCtrlArrow = cardCtrlArrowObject.GetComponent<CardCtrlArrow>();

        // 어빌리티 화살표 생성 초기 위치는 화면 밖
        Vector3 abilityArrowSpawnPosition = new Vector3(-100f, 0f, 0f);
        // 어빌리티 화살표 인디케이터 오브젝트 생성
        GameObject abilityArrowObject = Instantiate(
            netManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowEmitter")),
            abilityArrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(abilityArrowObject, connectionToClient);
        gamePlayerDeck.abilityCtrlArrow = abilityArrowObject.GetComponent<AbilityCtrlArrow>();

        // MapPlayerPiece 오브젝트 생성
        GamePlayerMap gamePlayerMap = GetComponent<GamePlayerMap>();
        GameObject mapPlayerPieceObject = Instantiate(
            netManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerPiece"),
            Vector3.zero,
            Quaternion.identity
        );
        MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
        mapPlayerPiece.steamId = SteamFriends.GetFriendPersonaName((CSteamID)GetComponent<PlayerInterface>().steamID); // 스팀아이디 값 세팅
        mapPlayerPiece.gamePlayer = GetComponent<GamePlayer>(); // 게임 플레이어 참조값 세팅
        NetworkServer.Spawn(mapPlayerPieceObject, connectionToClient);
        M_MapManager.instance.mapPlayerPieces.Add(mapPlayerPieceObject); // 매니저의 리스트에 생성된 맵 플레이어 추가
        gamePlayerMap.currentMapPlayerPiece = mapPlayerPiece; // 자신소유의 mapPlayerPiece 참조값 세팅

        // MapPlayerDestination 오브젝트 생성
        GameObject mapPlayerDestinationObject = Instantiate(
            netManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerDestination"),
            Vector3.zero,
            Quaternion.identity
        );
        MapPlayerDestination mapPlayerDestination = mapPlayerDestinationObject.GetComponent<MapPlayerDestination>();
        mapPlayerDestination.gamePlayer = GetComponent<GamePlayer>(); // 게임 플레이어 참조값 세팅
        NetworkServer.Spawn(mapPlayerDestinationObject, connectionToClient);
        gamePlayerMap.currentMapPlayerDestination = mapPlayerDestination; // 자신소유의 currentMapPlayerDestination 참조값 세팅
    }


    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//



}
