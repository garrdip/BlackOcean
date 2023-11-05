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
    [SyncVar(hook = nameof(OnChangedCharacter))]
    public Character character = Character.NONE;

    [SyncVar]
    public Color color;

    [SyncVar]
    public PlayOrder order = PlayOrder.UNDEFINED;

    [SyncVar(hook = nameof(ChangeReadyState))]
    public bool isReady = false;

    [SyncVar(hook = nameof(OnChangedSteamID))]
    public ulong steamID;

    [SyncVar]
    public string steamPersonaName;

    [SyncVar]
    public bool isValidAvatar;

    public readonly SyncList<byte> image = new SyncList<byte>();

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

    void OnChangedSteamID(ulong oldVal,  ulong newVal)
    {
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
    }

    [ClientRpc]
    void ChangeSaveDataFromServer(SaveDataPlayer saveDataPlayer)
    {
        if(isLocalPlayer)
        {
            character = saveDataPlayer.character;
        }
    }

    void OnChangedCharacter(Character oldVal, Character newVal)
    {
        RoomUI.instance.CMDReadyCheck();
    }

}

