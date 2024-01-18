using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using ProjectD;


public class PlayerInterfaceServer : NetworkBehaviour
{

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // 현재 선택된 플레이어 소유의 오브젝트들 생성(CardPocket, CardArrow, AbilityButton, AbilityArrow, MapPlayerPiece, MapPlayerDestination)
    [Command]
    public void GenerateGamePlayerOwnedObjects(GamePlayer gamePlayer)
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        PlayerInterface playerInterface = GetComponent<PlayerInterface>();
        GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>(); 

        // CardPocket 오브젝트 생성
        GameObject cardPocketObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardPocket")),
            Vector3.zero,
            Quaternion.identity);
        NetworkServer.Spawn(cardPocketObject, connectionToClient);
        gamePlayerDeck.cardPocket = cardPocketObject.GetComponent<CardPocket>();
        
        // 화살표 생성 초기 위치는 화면 밖
        Vector3 arrowSpawnPosition = new Vector3(-100f, 0f, 0f);
        // 화살표 인디케이터 오브젝트 생성
        GameObject cardCtrlArrowObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(cardCtrlArrowObject, connectionToClient);
        gamePlayerDeck.cardCtrlArrow = cardCtrlArrowObject.GetComponent<CardCtrlArrow>();

        // 단향 캐릭터인 경우 어빌리티 버튼 + 어빌리티 화살표 생성
        if(gamePlayer.character == Character.HONGDANHYANG){
            // 생성 초기 위치는 화면 밖
            Vector3 spawnPosition = new Vector3(-100f, 0f, 0f);   

            // 어빌리티 버튼 생성
            GameObject abilityButtonObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityButton")),
                spawnPosition,
                Quaternion.identity);
            NetworkServer.Spawn(abilityButtonObject, connectionToClient);
            gamePlayerDeck.abilityButton = abilityButtonObject.GetComponent<AbilityButton>();

            // 어빌리티 화살표 인디케이터 오브젝트 생성
            GameObject abilityArrowObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowEmitter")),
                spawnPosition,
                Quaternion.identity);
            NetworkServer.Spawn(abilityArrowObject, connectionToClient);
            gamePlayerDeck.abilityCtrlArrow = abilityArrowObject.GetComponent<AbilityCtrlArrow>();
        }

        // 맵 플레이어 오브젝트 생성
        GameObject mapPlayerObject = Instantiate(networkRoomManager.spawnPrefabs.Find(pref => pref.name == "MapPlayer"));
        MapPlayer mapPlayer = mapPlayerObject.GetComponent<MapPlayer>();
        mapPlayer.gamePlayer = gamePlayer;
        NetworkServer.Spawn(mapPlayerObject, connectionToClient);
        gamePlayer.mapPlayerNetId = mapPlayer.netId;

        // MapPlayerPiece 오브젝트 생성
        GamePlayerMap gamePlayerMap = gamePlayer.GetComponent<GamePlayerMap>();
        GameObject mapPlayerPieceObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerPiece"),
            M_MapManager.instance.currentRoom.position,
            Quaternion.identity
        );
        MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
        mapPlayerPiece.steamId = SteamFriends.GetFriendPersonaName((CSteamID)GetComponent<PlayerInterface>().steamID); // 스팀아이디 값 세팅
        mapPlayerPiece.playerIntefaceNetId = GetComponent<PlayerInterface>().netId; // 게임 플레이어 참조값 세팅
        NetworkServer.Spawn(mapPlayerPieceObject, connectionToClient);
      
        M_MapManager.instance.mapPlayerPieces.Add(mapPlayerPieceObject); // 매니저의 리스트에 생성된 맵 플레이어 추가
        gamePlayerMap.currentMapPlayerPiece = mapPlayerPiece; // 자신소유의 mapPlayerPiece 참조값 세팅

        // MapPlayerDestination 오브젝트 생성
        GameObject mapPlayerDestinationObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerDestination"),
            Vector3.zero,
            Quaternion.identity
        );
        MapPlayerDestination mapPlayerDestination = mapPlayerDestinationObject.GetComponent<MapPlayerDestination>();
        mapPlayerDestination.playerIntefaceNetId = GetComponent<PlayerInterface>().netId; // 게임 플레이어 참조값 세팅
        NetworkServer.Spawn(mapPlayerDestinationObject, connectionToClient);
        
        gamePlayerMap.currentMapPlayerDestination = mapPlayerDestination; // 자신소유의 currentMapPlayerDestination 참조값 세팅
    }


    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//
    
}
