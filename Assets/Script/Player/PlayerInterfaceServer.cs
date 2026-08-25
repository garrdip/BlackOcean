using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using ProjectD;


public class PlayerInterfaceServer : NetworkBehaviour
{

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // 현재 선택된 플레이어 소유의 오브젝트들 생성(CardPocket, CardArrow, AbilityButton, AbilityArrow) + 플레이어 오더 등록
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
        if(gamePlayer.character == Character.HONGDANHYANG || gamePlayer.character == Character.GEORK){
            // 생성 초기 위치는 화면 밖
            Vector3 spawnPosition = new Vector3(-100f, 0f, 0f);   

            // 어빌리티 버튼 생성
            GameObject abilityButtonObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityButton")),
                spawnPosition,
                Quaternion.identity);
            AbilityButton abilityButton = abilityButtonObject.GetComponent<AbilityButton>();
            abilityButton.character = gamePlayer.character;
            NetworkServer.Spawn(abilityButtonObject, connectionToClient);
            gamePlayerDeck.abilityButton = abilityButton;

            // 어빌리티 화살표 인디케이터 오브젝트 생성
            GameObject abilityArrowObject = Instantiate(
                networkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowEmitter")),
                spawnPosition,
                Quaternion.identity);
            NetworkServer.Spawn(abilityArrowObject, connectionToClient);
            gamePlayerDeck.abilityCtrlArrow = abilityArrowObject.GetComponent<AbilityCtrlArrow>();
        }

        // 플레이어 오더 슬롯 등록 (룸에서 정한 selectOrder 인덱스에 netId) — 맵 시스템 제거로 MapPlayer.OnStartServer 역할을 이관
        M_TurnManager.instance.RegisterPlayerOrder(gamePlayer.selectOrder, gamePlayer.netId);
    }


    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//
    
}
