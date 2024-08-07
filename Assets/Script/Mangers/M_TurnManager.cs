using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Spine.Unity;
using Spine.Unity.Examples;
using DG.Tweening;
using AYellowpaper.SerializedCollections;
using System.Linq;


public class M_TurnManager : NetworkSingletonD<M_TurnManager>
{
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


    // 서버에서 관리할 PlayerOrder SyncList : 요소값이 0인 인덱스는 빈 슬롯을 의미. 플레이어들이 추가될 때 0인 인덱스의 값을 제거하고 해당 플레이어의 netId를 추가
    public readonly SyncList<uint> playerOrder = new SyncList<uint>(){ 0, 0, 0 };

    // 각 클라이언트에서 참조할 현재 참가한 플레이어들의 타겟오브젝트 목록
    public readonly SyncList<uint> spawnedPlayerSyncList = new SyncList<uint>();

    // 카드 큐 데이터 저장할 Synclist
    public readonly SyncList<CardQueue> cardQueueList = new SyncList<CardQueue>();

    [Header("카드 큐 프리팹")]
    public GameObject cardQueueItemPrefab;
    public int currentCardQueueIndex; // 현재 카드 큐 인덱스
    private const int currentCardQueueInitalValue = -1; // 현재 카드 큐 인덱스 초기값 (리스트 인덱스와 맞추기 위해 초기값 -1)
    public List<GameObject> cardQueueItems = new List<GameObject>();
    public enum INDEX_OPERATION {
        INCREASE,
        DECREASE
    }
    
    [SerializedDictionary("게임플레이어", "보상카드선택유무")]
    public SerializedDictionary<GamePlayer, bool> playerRewardedDic = new SerializedDictionary<GamePlayer, bool>();

    private static float battelSceneCameraSize = 10.8f; // 전투씬에서 카메라 크기값
    private static float mapSceneCameraSize = 6.0f; // 맵씬에서 카메라 크기값
    public List<GameObject> rewardObjects = new List<GameObject>(); // 보상목록 오브젝트 리스트
    public List<GameObject> rewardCardObjects = new List<GameObject>(); // 보상카드 오브젝트 리스트
    
    public Vector3[] targetObjectPosition = {
        new Vector3(-15,-3,0),
        new Vector3(-11,-3,0),
        new Vector3(-7,-3,0),
        new Vector3(7,-3,0),
        new Vector3(11,-3,0),
        new Vector3(15,-3,0)
    };

    public bool isCardQueueOperating = false;


    public List<TargetObject> spawnedPlayerList = new List<TargetObject>();
    public List<TargetObject> spawnedMonsterList = new List<TargetObject>();
    List<TargetObject> monsterOrderList = new List<TargetObject>();
    
    public bool monsterDeathOperating = false;
    public bool preEffcetOperating = false;
    public bool monsterShieldInitialize = false;
    public List<TargetObject> dyingMonsers = new List<TargetObject>();

    // 카드와 타겟을 한쌍으로 저장하는 큐
    // TargetObject List 구조 : 
    /*
    Index : 내용
    0 : 카드 사용한 Player 
    1 : Target Monster
    이후 : 모든 플레이어 및 몬스터
    */
    public Queue<(GamePlayerDeck, int , CardOnHand, List<TargetObject>)> cardTargetPairQueue = new Queue<(GamePlayerDeck, int, CardOnHand, List<TargetObject>)>();



    protected override void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentManagers.Add(gameObject.name, gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentCardQueueIndex = currentCardQueueInitalValue;
        StartCoroutine(ProcessCardQueue());
    }

    public override void OnStartClient()
    {
        playerOrder.Callback += OnPlayerOrderUpdated;
        cardQueueList.Callback += OnCardQueueUpdated;
    }

    // -------------------------------------------------------------------- Normal Method ---------------------------------------------------------------------//

