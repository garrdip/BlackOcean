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
        get { return isServer ? NetLookup.Server<GamePlayer>(currentGamePlayerNetId) : NetLookup.Client<GamePlayer>(currentGamePlayerNetId); }
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

    public override void OnStartServer()
    {
        PlayerRegistry.Register(this);
    }

    public override void OnStartClient()
    {
        PlayerRegistry.Register(this);
    }

    void OnDestroy()
    {
        PlayerRegistry.Unregister(this);
    }

    public override void OnStartLocalPlayer()
    {
        PlayerRegistry.SetLocal(this);
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
            GamePlayer[] gamePlayers = FindObjectsByType<GamePlayer>(FindObjectsSortMode.None);
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
        // GamePlayer 오브젝트 생성
        GameObject gamePlayerObject = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "GamePlayer"),new Vector3(0,0,0),Quaternion.identity);
        GamePlayer gamePlayer = gamePlayerObject.GetComponent<GamePlayer>();
        gamePlayer.objectOwner = this;
        gamePlayer.character = character;
        gamePlayer.selectOrder = selectOrder;
        gamePlayer.HP = 50;
        gamePlayer.MaxHP = 50;
        gamePlayer.recoveryValue = 15;
        gamePlayer.gold = 100;
        NetworkServer.Spawn(gamePlayerObject, connectionToClient);

        // 게임씬에서 플레이어 오더 및 정보들을 보여주는 PlayerOrder 오브젝트 생성
        GameObject playerOrderObject = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "PlayerOrder"), Vector3.zero, Quaternion.identity);
        PlayerOrder playerOrder = playerOrderObject.GetComponent<PlayerOrder>();
        playerOrder.gamePlayerNetId = gamePlayer.netId;
        NetworkServer.Spawn(playerOrderObject, connectionToClient);
    }

    // ---------------------------------------------------------------- ClientRpc Method -------------------------------------------------------------//

    [ClientRpc]
    public void GenerateGamePlayerDeck()
    {
        if(isLocalPlayer)
        {
            foreach(GamePlayer gamePlayer in FindObjectsByType<GamePlayer>(FindObjectsSortMode.None))
            {
                if(gamePlayer.isOwned){
                    currentGamePlayerNetId = gamePlayer.netId;
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
    
    // 훅은 로컬 UI 갱신 + 서버에 "상태 변경됨" 알림만 담당하고,
    // 전원 상태 집계와 흐름 전이는 상태머신 소유자인 M_TurnManager가 판정한다 (순환 참조 축소).
    public void OnEndTurnStateChanged(bool oldVal, bool newVal)
    {
        if(isLocalPlayer){
            EndTurnButton endTurnButton = GameUIManager.instance.buttonEndTurn.GetComponent<EndTurnButton>();
            endTurnButton.SetEndTurnButtonActiveState(newVal);
        }
        if(isServer)
            M_TurnManager.instance.CheckAllPlayersEndTurn();
    }

    public void OnCompleteReward(bool oldVal, bool newVal)
    {
        if(isServer)
            M_TurnManager.instance.CheckAllPlayersRewardDone();
        if(isOwned){
            PopUpUIManager.instance.HandleHideBattleResultPopUp(); // 전투 결과 팝업 비활성화
        }
    }

    public void OnReadyStateChanged(bool oldVal, bool newVal)
    {
        if(isLocalPlayer){
            ReadyButtonOnMap readyButtonOnMap = MapUI.instance.readyButton.GetComponent<ReadyButtonOnMap>();
            readyButtonOnMap.SetReadyButtonViewByReadyState(newVal);
        }
        onChangeReady?.Invoke(newVal);
        if(isServer)
            M_TurnManager.instance.CheckAllPlayersReadyForMapMove();
    }


    void OnChangeCurrentGamePlayerNetId(uint oldVal, uint newVal)
    {
        if(isServer && oldVal != 0 && newVal != 0){
            // 이전에 선택한 플레이어 카드포켓
            CardPocket prevCardPocket = NetLookup.Server<GamePlayerDeck>(oldVal).cardPocket;
            
            // 현재 선택한 플레이어 카드포켓
            CardPocket currentCardPocket = NetLookup.Server<GamePlayerDeck>(newVal).cardPocket ;

            // 위치 스왑
            Sequence sequence = DOTween.Sequence();
            sequence.Append(prevCardPocket.transform.DOMoveY(-100f, 0.5f));
            sequence.Join(currentCardPocket.transform.DOMoveY(-8f, 0.5f));

            // 현재 선택한 플레이어의 PrefareDeck, TrashDeck, ForgottenDeck 카운트 텍스트 설정
            GamePlayerDeck currentGamePlayerDeck = NetLookup.Server<GamePlayerDeck>(newVal);
            
            GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonPrefareDeck);
            GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonTrashDeck);
            GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonForgottenDeck);
            GameUIManager.instance.textPrefareDeckCount.text = currentGamePlayerDeck.prefareDeck.Count.ToString();
            GameUIManager.instance.textTrashDeckCount.text = currentGamePlayerDeck.trashDeck.Count.ToString();
            GameUIManager.instance.textForgottenDeckCount.text = currentGamePlayerDeck.forgottenDeck.Count.ToString();
            GameUIManager.instance.currentIchiText.text = currentGamePlayerDeck.currentIchi.ToString();
            GameUIManager.instance.maxIchiText.text = currentGamePlayerDeck.maxIchi.ToString();

            // 현재 선택한 플레이어의 어빌리티 버튼만 활성화
            foreach(GamePlayer gamePlayer in ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                if(gamePlayerDeck.abilityButton != null){
                    gamePlayerDeck.abilityButton.gameObject.SetActive(gamePlayerDeck.netId == newVal);
                }
            }

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
            foreach(PlayerInterface pi in PlayerRegistry.All)
            {
                if(!pi.cardThrowAwayDone)
                    return;
            }
            StartCoroutine(M_CardManager.instance.CurseCardOperation());
        }
    }

    
}
