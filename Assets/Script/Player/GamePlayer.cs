using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    public int HP = 100;

    [SyncVar]
    public int MaxHP = 100;

    [SyncVar]
    public Character character;

    [SyncVar]
    public Color color;

    [SyncVar]
    public bool isInitializeDone = false;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar (hook = nameof(OnReadyStateChanged))]
    public bool isReady = false;

    [SyncVar (hook = nameof(OnCompleteReward))]
    public bool isRewardDone = false;

    [SyncVar]
    public ulong steamID;

    [SyncVar]
    public string steamPersonaName;

    [SyncVar (hook = nameof(OnEndTurnStateChanged))]
    public bool endTurnActive = false;

    [SyncVar]
    public bool isTargetObjectInitDone = false;

    public readonly SyncList<byte> avatarImage = new SyncList<byte>();

    [SyncVar]
    public int avatarWidth,avatarHeight;

    [SyncVar (hook = nameof(OnChangedAvatar))]
    public bool isAvatarUploadDone = false;

    public readonly SyncList<CardOnHand> destroyCards = new SyncList<CardOnHand>();

    [SyncVar]
    public bool isLoadDone = false;


    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            M_MapManager.instance.GenerateStartHexagonRoom(); // 육각형 방 생성(진행중)
            M_MapManager.instance.GenerateColorRegion();
        }
        if(isLocalPlayer)
        {
           
            M_MapManager.instance.GenerateHexgonGrid(40);
            HP = 100;
            MaxHP = 100;
            isInitializeDone = true;
            StartCoroutine(nameof(WaitPlayerList));
        }
    }

    public void SetOrderByUI(int num)
    {
        if(isLocalPlayer)
            selectOrder = num;
    }

    IEnumerator WaitPlayerList()
    {
        M_NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        WaitForSeconds loopSecond = new WaitForSeconds(0.01f);
        //GamePlayer가 모두 로드 될때까지 기다림
        while(true)
        {
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            if(users.Length == netManger.roomSlots.Count) break;
            yield return loopSecond;
        }
        //GamePlayer가 모두 Initial Value 초기화 될때까지 기다림
        while(true)
        {
            int cnt = 0;
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer user in users)
            {
                if(user.isInitializeDone) cnt++;
            }
            if(cnt == netManger.roomSlots.Count) break;
            yield return loopSecond;
        }
        SetUserStatusUI();
        M_TurnManager.instance.SetOrderButtonListener();
        // 플레이어 로딩이 끝나면 턴매니저로 플레이어 리스트를 전달함
        if(isServer)
        {
            M_TurnManager.instance.InitiateGamePlayerList();
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer user in users)
                user.UploadAvatar();
        }

        //UI Update
        MapUI.instance.UpdateProfile();
        MapUI.instance.SetOrderIndicator(selectOrder);
        if(isServer){
            M_MapManager.instance.SetRegionWithColorRPC();
            StartCoroutine(WaitLoadDone());
        }
    }

    IEnumerator WaitLoadDone()
    {
        M_NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        WaitForSeconds loopSecond = new WaitForSeconds(0.01f);
        while(true)
        {
            int cnt = 0;
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer user in users)
            {
                if(user.isLoadDone) cnt++;
            }
            if(cnt == netManger.roomSlots.Count) break;
            yield return loopSecond;
        }
        M_LoadingManager.instance.SetLoadingScreen(false);
    }

    public void SetUserStatusUI()
    {
        //변경 필요
    }

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    // 맵 씬 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSendChatMessage(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message)){
            string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
            RpcReceiveChatMessage(color, playerName, message.Trim());
        }
    }

    // 전투 씬 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSendChatMessageGameScene(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message)){
            string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
            RpcReceiveChatMessageGameScene(color, playerName, message.Trim());
        }
    }

    // ---------------------------------------------------------------- Server Method -----------------------------------------------------------------//

    [Server]
    public void SetPlayerOrder(int num)
    {
        SetPlayerOrderRPC(num);
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    public void RemoveDestroyCardList(CardOnHand cardOnHand)
    {
        if(isOwned)
        {
            destroyCards.Remove(cardOnHand);
        }
    }

    [ClientRpc]
    void UploadAvatar()
    {
        if(isLocalPlayer)
        {
            byte[] uploadableImage;
            int imageId = SteamFriends.GetSmallFriendAvatar((CSteamID)steamID);
            uploadableImage = M_SteamManager.instance.GetSteamImageAsByteArray(imageId,out bool isValid, out uint width, out uint height);
            if(isValid)
            {
                avatarWidth = (int)width;
                avatarHeight = (int)height;
                for(int i = 0 ;i < uploadableImage.Length ; i ++)
                    avatarImage.Add(uploadableImage[i]);
            }
            isAvatarUploadDone = true;
        }
    }

    [ClientRpc]
    public void SetIsReadyStateDefault()
    {
        if(netIdentity == NetworkClient.connection.identity)
            isReady = false;
    }

    [ClientRpc]
    public void SetEndTurnActiveStateDefault()
    {
        if(netIdentity == NetworkClient.connection.identity)
            endTurnActive = false;
    }

    [ClientRpc]
    public void SetCompleteRewardStateDefault()
    {
        if(netIdentity == NetworkClient.connection.identity)
            isRewardDone = false;
    }

    // 전투 씬 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessageGameScene(Color color, string playerName, string message)
    {
        GameUIManager.instance.AppendMessage(color, playerName, message);
    }

    // 맵 씬 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessage(Color color, string playerName, string message)
    {
        MapUI.instance.AppendMessage(color, playerName, message);
    }

    [ClientRpc]
    void SetPlayerOrderRPC(int num)
    {
        if(isLocalPlayer)
        {
            selectOrder = num;
        }
    }

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//
    
    public void OnEndTurnStateChanged(bool oldVal, bool newVal)
    {
        if(isServer)
        {
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer user in users)
            {
                if(!user.endTurnActive)return;
            }
            switch(M_TurnManager.instance.phase)
            {
                case BattleTurn.PLAYER_ACTIVE :
                    M_TurnManager.instance.phase = BattleTurn.PLAYER_ACTIVE_DONE;
                    break;
                case BattleTurn.NONE_BATTLE_SCENE :
                    M_TurnManager.instance.phase = BattleTurn.NONE_BATTLE_END;
                    break;
            }
        }
    }
    
    public void OnCompleteReward(bool oldVal, bool newVal)
    {
        if(isServer)
        {
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer player in users)
            {
                if(!player.isRewardDone) return;
            }
            foreach(GamePlayer player in users)player.SetCompleteRewardStateDefault();
            M_TurnManager.instance.NoneBattleEnd();
        }   
    }
    
    public void OnReadyStateChanged(bool oldVal, bool newVal)
    {
        MapUI.instance.UpdateProfile();
        if(isServer)
        {
            GamePlayer[] users = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer player in users)
            {
                if(!player.isReady) return;
            }
            foreach(GamePlayer player in users)player.SetIsReadyStateDefault(); // 레디 상태 모두 확인후 다시 Flase로 되돌림 (여러군데서 사용 예정)
            // 플레이어들이 투표한 결과 선택된 맵 위치로 이동
            HexagonMapRoom hexagonMapRoom = M_MapManager.instance.GetVoteHexagonMapRoomResult();
            if(hexagonMapRoom != null){
                M_TurnManager.instance.EnterTheRoom(hexagonMapRoom); // 모든 플레이어 레디상태 확인 시 전투 시작
            }
        }    
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        MapUI.instance.UpdateProfile();
        if(isLocalPlayer)
            MapUI.instance.SetOrderIndicator(newVal);
    }

    void OnChangedAvatar(bool oldVal, bool newVal)
    {
        if(newVal == true)
        {
            MapUI.instance.UpdateProfile();
        }
    }

}
