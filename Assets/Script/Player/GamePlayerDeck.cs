using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


public partial class GamePlayerDeck : NetworkBehaviour
{

    [SyncVar (hook = nameof(OnChangeCurrentDeckCount))]
    public int currentDeckCount = 0; // 현재 플레이어의 카드 카운트

    [SyncVar]
    public int maxShopCardCount = 0; // 상점에서 구매 가능한 카드 최대 갯수

    [SyncVar]
    public int maxRewardCardCount = 0; // 전투 보상 팝업에서 선택 가능한 카드 최대 갯수

    [SyncVar (hook = nameof(OnChangeMaxRemoveCardCount))]
    public int maxRemoveCardCount = 0; // 패 제거 팝업에서 선택 가능한 카드 최대 갯수

    [SyncVar]
    public CardPocket cardPocket; // 현재 플레이어 소유의 카드 포켓 오브젝트

    [SyncVar]
    public CardCtrlArrow cardCtrlArrow; // 현재 소환된 카드 화살표

    [SyncVar]
    public AbilityButton abilityButton; // 현재 소환된 어빌리티 버튼

    [SyncVar]
    public AbilityCtrlArrow abilityCtrlArrow; // 현재 소환된 어빌리티 화살표

    [SyncVar]
    public CardOnHand abilityCard; // 현재 소환된 어빌리티 카드

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 덱 총괄 데이터

    public readonly SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 덱
    
    public readonly SyncList<Card> trashDeck = new SyncList<Card>(); // 버린 덱

    public readonly SyncList<Card> forgottenDeck = new SyncList<Card>(); // 잊혀진 덱 찰나로 보내진 덱

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 패 카드 오브젝트 리스트

    public readonly SyncList<Reward> rewards = new SyncList<Reward>(); // 전투 보상 전체 목록
    
    public readonly SyncList<Card> rewardCards = new SyncList<Card>(); // 전투 보상 카드

    public readonly SyncList<Card> addtionDrawCards = new SyncList<Card>(); // 추가 드로우 카드

    public readonly SyncList<Card> shopCards = new SyncList<Card>(); // 상점 카드

    public int currentIndex = 0; // 패 제거 팝업에서 삭제하기 위해 선택된 카드들의 인덱스(순환용)

    public CardOnHand[] choosedCardOnHands;  // 패 제거 팝업에서 삭제하기 위해 선택된 카드 오브젝트들을 담을 배열

    public Queue<(CardOnHand,TargetObject)> serverCardPredictQueue = new Queue<(CardOnHand, TargetObject)>();// Server에서 Card Queue 관리를 위한 Queue

    [SyncVar(hook = nameof(PreviousCardTypeChanged))]
    public CardType previousCardType;

    public List<CardOnHand> destroyCardList = new List<CardOnHand>();

    public int numOfUsedIronTeeth = 0;

    [SyncVar(hook = nameof(OnChangedNumberOfUsedCard))]
    public int numOfUsedCard = 0;

    [SyncVar]
    public int AdditionalSizeOfIromDemon; //철귀 영구 크기 증가

    [SyncVar]
    public string usedCardName;

    [SyncVar]
    public int gainCurseCardCount = 0;

    private readonly float duration = 0.05f;
    
    private int prefareDeckCountDelay = 0;
    
    private int trashDeckCountDelay = 0;
    
    private int forgottenDeckCountDelay = 0;


    public override void OnStartServer()
    {
        SetInitialValue();
        StartCoroutine(EnQueueCardTargetPair());
        StartCoroutine(ServerDestroyCardOnHand()); 
    }

    public override void OnStartClient()
    {
        deck.Callback += OnDeckUpdated;
        cardOnHands.Callback += OnCardOnHandsUpdated;
        prefareDeck.Callback += OnPrefareDeckUpdated;
        trashDeck.Callback += OnTrashDeckUpdated;
        forgottenDeck.Callback += OnForgottenDeckUpdated;
        rewards.Callback += OnRewardUpdated;
        rewardCards.Callback += OnRewardCardUpdated;
        addtionDrawCards.Callback += OnAddtionCardUpdated;
        shopCards.Callback += OnShopCardUpdated;
        if(isOwned){
            GameUIManager.instance.currentIchiText.text = currentIchi.ToString(); // 현재 이치값 초기 뷰 세팅
            GameUIManager.instance.maxIchiText.text = maxIchi.ToString(); // 최대 이치값 초기 뷰 세팅
        }  
    }

    // 화살표 주인 카드 참조값 설정
    public void SetArrowOwnCardOnHand(CardOnHand cardOnHand)
    {
        cardCtrlArrow.arrowOwnedCardOnHand = cardOnHand;
    }

