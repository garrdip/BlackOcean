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

                case LOADING_STATE.HUB_PREPARE :
                    PrepareHub();
                    break;

                case LOADING_STATE.GAMEPLAYER_COMPONENT_GEN :
                    GenetateGamePlayerDeck();
                    break;

                case LOADING_STATE.UPLOAD_AVATAR :
                    UploadAvatar();
                    break;

                case LOADING_STATE.HUB_SCENE :
                    M_HubManager.instance.EnterHub(false); // 로딩 완료 → 거점 진입 (NPC 4종 스폰, 페이드 연출 없음 — 로딩 화면 뒤에 거점이 이미 준비되어 있음)
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

    // 맵 타일 시스템 제거 — 거점(M_HubManager)은 별도 생성 작업이 없으므로 바로 다음 단계로 진행
    void PrepareHub()
    {
        state++;
        OnChangedState(state,state);
    }

}
