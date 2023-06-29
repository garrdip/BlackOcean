using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using ProjectD;
using Steamworks;

public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar]
    public Character character = Character.NONE;

    [SyncVar]
    public PlayOrder order = PlayOrder.UNDEFINED;

    [SyncVar(hook = nameof(ChangeReadyState))]
    public bool isReady = false;

    [SyncVar]
    public ulong steamID;
    
    public void ChangeReadyState(bool oldVal, bool newVal)
    {
        if(isServer)
            RoomUI.instance.CMDReadyCheck();
    }

    // 로컬 플레이어로 시작 시 상단바 인디케이터 변경  Todo
    public override void OnStartLocalPlayer()
    {
        RoomUI.instance.SetActiveSelectedOrderMark(order);
        if(!isServer)RoomUI.instance.SetReadyButton("READY");
        else RoomUI.instance.SetReadyButton("");
        steamID = (ulong)SteamUser.GetSteamID();
        base.OnStartLocalPlayer();
    }

    // RoomPlayer 시작시 프로필 변경
    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    // 클라 + 호스트 종료 시 RoomPlayerForUI 오브젝트 파괴 및 리스트에서 제거
    public override void OnStopClient()
    {
        base.OnStopClient();
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
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
        RoomUI.instance.AppendMessage(playerName, message);
    }
}

