using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Steamworks;
using DG.Tweening;

public class PlayerInterface : NetworkBehaviour
{
    public GamePlayer currentGamePlayer {
        get { return isServer ? NetworkServer.spawned[currentGamePlayerNetId].GetComponent<GamePlayer>() : NetworkClient.spawned[currentGamePlayerNetId].GetComponent<GamePlayer>(); }
    }

    public readonly SyncList<GamePlayer> ownedPlayers = new SyncList<GamePlayer>();
    
    [SyncVar (hook = nameof(OnChangeCurrentGamePlayerNetId))]
    public uint currentGamePlayerNetId = 0;

    [SyncVar]
    public Character character;

    [SyncVar]
    public Color color;
    
    [SyncVar]
    public int selectOrder;

    [SyncVar]
    public bool isInitializeDone = false;

    [SyncVar (hook = nameof(OnReadyStateChanged))]
    public bool isReady = false;

    [SyncVar (hook = nameof(OnCompleteReward))]
    public bool isRewardDone = false;

    [SyncVar]
    public ulong steamID;

    [SyncVar]
    public string steamPersonaName;

    [SyncVar (hook = nameof(OnEndTurnStateChanged))]
    public bool endTurnActive = false;

    [SyncVar]
    public bool isTargetObjectInitDone = false;

    public readonly SyncList<byte> avatarImage = new SyncList<byte>();

    [SyncVar]
    public int avatarWidth,avatarHeight;

    [SyncVar (hook = nameof(OnChangedAvatar))]
    public bool isAvatarUploadDone = false;

    public readonly SyncList<CardOnHand> destroyCards = new SyncList<CardOnHand>();

    [SyncVar]
    public bool isLoadDone = false;



    public override void OnStartLocalPlayer()
    {
        // Server Loading 종료 후 1층 데이터 생성
        if(isLocalPlayer)
        {
            if(isServer)
            {
                if(M_SaveManager.instance.isSaveGame)
                {
                    M_MapManager.instance.RegenerateStartHexsagonRoom(M_SaveManager.instance.loadData); // 육각형 방 생성(진행중)
                    M_MapManager.instance.RegenerateColorRegion(M_SaveManager.instance.loadData);
                }
                else
                {
                    M_MapManager.instance.GenerateStartHexagonRoom(); // 육각형 방 생성(진행중)
                    M_MapManager.instance.GenerateColorRegion();
                }
            }
            GenerateGamePlayer();
            M_MapManager.instance.GenerateHexgonGrid(40);         
            StartCoroutine(WaitGamePlayerGen());
            StartCoroutine(nameof(WaitPlayerList));
        }
    }

