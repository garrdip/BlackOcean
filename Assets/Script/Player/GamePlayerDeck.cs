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

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 댁 총괄 데이터

    public readonly SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 카드(카드 총량에서 내 손에 있는 카드(5개)를 제외한 그 나머지 개수)
    
    public readonly SyncList<Card> trashDeck = new SyncList<Card>(); // 버릴 카드(사용된 카드 + 턴 종료될때 내 손에 있는 카드)

    public readonly SyncList<Card> forgottenDeck = new SyncList<Card>(); // 잊혀진 덱 찰나로 보내진 덱

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 실제 컨트롤 하는 플레이어 소유의 카드 네트워크 오브젝트 리스트

    public readonly SyncList<Card> rewardCards = new SyncList<Card>(); // 전투 보상 카드

    public readonly SyncList<Card> addtionDrawCards = new SyncList<Card>(); // 추가 드로우 카드

    private int currentIndex = 1; // removeCardOnHands SyncList에서 0번, 1번 인덱스 삽입을 반복하기 위해 사용되는 인덱스 변수

    public CardOnHand[] choosedCardOnHands = new CardOnHand[2];  // CardOnHands 리스트에서 삭제하기 위해 선택된 카드 오브젝트들을 담을 배열

    public Queue<(CardOnHand,TargetObject)> serverCardPredictQueue = new Queue<(CardOnHand, TargetObject)>();// Server에서 Card Queue 관리를 위한 Queue

    [SyncVar(hook = nameof(PreviousCardTypeChanged))]
    public CardType previousCardType;

    public List<CardOnHand> destroyCardList = new List<CardOnHand>();

    public int numOfUsedIronTeeth = 0;

    [SyncVar]
    public int AdditionalSizeOfIromDemon;

    public override void OnStartServer()
    {
        SetInitialValue();
        StartCoroutine(EnQueueCardTargetPair());
        StartCoroutine(ServerDestroyCardOnHand());
    }

    public override void OnStartClient()
    {
        cardOnHands.Callback += OnCardOnHandsUpdated;
        prefareDeck.Callback += OnPrefareDeckUpdated;
        trashDeck.Callback += OnTrashDeckUpdated;
        addtionDrawCards.Callback += OnAddtionCardUpdate;
        if(isOwned){
            GameUIManager.instance.currentIchiText.text = currentIchi.ToString(); // 현재 이치값 초기 뷰 세팅
            GameUIManager.instance.maxIchiText.text = maxIchi.ToString(); // 최대 이치값 초기 뷰 세팅
        }  
    }

    // choosedCardOnHands 배열에 선택한 카드를 추가
    public void AddChoosedCardOnHands(CardOnHand cardOnHand)
    {
        cardOnHand.isChoosed = true;
        // 인덱스 0, 1 반복
        if(choosedCardOnHands[0] == null){
            currentIndex = 0;
        }else{
            if(choosedCardOnHands[1] == null){
                currentIndex = 1;
            }else{
                currentIndex = (1 - currentIndex);
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
        if(choosedCardOnHands[0] == cardOnHand){
            choosedCardOnHands[0] = null;
        }else{
            choosedCardOnHands[1] = null;
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
            Character character = GetComponent<GamePlayer>().character;
            switch(character){
                case Character.GEORK:
                    for(int i = 0; i < 8; i++)
                    {
                        if(i % 2 == 0){
                            Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G3")));
                            deck.Add(attackCard);
                        }else{
                            Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G4")));
                            deck.Add(defenseCard);
                        }   
                    }
                    Card transformCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("GX")));
                    deck.Add(transformCard);
                    break;
                case Character.ERIS:
                    for(int i = 0; i < 8; i++)
                    {
                        if(i % 2 == 0){
                            Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("E0")));
                            deck.Add(attackCard);
                        }else{
                            Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("E1")));
                            deck.Add(defenseCard);
                        }
                    }
                    break;
                case Character.HONGDANHYANG:
                    for(int i = 0; i < 8; i++)
                    {
                        //Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H"+(i+2))));
                        //deck.Add(attackCard);
                        if(i % 2 == 0){
                            Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H43")));
                            deck.Add(attackCard);
                        }else{
                            Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H47")));
                            deck.Add(defenseCard);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
    
    public int GetTotalCostOfCardOnHand(CardOnHand cardOnHand)
    {
        int totalCost;
        if(cardOnHand.card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
        {
            if(cardOnHand.card.baseCard.cardType == previousCardType)
            {
                totalCost = ( cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition - 1 );
                if(totalCost < 0)totalCost = 0;
            }
            else
                totalCost = ( cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition + 1 );
        }
        else
            totalCost = cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition;
        return totalCost;
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

            M_TurnManager.instance.cardTargetPairQueue.Enqueue((this, totalCost, cardOnHand, targetObjects));
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
            cardOnHand.index = i; // 카드 인덱스
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            prefareDeck.RemoveAt(randomIndex);
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);

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
        deck.Clear();
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
            cardOnHand.index = i; // 카드 인덱스
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            prefareDeck.RemoveAt(randomIndex);
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);

            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
        }
    }

    // 추가 드로우 카드들을 생성하여 패로 이동. 인자값인 card는 팝업창에서 선택한 카드(중력 부여할 카드), index는 선택한 카드의 인덱스값
    [Command]
    public void CmdSpawnAddtionDrawCard(Card card, int index)
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        for(int i=0; i<addtionDrawCards.Count; i++){
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                Vector3.zero,
                Quaternion.identity
            );
            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            if(index == i){
                // TODO : 선택된 카드에 중력 부여
            }
            cardOnHand.card = addtionDrawCards[i];
            cardOnHand.isAddtionDrawCard = true;
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>();
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
            cardOnHands.Add(cardOnHand);
        }
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
        PlayerInterface playerInterface = NetworkClient.localPlayer.GetComponent<PlayerInterface>();
        while(true)
        {
            if(playerInterface.destroyCards.FindIndex(x => x == cardOnHand) != -1)
            {
                cardOnHand.isUsed = false;
                M_CardManager.instance.ResetCardAllState(cardOnHand,false);
                playerInterface.destroyCards.Remove(cardOnHand);
                break;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    // 카드 리스트에서 삭제, 댁카운트 감소, 카드 오브젝트 삭제, 사용된 댁에 추가
    [Command]
    public void CmdDestroyCardOnHand(CardOnHand cardOnHand)
    {
        trashDeck.Add(cardOnHand.card);
        cardOnHands.Remove(cardOnHand);
        NetworkServer.Destroy(cardOnHand.gameObject);
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
                    trashDeck.Add(cardOnHand.card);
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

    // 보상카드 Synclist 요소 모두 제거
    [Command]
    public void CmdClearRewardCards()
    {
        rewardCards.Clear();
    }

    // 플레이어의  손에 든 모든 카드 제거 및 댁카운트 0으로 초기화, 리스트 초기화, 사용된 댁에 추가
    [Command]
    public void CmdDestroyAllCardOnHand()
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            trashDeck.Add(cardOnHand.card);
            NetworkServer.Destroy(cardOnHand.gameObject);
        }
        cardOnHands.Clear();
    }

    // 플레이어의 손에 든 모든 카드 제거(사용된 댁으로 보내지 않고 제거만 수행)
    [Command]
    public void CmdDestroyAllCardOnHandWithOutTrashDeck()
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            deck.Add(cardOnHand.card.CardDeepCopy(true));
            NetworkServer.Destroy(cardOnHand.gameObject);
        }
        cardOnHands.Clear();
    }

    // 화살표 주인 카드 참조값 설정
    public void CmdSetArrowOwnCardOnHand(CardOnHand cardOnHand)
    {
        cardCtrlArrow.arrowOwnedCardOnHand = cardOnHand;
    }

    // 플레이어에 소유의 CardPocket 참조값 설정
    [Command]
    public void CmdSetPlayerOwnCardPocket(CardPocket cardPocket)
    {
        this.cardPocket = cardPocket;
    }

    // 플레이어에 소유의 CardCtrlArrow 참조값 설정
    [Command]
    public void CmdSetPlayerOwnCardCtrlArrow(CardCtrlArrow cardCtrlArrow)
    {
        this.cardCtrlArrow = cardCtrlArrow;
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
    
    [TargetRpc]
    public void TargetSetBattleRewardCard(NetworkConnectionToClient target, List<Card> rewardCards)
    {
        BattleResultPopUp battleResultPopUp = PopUpUIManager.instance.battleResultPopUp.GetComponent<BattleResultPopUp>();
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        if(!battleResultPopUp.playerRewardCardsDic.ContainsKey(gamePlayer) && !battleResultPopUp.playerRewardedDic.ContainsKey(gamePlayer)){ // 키 중복 방지
            battleResultPopUp.playerRewardCardsDic.Add(GetComponent<GamePlayer>(), rewardCards);
            battleResultPopUp.playerRewardedDic.Add(gamePlayer, false);
        }
    }

    [TargetRpc]
    public void TargetDrawPopUpShow()
    {
        PopUpUIManager.instance.HandleShowDeckDrawPopUp(); // 드로우 카드 팝업 활성화
    }

    // -------------------------------------------------SyncVar Hooks ---------------------------------------------------//
    
    public void OnChangeCurrentDeckCount(int oldCount, int newCount)
    {
        Debug.Log("현재 댁 갯수 변경 :" + newCount);
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

    // -------------------------------------------------SyncList Callback ---------------------------------------------------//
    
    // CardOnHand Callback
    void OnCardOnHandsUpdated(SyncList<CardOnHand>.Operation op, int index, CardOnHand oldCardOnHand, CardOnHand newCardOnHand)
    {
        switch (op)
        {
            case SyncList<CardOnHand>.Operation.OP_ADD:
                if(newCardOnHand.transform.position.x < 0)
                    M_CardManager.instance.CardOnHandDrawSequence(newCardOnHand, index);
                else
                    StartCoroutine(CardOnHandDrawSequenceFromTrashDeckCoroutine(newCardOnHand, index));
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
    }

    // PrefareDeck Callback
    void OnPrefareDeckUpdated(SyncList<Card>.Operation op, int index, Card oldPrefareDeck, Card newPrefareDeck)
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
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                
                break;
        }
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
            GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textPrefareDeckCount, prefareDeck.Count); // 현재 플레이어의 PrefareDeck Count 표시
        }
    }

    // TrashDeck Callback
    void OnTrashDeckUpdated(SyncList<Card>.Operation op, int index, Card oldTrashDeck, Card newTrashDeck)
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
                
                break;
            case SyncList<Card>.Operation.OP_CLEAR:
                
                break;
        }
        uint currentGamePlayerNetId = NetworkClient.localPlayer.GetComponent<PlayerInterface>().currentGamePlayerNetId;
        if(GetComponent<GamePlayer>().netId == currentGamePlayerNetId){
            GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textTrashDeckCount, trashDeck.Count); // 현재 플레이어의 TrashDeck Count 표시
        }
    }

    // 추가 드로우 카드 리스트 콜백
    void OnAddtionCardUpdate(SyncList<Card>.Operation op, int index, Card oldVal, Card newVal)
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
                    sequence.Join(cardOnDeck.transform.DOMove(deckDrawPopUp.addtionDrawCardPositions[index].transform.position, 0.5f).SetEase(Ease.InOutCirc).SetDelay(index * 0.1f));
                    sequence.Join(cardOnDeck.transform.DOScale(Vector3.one, 0.5f));
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
}