    public TargetObject GetPlayer(GamePlayerDeck conn)
    {     
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.GetComponent<GamePlayerDeck>() == conn){
                return tar;
            }
        }
        return null;
    }

    public List<TargetObject> GetPlayerObjects()
    {
        return spawnedPlayerList;
    }

    public List<TargetObject> GetMonsterObjects()
    {
        return spawnedMonsterList;
    }

    // 현재 플레이어의 TargetObject를 반환
    public TargetObject GetCurrentPlayerTargetObject(GamePlayer gamePlayer)
    {
        if(NetworkServer.activeHost){
            return NetworkServer.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkServer.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
        }else{
            return NetworkClient.spawned[M_TurnManager.instance.spawnedPlayerSyncList.Find(netId => NetworkClient.spawned[netId].GetComponent<TargetObject>().player == gamePlayer)].GetComponent<TargetObject>();
        }
    }

    // 현재 페이즈가 PLAYER_ACTIVE 상태인지 체크
    public bool IsActivePhase()
    {
        return phase == BattleTurn.PLAYER_ACTIVE ? true : false;
    }

    // 소유한 모든 플레이어가 보상 카드 받았는지 체크
    public void CheckAllPlayerRewarded(GamePlayer gamePlayer)
    {
        if(!M_TurnManager.instance.playerRewardedDic.ContainsValue(false) && gamePlayer.isOwned){ // 소유한 모든 플레이어 보상받았으면 종료
            PopUpUIManager.instance.HandleHideBattleResultPopUp(); // 전투 결과 팝업 비활성화
            GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
                NetworkClient.localPlayer.GetComponent<PlayerInterface>().isRewardDone = true; 
                gamePlayer.GetComponent<GamePlayerDeck>().CmdClearRewardCards();
            });
        }
    }

    // 보상 목록 오브젝트 모두 제거
    public void ClearRewardListItem()
    {
        foreach(GameObject gameObject in rewardObjects){
            Destroy(gameObject);
        }
        rewardObjects.Clear();
    }

    // 보상 목록 오브젝트 단일 제거
    public void RemoveRewardListItem(GameObject rewardObject)
    {
        M_TurnManager.instance.rewardObjects.Remove(rewardObject);
        Destroy(rewardObject);
    }

    // 보상 카드 오브젝트 제거 및 플레이어 보상 상태 데이터 정리
    public void ClearRewardCardAndPlayer()
    {
        foreach(GameObject gameObject in rewardCardObjects){
            Destroy(gameObject);
        }
        rewardCardObjects.Clear();
    }

    // 이벤트 방 대화 재생
    public void PlayEventConversation(bool isPositive)
    {
        AudioClip eventVoice = null;
        Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.character;
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, isPositive ? 86 : 92, 3);
                eventVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                break;
            case Character.GEORK:
                List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, isPositive ? 98 : 104, 3);
                eventVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                break;
            case Character.ERIS:
                List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, isPositive ? 144 : 150, 3);
                eventVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                break;
        }
        M_SoundManager.instance.PlayVoice(eventVoice, eventVoice.length);
    }

    // 해당 플레이어의 캐릭터 오브젝트를 마우스 오버 및 클릭 할 수 있는 상태로 변경(isSelectable 플래그 변수의 상태값에 따라 작동)
    public void SetPlayerSelectable(bool isSelectable)
    {
        for(int i=0; i<playerOrder.Count; i++){
            uint netId = playerOrder[i];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.isSelectable = isSelectable;
            }
        }
    }

    // -------------------------------------------------------------------- Server Method ---------------------------------------------------------------------//

    // 플레이어 오더 스왑
    [Server]
    public void SwapPlayerOrder(int oldIndex, int newIndex)
    {
        if(M_TurnManager.instance.phase == BattleTurn.PLAYER_ACTIVE) // 노병의 지혜
        {
            if(NetworkServer.spawned.ContainsKey(playerOrder[oldIndex]))
            if(NetworkServer.spawned[playerOrder[oldIndex]].GetComponent<GamePlayerTarget>().GetTargetObject().HasBuff(BuffType.WISDOMOFOLDSOLDIER))
                foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
                    CardData.instance.GeneralGetDefense(NetworkServer.spawned[playerOrder[oldIndex]].GetComponent<GamePlayerTarget>().GetTargetObject(),target,5,null);
            if(NetworkServer.spawned.ContainsKey(playerOrder[newIndex]))
            if(NetworkServer.spawned[playerOrder[newIndex]].GetComponent<GamePlayerTarget>().GetTargetObject().HasBuff(BuffType.WISDOMOFOLDSOLDIER))
                foreach(TargetObject target in M_TurnManager.instance.spawnedPlayerList)
                    CardData.instance.GeneralGetDefense(NetworkServer.spawned[playerOrder[newIndex]].GetComponent<GamePlayerTarget>().GetTargetObject(),target,5,null);          
        }
        uint temp = playerOrder[oldIndex];
        playerOrder[oldIndex] = playerOrder[newIndex];
        playerOrder[newIndex] = temp;
    }

    [Server]
    public void ProcessCardPredict(Card card,List<TargetObject> tar)
    {
        CardData.instance.RunCard(card,tar);
    }

    [Server]
    public void OnChangedPhase()
    {
        Debug.Log("Phase is " + phase);
        RpcChangePhase(phase);
        switch(phase)
        {
            case BattleTurn.NONE_BATTLE_SCENE :
                break;
            case BattleTurn.BATTLE_INITIALIZE :
                BattleInitialize();
                break;
            case BattleTurn.BATTLE_STANDBY :
                BattleStandby();
                break;
            case BattleTurn.PLAYER_PREEFFECT :
                StartCoroutine(PlayerPreEffect());
                break;
            case BattleTurn.PLAYER_DRAW :
                StartCoroutine(PlayerCardDraw());
                break;
            case BattleTurn.PLAYER_ACTIVE :
                break;
            case BattleTurn.PLAYER_ACTIVE_DONE :
                StartWaitCardQueue();
                break;
            case BattleTurn.PLAYER_END_TURN_EFFECT :
                StartCoroutine(PlayerEndTurnEffect());
                break;
            case BattleTurn.PLAYER_END :
                PlayerEndTurn();
                break;
            case BattleTurn.MONSTER_ORDERSELECT :
                PlayerCardThrowAwaySetDefault();
                MonsterSetOrder();
                phase = BattleTurn.MONSTER_PREEFFECT;
                break;
            case BattleTurn.MONSTER_PREEFFECT :
                StartCoroutine(MonsterPreEffect());
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
    void PlayerCardThrowAwaySetDefault()
    {
        foreach(PlayerInterface pi in FindObjectsOfType<PlayerInterface>())
            pi.SetDefaultStateofCardThrowDone();
    }

    [Server]
    IEnumerator MonsterPreEffect()
    {
        WaitForSeconds loopTime = new WaitForSeconds(0.01f);
        // 몬스터 방어도 초기화
        preEffcetOperating =true;
        yield return DebuffPreEffect();
        preEffcetOperating =false;
        while(monsterDeathOperating)
            yield return loopTime;
        monsterShieldInitialize = true;
        foreach(TargetObject tar in M_TurnManager.instance.spawnedMonsterList)
        {
            tar.defense = 0;
        }
        monsterShieldInitialize = false;
        phase = BattleTurn.MONSTER_ACTIVE;
    }

    IEnumerator DebuffPreEffect()
    {
        foreach(TargetObject tar in spawnedMonsterList)
        {
            List<int> currentKeys = tar.buffTrunBeginEffect.Keys.ToList();
            foreach(int buffIndex in currentKeys)
            { 
                yield return tar.buffTrunBeginEffect[buffIndex](tar,buffIndex,null);
            }
        }
    }

    IEnumerator IronDemonPreEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if(tar.player.character == Character.HONGDANHYANG)
            {
                while(true)
                {
                    yield return new WaitForSeconds(0.01f);
                    if(monsterDeathOperating) continue;
                    break;
                }
                if(tar.ironDemonLocation.objectType == ObjectType.PLAYER) // 플레이어의 경우 방어력 
                {
                    AnimIronDemon("Buff0",tar);
                    tar.ironDemonLocation.defense += tar.GetBuffValue(BuffType.IRONDEMON);
                    yield return new WaitForSeconds(1.33f);
                }
                else // 몬스터의 경우 데미지
                {
                    while(true)
                    {
                        yield return new WaitForSeconds(0.01f);
                        if(monsterDeathOperating) continue;
                        break;
                    }
                    if(Random.Range(0,2) == 0)AnimIronDemon("Attack0",tar);
                    else AnimIronDemon("Attack1",tar);
                    yield return new WaitForSeconds(0.4f);
                    StartCoroutine(tar.ironDemonLocation.monster.OnHitAnimation()); // 실제 피격 애니메이션
                    tar.ironDemonLocation.DamageToMonster(tar.GetBuffValue(BuffType.IRONDEMON), tar);
                    yield return new WaitForSeconds(0.6f);
                }
                AnimIronDemon("Idle",tar);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    [Server]
    public IEnumerator PlayerCardDraw()
    {
        foreach(uint netId in playerOrder){
            if(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer player = networkIdentity.GetComponent<GamePlayer>();
                player.GetComponent<GamePlayerDeck>().currentIchi = player.GetComponent<GamePlayerDeck>().maxIchi; 
            }
        }
        foreach(TargetObject tar in spawnedPlayerList) // 고행2 카드를 이미 가지고 있으면 쇠락 부여 
        {
            if(tar.player.GetComponent<GamePlayerDeck>().cardOnHands.FindIndex(cardOnhand => cardOnhand.card.baseCard.cardNumber ==  "G1") != -1)
                tar.GainBuff(BuffType.SOIRAK,1,true,false,true,false,tar,null);
        }
        EachPlayerCardDraw();
        foreach(TargetObject tar in spawnedPlayerList)
        {
            foreach(int buffIndex in tar.buffCardDrowEffect.Keys)
            { 
                yield return tar.buffCardDrowEffect[buffIndex](tar,buffIndex,null);
            }
        }
        phase = BattleTurn.PLAYER_ACTIVE;
    }

    [Server]
    public IEnumerator PlayerPreEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList) 
        {
            tar.defense = 0;
            tar.player.GetComponent<GamePlayerDeck>().numOfUsedIronTeeth = 0;
            List<int> currentKeys = tar.buffTrunBeginEffect.Keys.ToList();
            foreach(int buffIndex in currentKeys)
            { 
                yield return tar.buffTrunBeginEffect[buffIndex](tar,buffIndex,null);
            }   
            int indexOfOldItem = tar.buffs.Count;
            for(int i = indexOfOldItem -1 ; i >= 0 ; i--)
            {
                if(tar.buffs[i].type == BuffType.FLOWER)
                {
                    tar.buffs.RemoveAt(i);
                    continue;
                }
                if(tar.buffs[i].isDecrease)
                {
                    Buff modItem = new Buff(tar.buffs[i]);
                    modItem.value -= 1;
                    if(modItem.value == 0)
                        tar.buffs.RemoveAt(i);
                    else
                        tar.buffs[i] = modItem;
                }
            }
        }
        foreach(TargetObject tar in spawnedMonsterList) // 몬스터 디버프 스택 감소
        {
            int indexOfOldItem = tar.buffs.Count;
            for(int i = indexOfOldItem -1 ; i >= 0 ; i--)
            {
                if(tar.buffs[i].type == BuffType.FLOWER)
                {
                    tar.buffs.RemoveAt(i);
                    continue;
                }
                if(tar.buffs[i].isDecrease)
                {
                    Buff modItem = new Buff(tar.buffs[i]);
                    modItem.value -= 1;
                    if(modItem.value == 0)
                        tar.buffs.RemoveAt(i);
                    else
                        tar.buffs[i] = modItem;
                }
            }
        }
        phase = BattleTurn.PLAYER_DRAW;
        yield return null;
    }

    

    [Server]
    public void PlayerEndTurn()
    {
        ResetEndTurnState();
        EachPlayerEndTurn();
    }

    [Server]
    public void ResetEndTurnState()
    {
        foreach(TargetObject user in spawnedPlayerList)
        {
            user.player.objectOwner.SetEndTurnActiveStateDefault();
            user.usingGOHENG = false; // 고행 사용 초기화
        }
    }

    [Server]
    public void BattleEnd()
    {   
        // 전투 종료시 플레이어들의 캐릭터별 보상카드 랜덤추출하여 각 플레이어들에게 전달
        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values){
            PlayerInterface playerInterface = NetworkServer.spawned[conn.identity.netId].GetComponent<PlayerInterface>();
            PlayerInterfaceServer playerInterfaceServer = playerInterface.GetComponent<PlayerInterfaceServer>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                
                // TODO : 보상테이블 데이터 DB에서 조회해서 보상아이템 세팅(임시로 골드 + 카드 보상)
                string cardRewardGuid = System.Guid.NewGuid().ToString();
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = cardRewardGuid, reward_Type = Reward_Type.Card });
                gamePlayerDeck.rewards.Add(new Reward(){ netId = gamePlayer.netId, guid = System.Guid.NewGuid().ToString(), reward_Type = Reward_Type.Gold, rewardGold = 10 });
                
                // 카드 보상 데이터 세팅
                int rewardCardCount = gamePlayerDeck.maxRewardCardCount; // 플레이어별로 설정된 보상 카드 최대 갯수
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < rewardCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        Card rewardCard = cardsByCharacter[randomIndex].CardDeepCopy(false);
                        rewardCard.guid = cardRewardGuid;
                        gamePlayerDeck.rewardCards.Add(rewardCard);
                        cardsByCharacter.RemoveAt(randomIndex);
                    }
                }
                // 플레이어 보상 상태 데이터 세팅
                gamePlayerDeck.TargetPlayerRewarded(gamePlayerDeck.GetComponent<NetworkIdentity>().connectionToClient);

                // 플레이어의 모든 카드 데이터 제거
                gamePlayerDeck.trashDeck.Clear();
                gamePlayerDeck.prefareDeck.Clear();
                gamePlayerDeck.forgottenDeck.Clear();
                
                //코스트 리셋
                gamePlayerDeck.maxIchi = 3;
                gamePlayerDeck.currentIchi = 3;

                //해방 카드를 위한 카드 카운팅 종료
                gamePlayerDeck.numOfUsedCard = 0;

                //저주카드 획득량 제거
                gamePlayerDeck.gainCurseCardCount = 0;

                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands){
                    NetworkServer.Destroy(cardOnHand.gameObject);
                }
                gamePlayerDeck.cardOnHands.Clear();
            }
        }
        RpcShowBattleResultPopUp(); // 전투 종료 팝업 호출
        ResetEndTurnState(); // 턴종료 상태 리셋
        cardQueueList.Clear(); // 카드 큐 Synclist 클리어
        currentCardQueueIndex = currentCardQueueInitalValue; // 카드 큐 Synclist에 사용하는 index 값 초기화
    }

    [Server]
    public void NoneBattleEnd()
    {
        EachPlayerNoneBattleEnd();
        StopCoroutine(ProcessMonsterDeathCoroutine());
        foreach(PlayerInterface player in FindObjectsOfType<PlayerInterface>()){
            player.SetIsReadyStateDefault(); // 레디 상태 모두 확인후 다시 false 되돌림 (여러군데서 사용 예정)
            player.SetEndTurnActiveStateDefault(); // 앤드 턴 상태 모두 확인후 다시 false 되돌림
            player.SetCompleteRewardStateDefault();
        }
        foreach(HexagonMapRoom hexagonMapRoom in M_MapManager.instance.hexagonMapRooms){
            hexagonMapRoom.isSelected = false; // 맵 선택상태 모두 false 초기화
        }
        foreach(GamePlayer gamePlayer in FindObjectsOfType<GamePlayer>()){
            gamePlayer.GetComponent<GamePlayerDeck>().rewards.Clear();
            gamePlayer.GetComponent<GamePlayerDeck>().rewardCards.Clear();
        }
    }

    [Server]
    public void OnChangedMonsterList()
    {
        if(spawnedMonsterList.Count == 0)
            phase = BattleTurn.BATTLE_END;
    }

    [Server]
    public void ClearTargetObject()
    {
        ClearTargetObjectList(spawnedMonsterList);
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

    public IEnumerator ProcessCardQueue()
    {
        // 무한루프에서 인스턴스 생성시 생기는 가비지 방지를 위해 함수호출에서 미리 인스턴스 생성하여 캐싱후 루프 안에서 사용
        WaitForSeconds waitForLoop = new WaitForSeconds(0.01f);
        while (true)
        {
            yield return waitForLoop;
            if(CardData.instance.isCardOperating || monsterDeathOperating){
                continue;
            }
            else
            {
                if(cardTargetPairQueue.Count != 0){
                    CardData.instance.isCardOperating = true;
                    isCardQueueOperating = true;
                    (GamePlayerDeck gpd, int totalCost, CardOnHand cardOnHand,List<TargetObject> tar) = cardTargetPairQueue.Dequeue(); // 큐에서 하나씩 빼서 카드의 타겟에 대한 로직 수행
                    
                    SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.INCREASE); // 해당 인덱스의 카드 큐를 현재 카드 큐로 설정
                    if(cardOnHand.card.baseCard.isTargetable && tar[1] == null)
                    {
                        gpd.ReturnToCardOnHand(cardOnHand);
                        cardQueueList.RemoveAt(cardQueueList.Count - 1);
                        SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.DECREASE);
                        gpd.currentIchi += totalCost;
                        CardData.instance.isCardOperating = false;
                    }
                    else
                    {
                        if(tar[1].isDying)
                        {
                            gpd.ReturnToCardOnHand(cardOnHand);
                            cardQueueList.RemoveAt(cardQueueList.Count - 1);
                            SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.DECREASE);
                            gpd.currentIchi += totalCost;
                            CardData.instance.isCardOperating = false;
                            continue;
                        }

                        foreach(int index in tar[0].buffCardUseEffect.Keys)
                        {
                            yield return tar[0].buffCardUseEffect[index](tar[0],index,cardOnHand.card);
                        }
                        
                        yield return CardData.instance.RunCard(cardOnHand.card,tar);

                        if(CardData.instance.CheckCardCharacteristic(cardOnHand.card,CardCharacteristic.HWAHAP))
                            yield return CardData.instance.HWAHAP(tar[0]);
                        if(CardData.instance.CheckCardCharacteristic(cardOnHand.card,CardCharacteristic.SOOKREON))
                            cardOnHand.card.costAddition --;
                        if(CardData.instance.CheckCardCharacteristic(cardOnHand.card,CardCharacteristic.JOONGREUK))
                            cardOnHand.card.costAddition ++;

                        if(cardOnHand.card.isReturnable){
                            gpd.ReturnToCardOnHand(cardOnHand);
                            cardQueueList.RemoveAt(cardQueueList.Count - 1);
                            SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.DECREASE);
                        }else{
                            gpd.destroyCardList.Add(cardOnHand);
                        }
                        gpd.numOfUsedCard++;
                        // 카드 사용후 효과 여기서 발동

                    }
                }
                else
                {
                    isCardQueueOperating = false;
                }
            }
        }
    }

    // 카드 큐 Synclist에 데이터 추가
    [Server]
    public void AddCardQueueList(CardOnHand cardOnHand, uint netId)
    {
        if(!cardOnHand.card.baseCard.cardNumber.Equals("HA")){ // 철귀 이동 카드는 제외
            CardQueue cardQueue = new CardQueue(){
                cardOwnerNetId = netId,
                card = cardOnHand.card
            };
            M_TurnManager.instance.cardQueueList.Add(cardQueue);
        }
    }

    // 해당 인덱스의 카드 큐를 현재 카드 큐로 설정
    [Server]
    public void SerCurrentCardQueue(CardOnHand cardOnHand, uint netId, INDEX_OPERATION operation)
    {
        if(!cardOnHand.card.baseCard.cardNumber.Equals("HA")){ // 철귀 이동 카드는 제외  
            currentCardQueueIndex = operation == INDEX_OPERATION.INCREASE ? (currentCardQueueIndex + 1) : (currentCardQueueIndex - 1);
            CardQueue cardQueue = new CardQueue(){
                cardOwnerNetId = netId,
                card = cardOnHand.card
            };
            cardQueueList[currentCardQueueIndex] = cardQueue;
        }
    }

    public void ProcessMonsterDeath(TargetObject tar)
    {
        if(!dyingMonsers.Exists(x => x == tar))dyingMonsers.Add(tar);
    }

    public IEnumerator ProcessMonsterDeathCoroutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);
            if(!monsterDeathOperating)continue;
            foreach(TargetObject monster in dyingMonsers)
                if(monster.gameObject.activeSelf)monster.gameObject.SetActive(false);//우선 사망한 적 비활성화

            if(CardData.instance.isCardOperating || preEffcetOperating)
            {   
                foreach(TargetObject monster in dyingMonsers)
                    if(monster.isActiveAndEnabled)monster.gameObject.SetActive(false);
                continue; // 카드 사용이 끝날때까지 기다림
            }

            foreach(TargetObject monster in dyingMonsers) // 사망 몬스터 순차 처리
            {
                foreach(TargetObject target in spawnedPlayerList) // 철귀가 붙은 몬스터일경우 철귀 복귀
                {
                    if(target.player.character == Character.HONGDANHYANG)
                        if(target.ironDemonLocation == monster )
                        {
                            target.ironDemonLocation = target;
                            StartCoroutine(IronDemonReturnProcess(target));
                        }
                }
                // 실제 오브젝트 삭제 과정
                M_TurnManager.instance.spawnedMonsterList.Remove(monster);
                NetworkServer.Destroy(monster.gameObject);
                OnChangedMonsterList();
            }
            dyingMonsers.Clear();
            monsterDeathOperating = false;
        }
    }

    public IEnumerator IronDemonReturnProcess(TargetObject target)
    {
        M_TurnManager.instance.AnimIronDemon("TeleportGo",target); // 철귀 사라짐
        yield return new WaitForSeconds(0.333f); // 철귀 완전히 사라지는 시간
        M_TurnManager.instance.MoveIronDemon(target,target); // 철귀 적으로 이동
        M_TurnManager.instance.AnimIronDemon("TeleportBack",target); // 철귀 나타나기 시작
        yield return new WaitForSeconds(0.2f); // 적당히 나타날때까지 기다림
        M_TurnManager.instance.AnimIronDemon("Idle",target); // 철귀 나타나기 시작
    }

    [Server]
    public void BattleStandby()
    {
        foreach(TargetObject monster in spawnedMonsterList)
        {
            monster.monster.SetNextAction();
        }
        phase = BattleTurn.PLAYER_PREEFFECT;
    }

    public void BattleInitialize()
    {
        foreach(TargetObject player in spawnedPlayerList)
        {
            if(player.player.character == Character.HONGDANHYANG)
            {
                player.GainBuff(BuffType.IRONDEMON, 4 + player.player.GetComponent<GamePlayerDeck>().AdditionalSizeOfIromDemon, false, false, false, false, player, null);
            }
        }
        phase = BattleTurn.BATTLE_STANDBY;
    }

    [Server]
    public void MonsterActive()
    {
        StartCoroutine(MonsterActionSeuqence());
    }

    IEnumerator MonsterActionSeuqence()
    {
        WaitForSeconds loopWait = new WaitForSeconds(0.01f);
        for(int i=0; i<spawnedMonsterList.Count; i++)
        {
            TargetObject target = spawnedMonsterList[i];
            target.monster.isActive = true;
            StartCoroutine(target.monster.DoAction());
            while(true)
            {
                if(target.monster.isActive == false) break;
                yield return loopWait;
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
    public void GeneratePlayerUnit()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        for(int i = 0 ;i < playerOrder.Count ; i ++){
            if(playerOrder[i] != 0 && NetworkServer.spawned.TryGetValue(playerOrder[i], out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();

                Vector3 avatarOrderPosition = targetObjectPosition[gamePlayer.selectOrder]; // 게임플레이어의 오더값에 맞춰 생성될 아바타 위치 설정
                GameObject avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), avatarOrderPosition, Quaternion.identity);
                avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.PLAYER;
                avatar.GetComponent<TargetObject>().player = gamePlayer;
                avatar.GetComponent<TargetObject>().playerMaxHP = gamePlayer.MaxHP;
                avatar.GetComponent<TargetObject>().playerHP = gamePlayer.HP;
                NetworkServer.Spawn(avatar);

                spawnedPlayerList.Add(avatar.GetComponent<TargetObject>());
                spawnedPlayerSyncList.Add(avatar.GetComponent<NetworkIdentity>().netId);
                gamePlayer.GetComponent<GamePlayerTarget>().targetObject = avatar.GetComponent<NetworkIdentity>().netId;
            }
        }
    }

    [Server]
    public void GenerateMonster(HexagonMapRoom currentRoom)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        MonsterGroup selectedMonsterGroup = M_MonsterManager.instance.GetMonsterGroup(currentRoom.hazard);
        for(int i = 0 ; i < selectedMonsterGroup.monsters.Count ; i ++)
        {   
            Vector3 position = targetObjectPosition[i + 3];
            var monster = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == selectedMonsterGroup.monsters[i].name), position, Quaternion.identity).GetComponent<SpawnedMonster>();
            monster.monsterData = selectedMonsterGroup.monsters[i];
            monster.index = selectedMonsterGroup.monsters.Count - i;
            NetworkServer.Spawn(monster.gameObject);
            
            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), position, Quaternion.identity);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
            avatar.GetComponent<TargetObject>().monster = monster;
            NetworkServer.Spawn(avatar);

            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            monster.parent = avatar.GetComponent<TargetObject>(); // monster 오브젝트의 부모오브젝트 참조값 설정
        }
    }

    // 방타입에 따라 NPC 생성
    [Server]
    public void GnenrateNPCByRoomTpye(RoomType roomType)
    {
        switch(roomType){
            case RoomType.CAMP:
                GenerateCampNPC();
                break;
            case RoomType.CARD_NPC:
                GenerateCardShopNPC();
                break;
            case RoomType.ITEM_NPC:
                GenerateItemNPC();
                break;
        }
    }

    // 전초기지 NPC 생성
    [Server]
    public void GenerateCampNPC()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        int randomNumber = Random.Range(0, 2);
        if(randomNumber == 0){
            // RyuJinSol 생성
            var campRyuJinSol = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_RyuJinSol"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
            campRyuJinSol.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_RyuJinSol"));
            NetworkServer.Spawn(campRyuJinSol.gameObject);

            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
            avatar.GetComponent<TargetObject>().monster = campRyuJinSol;
            NetworkServer.Spawn(avatar);
            
            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            campRyuJinSol.parent = avatar.GetComponent<TargetObject>();
        }else{
            // Sophia 생성
            var campSophia = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_Sophia"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
            campSophia.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_Sophia"));
            NetworkServer.Spawn(campSophia.gameObject);

            var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
            avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
            avatar.GetComponent<TargetObject>().monster = campSophia;
            NetworkServer.Spawn(avatar);
            
            spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
            campSophia.parent = avatar.GetComponent<TargetObject>();
        }
        // 각 플레이어별 체력 회복 횟수 제한을 1로 설정
        for(int i=0; i<playerOrder.Count; i++){
            if(NetworkClient.spawned.TryGetValue(playerOrder[i], out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.recoveryLimitCount = 1;
            }
        }
    }

    // 아이템상점 NPC 생성
    [Server]
    public void GenerateItemNPC()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var itemShopNPC = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_ShadowMan"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
        itemShopNPC.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_ShadowMan"));
        NetworkServer.Spawn(itemShopNPC.gameObject);

        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
        avatar.GetComponent<TargetObject>().monster = itemShopNPC;
        NetworkServer.Spawn(avatar);
        
        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        itemShopNPC.parent = avatar.GetComponent<TargetObject>();
    }

    // 카드상점 NPC 생성
    [Server]
    public void GenerateCardShopNPC()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        var cardNPC = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "NPC_Mercurius"), new Vector3(11,-3,0), Quaternion.identity).GetComponent<SpawnedMonster>();
        NPC_Mercurius mercurius = cardNPC.GetComponent<NPC_Mercurius>();

        // 상점판매용 캐릭터별 카드 추출해서 NPC_Mercurius SyncDictionary에 추가
        foreach(uint netId in playerOrder){
            if(netId != 0 && NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                int shopCardCount = gamePlayerDeck.maxShopCardCount; // 플레이어별로 설정된 구매가능한 상점카드 최대 갯수
                List<Card> cardsByCharacter = M_CardManager.instance.cards.FindAll(card => card.baseCard.character == gamePlayer.character); // 카드매니저의 카드데이터 Synclist로부터 캐릭터별 카드 목록 추출
                if(cardsByCharacter.Count > 0){
                    for(int i = 0; i < shopCardCount; i++){
                        int randomIndex = Random.Range(0, cardsByCharacter.Count);
                        Card shopCard = cardsByCharacter[randomIndex].CardDeepCopy(false);
                        shopCard.guid = System.Guid.NewGuid().ToString();
                        shopCard.cardPrice = 1; // TODO : 카드 가격 설정. 임시로 가격 1원 설정
                        cardsByCharacter.RemoveAt(randomIndex);
                        gamePlayerDeck.shopCards.Add(shopCard); // 각 플레이어의 shopCards synclist에 상점카드 데이터 추가
                    }
                }
            }
        }
        cardNPC.monsterData = M_MonsterManager.instance.monsterDataList.Find(monster => monster.name.Equals("NPC_Mercurius"));
        NetworkServer.Spawn(cardNPC.gameObject);

        var avatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"), new Vector3(11,-3,0), Quaternion.identity);
        avatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.NPC;
        avatar.GetComponent<TargetObject>().monster = cardNPC;
        NetworkServer.Spawn(avatar);

        spawnedMonsterList.Add(avatar.GetComponent<TargetObject>());
        cardNPC.parent = avatar.GetComponent<TargetObject>();  // monster 오브젝트의 부모오브젝트 참조값 설정
    }

    [Server]
    public void GenerateBossMonster()
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        int randomNumber = Random.Range(0, 3);
        switch(randomNumber){
            case 0:
                var bossMoMos = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Momos"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
                bossMoMos.monsterData = M_MonsterManager.instance.monsterDataList.Find(x => x.name == "Boss_Momos");
                NetworkServer.Spawn(bossMoMos.gameObject);
                var bossMoMosAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
                bossMoMosAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
                bossMoMosAvatar.GetComponent<TargetObject>().monster = bossMoMos;
                NetworkServer.Spawn(bossMoMosAvatar);
                spawnedMonsterList.Add(bossMoMosAvatar.GetComponent<TargetObject>());
                bossMoMos.parent = bossMoMosAvatar.GetComponent<TargetObject>(); // monster 오브젝트의 부모오브젝트 참조값 설정
                break;
            case 1:
                var bossApates = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Apates"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
                bossApates.monsterData = M_MonsterManager.instance.monsterDataList.Find(x => x.name == "Boss_Apates");
                NetworkServer.Spawn(bossApates.gameObject);
                var bossApatesAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
                bossApatesAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
                bossApatesAvatar.GetComponent<TargetObject>().monster = bossApates;
                NetworkServer.Spawn(bossApatesAvatar);
                spawnedMonsterList.Add(bossApatesAvatar.GetComponent<TargetObject>());
                bossApates.parent = bossApatesAvatar.GetComponent<TargetObject>();
                break;
            case 2:
                var bossGeras = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "Boss_Geras"),targetObjectPosition[4],Quaternion.identity).GetComponent<SpawnedMonster>();
                bossGeras.monsterData = M_MonsterManager.instance.monsterDataList.Find(x => x.name == "Boss_Geras");
                NetworkServer.Spawn(bossGeras.gameObject);
                var bossGerasAvatar = Instantiate(netManager.spawnPrefabs.Find(prefab => prefab.name == "TargetObject"),targetObjectPosition[4],Quaternion.identity);
                bossGerasAvatar.GetComponent<TargetObject>().objectType = ProjectD.ObjectType.ENEMY;
                bossGerasAvatar.GetComponent<TargetObject>().monster = bossGeras;
                NetworkServer.Spawn(bossGerasAvatar);
                spawnedMonsterList.Add(bossGerasAvatar.GetComponent<TargetObject>());
                bossGeras.parent = bossGerasAvatar.GetComponent<TargetObject>();
                break;
        }
    }

    [Server]
    public void StartWaitCardQueue()
    {
        StartCoroutine(WaitCardQueue());
    }

    IEnumerator WaitCardQueue()
    {
        WaitForSeconds loopWait = new WaitForSeconds(0.01f);
        while(true)
        {
            if(!isCardQueueOperating && cardTargetPairQueue.Count == 0)
            {
                break;
            }
            yield return loopWait;
        }

        if(phase == BattleTurn.PLAYER_ACTIVE_DONE) // 아무때나 동작하지 않음 (광클방지)
        {
            phase = BattleTurn.PLAYER_END_TURN_EFFECT;
        }
    }

    public IEnumerator PlayerEndTurnEffect()
    {
        foreach(TargetObject tar in spawnedPlayerList) // 턴종료시 버프 효과들
        {
            // End Turn Card Effect
            List<int> currentKeys = tar.buffTurnEndEffect.Keys.ToList();
            foreach(int buffIndex in currentKeys)
            { 
                yield return tar.buffTurnEndEffect[buffIndex](tar,buffIndex,null);
            }   
        }
        yield return IronDemonPreEffect();
        phase = BattleTurn.PLAYER_END;
        yield return null;
    }

    [Server]
    public void EnterTheRoom(HexagonMapRoom hexagonMapRoom)
    {
        int actionCost = M_MapManager.instance.FindPath(M_MapManager.instance.currentRoom, hexagonMapRoom).Count;
        if(actionCost > M_MapManager.instance.currentActionCost){
            Debug.Log($"[행동 비용이 모자랍니다] 총 비용 : {M_MapManager.instance.currentActionCost} / 남은 비용 : {actionCost}");
        }else{
            // 맵 플레이어들 위치 이동
            foreach(GameObject mapPlayerPieceObject in M_MapManager.instance.mapPlayerPieces){
                MapPlayerPiece mapPlayerPiece = mapPlayerPieceObject.GetComponent<MapPlayerPiece>();
                mapPlayerPiece.RpcChangeMapPlayerPiecePosition(hexagonMapRoom.transform.position);
                M_MapManager.instance.SetDirection(hexagonMapRoom);
            }
            M_MapManager.instance.MoveToRoom();
        }
    }

    [Server]
    public void GenerateBattleObject(HexagonMapRoom hexagonMapRoom)
    {
        if(isServer)
        {
            GeneratePlayerUnit();
            if(hexagonMapRoom.roomType == RoomType.BOSS){ // 보스 몬스터 생성
                GenerateBossMonster();
                RpcCardPrefareForBattle();
                RpcStartBossBattleEvent();
            }else if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE){ // 일반 or 엘리트 몬스터 생성
                GenerateMonster(hexagonMapRoom);
                RpcCardPrefareForBattle();
                RpcStartBattleEvent(hexagonMapRoom.roomType);
            }else{ // NPC 생성
                GnenrateNPCByRoomTpye(hexagonMapRoom.roomType);
                RpcStartNoneBattleEvent(hexagonMapRoom.roomType);
            }
            // 전투 시작 이치 초기화 및 어빌리티 카드 생성
            foreach(GamePlayerDeck gamePlayerDeck in FindObjectsOfType<GamePlayerDeck>())
            {
                if(gamePlayerDeck.abilityCard == null)gamePlayerDeck.SpawnAbilityCardRPC();
            }
            StartCoroutine(WaitingForPlayer(hexagonMapRoom));
        }
    }

    IEnumerator WaitingForPlayer(HexagonMapRoom hexagonMapRoom)
    {
        M_NetworkRoomManager netManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        int cnt = 0;
        while(true)
        {
            cnt = 0;
            yield return new WaitForSeconds(0.1f);
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface user in users)
                if(user.isTargetObjectInitDone) cnt++;
            if(cnt != netManager.roomSlots.Count) continue;

            if(hexagonMapRoom.roomType == RoomType.MONSTER || hexagonMapRoom.roomType == RoomType.ELITE || hexagonMapRoom.roomType == RoomType.BOSS)
                phase = BattleTurn.BATTLE_INITIALIZE;
            else
                phase = BattleTurn.NONE_BATTLE_SCENE;
            break;
        }
        ClearTargetObjectInitFlag();
    }


    // -------------------------------------------------------------------- ClientRpc Method -----------------------------------------------------------------//
    
    // 전투 종료 보상 카드 팝업 호출
    [ClientRpc]
    public void RpcShowBattleResultPopUp()
    {
        // 전투 종료 음성 재생
        Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer.character;
        switch(character){
            case Character.HONGDANHYANG:
                List<AudioClip> battleWinVoicesDanhyang = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 68, 3);
                AudioClip audioClipDanhyang = battleWinVoicesDanhyang[Random.Range(0, battleWinVoicesDanhyang.Count)];
                M_SoundManager.instance.PlayVoice(audioClipDanhyang, audioClipDanhyang.length);
                break;
            case Character.GEORK:
                List<AudioClip> battleWinVoicesGeork = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 80, 3);
                AudioClip audioClipGeork = battleWinVoicesGeork[Random.Range(0, battleWinVoicesGeork.Count)];
                M_SoundManager.instance.PlayVoice(audioClipGeork, audioClipGeork.length);
                break;
            case Character.ERIS:
                List<AudioClip> battleWinVoicesEris = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 123, 3);
                AudioClip audioClipEris = battleWinVoicesEris[Random.Range(0, battleWinVoicesEris.Count)];
                M_SoundManager.instance.PlayVoice(audioClipEris, audioClipEris.length);
                break;
        }
        // 전투 종료 팝업 호출
        PopUpUIManager.instance.HandleShowBattleResultPopUp();
    }

    // 페이즈 상태 텍스트 업데이트
    [ClientRpc]
    void RpcChangePhase(BattleTurn phase)
    {
        GameUIManager.instance.textCurrentPhase.text = phase.ToString();
    }

    // 전투에 필요한 카드 준비 요청
    [ClientRpc]
    void RpcCardPrefareForBattle()
    {
        M_CardManager.instance.PrefareCardWithSuffle(); // 카드데이터 셔플 수행후 PrefareDeck에 추가
        M_CardManager.instance.ChangeAbilityButtonActiveState(true); // 어빌리티 버튼 활성화
    }
 
    // 보스전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBossBattleEvent()
    {
        AudioClip stageStartAudio = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("stage_start"));
        M_SoundManager.instance.PlaySFX(stageStartAudio, stageStartAudio.length);
        Camera.main.orthographicSize = battelSceneCameraSize;
        M_MessageManager.instance
            .MakeToast()
            .Position(ToastPosition.Top)
            .FadeInTime(1f)
            .FadeOutTime(1f)
            .MessageBoxColor(ColorUtils.HexToColor("#E700FF"))
            .TextColor(Color.white)
            .Text("전투 : 보스")
            .Show();
    }

    // 일반 몬스터 혹은 엘리트전 시작 수신 이벤트
    [ClientRpc]
    public void RpcStartBattleEvent(RoomType roomType)
    {
        AudioClip stageStartAudio = M_SoundManager.instance.sfxClips[SFX_TYPE.MainUI].Find((audioClip) => audioClip.name.Equals("stage_start"));
        M_SoundManager.instance.PlaySFX(stageStartAudio, stageStartAudio.length);
        Camera.main.orthographicSize = battelSceneCameraSize;
        Character character = NetworkClient.localPlayer.GetComponent<PlayerInterface>().character; // 로컬 플레이어가 선택한 캐릭터 조회
        switch(roomType)
        {
            case RoomType.MONSTER:
                // 토스트 메시지 표시
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("전투 : 일반 몬스터")
                    .Show();  
                
                // BGM 재생     
                string audioName = Random.Range(0, 2) == 0 ? "Monster_Battle_N_1" : "Monster_Battle_N_2";
                AudioClip audioClip_monster_n = M_SoundManager.instance.bgmClips[BGM_TYPE.Battle].Find((audioClip) => audioClip.name.Equals(audioName));
                M_SoundManager.instance.PlayBGM(audioClip_monster_n, MusicTransition.Swift, 1.5f);

                // 캐릭터별 일반 몬스터 전투 음성대화 재생
                switch(character){
                    case Character.HONGDANHYANG:
                        List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 98, 3);
                        AudioClip danhyangNormalBattleVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                        M_SoundManager.instance.PlayVoice(danhyangNormalBattleVoice, danhyangNormalBattleVoice.length);
                        break;
                    case Character.GEORK:
                        List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 110, 3);
                        AudioClip georkNormalBattleVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                        M_SoundManager.instance.PlayVoice(georkNormalBattleVoice, georkNormalBattleVoice.length);
                        break;
                    case Character.ERIS:
                        List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 156, 3);
                        AudioClip erisNormalBattleVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                        M_SoundManager.instance.PlayVoice(erisNormalBattleVoice, erisNormalBattleVoice.length);
                        break;
                }
                break;

            case RoomType.ELITE:
                // 토스트 메시지 표시
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.red)
                    .TextColor(Color.white)
                    .Text("전투 : 엘리트 몬스터")
                    .Show();
               
                // BGM 재생            
                AudioClip audioClip_monster_e = M_SoundManager.instance.bgmClips[BGM_TYPE.Battle].Find((audioClip) => audioClip.name.Equals("Monster_Battle_E"));
                M_SoundManager.instance.PlayBGM(audioClip_monster_e, MusicTransition.Swift, 1.5f);

                // 캐릭터별 엘리트 몬스터 전투 음성대화 재생
                switch(character){
                    case Character.HONGDANHYANG:
                        List<AudioClip> danhyangVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.HongDanHyang, 12, 3);
                        AudioClip danhyangEliteBattleVoice = danhyangVoices[Random.Range(0, danhyangVoices.Count)];
                        M_SoundManager.instance.PlayVoice(danhyangEliteBattleVoice, danhyangEliteBattleVoice.length);
                        break;
                    case Character.GEORK:
                        List<AudioClip> georkVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Geork, 12, 3);
                        AudioClip georkEliteBattleVoice = georkVoices[Random.Range(0, georkVoices.Count)];
                        M_SoundManager.instance.PlayVoice(georkEliteBattleVoice, georkEliteBattleVoice.length);
                        break;
                    case Character.ERIS:
                        List<AudioClip> erisVoices = M_SoundManager.instance.GetVoiceClipsByVoiceType(VOICE_TYPE.Eris, 13, 3);
                        AudioClip erisEliteBattleVoice = erisVoices[Random.Range(0, erisVoices.Count)];
                        M_SoundManager.instance.PlayVoice(erisEliteBattleVoice, erisEliteBattleVoice.length);
                        break;
                }
                break;
        }
    }

    // 엔피씨 방문 수신 이벤트
    [ClientRpc]
    public void RpcStartNoneBattleEvent(RoomType roomType)
    {
        Camera.main.orthographicSize = battelSceneCameraSize;
        switch(roomType)
        {
            case RoomType.EVENT_POSITIIVE:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.yellow)
                    .TextColor(Color.white)
                    .Text("긍정적 이벤트")
                    .Show();
                AudioClip audioClip_event_positive = M_SoundManager.instance.bgmClips[BGM_TYPE.Event].Find((audioClip) => audioClip.name.Equals("Positive_Event"));
                M_SoundManager.instance.PlayBGM(audioClip_event_positive, MusicTransition.Swift, 1.5f);
                PlayEventConversation(true);
                break;
            case RoomType.EVENT_NEGATIVE:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.yellow)
                    .TextColor(Color.white)
                    .Text("부정적 이벤트")
                    .Show();
                AudioClip audioClip_event_negative = M_SoundManager.instance.bgmClips[BGM_TYPE.Event].Find((audioClip) => audioClip.name.Equals("Negative_Event"));
                M_SoundManager.instance.PlayBGM(audioClip_event_negative, MusicTransition.Swift, 1.5f);
                PlayEventConversation(false);
                break;
            case RoomType.CAMP:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor( Color.green)
                    .TextColor(Color.white)
                    .Text("전초기지")
                    .Show();
                // 전초기지 배경음 재생
                AudioClip audioClip_base_camp = M_SoundManager.instance.bgmClips[BGM_TYPE.Event].Find((audioClip) => audioClip.name.Equals("Base_Camp"));
                M_SoundManager.instance.PlayBGM(audioClip_base_camp, MusicTransition.Swift, 1.5f);
                break;
            case RoomType.CARD_NPC:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.magenta)
                    .TextColor(Color.white)
                    .Text("카드 상점")
                    .Show();
                // 카드 상점 배경음 재생                    
                AudioClip audioClip_card_hop = M_SoundManager.instance.bgmClips[BGM_TYPE.Event].Find((audioClip) => audioClip.name.Equals("Card_Shop"));
                M_SoundManager.instance.PlayBGM(audioClip_card_hop, MusicTransition.Swift, 1.5f);
                break;
            case RoomType.ITEM_NPC:
                M_MessageManager.instance
                    .MakeToast()
                    .Position(ToastPosition.Top)
                    .FadeInTime(1f)
                    .FadeOutTime(1f)
                    .MessageBoxColor(Color.blue)
                    .TextColor(Color.white)
                    .Text("아이템 상점")
                    .Show();                
                // 아이템 상점 배경음 재생
                AudioClip audioClip_item_hop = M_SoundManager.instance.bgmClips[BGM_TYPE.Event].Find((audioClip) => audioClip.name.Equals("Item_Shop"));
                M_SoundManager.instance.PlayBGM(audioClip_item_hop, MusicTransition.Swift, 1.5f);            
                break;
        }
    }

    [ClientRpc]
    void ClearTargetObjectInitFlag()
    {
        NetworkClient.connection.identity.GetComponent<PlayerInterface>().isTargetObjectInitDone = false;
    }

    [ClientRpc]
    public void StartAnimation(TargetObject tar, int trackIndex,string animationName, bool loop )
    {
        if(tar != null)
        {
            SkeletonAnimation anim = tar.avatar.GetComponent<SkeletonAnimation>();
            Spine.TrackEntry track = anim.state.SetAnimation(trackIndex,animationName,loop);
            track.MixBlend = Spine.MixBlend.Replace;
        }
    }

    [ClientRpc]
    public void MoveIronDemon(TargetObject tar, TargetObject target)
    {
        if(tar != null && target != null){
            tar.ironDemon.GetComponent<SkeletonRenderTexture>().enabled = false;
            if(target.objectType == ObjectType.PLAYER){
                tar.ironDemon.GetComponent<MeshRenderer>().sortingOrder = target.avatar.GetComponent<MeshRenderer>().sortingOrder - 1;
            }else{
                tar.ironDemon.GetComponent<MeshRenderer>().sortingOrder = -1;
            }
            tar.ironDemon.GetComponent<SkeletonAnimation>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            int transformOffset = CalcOffset(tar); 
            if(target.monster != null)
                if(target.monster.monsterName == "Boss_Momos") // 모모스 키 적용 TODO: 몬스터 키적용 코드 추가
                    tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,5,0);
                else
                    tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
            else
                tar.ironDemon.transform.position = target.transform.position + new Vector3(transformOffset,0,0);
            if(target.objectType == ObjectType.PLAYER) tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[0];
            else tar.ironDemon.GetComponent<SkeletonAnimation>().skeletonDataAsset = tar.ironDemonData[1];
            tar.ironDemon.GetComponent<SkeletonAnimation>().Initialize(true);
            tar.ironDemon.GetComponent<SkeletonRenderTexture>().enabled = true;
        }
    }

    // 영웅능력으로 철귀 이동 시 음성 재생
    [ClientRpc]
    public void PlayIronDemonCommandVoice(TargetObject tar, TargetObject target)
    {
        if(target.player != null){
            if(target.player.objectOwner.isLocalPlayer){
                AudioClip abilitySound = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][55]; // 이리 오거라
                M_SoundManager.instance.PlayVoice(abilitySound, abilitySound.length);
            }else{
                List<AudioClip> clips = new List<AudioClip>();
                AudioClip abilitySound1 = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][53]; // 도와 주거라
                AudioClip abilitySound2 = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][56]; // 저리 가주거라
                clips.Add(abilitySound1);
                clips.Add(abilitySound2);
                AudioClip abilitySound = clips[Random.Range(0, clips.Count)];
                M_SoundManager.instance.PlayVoice(abilitySound, abilitySound.length);
            }
        }else{
            List<AudioClip> clips = new List<AudioClip>();
            AudioClip abilitySound1 = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][52]; // 부탁하마
            AudioClip abilitySound2 = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][54]; // 융융아 가거라
            AudioClip abilitySound3 = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][57]; // 물어 뜯어 주거라
            clips.Add(abilitySound1);
            clips.Add(abilitySound2);
            clips.Add(abilitySound3);
            AudioClip abilitySound = clips[Random.Range(0, clips.Count)];
            M_SoundManager.instance.PlayVoice(abilitySound, abilitySound.length);
        }
    }

    int CalcOffset(TargetObject tar)
    {
        int retVal = 0;
        if(tar.player == (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer)) retVal = 0;
        else
        {
            int addval = 0;
            foreach(uint netId in playerOrder){
                if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                    if(tar.player == networkIdentity.GetComponent<GamePlayer>())
                        break;
                    if(tar.player == (NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayer))
                        continue;
                    else
                        addval++;
                }
            }
            if(addval == 0) retVal = -1;
            else retVal = 1;
        }
        return retVal;
    }

    [ClientRpc]
    public void AnimIronDemon(string anim ,TargetObject tar)
    {
        if(tar != null){
            bool isLoop = anim == "Idle" ? true : false;
            if(anim == "TeleportBack")tar.ironDemon.GetComponent<SkeletonAnimation>().maskInteraction = SpriteMaskInteraction.None;
            tar.ironDemon.GetComponent<SkeletonAnimation>().state.SetAnimation(0,anim,isLoop);
            tar.ApllyIronDemonAnimationCallbackFunction();
        }
    }


    [ClientRpc]
    public void EachPlayerCardDraw()
    {
        if(NetworkClient.connection != null && NetworkClient.active){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            foreach(GamePlayer gamePlayer in playerInterface.ownedPlayers){
                GamePlayerDeck gamePlayerDeck = gamePlayer.GetComponent<GamePlayerDeck>();
                foreach(CardOnHand cardOnHand in gamePlayerDeck.cardOnHands) // 영원 카드의 경우도 변경된 정보 제공
                    cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);
                if(NetworkClient.spawned.ContainsKey(gamePlayer.GetComponent<GamePlayerTarget>().targetObject))
                    gamePlayerDeck.CmdSpawnCardOnHand();
            }
        }
    }

    [ClientRpc]
    public void EachPlayerEndTurn()
    {
        // 각 플레이어들의 모든 카드와 화살표 제거
        M_CardManager.instance.RemoveAllCurrentPlayerArrow();
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHands();
    }

    [ClientRpc]
    public void EachPlayerNoneBattleEnd()
    {
        M_CardManager.instance.RemoveAllCurrentPlayerCardOnHandsWithOutTrashDeck(); // 현재 플레이어 손에 있던 카드들을 삭제, 삭제 시 Trash Deck에 추가하지 않음.
        M_CardManager.instance.RemoveAllCurrentPlayerPrefareDeckAndTrashDeck(); // 플레이어의 PrefareDeck, TrashDeck 삭제
        M_CardManager.instance.ChangeAbilityButtonActiveState(false); // 어빌리티 버튼 비활성화
        ReturnToMap();
    }

    public void ReturnToMap()
    {
        string audioName = M_MapManager.instance.mapBoss == null ? "Stage_1_Map" : "Stage_1_Map_Boss_Spawn";
        AudioClip audioClip_map = M_SoundManager.instance.bgmClips[BGM_TYPE.Map].Find((audioClip) => audioClip.name.Equals(audioName));
        M_SoundManager.instance.PlayBGM(audioClip_map, MusicTransition.CrossFade, 2f);
        if(isServer){
            ClearTargetObject(); // 타겟오브젝트 정리
            M_MapManager.instance.ClearPlayerVoteHexagonMapRooms(); // 방 투표 목록 비움
            M_MapManager.instance.SetRoomStateComplete(); // 방 완료상태로 변경
            M_MapManager.instance.DecreaseTotalActionCost(); // 행동비용 감소
            M_MapManager.instance.ApproachBossToPlayer(); // 보스가 플레이어에게로 이동
            PlayerInterface[] users = FindObjectsOfType<PlayerInterface>();
            foreach(PlayerInterface player in users){
                player.SetIsReadyStateDefault();
            }
        }
        GameUIManager.instance.FadeBlackCurtain((blackCurtain) => {
            // 카메라 위치 리셋
            Vector3 currLoc = M_MapManager.instance.currentRoom.transform.position;
            Camera.main.transform.position = currLoc + new Vector3(0,0,-8);
            //Camera.main.orthographic = false; 
            Camera.main.orthographicSize = mapSceneCameraSize;

            // UI 활성화 상태 변경
            M_MapManager.instance.MapScene.SetActive(true);
            M_MapManager.instance.BattleScene.SetActive(false);
            M_MapManager.instance.BackgroundLight.GetComponent<MeshRenderer>().sortingLayerName = "Default"; // 배경 플레어 정렬 오더 변경

            // 임시 테스트용 UI
            //GameUIManager.instance.TestUI.gameObject.SetActive(false);
            
            // Dim배경 상태 변경
            blackCurtain.gameObject.SetActive(false);
            blackCurtain.DOFade(0.0f, 0.5f); // 원래 알파값으로 변경
            MapUI.instance.ChangeMapDimBackground(false);
            MapUI.instance.RemoveAllMapInfoPopUps();
        });
    }

    public void MoveToPlayer(GamePlayer player, MoveDirection direction)
    {
        TargetObject forwarding = null,backwarding = null;
        Vector3 forwardingDestination = new Vector3(0,0,0),backwardingDestination = new Vector3(0,0,0);
        if(direction == MoveDirection.FORWARD)
        {
            if(player.selectOrder == 0) return;
            uint netId = playerOrder[player.selectOrder - 1];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer swap = networkIdentity.GetComponent<GamePlayer>();
                playerOrder[player.selectOrder-1] = player.netId;
                playerOrder[player.selectOrder] = swap.netId;
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
            
        }
        if(direction == MoveDirection.BACKWARD)
        {
            if(player.selectOrder == NetworkServer.connections.Count - 1) return;
            uint netId = playerOrder[player.selectOrder + 1];
            if(NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity)){
                GamePlayer swap = networkIdentity.GetComponent<GamePlayer>();
                playerOrder[player.selectOrder+1] = player.netId;
                playerOrder[player.selectOrder] = swap.netId;
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
        }
        forwarding.transform.DOMove(forwardingDestination,0.5f,false).SetEase(Ease.OutQuart);
        backwarding.transform.DOMove(backwardingDestination,0.5f,false).SetEase(Ease.OutQuart);
    }

    public TargetObject[] GetTargetObjectFromActionTarget(ActionTarget target)
    {
        if(target == ActionTarget.FIXEDPLAYER || target == ActionTarget.RANDOM || target == ActionTarget.NONE){
            Debug.Log("ERROR : Next Target Error");
        }
        List<TargetObject> retVal = new List<TargetObject>();
        switch(target)
        {
            case ActionTarget.FRONT :
                if(M_TurnManager.instance.playerOrder[2] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[2]].GetComponent<GamePlayerTarget>().GetTargetObject());
                else retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.MIDDLE :
                if(M_TurnManager.instance.playerOrder[1] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[1]].GetComponent<GamePlayerTarget>().GetTargetObject());
                else retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.BACK :
                if(M_TurnManager.instance.playerOrder[0] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[0]].GetComponent<GamePlayerTarget>().GetTargetObject());
                else retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.FRONT_BACK :
                if(M_TurnManager.instance.playerOrder[2] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[2]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(M_TurnManager.instance.playerOrder[0] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[0]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.FRONT_MIDDLE :
                if(M_TurnManager.instance.playerOrder[2] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[2]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(M_TurnManager.instance.playerOrder[1] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[1]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.MIDDLE_BACK :
                if(M_TurnManager.instance.playerOrder[1] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[1]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(M_TurnManager.instance.playerOrder[0] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[0]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
            case ActionTarget.WHOLE :
                if(M_TurnManager.instance.playerOrder[0] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[0]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(M_TurnManager.instance.playerOrder[2] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[2]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(M_TurnManager.instance.playerOrder[1] != 0) retVal.Add(NetworkServer.spawned[M_TurnManager.instance.playerOrder[1]].GetComponent<GamePlayerTarget>().GetTargetObject());
                if(retVal.Count == 0)
                    retVal.AddRange(spawnedPlayerList);
                break;
        }
       

        return retVal.ToArray();
    }


    public List<TargetObject> GetTargetObjectFromActionTargetList(ActionTarget target)
    {
        if(target == ActionTarget.FIXEDPLAYER || target == ActionTarget.RANDOM || target == ActionTarget.NONE){
            Debug.Log("ERROR : Next Target Error");
        }
        List<TargetObject> retVal = new List<TargetObject>();
        foreach(TargetObject tar in spawnedPlayerList)
        {
            if( target == ActionTarget.WHOLE || 
                (target == ActionTarget.FRONT && tar.player.selectOrder == 2) ||
                (target == ActionTarget.MIDDLE && tar.player.selectOrder == 1) ||
                (target == ActionTarget.BACK && tar.player.selectOrder == 0) ||
                (target == ActionTarget.FRONT_MIDDLE && tar.player.selectOrder != 0) ||
                (target == ActionTarget.MIDDLE_BACK && tar.player.selectOrder != 1) ||
                (target == ActionTarget.FRONT_BACK && tar.player.selectOrder != 2) )
                retVal.Add(tar);
        }
        if(retVal.Count == 0)
            retVal.AddRange(spawnedPlayerList);

        return retVal;
    }

    // Synclist에서 오더 인덱스 변경 이벤트 수신하여 GamePlayer의 selectOrder Syncvar값을 변경
    public void SetGamePlayerOrder(uint gamePlayerNetId, int index)
    {
        if(isServer){
            if(NetworkServer.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.selectOrder = index;
                gamePlayer.OnChangedSelectOrder(index, index);
                gamePlayer.objectOwner.selectOrder = index;
            }
        }else{
            if(NetworkClient.spawned.TryGetValue(gamePlayerNetId, out NetworkIdentity networkIdentity)){
                GamePlayer gamePlayer = networkIdentity.GetComponent<GamePlayer>();
                gamePlayer.selectOrder = index;
                gamePlayer.OnChangedSelectOrder(index, index);
                gamePlayer.objectOwner.selectOrder = index;
            }
        }
    }

    // ---------------------------------------------------------------SyncList Callback -----------------------------------------------------------------//
    private void OnPlayerOrderUpdated(SyncList<uint>.Operation op, int index, uint oldVal, uint newVal)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
            
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:

                break;
            case SyncList<uint>.Operation.OP_SET:
                SetGamePlayerOrder(newVal, index);
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // 카드 큐 시스템
    // 1. 큐에서 Enqueue 될 때 Synclist에도 동일하게 추가(Add)하여 카드 큐 오브젝트를 생성
    // 2. 큐에서 Dequeue 될 때 타겟이 유효하지 않아 되돌아올 경우 Synclist에서 마지막 데이터를 제거(RemoveAt)하여 카드 큐 오브젝트 제거
    // 3. 큐에서 Dequeue 될 때 카드 사용이 유효하면, Synclist에 데이터 변경(Set)하여 현재 수행된 카드 큐 오브젝트 표시
    private void OnCardQueueUpdated(SyncList<CardQueue>.Operation op, int index, CardQueue oldVal, CardQueue newVal)
    {
        switch (op)
        {
            case SyncList<CardQueue>.Operation.OP_ADD:
                GameObject cardQueueItemObject = Instantiate(cardQueueItemPrefab, Vector3.zero, Quaternion.identity, GameUIManager.instance.cardQueueLayout.transform);
                CardQueueItem cardQueueItem = cardQueueItemObject.GetComponent<CardQueueItem>();
                cardQueueItem.cardQueue = newVal;
                cardQueueItems.Add(cardQueueItemObject);
                GameUIManager.instance.CardQueueScrollToEnd();
                break;
            case SyncList<CardQueue>.Operation.OP_INSERT:
                
                break;
            case SyncList<CardQueue>.Operation.OP_REMOVEAT:
                Destroy(cardQueueItems[index]);
                cardQueueItems.RemoveAt(index);
                break;
            case SyncList<CardQueue>.Operation.OP_SET:
                foreach(GameObject gameObject in cardQueueItems){
                    CardQueueItem item = gameObject.GetComponent<CardQueueItem>();
                    item.bigCardQueue.gameObject.SetActive(false);
                    item.smallCardQueue.gameObject.SetActive(true);
                }
                CardQueueItem currentCardQueue = cardQueueItems[index].GetComponent<CardQueueItem>();
                currentCardQueue.bigCardQueue.gameObject.SetActive(true);
                currentCardQueue.smallCardQueue.gameObject.SetActive(false);
                currentCardQueue.transform.DOScale(1.5f, 0.25f).OnComplete(() => {
                    currentCardQueue.transform.DOScale(1f, 0.25f);
                });
                break;
            case SyncList<CardQueue>.Operation.OP_CLEAR:
                foreach(GameObject cardQueueObject in cardQueueItems){
                    Destroy(cardQueueObject);
                }
                cardQueueItems.Clear();
                break;
        }
    }
}
