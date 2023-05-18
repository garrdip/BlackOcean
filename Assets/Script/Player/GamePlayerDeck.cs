using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using DG.Tweening;


public class GamePlayerDeck : NetworkBehaviour
{

    [SyncVar (hook = nameof(OnChangeCurrentDeckCount))]
    public int currentDeckCount = 0; // 현재 플레이어의 카드 카운트

    [SyncVar]
    public CardPocket cardPocket; // 현재 플레이어 소유의 카드 포켓 오브젝트

    [SyncVar]
    public CardCtrlArrow cardCtrlArrow; // 현재 소환된 카드 화살표

    public const int arrowNodeNum = 13; // 카드 컨트롤 화살표 몸통 개수

    public const int defaultCardOnHandCount = 10; // 카드 오브젝트 기본 개수

    public const int maxCardOnHandCount = 12; // 카드 오브젝트 최대 개수

    public readonly SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 카드 총량(시작 8개)

    public readonly  SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 카드(카드 총량에서 내 손에 있는 카드(5개)를 제외한 그 나머지 개수)
    
    public readonly SyncList<Card> trashDeck = new SyncList<Card>(); // 버릴 카드(사용된 카드 + 턴 종료될때 내 손에 있는 카드)

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 실제 컨트롤 하는 플레이어 소유의 카드 네트워크 오브젝트 리스트


    
    public override void OnStartServer()
    {
        SetInitialValue();
    }

    public override void OnStartClient()
    {
        cardOnHands.Callback += OnCardOnHandsUpdated;
        prefareDeck.Callback += OnPrefareDeckUpdated;
        trashDeck.Callback += OnTrashDeckUpdated;
    }

    // 플레이어 댁 정보 초기화
    public void SetInitialValue()
    {
        currentDeckCount = 5;
        Character character = GetComponent<GamePlayer>().character;
        switch(character){
            case Character.GEORK:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = new Card(CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("G_Target")));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = new Card(CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("G_NonTarget")));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.ERIS:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = new Card(CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("E_Target")));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = new Card(CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("E_NonTarget")));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.HONGDANHYANG:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = new Card(CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("H_Target")));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = new Card(CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("H_NonTarget")));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            default:
                break;
        }
    }

    // 현재 플레이어의 CardPocket 오브젝트 생성
    [Command]
    public void CmdSpawnCardPocket()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        // CardPocket 오브젝트 생성
        GameObject cardPocketObject = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardPocket")));
        NetworkServer.Spawn(cardPocketObject, connectionToClient);

        // 플레이어에 자신이 소환한 CardPocket 참조값 설정
        cardPocket = cardPocketObject.GetComponent<CardPocket>();
    }

    // 현재 플레이어의 CardOnHand 오브젝트 생성 (prefareDeck에서 랜덤으로 가져옴. prefareDeck이 0개일 경우 trashDeck에서 가져온뒤 뽑음)
    [Command]
    public void CmdSpawnCardOnHand()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        // CardOnHand 오브젝트 생성
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
            GameObject cardOnHand = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")));
            NetworkServer.Spawn(cardOnHand, connectionToClient);

            cardOnHand.GetComponent<CardOnHand>().index = i;
            cardOnHands.Add(cardOnHand.GetComponent<CardOnHand>()); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가

            // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            cardOnHand.GetComponent<CardOnHand>().card = prefareDeck[randomIndex];
            prefareDeck.RemoveAt(randomIndex); 

            // 소환된 카드들의 부모 오브젝트인 CardPocket참조값 설정
            cardOnHand.GetComponent<CardOnHand>().cardPocket = cardPocket;
        }
    }

    // 카드 컨트롤 화살표 인디케이터 생성(네트워크 오브젝트)
    [Command]
    public void CmdSpawnArrowEmitter()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;

        // 화면 밖에 화살표 생성
        Vector3 arrowSpawnPosition = new Vector3(-20f, 0f, 0f);

        // 화살표 노드들 담을 리스트
        List<GameObject> arrowNodes = new List<GameObject>();
                    
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
            arrowNodes.Add(arrowNode);
        }

        // 화살표 인디케이터 머리 생성
        GameObject arrowHead = Instantiate(
            M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowHead")),
            arrowSpawnPosition,
            Quaternion.identity);
        NetworkServer.Spawn(arrowHead, connectionToClient);
        arrowNodes.Add(arrowHead);

        // 화살표 머리와 몸체가 담긴 노드들을 클라이언트에 전달
        cardEmitter.GetComponent<CardCtrlArrow>().RpcSetArrowParts(arrowNodes);

        // 플레이어에 자신이 소환한 화살표 참조값 설정
        cardCtrlArrow = cardEmitter.GetComponent<CardCtrlArrow>();
    }

    // 화살표 주인 카드 참조값 설정
    [Command]
    public void CmdSetArrowOwnCardOnHand(CardOnHand cardOnHand)
    {
        cardCtrlArrow.arrowOwnedCardOnHand = cardOnHand;
    }

    // 카드 리스트에서 삭제, 댁카운트 감소, 카드 오브젝트 삭제, 사용된 댁에 추가
    [Command]
    public void CmdDestroyCardOnHand(CardOnHand cardOnHand)
    {
        trashDeck.Add(cardOnHand.card);
        cardOnHands.Remove(cardOnHand);
        NetworkServer.Destroy(cardOnHand.gameObject);
    }

    // 손에 든 모든 카드 제거 및 댁카운트 0으로 초기화, 리스트 초기화, 사용된 댁에 추가
    [Command]
    public void CmdDestroyAllCardOnHand()
    {
        foreach(CardOnHand cardOnHand in cardOnHands){
            trashDeck.Add(cardOnHand.card);
            NetworkServer.Destroy(cardOnHand.gameObject);
        }
        cardOnHands.Clear();
    }

    // 카드데이터와 카드의 액션수행 대상을 Dictionary로 key, value 쌍으로 묶어 저장
    [Command]
    public void CmdEnQueueCardTargetPair(Card card, TargetObject targetObjects)
    {
        M_TurnManager.instance.cardTargetPairQueue.Enqueue((card, targetObjects));
    }

    // -------------------------------------------------SyncVar Hooks ---------------------------------------------------//
    public void OnChangeCurrentDeckCount(int oldCount, int newCount)
    {
        Debug.Log("현재 댁 갯수 변경 :" + newCount);
    }

    // -------------------------------------------------SyncList Callback ---------------------------------------------------//
    
    // CardOnHand Callback
    void OnCardOnHandsUpdated(SyncList<CardOnHand>.Operation op, int index, CardOnHand oldCardOnHand, CardOnHand newCardOnHand)
    {
        switch (op)
        {
            case SyncList<CardOnHand>.Operation.OP_ADD:
                M_CardManager.instance.CardOnHandDrawSequence(newCardOnHand, index);
                break;
            case SyncList<CardOnHand>.Operation.OP_INSERT:
                
                break;
            case SyncList<CardOnHand>.Operation.OP_REMOVEAT:
                if(cardOnHands.Count <= 0 && isLocalPlayer){
                    CmdSpawnCardOnHand();  // 테스트용
                }
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
            DeckUI.instance.textPrefareDeckCount.text = prefareDeck.Count.ToString();
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
            DeckUI.instance.textTrashDeckCount.text = trashDeck.Count.ToString();
        }
        // TODO : 관전하려는 플레이어의 TrashDeck Count 표시
    }
}
