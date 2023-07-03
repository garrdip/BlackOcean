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
    public bool isInitializeDone = false;

    [SyncVar (hook = nameof(OnChangedSelectOrder))]
    public int selectOrder = 0;

    [SyncVar (hook = nameof(OnReadyStateChanged))]
    public bool isReady = false;

    [SyncVar (hook = nameof(OnCompleteReward))]
    public bool isRewardDone = false;

    [SyncVar]
    public Vector2 destination;

    [SyncVar]
    public ulong steamID;

    [SyncVar (hook = nameof(OnEndTurnStateChanged))]
    public bool endTurnActive = false;

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

    public void SetOrderByUI(int num)
    {
        if(isLocalPlayer)
            selectOrder = num;
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        MapUI.instance.UpdateProfile();
        if(isLocalPlayer)
            MapUI.instance.SetOrderIndicator(newVal);
    }

    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isServer)
        {
            M_MapManager.instance.GenerateFloor();
        }
        if(isLocalPlayer)
        {
            HP = 100;
            MaxHP = 100;
            isInitializeDone = true;
            Debug.Log("다른 플레이어 기다림 시작!");
            StartCoroutine(nameof(WaitPlayerList));
        }
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
            M_TurnManager.instance.InitiateGamePlayerList();

        //UI Update
        MapUI.instance.UpdateProfile();
        MapUI.instance.SetOrderIndicator(selectOrder);
    }

    public void SetUserStatusUI()
    {
        //변경 필요
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
            foreach(GamePlayer player in users) player.isReady = false; // 레디 상태 모두 확인후 다시 Flase로 되돌림 (여러군데서 사용 예정)
            // 플레이어들이 투표한 결과 선택된 맵 위치로 이동
            MapRoom mapRoom = M_MapManager.instance.GetVoteMapRoomResult();
            if(mapRoom != null){
                Vector3 position = mapRoom.GetComponent<Transform>().position;
                foreach(GameObject mapPlayerPieceObject in M_MapManager.instance.mapPlayerPieces){
                    MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
                    mapPlayerPiece.RpcChangeMapPlayerPiecePosition(position);
                    M_MapManager.instance.SetDirection(mapRoom.location, position);
                }
            }

            // All Player Ready !
            M_TurnManager.instance.HandleStartBattle(mapRoom);
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
            foreach(GamePlayer player in users) player.isRewardDone = false;
            M_TurnManager.instance.NoneBattleEnd();
        }   
    }

    // 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSendChatMessage(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message)){
            string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
            RpcReceiveChatMessage(playerName, message.Trim());
        }
    }

    // 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessage(string playerName, string message)
    {
        MapUI.instance.AppendMessage(playerName, message);
    }
}
