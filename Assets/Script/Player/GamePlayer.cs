using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


public class GamePlayer : NetworkBehaviour
{
    public delegate void OnChangePlayerOrder(int order);
    public OnChangePlayerOrder onChangePlayerOrder;

    public delegate void OnChangeGold(int gold);
    public OnChangeGold onChangeGold;

    [SyncVar (hook = nameof(OnChangePlayerGold))]
    public int gold = 0; // 현재 플레이어 보유 골드

    [SyncVar (hook = nameof(OnChangHpValue))]
    public int HP; // 체력
    
    [SyncVar]
    public int MaxHP; // 최대 체력

    [SyncVar]
    public int recoveryValue; // 체력 회복 수치

    [SyncVar]
    public int recoveryLimitCount; // 체력 회복 횟수 제한

    [SyncVar (hook = nameof(OnChangedObjectOwner))]
    public PlayerInterface objectOwner;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar]
    public Character character;

    [SyncVar(hook = nameof(OnChangeMapPlayerNetId))]
    public uint mapPlayerNetId;

    public ParticleSystem recoverParticle; // 체력 회복 파티클 이펙트

    public bool isSelectable = false; // CharacterSelector 클래스에서 사용되는 플래그 변수(캐릭터 오브젝트의 마우스 오버 및 클릭 가능 상태 변경 용도)

    public override void OnStartServer()
    {
        base.OnStartServer();

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
    
    [Command]
    public void CmdHpRecovery(uint targetPlayerNetId)
    {
        if(NetworkServer.spawned.TryGetValue(targetPlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer targetPlayer = networkIdentity.GetComponent<GamePlayer>();
            TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(targetPlayer);
            if(targetObject.player != null && recoveryLimitCount > 0){
                targetObject.playerHP += recoveryValue;
                recoveryLimitCount--;
                RpcHpRecovery(targetPlayerNetId);
            }
        }
    }

    // ------------------------------------------------------------- Server Method --------------------------------------------------------------------//

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

    [ClientRpc]
    public void RpcHpRecovery(uint targetPlayerNetId)
    { 
        if(NetworkClient.spawned.TryGetValue(targetPlayerNetId, out NetworkIdentity networkIdentity)){
            GamePlayer targetPlayer = networkIdentity.GetComponent<GamePlayer>();
            TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(targetPlayer);
            ParticleSystem particleSystem = Instantiate(recoverParticle, targetObject.transform.position, Quaternion.identity);
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "Effect";
        }
    }

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//

    public void OnChangePlayerGold(int oldVal, int newVal)
    {
        if(onChangeGold != null){
            onChangeGold.Invoke(newVal);
        }
    }

    public void OnChangHpValue(int oldVal, int newVal)
    {
        if(isServer){
            CheckAllPlayersIsDead();
        }
    }

    public void OnChangedObjectOwner(PlayerInterface oldVal, PlayerInterface newVal)
    {
        transform.SetParent(newVal.transform);
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        if(onChangePlayerOrder != null){
            onChangePlayerOrder.Invoke(newVal);
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
