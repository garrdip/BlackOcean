using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;

public class M_SteamManager : MonoBehaviour
{
    [SerializeField]
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyList;
    private const string HostAddressKey = "HostAddress";
    private const string PasswordKey = "Password";
    private const string LobbyNameKey = "LobbyName";
    public M_NetworkRoomManager networkManager;

    public static CSteamID enteredLobby;

    private void Start()
    {
        if(!SteamManager.Initialized){return;}
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequeseted);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        DontDestroyOnLoad(gameObject);
    }
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypePublic,3);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK)
        {
            return;
        }
        //Mirror Server Start
        networkManager.StartHost();
        //Steam Lobby Data 수정 
        enteredLobby = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressKey,
            SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequeseted(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t callback)
    {
        if(NetworkServer.active){ return;}
        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby),HostAddressKey);
        enteredLobby = new CSteamID(callback.m_ulSteamIDLobby);
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
    }

    public static void LeaveLobby()
    {
        SteamMatchmaking.LeaveLobby(enteredLobby);
    }

    public void GetLobbyList()
    {
        //로비 검색 필터는 이곳에 추가 RequestLobbyList 호출전에 추가되어야함
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault);
        SteamMatchmaking.RequestLobbyList();
        
    }

    private void OnLobbyMatchList(LobbyMatchList_t pCallback)
    {
        if (pCallback.m_nLobbiesMatching == 0)
        {
            // Handle error
            return;
        }

        // m_nLobbiesMatching 검색된 숫자로 반환되며 GetLobby 함수를 이용하여 Get 해야함
        for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            
            
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            Debug.Log(SteamMatchmaking.GetLobbyData(lobbyId,PasswordKey));
            // Do something with the lobby ID
        }
    }
}
