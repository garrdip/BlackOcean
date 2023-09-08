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

    [SyncVar]
    public string steamPersonaName;

    [SyncVar]
    public bool isValidAvatar;

    public SyncList<byte> image = new SyncList<byte>();

    [SyncVar]
    public int imageWidth, imageHeight;
    
    public void ChangeReadyState(bool oldVal, bool newVal)
    {
        if(isServer)
            RoomUI.instance.CMDReadyCheck();
    }

    // 로컬 플레이어로 시작 시 상단바 인디케이터 변경  Todo
    public override void OnStartLocalPlayer()
    {
        byte[] uploadableImage;

        RoomUI.instance.SetActiveSelectedOrderMark(order);
        if(!isServer)RoomUI.instance.SetReadyButton("READY");
        else RoomUI.instance.SetReadyButton("");
        steamID = (ulong)SteamUser.GetSteamID();

        steamPersonaName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
        // Avatar
        int imageId = SteamFriends.GetLargeFriendAvatar((CSteamID)steamID);
        uploadableImage = M_SteamManager.instance.GetSteamImageAsByteArray(imageId,out bool isValid, out uint width, out uint height);
        if(isValid)
        {
            imageWidth = (int)width;
            imageHeight = (int)height;
            isValidAvatar = true;
            Debug.Log(uploadableImage.Length);
            for(int i = 0 ;i < uploadableImage.Length ; i ++)
                image.Add(uploadableImage[i]);
        }

    }

    // 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSendChatMessage(string message, NetworkConnectionToClient sender = null)
    {
        if(!string.IsNullOrWhiteSpace(message)){
            string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
            RpcReceiveChatMessage(order, playerName, message.Trim());
        }
    }

    // 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessage(PlayOrder playOrder, string playerName, string message)
    {
        RoomUI.instance.AppendMessage(playOrder, playerName, message);
    }
}

