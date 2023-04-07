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
    public CardPocket currentPlayerCardPocket; // 현재 플레이어의 CardPocket

    [SyncVar]
    public bool isArrowSpawned = false; // 화살표는 한개만 생성되어야하므로 이미 생성되어 있는지 체크용 변수

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
                        Card attackCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Attack"));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Defense"));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.ERIS:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Attack"));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Heal"));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            case Character.HONGDANHYANG:
                for(int i = 0 ; i <8 ;i++)
                {
                    if(i % 2 == 0){
                        Card attackCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Attack"));
                        deck.Add(attackCard);
                        prefareDeck.Add(attackCard);
                    }else{
                        Card defenseCard = CardData.cards.Find(c => c.character.Equals(character) && c.name.Equals("Normal_Defense"));
                        deck.Add(defenseCard);
                        prefareDeck.Add(defenseCard);
                    }
                    
                }
                break;
            default:
                break;
        }
    }

    // 현재 플레이어의 CardOnHand 오브젝트 생성
    // prefareDeck에서 랜덤으로 가져옴. prefareDeck이 0개일 경우 trashDeck에서 가져온뒤 뽑음
    [Command]
    public void CmdSpawnCardOnHand()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
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
            cardOnHand.GetComponent<CardOnHand>().card.isTargetable = i % 2 == 0 ? true : false;
            cardOnHands.Add(cardOnHand.GetComponent<CardOnHand>()); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가

            // prefareDeck에서 랜덤으로 뽑아서 CardOnHand의 카드데이터에 추가
            cardOnHand.GetComponent<CardOnHand>().card = prefareDeck[randomIndex];
            prefareDeck.RemoveAt(randomIndex); 
        }
    }


    // 카드 컨트롤 화살표 인디케이터 생성(네트워크 오브젝트)
    [Command]
    public void CmdSpawnArrowEmitter(Vector3 cardPosition, CardOnHand cardOnHand)
    {
        if(!isArrowSpawned){
            M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            // 화살표 인디케이터 오브젝트 생성
            GameObject cardEmitter = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")));
            NetworkServer.Spawn(cardEmitter, connectionToClient);
            cardEmitter.GetComponent<CardCtrlArrow>().RpcArrowInit(cardPosition);
            cardEmitter.GetComponent<CardCtrlArrow>().arrowOwnedCardOnHand = cardOnHand; // 화살표를 소환한 카드를 화살표 주인으로 설정

            // 화살표 인디케이터 몸체 생성
            for(int i=0; i<arrowNodeNum; i++){
                GameObject arrowNode = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowNode")));
                NetworkServer.Spawn(arrowNode, connectionToClient);
                cardEmitter.GetComponent<CardCtrlArrow>().RpcSetArrowNode(arrowNode);
            }

            // 화살표 인디케이터 머리 생성
            GameObject arrowHead = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowHead")));
            NetworkServer.Spawn(arrowHead, connectionToClient);
            cardEmitter.GetComponent<CardCtrlArrow>().RpcSetArrowHead(arrowHead);

            isArrowSpawned = true;
        }
    }

    // 카드 컨트롤 화살표 인디케이터 제거(네트워크 오브젝트)
    [Command]
    public void CmdDestroyArrowEmitter(GameObject cardEmitter)
    {
        NetworkServer.Destroy(cardEmitter);
        isArrowSpawned = false;
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

    // 화살표의 타겟에게 액션 수행
    [Command]
    public void CmdActionToTarget(TargetObject targetObject, CardOnHand cardOnHand)
    {
        targetObject.TakeDamage(targetObject, cardOnHand.card.effectValue);
    }

    // 모든 타겟에게 액션 수행
    [Command]
    public void CmdActionToAllTarget(CardOnHand cardOnHand)
    {
        foreach(TargetObject targetObject in FindObjectsOfType<TargetObject>()){
            targetObject.TakeDamage(targetObject, cardOnHand.card.effectValue); 
        }
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
                newCardOnHand.CardOnHandDrawSequence(newCardOnHand);
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
    }
}
