using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;

public class GamePlayer : NetworkBehaviour
{
    public delegate void OnChangePlayerOrder(int order);
    public OnChangePlayerOrder onChangePlayerOrder;

    [SyncVar (hook = nameof(OnChangHpValue))]
    public int HP;
    [SyncVar]
    public int MaxHP;

    [SyncVar (hook = nameof(OnChangedObjectOwner))]
    public PlayerInterface objectOwner;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar]
    public Character character;

    [SyncVar]
    public uint playerOrderNetId;

    [SyncVar(hook = nameof(OnChangeMapPlayerNetId))]
    public uint mapPlayerNetId;


    public override void OnStartServer()
    {
        base.OnStartServer();

        GeneratePlayerOrder();
        if(M_SaveManager.instance.isSaveGame)
        {
            foreach(SaveDataPlayer saveDataPlayer in M_SaveManager.instance.loadData.players)
            {
                if(saveDataPlayer == null)break;
                if(saveDataPlayer.ownerSteamId == objectOwner.steamID)
                {
                    HP = saveDataPlayer.HP;
                    MaxHP = saveDataPlayer.MaxHP;
                }
            }
        }
    }

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // ------------------------------------------------------------- Server Method --------------------------------------------------------------------//

    // 게임씬에서 플레이어 오더 및 정보들을 보여주는 오브젝트 생성
    [Server]
    private void GeneratePlayerOrder()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject playerOrderObject = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "PlayerOrder"), Vector3.zero, Quaternion.identity);
        PlayerOrder playerOrder = playerOrderObject.GetComponent<PlayerOrder>();
        playerOrder.gamePlayer = this;
        NetworkServer.Spawn(playerOrderObject, connectionToClient);
        playerOrderNetId = playerOrder.netId;
    }


    [Server]
    public void SetPlayerOrder(int num)
    {
        SetPlayerOrderRPC(num);
    }

    [Server]
    public void CheckAllPlayersIsDead()
    {
        int gamePlayerCount = M_TurnManager.instance.playerOrder.FindAll((netId) => netId != 0).Count; // 현재 게임에 참가한 플레이어 수
        int deadPlayerCount = M_TurnManager.instance.playerOrder.FindAll((netId) => netId != 0 && IsPlayerHpZero(netId)).Count; // HP가 0인 플레이어 수
        if(deadPlayerCount == gamePlayerCount){
            RpcGameOver();
        }
    }

    // netId로 GamePlayer 조회하여 HP가 0 이하면 trun, 아니면 false 반환
    [Server]
    private bool IsPlayerHpZero(uint netId)
    {
        if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
           return networkIdentity.GetComponent<GamePlayer>().HP <= 0;
        }
        return false;
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    void SetPlayerOrderRPC(int num)
    {
        if(isLocalPlayer)
        {
            selectOrder = num;
        }
    }

    [ClientRpc]
    public void RpcGameOver()
    {
        PopUpUIManager.instance.HandleShowGameOverPopUp();
    }

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        if(onChangePlayerOrder != null){
            onChangePlayerOrder.Invoke(newVal);
        }
    }

    public void OnChangedObjectOwner(PlayerInterface oldVal, PlayerInterface newVal)
    {
        transform.SetParent(newVal.transform);
    }

    public void OnChangHpValue(int oldVal, int newVal)
    {
        if(isServer){
            CheckAllPlayersIsDead();
        }
    }

    public void OnChangeMapPlayerNetId(uint oldVal, uint newVal)
    {
        int index = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == GetComponent<NetworkIdentity>().netId); // PlayerOrder SyncList에서 각 플레이어들의 인덱스값 조회
        if(index != -1){
            M_MapManager.instance.InitMapPlayer(GetComponent<NetworkIdentity>().netId, index); // 조회한 인덱스 값으로 맵플레이어 오더 초기 설정
        }
    }
}
