using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;
using Spine.Unity;
using Spine;
using TMPro;
using DG.Tweening;

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

    public RoomType roomType;
    [Header("Scene Change Black Curtain")]
    public GameObject blackCurtain;

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
    [SyncVar]
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

    // 현재 페이즈가 PLAYER_ACTIVE 상태인지 체크
    public bool IsActivePhase()
    {
        return phase == BattleTurn.PLAYER_ACTIVE ? true : false;
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
    public void HandleStartBattle(MapRoom mapRoom)
    {
        roomType = mapRoom.roomType;
        M_MapManager.instance.StartBattle();
    }

    [Server]
    public void GenerateBattleObject()
    {
        if(isServer)
        {
            GeneratePlayerUnit();
            if(roomType == RoomType.MONSTER || roomType == RoomType.ELITE)
            {
                GenerateMonster();
                phase = BattleTurn.BATTLE_STANDBY;
            }
            else
            {
                GenerateNPC();
                phase = BattleTurn.NONE_BATTLE_SCENE;
            }
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
    }

    [Server]
    public void OnChangedPhase()
    {
        Debug.Log("Phase is " + phase);
        switch(phase)
        {
            case BattleTurn.NONE_BATTLE_SCENE :
                break;
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
            case BattleTurn.NONE_BATTLE_END :
                NoneBattleEnd();
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
            user.player.SetEndTurnActiveStateDefault();
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
        PlayerOrderSelectPhase();
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int i = 0 ;i < playerOrder.Count ; i ++)
        {
            GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),playerSpawnLocation.GetChild(i).transform.position,Quaternion.identity);
            NetworkServer.Spawn(avatar);
            avatar.GetComponent<TargetObject>().player = playerOrder[i];
            avatar.GetComponent<TargetObject>().playerHP = playerOrder[i].HP;
            avatar.GetComponent<TargetObject>().playerMaxHP = playerOrder[i].MaxHP;
            avatar.GetComponent<TargetObject>().conn = playerOrder[i].netIdentity;
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
            spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
            spawnedPlayerSyncList.Add(avatar.GetComponent<TargetObject>());

            // 타겟 유효 판단을 위한 클론 데이터 //
            GameObject clone = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),new Vector3(-300,-300,0),Quaternion.identity);
            NetworkServer.Spawn(clone);
            clone.GetComponent<TargetObject>().conn = playerOrder[i].netIdentity;
            clone.GetComponent<TargetObject>().playerHP = playerOrder[i].HP;
            clone.GetComponent<TargetObject>().playerMaxHP = playerOrder[i].MaxHP;
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

            // monster 오브젝트의 부모오브젝트 참조값 설정
            monster.parent = avatar.GetComponent<TargetObject>();
            cloneMonster.parent = cloneAvatar.GetComponent<TargetObject>();
        }
    }

    [Server]
    void GenerateNPC()
    {
        // 이벤트 , 상점, 전초기지 NPC 생성 위치
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
        // TODO : 전투 종료 혹은 이벤트방에서 개인별로 먼저 수행하고 넘어가는게 맞을지?, 팀원이 모두 수행을 끝낼때까지 기다리는게 맞을지?
        EachPlayerBattleEnd();
    }

    [Server]
    public void NoneBattleEnd()
    {
        EachPlayerNoneBattleEnd();
    }

    [ClientRpc]
    public void EachPlayerBattleEnd()
    {
        PopUpUIManager.instance.HandleShowBattleResultPopUp(); // 전투 결과 보상 팝업 활성화
    }

    [ClientRpc]
    public void EachPlayerNoneBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 삭제, 삭제 시 Trash Deck에 추가하지 않음.
        M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck(); // 플레이어의 PrefareDeck, TrashDeck 삭제
        ReturnToMap();
    }

    [Server]
    public void ClearTargetObject()
    {
        ClearTargetObjectList(cloneMonsterList);
        ClearTargetObjectList(spawnedMonsterList);
        ClearTargetObjectList(clonePlayerList);
        ClearTargetObjectList(spawnedPlayerList);
        spawnedPlayerSyncList.Clear();
    }

    private void ClearTargetObjectList(List<TargetObject> targets)
    {
        for(int i = targets.Count - 1 ; i >=0 ; i--)
        {
            TargetObject removeItem = targets[i];
            targets.Remove(removeItem);
            NetworkServer.Destroy(removeItem.gameObject);
        }
    }

    public void ReturnToMap()
    {
        if(isServer)ClearTargetObject();
        GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
            // 카메라 위치 리셋
            Vector3 currLoc = M_MapManager.instance.currentRoom.transform.position;
            Camera.main.transform.position = currLoc + new Vector3(0,0,-8);
            Camera.main.orthographic = false; 
            // UI 활성화 상태 변경
            M_MapManager.instance.roommaps.SetActive(true);
            M_MapManager.instance.game.SetActive(false);
            GameUIManager.instance.GameUI.gameObject.SetActive(false);
            GameUIManager.instance.GameBackGround.gameObject.SetActive(false);
            // Dim배경 상태 변경
            blackCurtain.gameObject.SetActive(false);
            blackCurtain.DOFade(0.0f, 0.5f); // 원래 알파값으로 변경
        });
    }

    public void ChangePlayerOrder(GamePlayer player, MoveDirection direction)
    {
        Debug.Log("ChangePlayerOrder Called");
        TargetObject forwarding = null,backwarding = null;
        Vector3 forwardingDestination = new Vector3(0,0,0),backwardingDestination = new Vector3(0,0,0);
        if(direction == MoveDirection.FORWARD)
        {
            if(player.selectOrder == 0) return;
            GamePlayer swap = playerOrder[player.selectOrder-1];
            playerOrder[player.selectOrder-1] = player;
            playerOrder[player.selectOrder] = swap;
            player.SetPlayerOrder(player.selectOrder - 1);
            swap.SetPlayerOrder(swap.selectOrder + 1);
            foreach(TargetObject tar in spawnedPlayerList)
            {
                if(tar.player == player)
                {   
                    forwarding = tar;
                    backwardingDestination = tar.transform.position;
                }
                if(tar.player == swap)
                {
                    backwarding = tar;
                    forwardingDestination = tar.transform.position;
                }
            }
        }
        if(direction == MoveDirection.BACKWARD)
        {
            if(player.selectOrder == NetworkServer.connections.Count - 1) return;
            GamePlayer swap = playerOrder[player.selectOrder+1];
            playerOrder[player.selectOrder+1] = player;
            playerOrder[player.selectOrder] = swap;
            player.SetPlayerOrder(player.selectOrder + 1);
            swap.SetPlayerOrder(swap.selectOrder - 1);
            foreach(TargetObject tar in spawnedPlayerList)
            {
                if(tar.player == player)
                {   
                    backwarding = tar;
                    forwardingDestination = tar.transform.position;
                }
                if(tar.player == swap)
                {
                    forwarding = tar;
                    backwardingDestination = tar.transform.position;
                }
            }
        }
        forwarding.transform.DOMove(forwardingDestination,0.5f,false).SetEase(Ease.OutQuart);
        backwarding.transform.DOMove(backwardingDestination,0.5f,false).SetEase(Ease.OutQuart);
    }

    public TargetObject[] GetTargetObjectFromOrder(PlayOrder PlayOrder)
    {
        TargetObject[] retVal = new TargetObject[2];
        foreach(TargetObject tar in spawnedPlayerList)
            if(playerOrder[(int)PlayOrder] == tar.player) retVal[0] = tar;
        foreach(TargetObject tar in clonePlayerList)
            if(playerOrder[(int)PlayOrder] == tar.player) retVal[1] = tar;
        return retVal;
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
