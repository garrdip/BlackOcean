using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using AYellowpaper.SerializedCollections;


public class PlayerInterfaceServer : NetworkBehaviour
{

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // 현재 선택된 플레이어 소유의 오브젝트들 생성(CardPocket, CardArrow, AbilityArrow, MapPlayerPiece, MapPlayerDestination)
    [Command]
    public void GenerateGamePlayerOwnedObjects(GamePlayer gamePlayer)
    {
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        PlayerInterface playerInterface = GetComponent<PlayerInterface>();
        GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>(); 
        gamePlayerDeck.InitIchi();

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

        // 어빌리티 화살표 생성 초기 위치는 화면 밖
        Vector3 abilityArrowSpawnPosition = new Vector3(-100f, 0f, 0f);
        // 어빌리티 화살표 인디케이터 오브젝트 생성
        GameObject abilityArrowObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowEmitter")),
            abilityArrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(abilityArrowObject, connectionToClient);
        gamePlayerDeck.abilityCtrlArrow = abilityArrowObject.GetComponent<AbilityCtrlArrow>();

        // MapPlayerPiece 오브젝트 생성
        GamePlayerMap gamePlayerMap = gamePlayer.GetComponent<GamePlayerMap>();
        GameObject mapPlayerPieceObject = Instantiate(
            networkRoomManager.spawnPrefabs.Find(prefab => prefab.name == "MapPlayerPiece"),
            Vector3.zero,
            Quaternion.identity
        );
        MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
        mapPlayerPiece.steamId = SteamFriends.GetFriendPersonaName((CSteamID)GetComponent<PlayerInterface>().steamID); // 스팀아이디 값 세팅
        mapPlayerPiece.gamePlayer = GetComponent<PlayerInterface>().netId; // 게임 플레이어 참조값 세팅
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
        mapPlayerDestination.gamePlayer = GetComponent<PlayerInterface>().netId; // 게임 플레이어 참조값 세팅
        NetworkServer.Spawn(mapPlayerDestinationObject, connectionToClient);
        
        gamePlayerMap.currentMapPlayerDestination = mapPlayerDestination; // 자신소유의 currentMapPlayerDestination 참조값 세팅
    }


    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//
    
    [TargetRpc]
    public void TargetBattleRewardPopUp(NetworkConnectionToClient target)
    {
        PopUpUIManager.instance.HandleShowBattleResultPopUp(); // 전투 결과 보상 팝업 활성
    }

}
