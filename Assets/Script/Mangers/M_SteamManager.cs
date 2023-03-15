using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;

public class M_SteamManager : MonoBehaviour
{
    [SerializeField]
    private GameObject buttons = null;
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    private const string HostAddressKey = "HostAddress";
    public M_NetworkRoomManager networkManager;

    private void Start()
    {
        if(!SteamManager.Initialized){return;}
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequeseted);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
    }
    public void HostLobby()
    {
        buttons.SetActive(false);
        SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypeFriendsOnly,networkManager.maxConnections);

    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK)
        {
            buttons.SetActive(true);
            return;
        }

        networkManager.StartHost();
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

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
        buttons.SetActive(false);
    }

}
