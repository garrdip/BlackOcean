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
            // 싱글 플레이(처음부터/이어하기 공통): 고정 파티의 대표 캐릭터가 룸플레이어 생성 시 정해지므로(M_NetworkRoomManager.SinglePlayParty) 룸 대기 없이 바로 게임 씬으로 (멀티는 호스트가 START)
            M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            if(netManager != null && netManager.isSinglePlay && character != Character.NONE)
                StartCoroutine(AutoStartSingle());
        }
    }

    // 싱글 플레이 자동 시작 — 캐릭터 선택 화면을 거치지 않고 로딩 화면을 띄운 채 바로 게임 씬으로 (START 버튼의 ChangeGameScene과 같은 절차, 룸 UI 비의존)
    IEnumerator AutoStartSingle()
    {
        yield return null; // GenerateManagers로 스폰한 매니저의 Start(싱글톤 등록)가 먼저 돌도록 한 프레임 대기
        float waited = 0f;
        while(M_LoadingManager.instance == null && waited < 3f){
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }
        if(!NetworkServer.active || character == Character.NONE) yield break;
        if(M_LoadingManager.instance == null){
            Debug.LogError("[RoomPlayer] 이어하기 자동 시작 실패 — M_LoadingManager가 없어 룸 화면에서 수동 시작이 필요합니다");
            yield break;
        }
        M_LoadingManager.instance.SetLoadingScreen(true);
        M_LoadingManager.instance.state = LOADING_STATE.SCENE_LOADING;
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        netManager.ServerChangeScene(netManager.GameplayScene);
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

