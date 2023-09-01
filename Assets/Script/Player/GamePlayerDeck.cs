using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


public partial class GamePlayerDeck : NetworkBehaviour
{

    [SyncVar (hook = nameof(OnChangeCurrentDeckCount))]
    public int currentDeckCount = 0; // 현재 플레이어의 카드 카운트

    [SyncVar]
    public CardPocket cardPocket; // 현재 플레이어 소유의 카드 포켓 오브젝트

    [SyncVar]
    public CardCtrlArrow cardCtrlArrow; // 현재 소환된 카드 화살표

    [SyncVar]
    public AbilityCtrlArrow abilityCtrlArrow; // 현재 소환된 어빌리티 화살표

    public const int arrowNodeNum = 13; // 카드 컨트롤 화살표 몸통 개수

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 댁 총괄 데이터

    public readonly  SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 카드(카드 총량에서 내 손에 있는 카드(5개)를 제외한 그 나머지 개수)
    
    public readonly SyncList<Card> trashDeck = new SyncList<Card>(); // 버릴 카드(사용된 카드 + 턴 종료될때 내 손에 있는 카드)

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 실제 컨트롤 하는 플레이어 소유의 카드 네트워크 오브젝트 리스트

    private int currentIndex = 1; // removeCardOnHands SyncList에서 0번, 1번 인덱스 삽입을 반복하기 위해 사용되는 인덱스 변수

    public CardOnHand[] choosedCardOnHands = new CardOnHand[2];  // CardOnHands 리스트에서 삭제하기 위해 선택된 카드 오브젝트들을 담을 배열

    public Queue<(CardOnHand,TargetObject,NetworkIdentity)> serverCardPredictQueue = new Queue<(CardOnHand, TargetObject, NetworkIdentity)>();// Server에서 Card Queue 관리를 위한 Queue

    [SyncVar(hook = nameof(PreviousCardTypeChanged))]
    public CardType previousCardType;

    public List<CardOnHand> destroyCardList = new List<CardOnHand>();

