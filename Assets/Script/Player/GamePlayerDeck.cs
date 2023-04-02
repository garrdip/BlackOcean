using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;


public class GamePlayerDeck : NetworkBehaviour
{

    [SyncVar (hook = nameof(OnChangeCurrentDeckCount))]
    public int currentDeckCount = 0; // 현재 플레이어의 카드 카운트

    [SyncVar]
    public bool isArrowSpawned = false; // 화살표는 한개만 생성되어야하므로 이미 생성되어 있는지 체크용 변수

    public const int arrowNodeNum = 13; // 카드 컨트롤 화살표 몸통 개수

    public const int defaultCardOnHandCount = 10; // 카드 오브젝트 기본 개수

    public const int maxCardOnHandCount = 12; // 카드 오브젝트 최대 개수

    public readonly SyncList<Artifact> artifacts = new SyncList<Artifact>();

    public readonly SyncList<Card> deck =  new SyncList<Card>(); // 카드 총량(시작 8개)

    public readonly  SyncList<Card> prefareDeck =  new SyncList<Card>(); // 뽑을 카드(카드 총량에서 내 손에 있는 카드(5개)를 제외한 그 나머지 개수)
    
    public readonly SyncList<Item> trashDeck = new SyncList<Item>(); // 버릴 카드(사용된 카드 + 턴 종료될때 내 손에 있는 카드)

    public readonly SyncList<CardOnHand> cardOnHands = new SyncList<CardOnHand>(); // 실제 컨트롤 하는 플레이어 소유의 카드 네트워크 오브젝트 리스트


    
    public override void OnStartServer()
    {
        SetInitialValue();
    }

    public override void OnStartClient()
    {
        cardOnHands.Callback += OnCardOnHandsUpdated;
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

    // 카드 생성 요청
    [Command]
    public void CmdSpawnCardOnHand()
    {
        M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        
        // CardPocket 생성
        GameObject cardPocket = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardPocket")));
        NetworkServer.Spawn(cardPocket, connectionToClient);

        // CardOnHand 생성
        for(int i=0; i<currentDeckCount; i++){
            GameObject cardOnHand = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("CardOnHand")));
            NetworkServer.Spawn(cardOnHand, connectionToClient);
            cardOnHand.GetComponent<CardOnHand>().index = i;
            cardOnHand.GetComponent<CardOnHand>().card = prefareDeck[i];
            cardOnHand.GetComponent<CardOnHand>().card.isTargetable = i % 2 == 0 ? true : false;
            cardOnHands.Add(cardOnHand.GetComponent<CardOnHand>()); // 카드가 생성되면 자신의 권한을 가진 카드 오브젝트들 syncList에 추가
            
            RpcSpawnCardOnHand(
                cardOnHand.GetComponent<CardOnHand>(),
                cardPocket.GetComponent<CardPocket>()
            );
        }
    }

    // 카드가 생성되면 CardOnHand오브젝트를 CardPocket을 부모오브젝트로 설정
    [ClientRpc]
    public void RpcSpawnCardOnHand(CardOnHand cardOnHand, CardPocket cardPocket)
    {
        cardOnHand.gameObject.transform.SetParent(cardPocket.transform);
    }

    // 카드 컨트롤 화살표 인디케이터 생성(네트워크 오브젝트)
    [Command]
    public void CmdSpawnArrowEmitter(Vector3 cardPosition)
    {
        if(!isArrowSpawned){
            M_NetworkRoomManager M_NetworkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
            // 화살표 인디케이터 오브젝트 생성
            GameObject cardEmitter = Instantiate(M_NetworkRoomManager.spawnPrefabs.Find(prefab => prefab.name.Equals("ArrowEmitter")));
            NetworkServer.Spawn(cardEmitter, connectionToClient);
            cardEmitter.GetComponent<CardCtrlArrow>().RpcArrowInit(cardPosition);

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

    // -------------------------------------------------SyncVar Hooks ---------------------------------------------------//
    public void OnChangeCurrentDeckCount(int oldCount, int newCount)
    {
        Debug.Log("현재 댁 갯수 변경 :" + newCount);
    }

    // -------------------------------------------------SyncList Callback ---------------------------------------------------//
    void OnCardOnHandsUpdated(SyncList<CardOnHand>.Operation op, int index, CardOnHand oldCardOnHand, CardOnHand newCardOnHand)
    {
        switch (op)
        {
            case SyncList<CardOnHand>.Operation.OP_ADD:
                // index is where it was added into the list
                // newItem is the new item
                break;
            case SyncList<CardOnHand>.Operation.OP_INSERT:
                // index is where it was inserted into the list
                // newItem is the new item
                break;
            case SyncList<CardOnHand>.Operation.OP_REMOVEAT:
                // index is where it was removed from the list
                // oldItem is the item that was removed
                break;
            case SyncList<CardOnHand>.Operation.OP_SET:
                // index is of the item that was changed
                // oldItem is the previous value for the item at the index
                // newItem is the new value for the item at the index
                break;
            case SyncList<CardOnHand>.Operation.OP_CLEAR:
                // list got cleared
                break;
        }
    }
}