    // choosedCardOnHands 배열에 선택한 카드를 추가
    public void AddChoosedCardOnHands(CardOnHand cardOnHand)
    {
        cardOnHand.isChoosed = true;
        // 인덱스 순환 반복
        currentIndex = (currentIndex + 1) % choosedCardOnHands.Length;
        for(int i=0; i<choosedCardOnHands.Length; i++){
            if(choosedCardOnHands[i] == null){
                currentIndex = i;
                break;
            }
        }
        if(choosedCardOnHands[currentIndex] != null){
            M_CardManager.instance.ResetCardAllState(choosedCardOnHands[currentIndex], false);
        }
        choosedCardOnHands[currentIndex] = cardOnHand;
        M_CardManager.instance.CardOnHandChooseForRemoveSequence(cardOnHand, currentIndex);
    }

    // choosedCardOnHands 배열에 선택한 카드를 제거
    public void RemoveChoosedCardOnHands(CardOnHand cardOnHand)
    {
        for(int i = 0; i < choosedCardOnHands.Length; i++){
            if(choosedCardOnHands[i] == cardOnHand){
                choosedCardOnHands[i] = null;
            }
        }
        cardOnHand.isChoosed = false;
        M_CardManager.instance.ResetCardAllState(cardOnHand, false);
    }

    // ---------------------------------------------------------------------- Server Method -----------------------------------------------------------------//
    
    // 플레이어 댁 정보 초기화
    [Server]
    public void SetInitialValue()
    {
        if(M_SaveManager.instance.isSaveGame)
        {
            SetInitialIchi();
            currentDeckCount = 5;
            maxShopCardCount = 6;
            maxRewardCardCount = 3;
            maxRemoveCardCount= 3;
            foreach(SaveDataPlayer saveDataPlayer in M_SaveManager.instance.loadData.players)
            {
                if(saveDataPlayer == null)break;
                if(saveDataPlayer.ownerSteamId == GetComponent<GamePlayer>().objectOwner.steamID)
                {
                    foreach(Card card in saveDataPlayer.cards)
                    {
                        Card savedCard = card;
                        deck.Add(savedCard);
                    }
                }
            }
        }
        else
        {
            SetInitialIchi();
            currentDeckCount = 5;
            maxShopCardCount = 6;
            maxRewardCardCount = 3;
            maxRemoveCardCount = 3;
            Character character = GetComponent<GamePlayer>().character;
            switch(character){
                case Character.GEORK:
                    //for(int i = 0; i < 8; i++)
                    //{
                    //    if(i % 2 == 0){
                    //        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G1_H")));
                    //        deck.Add(attackCard);
                    //    }else{
                    //        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G0")));
                    //        deck.Add(defenseCard);
                    //    }   
                    //}
                    break;
                case Character.ERIS:
                    //for(int i = 0; i < 8; i++)
                    //{
                    //    if(i % 2 == 0){
                    //        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("E0")));
                    //        deck.Add(attackCard);
                    //    }else{
                    //        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("E1")));
                    //        deck.Add(defenseCard);
                    //    }
                    //}
                    break;
                case Character.HONGDANHYANG:
                    //for(int i = 0; i < 10; i++)
                    //{
                        //Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H"+(i+2))));
                        //deck.Add(attackCard);
                    //    if(i % 2 == 0){
                    //        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H1")));
                    //        deck.Add(attackCard);
                    //    }else{
                    //        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H41")));
                    //        deck.Add(defenseCard);
                    //    }
                    //    
                    //   
                    //}
                    // Card additional = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H34")));
                    //        deck.Add(additional);

                    break;
                default:
                    break;
            }
        }
    }
    
