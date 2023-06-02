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
    public List<TargetObject> spawnedMonsterList = new List<TargetObject>();
    public List<TargetObject> cloneMonsterList = new List<TargetObject>();
    List<TargetObject> monsterOrderList = new List<TargetObject>();

    // 카드와 타겟을 한쌍으로 저장하는 Dictionary타입의 큐
    public Queue<(Card, TargetObject[])> cardTargetPairQueue = new Queue<(Card, TargetObject[])>();

    
    // Turn 관리는 서버
    BattleTurn Phase;
    BattleTurn phase {get{
        return Phase;
    }
    set{
        Phase = value;
        OnChangedPhase();
    }}

    public TargetObject[] GetTargetObjects()
    {
        return spawnedMonsterList.ToArray();
    }

    public TargetObject[] GetCloneTargetObject()
    {
        return cloneMonsterList.ToArray();
    }

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
        Debug.Log("시작!!!!!!!!!!!");
        M_MapManager.instance.StartBattle();
        GeneratePlayerUnit();
        GenerateMonster();
        phase = BattleTurn.BATTLE_STANDBY;
        StartCoroutine(ProcessCardQueue());
    }
 
    public IEnumerator ProcessCardQueue()
    {
        // 무한루프에서 인스턴스 생성시 생기는 가비지 방지를 위해 함수호출에서 미리 인스턴스 생성하여 캐싱후 루프 안에서 사용
        WaitForSeconds waitForDelay = new WaitForSeconds(5.0f);
        WaitForSeconds waitForLoop = new WaitForSeconds(0.01f);
        while (true)
        {
            if(cardTargetPairQueue.Count != 0){
                // TODO : 큐에서 하나씩 빼서 카드의 타겟에 대한 로직 수행
                (Card card,TargetObject[] tar) = cardTargetPairQueue.Dequeue();
               
                Debug.Log("카드: " + card.baseCard.name);
                if(card.baseCard.isTargetable)
                {
                    if(tar[0].player == null)
                        Debug.Log("타겟: " + tar[0].monster.monsterData.name);
                    else
                        Debug.Log("타겟: " + tar[0].player.netIdentity);
                }

                CardData.RunCard(card,tar);
                yield return waitForDelay;
            }
            yield return waitForLoop;
        }
    }
    [Server]
    public void ProcessCardPredict(Card card,TargetObject[] tar)
    {
        CardData.RunCard(card,tar);
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
        Debug.Log("Phase is " + phase);
        switch(phase)
        {
            case BattleTurn.BATTLE_STANDBY :
                BattleStandby();
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
        phase = BattleTurn.PLAYER_PREEFFECT;
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
        EachPlayerCardDraw();
    }

    [ClientRpc]
    public void EachPlayerCardDraw()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            GamePlayerDeck gamePlayerDeck = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayerDeck>();
            if(gamePlayerDeck.isLocalPlayer){
                gamePlayerDeck.CmdSpawnCardOnHand();
            }
        }
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
        phase = BattleTurn.MONSTER_ORDERSELECT;
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
            // 타겟 유효 판단을 위한 클론 데이터 //
            GameObject clone = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);
            NetworkServer.Spawn(clone);
            clone.GetComponent<TargetObject>().cloneGamePlayer = new CloneGamePlayer(playerOrder[i]);
            clone.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;

            avatar.GetComponent<TargetObject>().clone = clone.GetComponent<TargetObject>();

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
            var cloneMonster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "SpawnedMonster"),new Vector3(-300,-300,0),Quaternion.identity).GetComponent<SpawnedMonster>();

            NetworkServer.Spawn(monster.gameObject);
            NetworkServer.Spawn(cloneMonster.gameObject);

            monster.monsterData = M_MonsterManager.monsterGroups[num].monsters[i];
            cloneMonster.monsterData = M_MonsterManager.monsterGroups[num].monsters[i];


            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),monsterSpawnLocation.GetChild(i).transform.position,Quaternion.identity);
            var cloneAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);

            NetworkServer.Spawn(avatar);
            NetworkServer.Spawn(cloneAvatar);

            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;

            cloneAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            cloneAvatar.GetComponent<TargetObject>().monster = cloneMonster;
            avatar.GetComponent<TargetObject>().clone = cloneAvatar.GetComponent<TargetObject>();

            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            cloneMonsterList.Add(cloneAvatar.GetComponent<TargetObject>());
            RpcMonsterInit(avatar.GetComponent<TargetObject>(), monster);
            RpcMonsterInit(cloneAvatar.GetComponent<TargetObject>(), cloneMonster);
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

    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//
    private void OnPlayerOrderUpdated(SyncList<GamePlayer>.Operation op, int index, GamePlayer oldGamePlayer, GamePlayer newGamePlayer)
    {
        switch (op)
        {
            case SyncList<GamePlayer>.Operation.OP_ADD:

                break;
            case SyncList<GamePlayer>.Operation.OP_INSERT:
                
                break;
            case SyncList<GamePlayer>.Operation.OP_REMOVEAT:

                break;
            case SyncList<GamePlayer>.Operation.OP_SET:
                // TODO : 인덱스가 바뀔 때(플레이어 정렬 순서 바뀔 때 로직 구현부)
                break;
            case SyncList<GamePlayer>.Operation.OP_CLEAR:
                
                break;
        }
    }
}
