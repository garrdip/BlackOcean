using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;
using Spine.Unity;
using Spine;
using UnityEngine.UI;
using TMPro;

public class M_TurnManager : NetworkBehaviour
{
    public static M_TurnManager Instance = null;

    // 서버에서 관리할 player 리스트
    public List<GamePlayer> players;

    // 서버에서 관리하지만 유저도 쓸지 몰라서 일단 SyncList
    public readonly SyncList<GamePlayer> playerOrder = new SyncList<GamePlayer>();

    // 각 클라이언트에서 참조할 현재 참가한 플레이어들의 타겟오브젝트 목록
    public readonly SyncList<TargetObject> spawnedPlayerSyncList = new SyncList<TargetObject>();

    [SyncVar]
    public GamePlayer currentPlayer;

    public GameObject orderUI;

    public bool isOrderSelect = false;
    public bool isMyTurn = false;
    public bool isCardQueueOperating = false;

    public List<Button> selectOrderButtons;

    public GameObject startButton;

    [Header("Spwan Object Transfrom Group")]
    public Transform playerSpawnLocation;
    public Transform monsterSpawnLocation;

    public List<TargetObject> spawnedPlayerList = new List<TargetObject>();
    public List<TargetObject> clonePlayerList = new List<TargetObject>();
    public List<TargetObject> spawnedMonsterList = new List<TargetObject>();
    public List<TargetObject> cloneMonsterList = new List<TargetObject>();
    List<TargetObject> monsterOrderList = new List<TargetObject>();

    // 카드와 타겟을 한쌍으로 저장하는 큐
    public Queue<(Card, List<TargetObject>)> cardTargetPairQueue = new Queue<(Card, List<TargetObject>)>();
    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */
    
    // Turn 관리는 서버
    public BattleTurn Phase;
    public BattleTurn phase {get{
        return Phase;
    }
    set{
        Phase = value;
        OnChangedPhase();
    }}

