using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
public class M_LoadingManager : NetworkSingletonD<M_LoadingManager>
{
    public GameObject loadingScreen;

    [SyncVar (hook = nameof(OnChangedState))]
    public LOADING_STATE state = LOADING_STATE.ROOM_SCENE;

    [Server]
    public void SetLoadingScreen(bool onOff)
    {
        SetLoadingScreenOnOff(onOff);
    }

    [ClientRpc]
    public void SetLoadingScreenOnOff(bool onOff)
    {
        loadingScreen.SetActive(onOff);
    }

    public void CheckWorkDone()
    {
        IReadOnlyList<PlayerInterface> users = PlayerRegistry.All;
        foreach(PlayerInterface user in users)
            if(!user.workDone)return;
        if(users.Count == NetworkServer.connections.Count)
        {
            foreach(PlayerInterface user in users)
                user.ClearWorkDone();
        }
    }

    public void CheckWorkDoneClear()
    {
        IReadOnlyList<PlayerInterface> users = PlayerRegistry.All;
        foreach(PlayerInterface user in users)
            if(user.workDone) return;
        if(users.Count == NetworkServer.connections.Count)
            state++;
    }

    void OnChangedState(LOADING_STATE oldVal, LOADING_STATE newVal)
    {
        Debug.Log(newVal);
        if(isServer)
        {
            switch(newVal)
            {
                
                case LOADING_STATE.ROOM_SCENE :
                    break;

                case LOADING_STATE.SCENE_LOADING :
                    break;

                case LOADING_STATE.MAP_GENERATE :
                    GenerateRooms();
                    break;
                    
                case LOADING_STATE.GAMEPLAYER_COMPONENT_GEN :
                    GenetateGamePlayerDeck();
                    break;
                
                case LOADING_STATE.UPLOAD_AVATAR :
                    UploadAvatar();
                    break;
                
                case LOADING_STATE.MAP_SCENE :
                    M_MapManager.instance.SetRegionWithColorRPC();
                    M_LoadingManager.instance.SetLoadingScreen(false);
                    break;
                
                case LOADING_STATE.LOADING_GAME_SCENE :
                    break;
                case LOADING_STATE.GAME_SCENE :
                    break;

            }   
        }
    }

    void GenetateGamePlayerDeck()
    {
        IReadOnlyList<PlayerInterface> users = PlayerRegistry.All;
        foreach(PlayerInterface user in users)
        {
            user.GenerateGamePlayerDeck();
        }
    }

    void UploadAvatar()
    {
        IReadOnlyList<PlayerInterface> users = PlayerRegistry.All;
        foreach(PlayerInterface user in users)
        {
            user.UploadAvatar();
        }            
    }

    void GenerateRooms()
    {
        if(M_SaveManager.instance.isSaveGame)
        {
            M_MapManager.instance.RegenerateStartHexsagonRoom(M_SaveManager.instance.loadData); // 세이브 데이터로 육각형 방 재생성
            M_MapManager.instance.RegenerateColorRegion(M_SaveManager.instance.loadData); // 세이브 데이터로 거점지역 재생성
        }
        else
        { 
            M_MapManager.instance.GenerateStartHexagonRoom(); // 육각형 방 생성
            M_MapManager.instance.GenerateColorRegion(); // 거점지역 생성
        }
        state++;
        OnChangedState(state,state);
    }

}
