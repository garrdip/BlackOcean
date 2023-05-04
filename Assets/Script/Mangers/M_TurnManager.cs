using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;

public class M_TurnManager : NetworkBehaviour
{
    public static M_TurnManager Instance = null;

    // 서버에서 관리할 player 리스트
    public List<GamePlayer> players;

    // 서버에서 관리하지만 유저도 쓸지 몰라서 일단 SyncList
    public readonly SyncList<GamePlayer> playerOrder = new SyncList<GamePlayer>();

    [SyncVar]
    public GamePlayer currentPlayer;

    public GameObject orderUI;

    public bool isOrderSelect = false;
    public bool isMyTurn = false;

    public List<Button> selectOrderButtons;

    public GameObject startButton;

    [Header("Spwan Object Transfrom Group")]
    public Transform playerSpawnLocation;
    public Transform monsterSpawnLocation;

    public List<TargetObject> spawnedPlayerList = new List<TargetObject>();
    List<TargetObject> spawnedMonsterList = new List<TargetObject>();
    List<TargetObject> monsterOrderList = new List<TargetObject>();

    // 카드와 타겟을 한쌍으로 저장하는 Dictionary타입의 큐
    public Queue<Dictionary<Card, TargetObject>> cardTargetPairQueue = new Queue<Dictionary<Card, TargetObject>>();

    
    // Turn 관리는 서버
    BattleTurn Phase;
    BattleTurn phase {get{
        return Phase;
    }
    set{
        Phase = value;
        OnChangedPhase();
    }}

