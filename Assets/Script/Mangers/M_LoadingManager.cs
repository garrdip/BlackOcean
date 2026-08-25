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
        // USE_3D_MAP=0(BalanceDB)이면 기존 2D 육각형 맵 생성, 1이면 3D 구체 맵(SphereMapNetwork)이 대체.
        // 거점지역(Region) 시스템은 제거됨 — 빈땅(ROAD) 위주 + 희소 특수타일 세계 (GetRoomType 참조).
        // RPG 저장(GameSaveService)은 프로필만 복원하고 2D 맵은 매 세션 새로 생성된다.
        if(!SphereMapNetwork.Use3DMap)
        {
            M_MapManager.instance.GenerateStartHexagonRoom(); // 육각형 방 생성
        }
        state++;
        OnChangedState(state,state);
    }

}