    IEnumerator WaitGamePlayerGen()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        while(true)
        {
            GamePlayer[] gamePlayers = FindObjectsOfType<GamePlayer>();
            if(gamePlayers.Length == netManager.roomSlots.Count) break;
            yield return new WaitForSeconds(0.01f);
        }
        isInitializeDone = true;
    }

    IEnumerator WaitPlayerList()
    {
        M_NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        WaitForSeconds loopSecond = new WaitForSeconds(0.01f);
        //GamePlayer가 모두 로드 될때까지 기다림
        while(true)
        {
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            if(users.Length == netManger.roomSlots.Count) break;
            yield return loopSecond;
        }
        //GamePlayer가 모두 Initial Value 초기화 될때까지 기다림
        while(true)
        {
            int cnt = 0;
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
            {
                if(user.isInitializeDone) cnt++;
            }
            if(cnt == netManger.roomSlots.Count) break;
            yield return loopSecond;
        }
        if(isLocalPlayer)
        {
            foreach(GamePlayer gamePlayer in FindObjectsOfType<GamePlayer>())
            {
                if(gamePlayer.isOwned){
                    currentGamePlayerNetId = gamePlayer.netId;
                    M_CardManager.instance.SetCurrentGamePlayerDeck(gamePlayer.GetComponent<GamePlayerDeck>());
                }
            }
            ownedPlayers.Add(currentGamePlayer);
            GetComponent<PlayerInterfaceServer>().GenerateGamePlayerOwnedObjects(currentGamePlayer);
        }
        SetUserStatusUI();
        M_TurnManager.instance.SetOrderButtonListener();
        // 플레이어 로딩이 끝나면 턴매니저로 플레이어 리스트를 전달함
        if(isServer)
        {
            M_TurnManager.instance.InitiateGamePlayerList();
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
                user.UploadAvatar();
        }
        //UI Update
        MapUI.instance.UpdateProfile();
        if(isServer){
            M_MapManager.instance.SetRegionWithColorRPC();
            StartCoroutine(WaitLoadDone());
        }
    }

    IEnumerator WaitLoadDone()
    {
        M_NetworkRoomManager netManger = NetworkRoomManager.singleton as M_NetworkRoomManager;
        WaitForSeconds loopSecond = new WaitForSeconds(0.01f);
        while(true)
        {
            int cnt = 0;
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
            {
                if(user.isLoadDone) cnt++;
            }
            if(cnt == netManger.roomSlots.Count) break;
            yield return loopSecond;
        }
        M_LoadingManager.instance.SetLoadingScreen(false);
    }

    public void SetUserStatusUI()
    {
        //변경 필요
    }

    // ------------------------------------------------------------- Command Method ------------------------------------------------------------------//

    [Command]
    void GenerateGamePlayer()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        var cloneAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "GamePlayer"),new Vector3(0,0,0),Quaternion.identity);
        
        GamePlayer gamePlayer = cloneAvatar.GetComponent<GamePlayer>();
        gamePlayer.objectOwner = this;
        gamePlayer.character = character;
        gamePlayer.selectOrder = selectOrder;
        NetworkServer.Spawn(cloneAvatar,connectionToClient);
    }

    // 맵 씬 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSendChatMessage(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message)){
            string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
            RpcReceiveChatMessage(color, playerName, message.Trim());
        }
    }

    // 전투 씬 채팅 메시지 이벤트 송신
    [Command]
    public void CmdSendChatMessageGameScene(string message, NetworkConnectionToClient sender = null)
    {
        if (!string.IsNullOrWhiteSpace(message)){
            string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
            RpcReceiveChatMessageGameScene(color, playerName, message.Trim());
        }
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    public void RemoveDestroyCardList(CardOnHand cardOnHand)
    {
        if(isOwned)
        {
            destroyCards.Remove(cardOnHand);
        }
    }

    [ClientRpc]
    void UploadAvatar()
    {
        if(isLocalPlayer)
        {
            byte[] uploadableImage;
            int imageId = SteamFriends.GetSmallFriendAvatar((CSteamID)steamID);
            uploadableImage = M_SteamManager.instance.GetSteamImageAsByteArray(imageId,out bool isValid, out uint width, out uint height);
            if(isValid)
            {
                avatarWidth = (int)width;
                avatarHeight = (int)height;
                for(int i = 0 ;i < uploadableImage.Length ; i ++)
                    avatarImage.Add(uploadableImage[i]);
            }
            isAvatarUploadDone = true;
        }
    }

    [ClientRpc]
    public void SetIsReadyStateDefault()
    {
        if(netIdentity == NetworkClient.connection.identity)
            isReady = false;
    }

    [ClientRpc]
    public void SetEndTurnActiveStateDefault()
    {
        if(netIdentity == NetworkClient.connection.identity)
            endTurnActive = false;
    }

    [ClientRpc]
    public void SetCompleteRewardStateDefault()
    {
        if(netIdentity == NetworkClient.connection.identity)
            isRewardDone = false;
    }

    // 전투 씬 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessageGameScene(Color color, string playerName, string message)
    {
        GameUIManager.instance.AppendMessage(color, playerName, message);
    }

    // 맵 씬 채팅 메시지 이벤트 수신
    [ClientRpc]
    void RpcReceiveChatMessage(Color color, string playerName, string message)
    {
        MapUI.instance.AppendMessage(color, playerName, message);
    }


    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//
    
    public void OnEndTurnStateChanged(bool oldVal, bool newVal)
    {
        if(isServer)
        {
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
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
    
    public void OnCompleteReward(bool oldVal, bool newVal)
    {
        if(isServer)
        {
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface player in users)
            {
                if(!player.isRewardDone) return;
            }
            foreach(PlayerInterface player in users)player.SetCompleteRewardStateDefault();
            M_TurnManager.instance.NoneBattleEnd();
        }   
    }
    
    public void OnReadyStateChanged(bool oldVal, bool newVal)
    {
        MapUI.instance.UpdateProfile();
        if(isServer)
        {
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface player in users){
                if(!player.isReady) return;
            }
            foreach(PlayerInterface player in users){
                player.SetIsReadyStateDefault(); // 레디 상태 모두 확인후 다시 Flase로 되돌림 (여러군데서 사용 예정)
            }
            // 플레이어들이 투표한 결과 선택된 맵 위치로 이동
            HexagonMapRoom hexagonMapRoom = M_MapManager.instance.GetVoteHexagonMapRoomResult();
            if(hexagonMapRoom != null){
                M_TurnManager.instance.EnterTheRoom(hexagonMapRoom); // 모든 플레이어 레디상태 확인 시 전투 시작
            }
        }    
    }

    public void OnChangedSelectOrder(int oldVal,int newVal)
    {
        MapUI.instance.UpdateProfile();
        if(isLocalPlayer)
            MapUI.instance.SetOrderIndicator(newVal);
    }

    void OnChangedAvatar(bool oldVal, bool newVal)
    {
        if(newVal == true)
        {
            MapUI.instance.UpdateProfile();
        }
    }

    void OnChangeCurrentGamePlayerNetId(uint oldVal, uint newVal)
    {
        if(isServer && oldVal != 0 && newVal != 0){
            // 이전에 선택한 플레이어 카드포켓
            CardPocket prevCardPocket = NetworkServer.spawned[oldVal].GetComponent<GamePlayerDeck>().cardPocket;
            
            // 현재 선택한 플레이어 카드포켓
            CardPocket currentCardPocket = NetworkServer.spawned[newVal].GetComponent<GamePlayerDeck>().cardPocket ;

            // 위치 스왑
            Sequence sequence = DOTween.Sequence();
            sequence.Append(prevCardPocket.transform.DOMoveY(-100f, 0.5f));
            sequence.Join(currentCardPocket.transform.DOMoveY(-8f, 0.5f));

            // 현재 선택한 플레이어의 PrefareDeck, TrashDeck 카운트 텍스트 설정
            GamePlayerDeck currentGamePlayerDeck = NetworkServer.spawned[newVal].GetComponent<GamePlayerDeck>();
            GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textPrefareDeckCount, currentGamePlayerDeck.prefareDeck.Count);
            GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textTrashDeckCount, currentGamePlayerDeck.trashDeck.Count);

            // 현재 선택한 플레이어가 단향일 경우 어빌리티 버튼 활성화 상태 변경
            GamePlayer gamePlayer = currentGamePlayerDeck.GetComponent<GamePlayer>();
            M_CardManager.instance.ChangeAbilityButtonActiveState(gamePlayer.character == Character.HONGDANHYANG);
        }
    }
}
