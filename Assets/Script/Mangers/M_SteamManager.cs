using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;

public class M_SteamManager : InstanceD<M_SteamManager>
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

    public GameObject steamFailUI;

    public static CSteamID enteredLobby;

    string blobbyName;
    string bpassword;

    bool isFirstRequest = false;

    private void Start()
    {
        if(!SteamManager.Initialized){SteamAccessFail(); return;}
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequeseted);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    public void SteamAccessFail()
    {
        steamFailUI.SetActive(true);
    }

    public void HostLobby(string lobbyName, string password)
    {
        SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypePublic,3);
        blobbyName = lobbyName;
        bpassword = password;
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
        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            LobbyNameKey,
            blobbyName);
        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            PasswordKey,
            bpassword);

        Debug.Log("Host Address is " + SteamUser.GetSteamID().ToString());
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

    public void EnterLobby(CSteamID lobbyId)
    {
        string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId,HostAddressKey);
        Debug.Log("Entering " + hostAddress);
        enteredLobby = lobbyId;
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
        if( !isFirstRequest )
        {
            Debug.Log("First Time Request!! Request Again!");
            MultiplayUI.instance.ClearLobbyList();
            isFirstRequest = true;
            SteamMatchmaking.RequestLobbyList();
            return;
        }
        MultiplayUI.instance.ClearLobbyList();
        // m_nLobbiesMatching 검색된 숫자로 반환되며 GetLobby 함수를 이용하여 Get 해야함
        for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId,LobbyNameKey);
            Debug.Log(lobbyName);
            string password = SteamMatchmaking.GetLobbyData(lobbyId,PasswordKey);
            MultiplayUI.instance.AddLobbyData(lobbyId,lobbyName,(password == "")? false : true);
        }
    }

    public byte[] GetSteamImageAsByteArray(int iImage, out bool pIsValid, out uint pWidth, out uint pHeight)
    {
        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);

        if(isValid)
        {
            byte[] image = new byte[width * height * 4];
            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));
            if(isValid) {
                pIsValid = true;
                pWidth = width;
                pHeight = height;
                return image;
            }
        }
      
        pIsValid = false;
        pWidth = 0;
        pHeight = 0;
        return null;
    }

    public Texture2D GetSteamImageAsTexture(byte[] image, int width, int height)
    {
        Texture2D texture = null;

        texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
        texture.LoadRawTextureData(image);
        FlipTextureVertically(texture);
        texture.Apply();

        return texture;
    }

    public Texture2D GetSteamImageAsTextureByImageId(int iImage)
    {
        Texture2D texture = null;
        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if(isValid){
            byte[] image = new byte[width * height * 4];
            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));
            if(isValid){
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false , true);
                texture.LoadRawTextureData(image);
                FlipTextureVertically(texture);
                texture.Apply();
            }
        }
        return texture;
    }

    public void FlipTextureVertically(Texture2D original)
    {
        var originalPixels = original.GetPixels();
        var newPixels = new Color[originalPixels.Length];

        var width = original.width;
        var rows = original.height;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                newPixels[x + y * width] = originalPixels[x + (rows - y - 1) * width];
            }
        }

        original.SetPixels(newPixels);
        original.Apply();
    }
}