    public TargetObject GetPlayer(NetworkIdentity conn)
    {     
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.conn == conn)
            return tar;
        }
        return null;
    }

    public TargetObject GetClonePlayer(NetworkIdentity conn)
    {
        foreach(TargetObject tar in clonePlayerList)
        {
            if(tar.conn == conn)
            return tar;
        }
        return null;
    }

    public List<TargetObject> GetPlayerObjects()
    {
        return spawnedPlayerList;
    }

    public List<TargetObject> GetClonePlayerObjects()
    {
        return clonePlayerList;
    }
    public List<TargetObject> GetMonsterObjects()
    {
        return spawnedMonsterList;
    }

    public List<TargetObject> GetCloneMonsterObjects()
    {
        return cloneMonsterList;
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

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(ProcessCardQueue());
        Debug.Log("Card Queue 코루틴 시작 ");
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

    [Server]
    public void StartWaitCardQueue()
    {
        StartCoroutine(WaitCardQueue());
    }

    IEnumerator WaitCardQueue()
    {
        while(true)
        {
            if(!isCardQueueOperating && cardTargetPairQueue.Count == 0)
            {
                break;
            }
            yield return new WaitForSeconds(0.01f);
        }
        if(phase == BattleTurn.PLAYER_ACTIVE_DONE) // 아무때나 동작하지 않음 (광클방지)
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
    }

    [Server]
    public void GenerateBattleObject()
    {
        if(isServer)
        {
            GeneratePlayerUnit();
            GenerateMonster();
            phase = BattleTurn.BATTLE_STANDBY;
        }
    }
 
    public IEnumerator ProcessCardQueue()
    {
        // 무한루프에서 인스턴스 생성시 생기는 가비지 방지를 위해 함수호출에서 미리 인스턴스 생성하여 캐싱후 루프 안에서 사용
        WaitForSeconds waitForLoop = new WaitForSeconds(0.01f);
        while (true)
        {
            yield return waitForLoop;
            if(CardData.instance.isCardOperating){
                continue;
            }
            else
            {
                if(cardTargetPairQueue.Count != 0){
                    CardData.instance.isCardOperating = true;
                    isCardQueueOperating = true;
                    // TODO : 큐에서 하나씩 빼서 카드의 타겟에 대한 로직 수행
                    (Card card,List<TargetObject> tar) = cardTargetPairQueue.Dequeue();

                    CardData.instance.RunCard(card,tar);
                }
                else
                {
                    isCardQueueOperating = false;
                }
            }
        }
    }

    [ClientRpc]
    public void StartAnimation(TargetObject tar, int trackIndex,string animationName, bool loop )
    {
        if(!tar.isCloneData) // Clone 데이터 검증은 Animation 스킵
        {
            Debug.Log("Animation Start!");
            SkeletonAnimation anim = tar.avatar.GetComponent<SkeletonAnimation>();
            anim.state.SetAnimation(trackIndex,animationName,loop);
        }
    }

    [Server]
    public void ProcessCardPredict(Card card,List<TargetObject> tar)
    {
        CardData.instance.RunCard(card,tar);
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
            case BattleTurn.PLAYER_ACTIVE_DONE :
                StartWaitCardQueue();
                break;
            case BattleTurn.PLAYER_END :
                PlayerEndTurn();
                break;
            case BattleTurn.MONSTER_ORDERSELECT :
                MonsterSetOrder();
                phase = BattleTurn.MONSTER_PREEFFECT;
                break;
            case BattleTurn.MONSTER_PREEFFECT :
                MonsterPreEffect();
                break;
            case BattleTurn.MONSTER_ACTIVE :
                MonsterActive();
                break;
            case BattleTurn.BATTLE_END :
                BattleEnd();
                break;
        }
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
            monster.isAnimating = true;
            monster.monster.DoAction();
            while(true)
            {
                if(monster.isAnimating == false) break;
                yield return new WaitForSeconds(0.01f);
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
        phase = BattleTurn.PLAYER_ACTIVE;
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
        ResetEndTurnState();
        phase = BattleTurn.MONSTER_ORDERSELECT;
        EachPlayerEndTurn();
    }

    [Server]
    public void ResetEndTurnState()
    {
        foreach(TargetObject user in spawnedPlayerList)
        {
            user.player.endTurnActive = false;
        }
    }

    [ClientRpc]
    public void EachPlayerEndTurn()
    {
        // 각 플레이어들의 모든 카드와 화살표 제거
        M_CardManager.instance.RemoveAllCurrentPlayerArrow();
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHands();
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
            avatar.GetComponent<TargetObject>().conn = playerOrder[i].netIdentity;
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
            spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
            spawnedPlayerSyncList.Add(avatar.GetComponent<TargetObject>());

            // 타겟 유효 판단을 위한 클론 데이터 //
            GameObject clone = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);
            NetworkServer.Spawn(clone);
            clone.GetComponent<TargetObject>().cloneGamePlayer = new CloneGamePlayer(playerOrder[i]);
            clone.GetComponent<TargetObject>().conn = playerOrder[i].netIdentity;
            clone.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
            clone.GetComponent<TargetObject>().isCloneData = true;

            avatar.GetComponent<TargetObject>().clone = clone.GetComponent<TargetObject>();
            clonePlayerList.Add(clone.GetComponent<TargetObject>());
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
            num = Random.Range(0,M_MonsterManager.instance.monsterGroups.Count - 1);
            break;
        }
        for(int i = 0 ; i < M_MonsterManager.instance.monsterGroups[num].monsters.Count ; i ++)
        {
            Debug.Log(M_MonsterManager.instance.monsterGroups[num].monsters[i].name);
            var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == M_MonsterManager.instance.monsterGroups[num].monsters[i].name),monsterSpawnLocation.GetChild(i).transform.position,Quaternion.identity).GetComponent<SpawnedMonster>();
            var cloneMonster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == M_MonsterManager.instance.monsterGroups[num].monsters[i].name),new Vector3(-300,-300,0),Quaternion.identity).GetComponent<SpawnedMonster>();

            NetworkServer.Spawn(monster.gameObject);
            NetworkServer.Spawn(cloneMonster.gameObject);

            monster.monsterData = M_MonsterManager.instance.monsterGroups[num].monsters[i];
            cloneMonster.monsterData = M_MonsterManager.instance.monsterGroups[num].monsters[i];


            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),monsterSpawnLocation.GetChild(i).transform.position,Quaternion.identity);
            var cloneAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);

            NetworkServer.Spawn(avatar);
            NetworkServer.Spawn(cloneAvatar);

            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;

            cloneAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            cloneAvatar.GetComponent<TargetObject>().monster = cloneMonster;
            cloneAvatar.GetComponent<TargetObject>().isCloneData = true;
            avatar.GetComponent<TargetObject>().clone = cloneAvatar.GetComponent<TargetObject>();

            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            cloneMonsterList.Add(cloneAvatar.GetComponent<TargetObject>());
            RpcMonsterInit(avatar.GetComponent<TargetObject>(), monster);
            RpcMonsterInit(cloneAvatar.GetComponent<TargetObject>(), cloneMonster);
        }
    }

    // 몬스터 오브젝트 생성되면 몬스터 이름, HP등 뷰 요소의 값을 몬스터 데이터값에 따라 세팅
    [ClientRpc]
    public void RpcMonsterInit(TargetObject avatar, SpawnedMonster monster)
    {
        monster.transform.SetParent(avatar.transform);
        Slider hpbar = avatar.transform.GetChild(0).GetChild(3).GetComponent<Slider>();
        TextMeshProUGUI textMonsterName = avatar.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
        textMonsterName.text = monster.monsterName;
        hpbar.maxValue = monster.MAXHP;;
        hpbar.value = monster.HP;
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

    // 플레이어 정보 및 턴 정보 뷰 세팅
    public void SetPlayerOrderView(int index)
    {
        GamePlayer gamePlayer = M_TurnManager.instance.playerOrder[index];
        OrderUI orderUI = GameUIManager.instance.playerOrderList[index].GetComponent<OrderUI>();
        orderUI.textPlayerName.text = SteamFriends.GetFriendPersonaName((CSteamID)gamePlayer.steamID);
        if(gamePlayer.isLocalPlayer){
            orderUI.playerOwnMenu.gameObject.SetActive(true); // 전용 메뉴 활성화
            float width = orderUI.buttonPlayerOrder.GetComponent<RectTransform>().rect.width;
            float height = orderUI.buttonPlayerOrder.GetComponent<RectTransform>().rect.height;
            orderUI.buttonPlayerOrder.GetComponent<RectTransform>().sizeDelta = new Vector2(width + 30f, height + 30f); // 버튼 크기 크게 변경(변경된 값이 native size)
        }
    }

    [ClientRpc]
    public void PopUpOrderUI()
    {
        orderUI.SetActive(true);
        isOrderSelect = true;
    }

    [Server]
    public void OnChangedMonsterList()
    {
        if(spawnedMonsterList.Count == 0)
            phase = BattleTurn.BATTLE_END;
    }

    [Server]
    public void BattleEnd()
    {
        Debug.Log("전투 종료");
        EachPlayerBattleEnd();
    }

    [ClientRpc]
    public void EachPlayerBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 소멸
        PopUpUIManager.instance.HandleShowBattleResultPopUp(); // 전투 결과 보상 팝업 활성화
    }

    [Server]
    public void ClearTargetObject()
    {
        for(int i = clonePlayerList.Count - 1 ; i >=0 ; i--)
        {
            Debug.Log("Clone Destroy!!");
            TargetObject removeItem = clonePlayerList[i];
            clonePlayerList.Remove(removeItem);
            NetworkServer.Destroy(removeItem.gameObject);
        }
        for(int i = spawnedPlayerList.Count - 1 ; i >=0 ; i--)
        {
            Debug.Log("Player Target Destroy!!");
            TargetObject removeItem = spawnedPlayerList[i];
            spawnedPlayerList.Remove(removeItem);
            NetworkServer.Destroy(removeItem.gameObject);
        }
        spawnedPlayerSyncList.Clear();
    }

    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//
    private void OnPlayerOrderUpdated(SyncList<GamePlayer>.Operation op, int index, GamePlayer oldGamePlayer, GamePlayer newGamePlayer)
    {
        switch (op)
        {
            case SyncList<GamePlayer>.Operation.OP_ADD:
                SetPlayerOrderView(index);
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