    public static M_TurnManager instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<M_TurnManager>();
            }
            return Instance;
        }
    }

    public override void OnStartClient()
    {
        playerOrder.Callback += OnPlayerOrderUpdated;
    }

    public void SetOrderButtonListener()
    {
        selectOrderButtons[0].onClick.AddListener(() => SelectOrder(1));
        selectOrderButtons[1].onClick.AddListener(() => SelectOrder(2));
        selectOrderButtons[2].onClick.AddListener(() => SelectOrder(3));
    }

    

    [Command(requiresAuthority = false)]
    public void SetNextTurn()
    {
        phase = BattleTurn.PLAYER_END;
    }

    [Server]
    public void SetCurrentPlayer()
    {
        if(currentPlayer == null)
            currentPlayer = playerOrder[0];
        else if(currentPlayer == playerOrder[playerOrder.Count -1])
            currentPlayer = playerOrder[0];
        else
        {
            for(int i = 0 ; i < playerOrder.Count ; i ++)        
            {
                if(currentPlayer == playerOrder[i])
                {
                    currentPlayer = playerOrder[i+1];
                    break;
                }
            }
        }
    }

    [Server]
    public void HandleStartBattle()
    {
        M_MapManager.instance.StartBattle();
        GeneratePlayerUnit();
        GenerateMonster();
        phase = BattleTurn.BATTLE_STANDBY;
        StartCoroutine(ProcessCardQueue());
    }
 
    public IEnumerator ProcessCardQueue()
    {
        // 무한루프에서 인스턴스 생성시 생기는 가비지 방지를 위해 함수호출에서 미리 인스턴스 생성하여 캐싱후 루프 안에서 사용
        WaitForSeconds waitForDelay = new WaitForSeconds(2.0f);
        WaitForSeconds waitForLoop = new WaitForSeconds(0.01f);
        while (true)
        {
            if(cardTargetPairQueue.Count != 0){
                // TODO : 큐에서 하나씩 빼서 카드의 타겟에 대한 로직 수행
                Dictionary<Card, TargetObject> pairs = cardTargetPairQueue.Dequeue();
                foreach(KeyValuePair<Card, TargetObject> pair in pairs){
                    Debug.Log("카드: " + pair.Key);
                    Debug.Log("타겟: " + pair.Value);
                }
                yield return waitForDelay;
            }
            yield return waitForLoop;
        }
    }

    [Server]
    public void PlayerOrderSelectPhase()
    {
        foreach(GamePlayer player in players)
        {
            int order = 0;
            for(int i = 0 ; i < players.Count ; i ++)
            {
                if(player.selectOrder > players[i].selectOrder) order++;
            }
            playerOrder[order] = player;
        }
        phase = BattleTurn.PLAYER_PREEFFECT;
    }

    [Server]
    public void OnChangedPhase()
    {
        switch(phase)
        {
            case BattleTurn.BATTLE_STANDBY :
                BattleStandby();
                break;
            case BattleTurn.PLAYER_ORDERSELECT :
                PlayerOrderSelectPhase();
                break;
            case BattleTurn.PLAYER_PREEFFECT :
                PlayerPreEffect();
                break;
            case BattleTurn.PLAYER_DRAW :
                PlayerCardDraw();
                break;
            case BattleTurn.PLAYER_ACTIVE :
                break;
            case BattleTurn.PLAYER_END :
                PlayerEndTurn();
                break;
            case BattleTurn.MONSTER_ORDERSELECT :
                MonsterSetOrder();
                StartCoroutine(DebugDelay()); // Todo
                break;
            case BattleTurn.MONSTER_PREEFFECT :
                MonsterPreEffect();
                break;
            case BattleTurn.MONSTER_ACTIVE :
                MonsterActive();
                break;
        }
    }

    // 디버그 용도로 딜레이 주는 함수(TEMP)
    IEnumerator DebugDelay()
    {
        yield return new WaitForSeconds(1.0f);
        phase = BattleTurn.MONSTER_PREEFFECT;
    }

    [Server]
    public void BattleStandby()
    {
        foreach(TargetObject monster in spawnedMonsterList)
        {
            monster.monster.SetNextAction();
            monster.monster.SetNextTarget();
        }
        phase = BattleTurn.PLAYER_ORDERSELECT;
    }

    [Server]
    public void MonsterActive()
    {
        StartCoroutine(MonsterActionSeuqence());
    }
    IEnumerator MonsterActionSeuqence()
    {
        foreach(TargetObject monster in spawnedMonsterList)
        {
            monster.monster.DoAction();
            while(true)
            {
                yield return new WaitForSeconds(0.01f);
                if(!monster.GetComponentInChildren<AnimationEventHandler>().isAnimating) break;
            }
        }
        phase = BattleTurn.BATTLE_STANDBY;
    }


    [Server]
    public void MonsterSetOrder()
    {
        monsterOrderList.Clear();
        // 일반적으로 전열의 몬스터먼저 행동 // 다른경우 이부분 수정
        for(int i = 0 ;i < spawnedMonsterList.Count ; i ++)
        {
            monsterOrderList.Add(spawnedMonsterList[i]);
        }
        //phase = BattleTurn.MONSTER_PREEFFECT;
    }

    [Server]
    public void MonsterPreEffect()
    {
        phase = BattleTurn.MONSTER_ACTIVE;
    }

    [Server]
    public void PlayerCardDraw()
    {
        OnTurnChanged(currentPlayer);
    }

    [Server]
    public void PlayerPreEffect()
    {
        SetCurrentPlayer();
        phase = BattleTurn.PLAYER_DRAW;
    }

    [Server]
    public void PlayerEndTurn()
    {
        if(currentPlayer == playerOrder[playerOrder.Count - 1])
            phase = BattleTurn.MONSTER_ORDERSELECT;
        else
        {
            SetCurrentPlayer();
            phase = BattleTurn.PLAYER_DRAW;
        }
    }

    [Server]
    public void GeneratePlayerUnit()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int i = 0 ;i < playerOrder.Count ; i ++)
        {
            GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),playerSpawnLocation.GetChild(i).transform.position,Quaternion.identity);
            NetworkServer.Spawn(avatar);
            avatar.GetComponent<TargetObject>().player = playerOrder[i];
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
            spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
        }
    }

    [Server]
    public void GenerateMonster()
    {
        int num;
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        while(true)
        {
            //위험도 일치하는 선에서 랜덤한 몹 찾아야함
            num = Random.Range(0,M_MonsterManager.monsterGroups.Count - 1);
            break;
        }
        for(int i = 0 ; i < M_MonsterManager.monsterGroups[num].monsters.Count ; i ++)
        {
            var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "SpawnedMonster"),monsterSpawnLocation.GetChild(i).transform.position,Quaternion.identity).GetComponent<SpawnedMonster>();
            NetworkServer.Spawn(monster.gameObject);
            monster.monsterData = M_MonsterManager.monsterGroups[num].monsters[i];
            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),monsterSpawnLocation.GetChild(i).transform.position,Quaternion.identity);
            NetworkServer.Spawn(avatar);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;
            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            RpcMonsterInit(avatar.GetComponent<TargetObject>(), monster);
        }
    }

    [ClientRpc]
    public void RpcMonsterInit(TargetObject avatar, SpawnedMonster monster)
    {
        monster.transform.SetParent(avatar.transform);
    }

    [Server]
    public void InitiateGamePlayerList()
    {
        players = new List<GamePlayer>(FindObjectsOfType<GamePlayer>());
        foreach(GamePlayer player in players)
        {
            playerOrder.Add(player);
        }
    }

    public void SelectOrder(int num)
    {
        NetworkClient.localPlayer.GetComponent<GamePlayer>().SetOrderByUI(num);
    }

    [ClientRpc]
    public void PopUpOrderUI()
    {
        orderUI.SetActive(true);
        isOrderSelect = true;
    }

    [ClientRpc]
    public void OnTurnChanged(GamePlayer newGamePlayer)
    {
        if(IsCurrentPlayerTurn(newGamePlayer)){
            isMyTurn = true;
            GetCardFromPrefareDeck();
        }else{
            isMyTurn = false;
        }
    }

    // prefareDeck에서 카드 가져와서 생성
    public void GetCardFromPrefareDeck()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdSpawnCardOnHand();
            }
        }
    }

    // 현재 로컬플레이어의 턴인지 아닌지 bool값 반환
    public bool IsCurrentPlayerTurn(GamePlayer currentTurnPlayer)
    {
        return NetworkClient.connection != null 
            && NetworkClient.active
            && NetworkClient.connection.identity == currentTurnPlayer.GetComponent<NetworkIdentity>()
            && currentTurnPlayer.isLocalPlayer;
    }

    // 게임화면의 DeckUI에 있는 플레이어 정보 및 턴 정보 뷰 세팅
    private void SetPlayerOrderView(GamePlayer gamePlayer, int index)
    {
        OrderUI orderUI =  DeckUI.instance.playerOrderList[index].GetComponent<OrderUI>();
        orderUI.textPlayerName.text = SteamFriends.GetFriendPersonaName((CSteamID)gamePlayer.steamID);
        if(gamePlayer.isLocalPlayer){
            orderUI.playerOwnMenu.gameObject.SetActive(true); // 전용 메뉴 활성화
            float width = orderUI.buttonPlayerOrder.GetComponent<RectTransform>().rect.width;
            float height = orderUI.buttonPlayerOrder.GetComponent<RectTransform>().rect.height;
            orderUI.buttonPlayerOrder.GetComponent<RectTransform>().sizeDelta = new Vector2(width + 30f, height + 30f); // 버튼 크기 크게 변경(변경된 값이 native size)
        }
    }


    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//
    private void OnPlayerOrderUpdated(SyncList<GamePlayer>.Operation op, int index, GamePlayer oldGamePlayer, GamePlayer newGamePlayer)
    {
        switch (op)
        {
            case SyncList<GamePlayer>.Operation.OP_ADD:
                SetPlayerOrderView(newGamePlayer, index);
                break;
            case SyncList<GamePlayer>.Operation.OP_INSERT:
                
                break;
            case SyncList<GamePlayer>.Operation.OP_REMOVEAT:

                break;
            case SyncList<GamePlayer>.Operation.OP_SET:
                // TODO : 인덱스가 바뀔 때
                break;
            case SyncList<GamePlayer>.Operation.OP_CLEAR:
                
                break;
        }
    }
}