    [SyncVar]
    public CardOnHand abilityCard;

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
    }

    public override void OnStartLocalPlayer()
    {
        CmdSpawnCardPocket(); // 카드 포켓 생성 서버 요청
        CmdSpawnArrowEmitter(); // 화살표 생성 서버 요청
        CmdSpawnAbilityArrowEmitter();
        InitIchi();
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
        currentDeckCount = 5;
        Character character = GetComponent<GamePlayer>().character;
        switch(character){
            case Character.GEORK:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G3")));
                        deck.Add(attackCard);
                    }else{
                        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("G4")));
                        deck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.ERIS:
                for(int i = 0 ; i <8 ;i++)
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
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H0")));
                        deck.Add(attackCard);
                    }else{
                        Card defenseCard = new Card(CardData.instance.cards.Find(c => c.character.Equals(character) && c.cardNumber.Equals("H3")));
                        deck.Add(defenseCard);
                    }
                }
                break;
            default:
                break;
        }
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
        NetworkIdentity conn;
        int totalCost;

        while(true)
        {
            yield return loopTime; // 0.01s

            if(serverCardPredictQueue.Count == 0) continue; //카드큐가 비어있을경우 스킵 
            
            ( cardOnHand,targetObject,conn) = serverCardPredictQueue.Dequeue(); // Command가 왔기때문에 Dequeue하여 판단

            if(cardOnHand.card.baseCard.cardCharacteristics.Exists(x => x == CardCharacteristic.EUNHASOO)) // 은하수 카드 코스트 계산
            {
                if(cardOnHand.card.baseCard.cardType == previousCardType)
                {
                    totalCost = ( cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition - 1 );
                    if(totalCost < 0)totalCost = 0;
                }
                else
                {
                     totalCost = ( cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition + 1 );
                }
            }
            else
                totalCost = cardOnHand.card.baseCard.cost + cardOnHand.card.costAddition ;
            if(totalCost > currentIchi) // 카드 코스트 계산 하는곳
                continue;

            if(targetObject == null)
            {
                ReturnToCardOnHand(cardOnHand,conn);
                continue;
            }

            if(cardOnHand.card.baseCard.isTargetable && targetObject.objectType != ObjectType.PLAYER && targetObject.clone == null)// Clone이 없을경우 Target 오브젝트는 존재하지 않는것으로 판단 Return 함
            {
                //카드와 이치 다시 돌려보내는곳
                ReturnToCardOnHand(cardOnHand,conn);
                continue;
            }

            currentIchi -= totalCost ;
            
            destroyCardList.Add(cardOnHand);

            // 여기부터 카드사용이 확정 되는곳
            previousCardType = cardOnHand.card.baseCard.cardType;

            List<TargetObject> tar = new List<TargetObject>();
            tar.Add(M_TurnManager.instance.GetClonePlayer(conn)); // Index 0 
            if(cardOnHand.card.baseCard.isTargetable)tar.Add(targetObject.clone);// Index 1 // TargetAble이 아닐경우 Index1은 비워짐
            tar.AddRange(M_TurnManager.instance.GetClonePlayerObjects());
            tar.AddRange(M_TurnManager.instance.GetCloneMonsterObjects());
            M_TurnManager.instance.ProcessCardPredict(cardOnHand.card,tar);
            
            List<TargetObject> targetObjects = new List<TargetObject>();
            targetObjects.Add(M_TurnManager.instance.GetPlayer(conn)); // Index 0 
            if(cardOnHand.card.baseCard.isTargetable)targetObjects.Add(targetObject);// Index 1 // TargetAble이 아닐경우 Index1은 비워짐
            targetObjects.AddRange(M_TurnManager.instance.GetPlayerObjects());
            targetObjects.AddRange(M_TurnManager.instance.GetMonsterObjects());

            M_TurnManager.instance.cardTargetPairQueue.Enqueue((cardOnHand.card, targetObjects));
        }
    }

    // ---------------------------------------------------------------------- Command Method ----------------------------------------------------------------//

    // 현재 플레이어의 CardPocket 오브젝트 생성
    [Command]
    public void CmdSpawnCardPocket()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        // CardPocket 오브젝트 생성
        GameObject cardPocketObject = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardPocket")));
        NetworkServer.Spawn(cardPocketObject, connectionToClient);

        cardPocket = cardPocketObject.GetComponent<CardPocket>();
    }

    // 현재 플레이어의 카드 컨트롤 화살표 인디케이터 생성
    [Command]
    public void CmdSpawnArrowEmitter()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        // 화살표 생성 초기 위치는 화면 밖
        Vector3 arrowSpawnPosition = new Vector3(-100f, 0f, 0f);

        // 화살표 인디케이터 오브젝트 생성
        GameObject cardEmitter = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(cardEmitter, connectionToClient);

        // 화살표 인디케이터 몸체 생성
        for(int i=0; i<arrowNodeNum; i++){
            GameObject arrowNode = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowNode")),
                arrowSpawnPosition,
                Quaternion.identity);
            NetworkServer.Spawn(arrowNode, connectionToClient);
            arrowNode.GetComponent<CardCtrlArrowNode>().cardCtrlArrow = cardEmitter.GetComponent<CardCtrlArrow>(); // 화살표 몸통에 SyncVar로 선언된 부모 오브젝트(화살표) 참조값 설정
        }

        // 화살표 인디케이터 머리 생성
        GameObject arrowHead = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowHead")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(arrowHead, connectionToClient);
        arrowHead.GetComponent<CardCtrlArrowHead>().cardCtrlArrow = cardEmitter.GetComponent<CardCtrlArrow>();  // 화살표 머리에 SyncVar로 선언된 부모 오브젝트(화살표) 참조값 설정
    
        cardCtrlArrow = cardEmitter.GetComponent<CardCtrlArrow>();
    }

    [Command]
    public void CmdGenerateAbilityButton()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        GameObject abilityButton = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityButton")),
            new Vector3(-100,0,0),
            Quaternion.identity);
        NetworkServer.Spawn(abilityButton, connectionToClient);
    }

    [Command]
    public void CmdSpawnAbilityArrowEmitter()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        // 화살표 생성 초기 위치는 화면 밖
        Vector3 arrowSpawnPosition = new Vector3(-100f, 0f, 0f);

        // 화살표 인디케이터 오브젝트 생성
        GameObject abilityEmitter = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowEmitter")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(abilityEmitter, connectionToClient);

        // 화살표 인디케이터 몸체 생성
        for(int i=0; i<arrowNodeNum; i++){
            GameObject arrowNode = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowNode")),
                arrowSpawnPosition,
                Quaternion.identity);
            NetworkServer.Spawn(arrowNode, connectionToClient);
            arrowNode.GetComponent<AbilityCtrlArrowNode>().abilityCtrlArrow = abilityEmitter.GetComponent<AbilityCtrlArrow>(); // 화살표 몸통에 SyncVar로 선언된 부모 오브젝트(화살표) 참조값 설정
        }

        // 화살표 인디케이터 머리 생성
        GameObject arrowHead = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("AbilityArrowHead")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(arrowHead, connectionToClient);
        arrowHead.GetComponent<AbilityCtrlArrowHead>().abilityCtrlArrow = abilityEmitter.GetComponent<AbilityCtrlArrow>();  // 화살표 머리에 SyncVar로 선언된 부모 오브젝트(화살표) 참조값 설정
    
        abilityCtrlArrow = abilityEmitter.GetComponent<AbilityCtrlArrow>();
    }

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
            int randomIndex = Random.Range(0, prefareDeck.Count);
            GameObject cardOnHandObject = Instantiate(
                M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")),
                cardSpawnPosition,
                Quaternion.identity
            );

            CardOnHand cardOnHand = cardOnHandObject.GetComponent<CardOnHand>();
            cardOnHand.index = i; // 카드 인덱스
            cardOnHand.card = prefareDeck[randomIndex]; // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            prefareDeck.RemoveAt(randomIndex); 
            cardOnHands.Add(cardOnHand); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
            if(cardPocket != null){
                cardOnHand.parent = cardPocket.GetComponent<CardPocket>(); // 소환된 CardOnHand를 CardPocket의 자식오브젝트로 설정
            }
            NetworkServer.Spawn(cardOnHandObject, connectionToClient);
        }
    }

    [ClientRpc]
    public void SpawnAbilityCardRPC()
    {
        if(isOwned)CmdSpawnAbilityCard();
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

    [ClientRpc]
    public void ReturnToCardOnHand(CardOnHand cardOnHand,NetworkIdentity conn)
    {
        if(conn == NetworkClient.connection.identity)
        {
           StartCoroutine(ReturnToCardOnHandCoroutine(cardOnHand));
        }
    }
   
    IEnumerator ReturnToCardOnHandCoroutine(CardOnHand cardOnHand)
    {
        GamePlayer gamePlayer = GetComponent<GamePlayer>();
        while(true)
        {
            if(gamePlayer.destroyCards.FindIndex(x => x == cardOnHand) != -1)
            {
                cardOnHand.isUsed = false;
                M_CardManager.instance.ResetCardAllState(cardOnHand,false);
                cardOnHand.GetComponent<SpriteRenderer>().color = new Color(1,1,1,1);
                GetComponent<GamePlayer>().destroyCards.Remove(cardOnHand);
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
            yield return new WaitForSeconds(0.01f);
            for(int i = 0 ;i < destroyCardList.Count ; i++)
            {
                CardOnHand cardOnHand = destroyCardList[i];
                if(GetComponent<GamePlayer>().destroyCards.FindIndex(x => x == cardOnHand) != -1)
                { 
                    GetComponent<GamePlayer>().RemoveDestroyCardList(cardOnHand);
                    trashDeck.Add(cardOnHand.card);
                    cardOnHands.Remove(cardOnHand);
                    destroyCardList.Remove(cardOnHand);
                    while(true)
                    {
                        if(GetComponent<GamePlayer>().destroyCards.FindIndex(x => x == cardOnHand) == -1)
                            break;
                        yield return new WaitForSeconds(0.01f);
                    }
                    NetworkServer.Destroy(cardOnHand.gameObject);
                }
            }
        }
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
        // 로컬플레이어의 PrefareDeck Count 표시
        if(isLocalPlayer){
            GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textPrefareDeckCount, prefareDeck.Count);
        }
        // TODO : 관전하려는 플레이어의 PrefareDeck Count 표시
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
        // 로컬플레이어의 TrashDeck Count 표시
        if(isLocalPlayer){
            GameUIManager.instance.DeckCountTextScaleAnimation(GameUIManager.instance.textTrashDeckCount, trashDeck.Count);
        }
        // TODO : 관전하려는 플레이어의 TrashDeck Count 표시
    }
}
