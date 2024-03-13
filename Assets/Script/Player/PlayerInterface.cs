using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Steamworks;
using DG.Tweening;

public class PlayerInterface : NetworkBehaviour
{
    public delegate void OnChangeReady(bool isReady);
    public OnChangeReady onChangeReady;

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

    [SyncVar]
    public bool isAvatarUploadDone = false;

    public readonly SyncList<CardOnHand> destroyCards = new SyncList<CardOnHand>();

    [SyncVar (hook = nameof(OnChangedWorkDoneState))]
    public bool workDone = false;

    [SyncVar (hook = nameof(OnChangedCardThroAwayDone))]
    public bool cardThrowAwayDone = false;

    [ClientRpc]
    public void ClearWorkDone()
    {
        if(isLocalPlayer)
        {
            workDone = false;
        }
    }

    public override void OnStartLocalPlayer()
    {
        if(isLocalPlayer)
        {
            GenerateGamePlayer();
            StartCoroutine(WaitInitState());
        }
    }

    IEnumerator WaitInitState()
    {
        while(!workDone)
        {
            yield return new WaitForSeconds(0.01f);
            GamePlayer[] gamePlayers = FindObjectsOfType<GamePlayer>();
            foreach(GamePlayer gamePlayer in gamePlayers)
                if(gamePlayer.isOwned)
                {
                    workDone = true;
                }
        }
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
        gamePlayer.HP = 50;
        gamePlayer.MaxHP = 50;
        NetworkServer.Spawn(cloneAvatar,connectionToClient);
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    public void GenerateGamePlayerDeck()
    {
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
        workDone = true;
    }

    [ClientRpc]
    public void RemoveDestroyCardList(CardOnHand cardOnHand)
    {
        if(isOwned)
        {
            destroyCards.Remove(cardOnHand);
        }
    }

    [ClientRpc]
    public void UploadAvatar()
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
            workDone = true;
        }
    }

    [ClientRpc]
    public void SetIsReadyStateDefault()
    {
        if(isOwned){
            isReady = false;
            OnReadyStateChanged(false, false);
        }
    }

    [ClientRpc]
    public void SetEndTurnActiveStateDefault()
    {
        if(isOwned){
            endTurnActive = false;
            OnEndTurnStateChanged(false, false);
        }
    }

    [ClientRpc]
    public void SetCompleteRewardStateDefault()
    {
        if(isOwned){
            isRewardDone = false;
            OnCompleteReward(false, false);
        }
    }

    [ClientRpc]
    public void SetDefaultStateofCardThrowDone()
    {
        if(isOwned)
            cardThrowAwayDone = false;
    }

    // ---------------------------------------------------------------- SyncVar Hook Method ----------------------------------------------------------//
    
    public void OnEndTurnStateChanged(bool oldVal, bool newVal)
    {
        if(isLocalPlayer){
            EndTurnButton endTurnButton = GameUIManager.instance.buttonEndTurn.GetComponent<EndTurnButton>();
            endTurnButton.SetEndTurnButtonActiveState(newVal);
        }
        if(isServer)
        {
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
            {
                if(!user.endTurnActive && user.currentGamePlayer.HP > 0)return;
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
        if(isLocalPlayer){
            ReadyButtonOnMap readyButtonOnMap = MapUI.instance.readyButton.GetComponent<ReadyButtonOnMap>();
            readyButtonOnMap.SetReadyButtonViewByReadyState(newVal);
        }
        if(onChangeReady != null){
            onChangeReady.Invoke(newVal);
        }
        if(isServer)
        {
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface player in users){
                if(!player.isReady) return;
            }
            // 플레이어들이 투표한 결과 선택된 맵 위치로 이동
            HexagonMapRoom hexagonMapRoom = M_MapManager.instance.GetVoteHexagonMapRoomResult();
            if(hexagonMapRoom != null){
                if(hexagonMapRoom == M_MapManager.instance.currentRoom) return;
                M_TurnManager.instance.EnterTheRoom(hexagonMapRoom); // 모든 플레이어 레디상태 확인 시 전투 시작
            }
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
            GameUIManager.instance.currentIchiText.text = currentGamePlayerDeck.currentIchi.ToString();
            GameUIManager.instance.maxIchiText.text = currentGamePlayerDeck.maxIchi.ToString();

            // 현재 선택한 플레이어가 단향일 경우 어빌리티 버튼 활성화 상태 변경
            GamePlayer gamePlayer = currentGamePlayerDeck.GetComponent<GamePlayer>();
            M_CardManager.instance.ChangeAbilityButtonActiveState(gamePlayer.character == Character.HONGDANHYANG);

            // 현재 선택한 플레이어의 패 제거 카드 배열값 설정
            currentGamePlayerDeck.choosedCardOnHands = new CardOnHand[currentGamePlayerDeck.maxRemoveCardCount];
        }
    }

    void OnChangedWorkDoneState(bool oldVal, bool newVal)
    {
        if(isServer)
        {
            if(newVal)
                M_LoadingManager.instance.CheckWorkDone();
            else
                M_LoadingManager.instance.CheckWorkDoneClear(); 
        }       
    }

    void OnChangedCardThroAwayDone(bool oldVal,bool newVal)
    {
        if(isServer)
        {
            foreach(PlayerInterface pi in FindObjectsOfType<PlayerInterface>())
            {
                if(!pi.cardThrowAwayDone)
                    return;
            }
            StartCoroutine(M_CardManager.instance.CurseCardOperation());
        }
    }

    
}
