using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using ProjectD;


public class RoomPlayer : NetworkRoomPlayer
{
    public delegate void OnSelectCompleteCharacter(Character character);
    public OnSelectCompleteCharacter onSelectCompleteCharacter;

    public delegate void OnChangeReadyState(bool isReady);
    public OnChangeReadyState onChangeReadyState;

    [SyncVar(hook = nameof(OnChangedCharacter))]
    public Character character = Character.NONE;

    [SyncVar]
    public Color color;

    [SyncVar]
    public PlayOrder order = PlayOrder.FIRST;

    [SyncVar(hook = nameof(OnChangeReady))]
    public bool isReady = false;

    [SyncVar(hook = nameof(OnChangedSteamID))]
    public ulong steamID;

    [SyncVar]
    public string steamPersonaName;

    

    public override void OnStartLocalPlayer()
    {
        steamID = (ulong)SteamUser.GetSteamID();
        steamPersonaName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
        RoomUI.instance.SetReadyButton(!isServer ? "READY" : "");
        if(isServer){
            GenerateManagers();
        }
    }

    // 방에 다른 유저 들어오면 로컬플레이어의 레디상태 해제
    public override void OnClientEnterRoom()
    {
        base.OnClientEnterRoom();
        if(isLocalPlayer){
            isReady = false;
            OnChangeReady(false, false);
        }
    }

    // ----------------------------------------------------------------- Server Method --------------------------------------------------------------------------------//

    [Server]
    public void GenerateManagers()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
       
        GameObject loadingManager = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("M_LoadingManager")),
                Vector3.zero,
                Quaternion.identity
        );
        NetworkServer.Spawn(loadingManager);
        GameObject saveManager = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("M_SaveManager")),
                Vector3.zero,
                Quaternion.identity
        );
        NetworkServer.Spawn(saveManager);
    }

    // ----------------------------------------------------------------- Rpc Method --------------------------------------------------------------------------------//

    [ClientRpc]
    void ChangeSaveDataFromServer(SaveDataPlayer saveDataPlayer)
    {
        if(isLocalPlayer)
        {
            character = saveDataPlayer.character;
        }
    }

    // ----------------------------------------------------------------- SyncVar Hook --------------------------------------------------------------------------------//

    public void OnChangeReady(bool oldVal, bool newVal)
    {
        if(isLocalPlayer){
            ReadyButtonOnRoom readyButtonOnRoom = RoomUI.instance.readyButton.GetComponent<ReadyButtonOnRoom>();
            readyButtonOnRoom.SetReadyButtonViewByReadyState(newVal);
        }
        if(isServer){
            M_LobbyMananger.instance.RoomPlayerReadyCheck();
        } 
        onChangeReadyState?.Invoke(newVal);
    }

    public void OnChangedCharacter(Character oldVal, Character newVal)
    {
        if(isServer){
            M_LobbyMananger.instance.RoomPlayerReadyCheck();
        }
        onSelectCompleteCharacter?.Invoke(newVal);
    }

    void OnChangedSteamID(ulong oldVal,  ulong newVal)
    {
        /*
        if(M_SaveManager.instance.isSaveGame && isServer)
        {
            foreach(SaveDataPlayer saveDataPlayer in M_SaveManager.instance.loadData.players)
            {
                if(saveDataPlayer == null)return;
                if(saveDataPlayer.ownerSteamId == newVal)
                {
                    ChangeSaveDataFromServer(saveDataPlayer);
                }
            }
        }
        */
    }
}