    public int GetTotalCostOfCardOnHand(CardOnHand cardOnHand)
    {
        int totalCost;
        totalCost = cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition + (GetComponent<GamePlayerTarget>().GetTargetObject().buffs.FindIndex(x => x.type == BuffType.GOHANG3_DEBUFF) == -1 ? 0 : 1);

        if(cardOnHand.card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(cardOnHand.card.baseCard.cardType == previousCardType)
                totalCost -= 1;
            else
                totalCost += 1;
        }
        
        if(cardOnHand.card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.HEBANG))
            totalCost -= numOfUsedCard;
        if(GetComponent<GamePlayerTarget>().GetTargetObject().buffs.FindIndex(x => x.type == BuffType.GOHANG3) != -1)totalCost = 1;
        return (totalCost < 0) ? 0 : totalCost;
    }

    [Server]
    IEnumerator EnQueueCardTargetPair()
    {
        // TargetObject List 구조 : 
        /*
            Index : 내용
            0 : 카드 사용한 Player 
            1 : Target Monster
            이후 : 모든 플레이어 및 몬스터
        */
        WaitForSeconds loopTime = new WaitForSeconds(0.01f);
        CardOnHand cardOnHand;
        TargetObject targetObject;

        while(true)
        {
            yield return loopTime; // 0.01s

            if(serverCardPredictQueue.Count == 0) continue; //카드큐가 비어있을경우 스킵 
            
            (cardOnHand,targetObject) = serverCardPredictQueue.Dequeue(); // Command가 왔기때문에 Dequeue하여 판단

            int totalCost = GetTotalCostOfCardOnHand(cardOnHand);

            if(totalCost > currentIchi) // 카드 코스트 계산 하는곳
            {
                ReturnToCardOnHand(cardOnHand);
                continue;
            }
            if(cardOnHand.card.baseCard.isTargetable && targetObject == null)
            {
                ReturnToCardOnHand(cardOnHand);
                continue;
            }
            currentIchi -= totalCost ;

            // 여기부터 카드사용이 확정 되는곳
            previousCardType = cardOnHand.card.baseCard.cardType;
           
            List<TargetObject> targetObjects = new List<TargetObject>();
            targetObjects.Add(M_TurnManager.instance.GetPlayer(this)); // Index 0 
            if(cardOnHand.card.baseCard.isTargetable)targetObjects.Add(targetObject);// Index 1 // TargetAble이 아닐경우 Index1은 비워짐
            targetObjects.AddRange(M_TurnManager.instance.GetPlayerObjects());
            targetObjects.AddRange(M_TurnManager.instance.GetMonsterObjects());

            M_TurnManager.instance.cardTargetPairQueue.Enqueue((this, totalCost, cardOnHand, targetObjects)); // 큐에 데이터 추가

            M_TurnManager.instance.AddCardQueueList(cardOnHand, this.netId); // 카드 큐 Synclist에 데이터 추가
        }
    }

    [Server]
    public void CmdSpawnCardOnHand(int cardCount)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        if(prefareDeck.Count == 0 && trashDeck.Count == 0)
        {
            CmdAddPrefareDeckWithShuffle();
        }
        // 카드 생성 초기 위치는 화면 밖
        Vector3 cardSpawnPosition = new Vector3(-100f, 0f, 0f);

        for(int i=0; i<cardCount; i++){
            // TODO : 버린댁과 뽑을댁 모두 비엇을떄 예외처리 필요
            if(prefareDeck.Count == 0){
                while(trashDeck.Count != 0){
                    Card card = trashDeck[0];
                    card.isChargedCard = true; // prefareDeck에 충전용 카드 플래그 변수 true로 설정
                    trashDeck.RemoveAt(0);
                    prefareDeck.Add(card);
                }
            }
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );

            int randomIndex = Random.Range(0, prefareDeck.Count);
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            cardOnHand.card.isChargedCard = false; // prefareDeck에 카드 충전한 이후이기 때문에 충전 플래그 변수 false로 설정
            prefareDeck.RemoveAt(randomIndex);
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            if(cardOnHand.card.baseCard.cardNumber.Contains("G57"))currentIchi++;
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
        }
    }

    [Server]
    public void GenerateCardOnHand(Card card,int count)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        Vector3 cardSpawnPosition = new Vector3(0f, -100f, 0f);
        for(int i = 0 ; i < count ; i ++)
        {
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = card;
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
        }
    }

    // 뽑을 덱에서 랜덤으로 카드 뽑아 addtionDrawCards에 추가
    [Server]
    public void AddDrawCard(int cardCount)
    {
        for(int i=0; i<cardCount; i++){
            int randomIndex = Random.Range(0, prefareDeck.Count);
            addtionDrawCards.Add(prefareDeck[randomIndex]);
            prefareDeck.RemoveAt(randomIndex);
        }
        TargetDrawPopUpShow(); // 카드 사용한 유저에게 추가 드로우 팝업 호출 이벤트 전송
    }

    // ---------------------------------------------------------------------- Command Method ----------------------------------------------------------------//

    // deck에 추가
    [Command]
    public void CmdAddDeck(Card card)
    {
        deck.Add(card);
    }

    // deck에서 선택한 카드의 guid와 동일한 카드정보 조회하여 강화 카드로 교체 -> deck update callback의 OP_SET 호출됨
    [Command]
    public void CmdEnhanceDeck(string guid)
    {
        int index = deck.FindIndex(card => card.guid == guid);
        if(index != -1){
            Card card = deck[index].CardDeepCopy(false);
            card.isEnhanced = true;
            deck[index] = card;
        }
    }

    // deck에서 선택한 카드의 guid와 동일한 카드정보 조회하여 제거 -> deck update callback의 OP_REMOVEAT 호출됨
    [Command]
    public void CmdRemoveDeck(string guid)
    {
        int index = deck.FindIndex(card => card.guid == guid);
        if(index != -1){
            deck.RemoveAt(index);
        }
    }

    // prefareDeck에 추가
    [Command]
    public void CmdAddPrefareDeck(Card card)
    {
        prefareDeck.Add(card);
    }

    // prefareDeck과 TrashDeck의 모든 데이터 제거
    [Command]
    public void CmdClearPrefareDeckAndTrashDeck()
    {
        foreach(Card card in prefareDeck)
            deck.Add(card.CardDeepCopy(true));
        foreach(Card card in trashDeck)
            deck.Add(card.CardDeepCopy(true));
        prefareDeck.Clear();
        trashDeck.Clear();
    }

    // 전투 시작시 deck -> prefareDeck 으로 Card 데이터를 깊은복사 후 랜덤 셔플 수행
    [Command]
    public void CmdAddPrefareDeckWithShuffle()
    {
        foreach(Card card in deck){
            Card copyCard = card.CardDeepCopy(false);
            prefareDeck.Add(copyCard);
        }
        M_CardManager.instance.Shuffle(prefareDeck);
    }

    // 현재 플레이어의 CardOnHand 오브젝트 생성
    // prefareDeck에서 랜덤으로 가져옴. prefareDeck이 0개일 경우 trashDeck에서 가져온뒤 뽑음
    [Command]
    public void CmdSpawnCardOnHand()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        if(prefareDeck.Count == 0 && trashDeck.Count == 0)
        {
            CmdAddPrefareDeckWithShuffle();
        }
        // 카드 생성 초기 위치는 화면 밖
        Vector3 cardSpawnPosition = new Vector3(-100f, 0f, 0f);

        for(int i=0; i<currentDeckCount; i++){
            // TODO : 버린댁과 뽑을댁 모두 비엇을떄 예외처리 필요
            if(prefareDeck.Count == 0){
                while(trashDeck.Count != 0){
                    Card card = trashDeck[0];
                    card.isChargedCard = true; // prefareDeck에 충전용 카드 플래그 변수 true로 설정
                    trashDeck.RemoveAt(0);
                    prefareDeck.Add(card);
                }
            }
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );

            int randomIndex = Random.Range(0, prefareDeck.Count);
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            cardOnHand.card.isChargedCard = false; // prefareDeck에 카드 충전한 이후이기 때문에 충전 플래그 변수 false로 설정
            prefareDeck.RemoveAt(randomIndex);
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            if(cardOnHand.card.baseCard.cardNumber.Contains("G57"))currentIchi++;
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
        }
    }

    // 추가 드로우 카드들을 생성하여 패로 이동. 인자값인 card는 팝업창에서 선택한 카드(중력 부여할 카드), index는 선택한 카드의 인덱스값
    [Command]
    public void CmdSpawnAddtionDrawCard(Card card, int index)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        List<CardOnHand> cards = new List<CardOnHand>();
        for(int i=0; i<addtionDrawCards.Count; i++){
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                Vector3.zero,
                Quaternion.identity
            );
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.card = addtionDrawCards[i];
            cardOnHand.index = i;
            cardOnHand.isAddtionDrawCard = true;
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>();
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            if(cardOnHand.card.baseCard.cardNumber.Contains("G57"))currentIchi++;
            cardOnHands.Add(cardOnHand);
            if(i != index) cards.Add(cardOnHand);
            else cards.Insert(0,cardOnHand);
        }
        CardData.instance.cardSelectCallBack(this,cards);
        addtionDrawCards.Clear();
    }

    // 추가 드로우된 카드의 isAddtionDrawCard 상태값을 변경(패에 있는 CardOnHand와 추가 드로우된 CardOnHand를 구분하기 위한 상태값)
    [Command]
    public void CmdChangeCardOnHandIsAddtionDraw(CardOnHand cardOnHand, bool isAddtion)
    {
        cardOnHand.isAddtionDrawCard = isAddtion;
    }

    [Command]
    public void CmdSpawnAbilityCard()
    {
        Vector3 cardSpawnPosition = new Vector3(-100f, 0f, 0f);
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        Card abilityCardBase = new Card();
        switch(GetComponent<GamePlayer>().character)
        {
            case Character.HONGDANHYANG :
                abilityCardBase = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals("HA"))); 
                break;
            case Character.ERIS :
                abilityCardBase = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals("HA"))); 
                break;
            case Character.GEORK :
                abilityCardBase = new Card(CardData.instance.cards.Find(c => c.cardNumber.Equals("HA"))); 
                break;
        }
        GameObject abilityCardObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
        );
        NetworkServer.Spawn(abilityCardObject, connectionToClient);
        abilityCardObject.GetComponent<CardOnHand>().card = abilityCardBase;
        abilityCardObject.GetComponent<CardOnHand>().RpcCardOnHandSetParent(gameObject);
        abilityCard = abilityCardObject.GetComponent<CardOnHand>();
    }

   
   
    IEnumerator ReturnToCardOnHandCoroutine(CardOnHand cardOnHand)
    {
        if(cardOnHand != null){
            PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            while(true)
            {
                if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) != -1)
                {
                    cardOnHand.isUsed = false; // 요기 널오류
                    M_CardManager.instance.ResetCardAllState(cardOnHand,false);
                    playerInterface.destroyCards.Remove(cardOnHand);
                    break;
                }
                yield return new WaitForSeconds(0.01f);
            }
        }
    }

    IEnumerator ServerDestroyCardOnHand()
    {
        while(true)
        {
            PlayerInterface playerInterface = GetComponent<GamePlayer>().objectOwner;
            yield return new WaitForSeconds(0.01f);
            for(int i = 0 ;i < destroyCardList.Count ; i++)
            {
                CardOnHand cardOnHand = destroyCardList[i];
                if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) != -1)
                { 
                    playerInterface.RemoveDestroyCardList(cardOnHand);
                    bool isChalna = CardData.instance.CheckCardCharacteristic(cardOnHand.card, CardCharacteristic.CHALNA);
                    if(isChalna){
                        forgottenDeck.Add(cardOnHand.card);
                    }else{
                        trashDeck.Add(cardOnHand.card);
                    }
                    cardOnHands.Remove(cardOnHand);
                    destroyCardList.Remove(cardOnHand);
                    while(true)
                    {
                        if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) == -1)
                            break;
                        yield return new WaitForSeconds(0.01f);
                    }
                    NetworkServer.Destroy(cardOnHand.gameObject);
                }
            }
        }
    }

    // 보상목록 Synclist 데이터에서 netId값이 동일한 첫번째 reward 데이터를 검색해서 제거
    [Command]
    public void CmdRewardRemove(string guid, Reward_Type reward_Type)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        int index = rewards.FindIndex((reward) => reward.guid.Equals(guid) && reward.reward_Type == reward_Type);
        if(index != -1){
            if(reward_Type == Reward_Type.Gold){
                gamePlayer.gold += rewards[index].rewardGold; // 골드 보상인 경우 플레이어 소유 골드에 추가
            }
            rewards.RemoveAt(index);
        }
    }

    // 보상목록 Synclist 요소 모두 제거
    [Command]
    public void CmdRewardClear()
    {
        rewards.Clear();
    }

    // 보상카드 Synclist 요소 모두 제거
    [Command]
    public void CmdClearRewardCards()
    {
        rewardCards.Clear();
    }

    // 카드 제거(버려진 덱으로 보내는 경우)
    [Command]
    public void CmdDestroyCardOnHandToTrash(CardOnHand cardOnHand)
    {
        trashDeck.Add(cardOnHand.card);
        cardOnHands.Remove(cardOnHand);
        NetworkServer.Destroy(cardOnHand.gameObject);
    }

    // 카드 제거(잊혀진 덱으로 보내는 경우)
    [Command]
    public void CmdDestroyCardOnHandToForgotten(CardOnHand cardOnHand)
    {
        forgottenDeck.Add(cardOnHand.card);
        cardOnHands.Remove(cardOnHand);
        NetworkServer.Destroy(cardOnHand.gameObject);
    }

    // 카드 제거(버려진 or 잊혀진 댁으로 보내지 않고 제거만 수행)
    [Command]
    public void CmdDestroyAllCardOnHandWithOutTrashDeck()
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            deck.Add(cardOnHand.card.CardDeepCopy(true));
            NetworkServer.Destroy(cardOnHand.gameObject);
        }
        cardOnHands.Clear();
    }

    // 재조물 카드 콜백 커맨드
    [Command]
    public void Cmd_H26_CallBack(GamePlayer gamePlayer, int choosedCardCount)
    {
        TargetObject targetObject = M_TurnManager.instance.GetCurrentPlayerTargetObject(gamePlayer);
        CardData.instance.H26_CallBack(targetObject, choosedCardCount); // 패 제거 팝업에서 선택한 카드 갯수 넘겨줌
    }

    // 상점 카드 구매 요청 커맨드
    [Command]
    public void CmdPurchaseShopCard(string guid)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        int index = shopCards.FindIndex((c) => c.guid.Equals(guid));
        if(index != -1){
            Card purchasedCard = shopCards[index];
            purchasedCard.isSoldout = true;
            shopCards[index] = purchasedCard;
            gamePlayer.gold -= shopCards[index].cardPrice; // 구매한 플레이어가 소유한 골드에서 카드 가격만큼 감소
        }
    }

    // ------------------------------------------------- Rpc Method ---------------------------------------------------//

    [ClientRpc]
    public void SpawnAbilityCardRPC()
    {
        if(isOwned)CmdSpawnAbilityCard();
    }

    [ClientRpc]
    public void ReturnToCardOnHand(CardOnHand cardOnHand)
    {
        if(isOwned)
            StartCoroutine(ReturnToCardOnHandCoroutine(cardOnHand));
    }
    
    // 전투 보상 데이터 세팅 RPC 수신
    [TargetRpc]
    public void TargetPlayerRewarded(NetworkConnectionToClient target)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        if(!M_TurnManager.instance.playerRewardedDic.ContainsKey(gamePlayer)){ // 키 중복 방지
            M_TurnManager.instance.playerRewardedDic.Add(gamePlayer, false);
        }
    }

    // 드로우 카드 팝업 활성화 RPC 수신
    [TargetRpc]
    public void TargetDrawPopUpShow()
    {
        PopUpUIManager.instance.HandleShowDeckDrawPopUp();
    }

    // 패 카드 제거 팝업 활성화 RPC 수신
    [TargetRpc]
    public void TargetCardOnHandRemovePopUpShow()
    {
        PopUpUIManager.instance.HandleShowCardOnHandRemovePopUp();
    }

    // 잊혀진 덱으로 카드 보내는 RPC 수신
    [TargetRpc]
    public void TargetCardOnHandRemoveToForgotenDeck(CardOnHand cardOnHand)
    {
        M_CardManager.instance.CardOnHandThrowAwaySequenceToForgotenDeck(cardOnHand);
    }

    // -------------------------------------------------SyncVar Hooks ---------------------------------------------------//

    public void OnChangeCurrentDeckCount(int oldCount, int newCount)
    {
        Debug.Log("현재 댁 갯수 변경 :" + newCount);
    }

    public void OnChangeMaxRemoveCardCount(int oldCount, int newCount)
    {
        choosedCardOnHands = new CardOnHand[newCount]; // 패 제거 팝업에서 사용할 배열의 크기 초기화
        if(isOwned){
            // 패 제거 팝업에서 카드들의 위치값을 잡아주는 슬롯오브젝트 생성. 갯수 변경되면 모두 지우고 변경된 갯수만큼 생성
            CardOnHandRemovePopUp cardOnHandRemovePopUp = PopUpUIManager.instance.cardOnHandRemovePopUp.GetComponent<CardOnHandRemovePopUp>();
            foreach(GameObject removeCardSlot in cardOnHandRemovePopUp.removeCardSlots){
                Destroy(removeCardSlot);
            }
            cardOnHandRemovePopUp.removeCardSlots.Clear();
            for(int i=0; i<newCount; i++){
                GameObject removeCardSlot = Instantiate(PopUpUIManager.instance.RemoveCardSlotPrefab);
                removeCardSlot.transform.SetParent(cardOnHandRemovePopUp.gridLayoutGroup.transform);
                removeCardSlot.transform.localPosition = Vector3.zero;
                cardOnHandRemovePopUp.removeCardSlots.Add(removeCardSlot);
            }
        }
    }

    public void PreviousCardTypeChanged(CardType oldVal, CardType newVal)
    {
        foreach(CardOnHand cardOnHand in cardOnHands)
            if(!cardOnHand.isMoving)cardOnHand.CardInfoChangedEvent.Invoke();
    }

    IEnumerator CardOnHandDrawSequenceFromTrashDeckCoroutine(CardOnHand cardOnHand, int index)
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);
            if(true)
            {
                M_CardManager.instance.CardOnHandDrawSequenceFromTrashDeck(cardOnHand, index);
                break;
            }
        }
    }

    public void OnChangedNumberOfUsedCard(int oldVal, int newVal)
    {
        foreach(CardOnHand cardOnHand in cardOnHands)
            cardOnHand.OnChangeCardData(cardOnHand.card,cardOnHand.card);
    }

    // -------------------------------------------------SyncList Callback ---------------------------------------------------//
    
    // Deck Callback
    void OnDeckUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:

                break;
            case SyncList<Card>.Operation.OP_INSERT:
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                if(PopUpUIManager.instance.isCardRemovePopUpOpen){
                    CardRemovePopUp cardRemovePopUp = PopUpUIManager.instance.cardRemovePopUp.GetComponent<CardRemovePopUp>();
                    cardRemovePopUp.ClearRemoveableCards();
                    cardRemovePopUp.CreateRemoveableCards();
                }
                break;
            case SyncList<Card>.Operation.OP_SET:
                if(PopUpUIManager.instance.isCardEnhancePopUpOpen){
                    CardEnhancePopUp cardEnhancePopUp = PopUpUIManager.instance.cardEnhancePopUp.GetComponent<CardEnhancePopUp>();
                    cardEnhancePopUp.ClearAllEnhanceableCards();
                    cardEnhancePopUp.CreateEnhanceableCards();
                }
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // CardOnHand Callback
    void OnCardOnHandsUpdated(SyncList<CardOnHand>.Operation op, int index, CardOnHand oldCardOnHand, CardOnHand newCardOnHand)
    {
        switch (op)
        {
            case SyncList<CardOnHand>.Operation.OP_ADD:
                if(newCardOnHand.isAddtionDrawCard){
                    M_CardManager.instance.CardOnHandAdditionDrawSequence(newCardOnHand, index);
                }else{
                    if(newCardOnHand.transform.position.x < 0){
                        M_CardManager.instance.CardOnHandDrawSequence(newCardOnHand, index);
                    }else{
                        StartCoroutine(CardOnHandDrawSequenceFromTrashDeckCoroutine(newCardOnHand, index));
                    }    
                }
                if(newCardOnHand.card.baseCard.cardType == CardType.CURSE)gainCurseCardCount++;
                break;
            case SyncList<CardOnHand>.Operation.OP_INSERT:
                
                break;
            case SyncList<CardOnHand>.Operation.OP_REMOVEAT:

                break;
            case SyncList<CardOnHand>.Operation.OP_SET:
                
                break;
            case SyncList<CardOnHand>.Operation.OP_CLEAR:
                
                break;
        }
        M_CardManager.instance.RefreshCardOnHandsSortingOrder(cardOnHands); // CardOnHand 리스트 값이 변경될 때 마다 CardOnHand의 정렬값 재설정
    }

    // 뽑을 덱 리스트 콜백
    void OnPrefareDeckUpdated(SyncList<Card>.Operation op, int index, Card oldPrefareDeck, Card newPrefareDeck)
    {
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(newPrefareDeck.baseCard.cardType == CardType.CURSE){
                    gainCurseCardCount++;
                }
                // 현재 플레이하는 캐릭터의 카드 충전 이펙트 실행
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId && newPrefareDeck.isChargedCard){
                    Vector3 startPosition = GameUIManager.instance.buttonTrashDeck.transform.position;
                    Vector3 endPosition = GameUIManager.instance.buttonPrefareDeck.transform.position;
                    M_CardManager.instance.CardOnHandChargedSequence(newPrefareDeck, index, startPosition, endPosition);
                }
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    int count = prefareDeck.Count;
                    prefareDeckCountDelay++;
                    GameUIManager.instance.textPrefareDeckCount.transform.DOScale(1.2f, prefareDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonPrefareDeck);
                        GameUIManager.instance.textPrefareDeckCount.text = count.ToString();
                        prefareDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    int count = prefareDeck.Count;
                    prefareDeckCountDelay++;
                    GameUIManager.instance.textPrefareDeckCount.transform.DOScale(1.2f, prefareDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.textPrefareDeckCount.text = count.ToString();
                        prefareDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_SET:
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){        
                    GameUIManager.instance.textPrefareDeckCount.text = "0";
                }
                break;
        }
    }

    // 버린 덱 리스트 콜백
    void OnTrashDeckUpdated(SyncList<Card>.Operation op, int index, Card oldTrashDeck, Card newTrashDeck)
    {
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = trashDeck.Count;
                    trashDeckCountDelay++;
                    GameUIManager.instance.textTrashDeckCount.transform.DOScale(1.2f, trashDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonTrashDeck);
                        GameUIManager.instance.textTrashDeckCount.text = count.ToString();
                        trashDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                 if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = trashDeck.Count;
                    trashDeckCountDelay++;
                    GameUIManager.instance.textTrashDeckCount.transform.DOScale(1.2f, trashDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.textTrashDeckCount.text = count.ToString();
                        trashDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_SET:
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    GameUIManager.instance.textTrashDeckCount.text = "0";
                }
                break;
        }
    }

    // 잊혀진 덱 리스트 콜백
    void OnForgottenDeckUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = forgottenDeck.Count;
                    forgottenDeckCountDelay++;
                    GameUIManager.instance.textForgottenDeckCount.transform.DOScale(1.2f, forgottenDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.DeckButtonScaleAnimation(GameUIManager.instance.buttonForgottenDeck);
                        GameUIManager.instance.textForgottenDeckCount.text = count.ToString();
                        forgottenDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:
                if(isOwned && currentGamePlayerNetId == GetComponent<GamePlayer>().netId){
                    int count = forgottenDeck.Count;
                    forgottenDeckCountDelay++;
                    GameUIManager.instance.textForgottenDeckCount.transform.DOScale(1.2f, forgottenDeckCountDelay * duration).OnComplete(() => {
                        GameUIManager.instance.textForgottenDeckCount.text = count.ToString();
                        forgottenDeckCountDelay--;
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_SET:
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
                    GameUIManager.instance.textForgottenDeckCount.text = "0";
                }
                break;
        }
    }

    // 전체 보상 리스트 콜백
    void OnRewardUpdated(SyncList<Reward>.Operation op, int index, Reward oldVal, Reward newVal)
    {
        switch (op)
        {
            case SyncList<Reward>.Operation.OP_ADD:
                BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
                GamePlayer gamePlayer = GetComponent<GamePlayer>();
                int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);          
                GameObject rewardListItemObject = Instantiate(PopUpUIManager.instance.RewardListItemPrefab);
                RewardListItem rewardListItem = rewardListItemObject.GetComponent<RewardListItem>();
                rewardListItem.reward = newVal;
                rewardListItem.rewardOwner = gamePlayer;
                rewardListItem.transform.SetParent(battleResultPopUp.rewardLayoutGroups[orderIndex].transform);
                rewardListItem.transform.localScale = new Vector3(1, 1, 1);
                M_TurnManager.instance.rewardObjects.Add(rewardListItemObject);
                break;
            case SyncList<Reward>.Operation.OP_INSERT:
                
                break;
            case SyncList<Reward>.Operation.OP_REMOVEAT:
                if(isOwned && rewards.Count <= 0){
                    // 더 보상받을 데이터 없는 경우 보상완료상태 세팅
                    M_TurnManager.instance.playerRewardedDic[GetComponent<GamePlayer>()] = true;
                    M_TurnManager.instance.CheckAllPlayerRewarded(GetComponent<GamePlayer>());
                }
                break;
            case SyncList<Reward>.Operation.OP_SET:
                
                break;
            case SyncList<Reward>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // 보상 카드 리스트 콜백
    void OnRewardCardUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
                GamePlayer gamePlayer = GetComponent<GamePlayer>();

                // 보상 카드 오브젝트 생성
                GameObject cardOnDeck = Instantiate(PopUpUIManager.instance.CardOnDeckPrefab);
                cardOnDeck.GetComponent<CardOnDeck>().card = newVal;
                cardOnDeck.GetComponent<CardOnDeck>().cardOwner = gamePlayer;
                
                int orderIndex = M_TurnManager.instance.playerOrder.FindIndex((netId) => netId == gamePlayer.netId);
                if(orderIndex != -1){
                    cardOnDeck.transform.SetParent(battleResultPopUp.rewardCardLayoutGroups[orderIndex].transform);
                    cardOnDeck.transform.localScale = new Vector3(1, 1, 1);
                    battleResultPopUp.SetTabButtonIconByClass(gamePlayer.character, orderIndex);
                    if(isOwned){
                        battleResultPopUp.ChangeTab(orderIndex);
                        battleResultPopUp.tabButtons[orderIndex].gameObject.SetActive(isOwned);
                    }
                }
                M_TurnManager.instance.rewardCardObjects.Add(cardOnDeck);
                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:

                break;
            case SyncList<Card>.Operation.OP_SET:
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // 추가 드로우 카드 리스트 콜백
    void OnAddtionCardUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:
                if(isOwned){
                    DeckDrawPopUp deckDrawPopUp = PopUpUIManager.instance.deckDrawPopUp.GetComponent<DeckDrawPopUp>();
                    GameObject cardOnDeckObject = Instantiate(
                        PopUpUIManager.instance.CardOnDeckPrefab,
                        GameUIManager.instance.buttonPrefareDeck.transform.position,
                        Quaternion.identity,
                        PopUpUIManager.instance.deckDrawPopUp.transform
                    );
                    CardOnDeck cardOnDeck =  cardOnDeckObject.GetComponent<CardOnDeck>();
                    cardOnDeck.card = newVal;
                    deckDrawPopUp.addtionDrawCardObjects.Add(cardOnDeckObject);
                    cardOnDeck.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    
                    Sequence sequence = DOTween.Sequence();
                    sequence.PrependCallback(() => { 
                        GameObject cardPosition = Instantiate(
                            PopUpUIManager.instance.AddtionDrawCardSlotPrefab,
                            Vector3.zero,
                            Quaternion.identity,
                            deckDrawPopUp.gridLayoutGroup.transform
                        );
                        deckDrawPopUp.addtionDrawCardSlots.Add(cardPosition);
                    });
                    sequence.InsertCallback(0.1f, () => {
                        Sequence cardSequence = DOTween.Sequence();
                        cardSequence.Append(cardOnDeck.transform.DOMove(deckDrawPopUp.addtionDrawCardSlots[index].transform.position, 0.5f).SetEase(Ease.InOutCirc).SetDelay(index * 0.1f));
                        cardSequence.Join(cardOnDeck.transform.DOScale(Vector3.one, 0.5f)).OnComplete(() => { cardSequence.Kill(); });
                    });
                    sequence.OnComplete(() => {
                        sequence.Kill();
                    });
                }
                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:

                break;
            case SyncList<Card>.Operation.OP_SET:
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                
                break;
        }
    }

    // 상점 카드 리스트 콜백
    void OnShopCardUpdated(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
    {
        switch (op)
        {
            case SyncList<Card>.Operation.OP_ADD:

                break;
            case SyncList<Card>.Operation.OP_INSERT:
                
                break;
            case SyncList<Card>.Operation.OP_REMOVEAT:

                break;
            case SyncList<Card>.Operation.OP_SET:
                MercuriusPopUp mercuriusPopUp = PopUpUIManager.instance.mercuriusPopUp.GetComponent<MercuriusPopUp>();
                if(PopUpUIManager.instance.isMercuriusPopUpOpen){
                    mercuriusPopUp.RemoveShopCards();
                    mercuriusPopUp.CreateShopCards();
                }
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                
                break;
        }
    }
}
